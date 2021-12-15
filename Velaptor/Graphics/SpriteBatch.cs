// <copyright file="SpriteBatch.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace Velaptor.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Linq;
    using System.Numerics;
    using FreeTypeSharp.Native;
    using Velaptor.Content;
    using Velaptor.Exceptions;
    using Velaptor.NativeInterop.FreeType;
    using Velaptor.NativeInterop.OpenGL;
    using Velaptor.Observables;
    using Velaptor.Observables.Core;
    using Velaptor.OpenGL;
    using Velaptor.Services;
    using NETRect = System.Drawing.Rectangle;
    using NETSizeF = System.Drawing.SizeF;

    /// <inheritdoc/>
    internal sealed class SpriteBatch : ISpriteBatch
    {
        private const char InvalidCharacter = '□';
        private readonly Dictionary<string, CachedValue<uint>> cachedUIntProps = new ();
        private readonly IGLInvoker gl;
        private readonly IGLInvokerExtensions glExtensions;
        private readonly IFreeTypeInvoker freeTypeInvoker;
        private readonly IShaderProgram textureShader;
        private readonly IShaderProgram fontShader;
        private readonly IGPUBuffer<SpriteBatchItem> textureBuffer;
        private readonly IGPUBuffer<SpriteBatchItem> fontBuffer;
        private readonly IBatchManagerService<SpriteBatchItem> textureBatchService;
        private readonly IBatchManagerService<SpriteBatchItem> fontBatchService;
        private CachedValue<Color> cachedClearColor;
        private bool isDisposed;
        private bool hasBegun;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteBatch"/> class.
        /// NOTE: Used for unit testing to inject a mocked <see cref="IGLInvoker"/>.
        /// </summary>
        /// <param name="gl">Invokes OpenGL functions.</param>
        /// <param name="glExtensions">Invokes OpenGL extensions methods.</param>
        /// <param name="freeTypeInvoker">Loads and manages fonts.</param>
        /// <param name="textureShader">The shader used for rendering textures.</param>
        /// <param name="fontShader">The shader used for rendering text.</param>
        /// <param name="textureBuffer">Updates the data in the GPU related to rendering textures.</param>
        /// <param name="fontBuffer">Updates the data in the GPU related to rendering text.</param>
        /// <param name="textureBatchService">Manages the batch of textures to render textures.</param>
        /// <param name="fontBatchService">Manages the batch of textures to render text.</param>
        /// <param name="glObservable">Provides push notifications to OpenGL related events.</param>
        /// <remarks>
        ///     <paramref name="glObservable"/> is subscribed to in this class.  <see cref="GLWindow"/>
        ///     pushes the notification that OpenGL has been initialized.
        /// </remarks>
        [ExcludeFromCodeCoverage]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// The reason for ignoring this warning for the `cachedClearColor` not being set in constructor while
// it is set to not be null is due to the fact that we do not want warnings expressing an issue that
// does not exist.  The SetupPropertyCaches() method takes care of making sure it is not null.
        public SpriteBatch(
            IGLInvoker gl,
            IGLInvokerExtensions glExtensions,
            IFreeTypeInvoker freeTypeInvoker,
            IShaderProgram textureShader,
            IShaderProgram fontShader,
            IGPUBuffer<SpriteBatchItem> textureBuffer,
            IGPUBuffer<SpriteBatchItem> fontBuffer,
            IBatchManagerService<SpriteBatchItem> textureBatchService,
            IBatchManagerService<SpriteBatchItem> fontBatchService,
            OpenGLInitObservable glObservable)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            this.gl = gl ?? throw new ArgumentNullException(nameof(gl), $"The '{nameof(IGLInvoker)}' must not be null.");
            this.glExtensions = glExtensions ?? throw new ArgumentNullException(nameof(glExtensions), $"The '{nameof(IGLInvokerExtensions)}' must not be null.");
            this.freeTypeInvoker = freeTypeInvoker;
            this.textureShader = textureShader ?? throw new ArgumentNullException(nameof(textureShader), $"The '{nameof(textureShader)}' must not be null.");
            this.fontShader = fontShader ?? throw new ArgumentNullException(nameof(fontShader), $"The '{nameof(fontShader)}' must not be null.");

            this.textureBuffer = textureBuffer ?? throw new ArgumentNullException(nameof(textureBuffer), $"The '{nameof(textureBuffer)}' must not be null.");
            this.fontBuffer = fontBuffer ?? throw new ArgumentNullException(nameof(fontBuffer), $"The '{nameof(fontBuffer)}' must not be null.");

            this.textureBatchService = textureBatchService;
            this.textureBatchService.BatchSize = ISpriteBatch.BatchSize;
            this.textureBatchService.BatchFilled += TextureBatchService_BatchFilled;

            this.fontBatchService = fontBatchService;
            this.fontBatchService.BatchSize = ISpriteBatch.BatchSize;
            this.textureBatchService.BatchFilled += FontBatchService_BatchFilled;

            // Receive a push notification that OpenGL has initialized
            GLObservableUnsubscriber = glObservable.Subscribe(new Observer<bool>(
                _ =>
                {
                    this.cachedUIntProps.Values.ToList().ForEach(i => i.IsCaching = false);

                    if (this.cachedClearColor is not null)
                    {
                        this.cachedClearColor.IsCaching = false;
                    }

                    Init();
                }));

            SetupPropertyCaches();
        }

        /// <inheritdoc/>
        public uint RenderSurfaceWidth
        {
            get => this.cachedUIntProps[nameof(RenderSurfaceWidth)].GetValue();
            set => this.cachedUIntProps[nameof(RenderSurfaceWidth)].SetValue(value);
        }

        /// <inheritdoc/>
        public uint RenderSurfaceHeight
        {
            get => this.cachedUIntProps[nameof(RenderSurfaceHeight)].GetValue();
            set => this.cachedUIntProps[nameof(RenderSurfaceHeight)].SetValue(value);
        }

        /// <inheritdoc/>
        public Color ClearColor
        {
            get => this.cachedClearColor.GetValue();
            set => this.cachedClearColor.SetValue(value);
        }

        /// <summary>
        /// Gets the unsubscriber for the subscription
        /// to the <see cref="OpenGLInitObservable"/>.
        /// </summary>
        private IDisposable GLObservableUnsubscriber { get; }

        /// <inheritdoc/>
        public void BeginBatch() => this.hasBegun = true;

        /// <inheritdoc/>
        public void Clear() => this.gl.Clear(GLClearBufferMask.ColorBufferBit);

        /// <inheritdoc/>
        public void Render(ITexture texture, int x, int y) => Render(texture, x, y, Color.White);

        /// <inheritdoc/>
        public void Render(ITexture texture, int x, int y, RenderEffects effects) => Render(texture, x, y, Color.White, effects);

        /// <inheritdoc/>
        public void Render(ITexture texture, int x, int y, Color color) => Render(texture, x, y, color, RenderEffects.None);

        /// <inheritdoc/>
        public void Render(ITexture texture, int x, int y, Color color, RenderEffects effects)
        {
            if (!this.hasBegun)
            {
                throw new Exception($"The '{nameof(SpriteBatch.BeginBatch)}()' method must be invoked first before the '{nameof(SpriteBatch.Render)}()' method.");
            }

            if (texture is null)
            {
                throw new ArgumentNullException(nameof(texture), "The texture must not be null.");
            }

            // Render the entire texture
            var srcRect = new NETRect()
            {
                X = 0,
                Y = 0,
                Width = (int)texture.Width,
                Height = (int)texture.Height,
            };

            var destRect = new NETRect(x, y, (int)texture.Width, (int)texture.Height);

            Render(texture, srcRect, destRect, 1, 0, color, effects);
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidRenderEffectsException">
        ///     Thrown if the given <paramref name="effects"/> is invalid.
        /// </exception>
        public void Render(
            ITexture texture,
            NETRect srcRect,
            NETRect destRect,
            float size,
            float angle,
            Color color,
            RenderEffects effects)
        {
            if (!this.hasBegun)
            {
                throw new Exception($"The '{nameof(SpriteBatch.BeginBatch)}()' method must be invoked first before the '{nameof(SpriteBatch.Render)}()' method.");
            }

            if (srcRect.Width <= 0 || srcRect.Height <= 0)
            {
                throw new ArgumentException("The source rectangle must have a width and height greater than zero.", nameof(srcRect));
            }

            if (texture is null)
            {
                throw new ArgumentNullException(nameof(texture), "The texture must not be null.");
            }

            var itemToAdd = default(SpriteBatchItem);

            itemToAdd.SrcRect = srcRect;
            itemToAdd.DestRect = destRect;
            itemToAdd.Size = size;
            itemToAdd.Angle = angle;
            itemToAdd.TintColor = color;
            itemToAdd.Effects = effects;
            itemToAdd.ViewPortSize = new SizeF(RenderSurfaceWidth, RenderSurfaceHeight);
            itemToAdd.TextureId = texture.Id;

            this.textureBatchService.Add(itemToAdd);
        }

        /// <inheritdoc/>
        public void Render(IFont font, string text, int x, int y)
            => Render(font, text, x, y, 1f, 0f, Color.White);

        /// <inheritdoc/>
        public void Render(IFont font, string text, Vector2 position)
            => Render(font, text, (int)position.X, (int)position.Y, 1f, 0f, Color.White);

        /// <inheritdoc/>
        public void Render(IFont font, string text, int x, int y, float size, float angle)
            => Render(font, text, x, y, size, angle, Color.White);

        /// <inheritdoc/>
        public void Render(IFont font, string text, Vector2 position, float size, float angle)
            => Render(font, text, (int)position.X, (int)position.Y, size, angle, Color.White);

        /// <inheritdoc/>
        public void Render(IFont font, string text, int x, int y, Color color)
            => Render(font, text, x, y, 1f, 0f, color);

        /// <inheritdoc/>
        public void Render(IFont font, string text, Vector2 position, Color color)
            => Render(font, text, (int)position.X, (int)position.Y, 0f, 0f, color);

        /// <inheritdoc/>
        public void Render(IFont font, string text, int x, int y, float angle, Color color)
            => Render(font, text, x, y, 1f, angle, color);

        /// <inheritdoc/>
        public void Render(IFont font, string text, Vector2 position, float angle, Color color)
            => Render(font, text, (int)position.X, (int)position.Y, 1f, angle, color);

        /// <inheritdoc/>
        public void Render(IFont font, string text, int x, int y, float size, float angle, Color color)
        {
            size = size < 0f ? 0f : size;

            if (!this.hasBegun)
            {
                throw new Exception($"The '{nameof(BeginBatch)}()' method must be invoked first before the '{nameof(Render)}()' method.");
            }

            var originalX = (float)x;
            var originalY = (float)y;
            var normalizedSize = size - 1f;
            var characterY = (float)y;

            var lines = text.Split('\n').TrimAllEnds();

            var isMultiLine = lines.Length > 1;
            var lineSizes = lines.Select(font.Measure).ToArray();
            lineSizes = lineSizes.Select(l => l.ApplySize(normalizedSize)).ToArray();

            var lineSpacing = font.LineSpacing.ApplySize(normalizedSize);

            var textSize = new SizeF
            {
                Width = lineSizes.Max(l => l.Width),
                Height = lineSizes.Sum(l => l.Height),
            };

            var textHalfWidth = textSize.Width / 2f;
            var textHalfHeight = textSize.Height / 2f;

            var atlasWidth = font.FontTextureAtlas.Width.ApplySize(normalizedSize);
            var atlasHeight = font.FontTextureAtlas.Height.ApplySize(normalizedSize);
            var validCharacters = font.GetAvailableGlyphCharacters();

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var currentLineSize = lineSizes[i];

                var charGlyphs = line.Select(character
                    => (from m in font.Metrics
                        where m.Glyph == (validCharacters.Contains(character)
                            ? character
                            : InvalidCharacter)
                        select m).FirstOrDefault()).ToList();

                if (i == 0)
                {
                    var maxDecent = Math.Abs(charGlyphs.Max(g => g.Descender).ApplySize(normalizedSize));
                    var textTop = originalY + (currentLineSize.Height / 2f) + (maxDecent / 2f);

                    characterY = isMultiLine
                        ? textTop - textHalfHeight
                        : originalY + textHalfHeight;
                }
                else
                {
                    characterY += lineSpacing;
                }

                var firstCharBearingX = charGlyphs[0].HoriBearingX.ApplySize(normalizedSize);
                var characterX = originalX - textHalfWidth + firstCharBearingX;

                var textLinePos = new Vector2(characterX, characterY);

                var glyphString = BuildGlyphString(
                    textLinePos,
                    charGlyphs.ToArray(),
                    font.HasKerning,
                    font.FontTextureAtlas.Id,
                    new Vector2(x, y),
                    normalizedSize,
                    angle,
                    color,
                    atlasWidth,
                    atlasHeight);

                foreach (var glyphBatchItem in glyphString)
                {
                    this.fontBatchService.Add(glyphBatchItem);
                }
            }
        }

        /// <inheritdoc/>
        public void EndBatch()
        {
            TextureBatchService_BatchFilled(this.textureBatchService, EventArgs.Empty);
            FontBatchService_BatchFilled(this.fontBatchService, EventArgs.Empty);

            this.hasBegun = false;
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// <inheritdoc cref="IDisposable.Dispose"/>
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if managed resources should be disposed of.</param>
        private void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            if (disposing)
            {
                this.textureBatchService.BatchFilled -= TextureBatchService_BatchFilled;
                this.cachedUIntProps.Clear();
                this.textureShader.Dispose();
                this.textureBuffer.Dispose();
                GLObservableUnsubscriber.Dispose();
            }

            this.isDisposed = true;
        }

        /// <summary>
        /// Initializes the sprite batch.
        /// </summary>
        private void Init()
        {
            this.gl.Enable(GLEnableCap.Blend);
            this.gl.BlendFunc(GLBlendingFactor.SrcAlpha, GLBlendingFactor.OneMinusSrcAlpha);

            this.isDisposed = false;
        }

        /// <summary>
        /// Invoked every time the batch is ready to be rendered.
        /// </summary>
        private void TextureBatchService_BatchFilled(object? sender, EventArgs e)
        {
            var textureIsBound = false;

            this.textureShader.Use();

            var totalItemsToRender = 0u;

            for (var i = 0u; i < this.textureBatchService.AllBatchItems.Count; i++)
            {
                var (shouldRender, batchItem) = this.textureBatchService.AllBatchItems[i];

                if (shouldRender is false || batchItem.IsEmpty())
                {
                    continue;
                }

                if (!textureIsBound)
                {
                    this.gl.ActiveTexture(GLTextureUnit.Texture0);
                    this.gl.BindTexture(GLTextureTarget.Texture2D, batchItem.TextureId);
                    textureIsBound = true;
                }

                this.textureBuffer.UpdateData(batchItem, i);
                totalItemsToRender += 1;
            }

            // Only render the amount of elements for the amount of batch items to render.
            // 6 = the number of vertices per quad and each batch is a quad. batchAmountToRender is the total quads to render
            if (totalItemsToRender > 0)
            {
                this.gl.DrawElements(GLPrimitiveType.Triangles, 6u * totalItemsToRender, GLDrawElementsType.UnsignedInt, IntPtr.Zero);
            }

            // Empty the batch items
            this.textureBatchService.EmptyBatch();
        }

        private void FontBatchService_BatchFilled(object? sender, EventArgs e)
        {
            var fontTextureIsBound = false;

            this.gl.BeginGroup($"Render Text Process With {this.fontShader.Name} Shader");

            this.fontShader.Use();

            var totalItemsToRender = 0u;

            for (var i = 0u; i < this.fontBatchService.AllBatchItems.Count; i++)
            {
                var (shouldRender, batchItem) = this.fontBatchService.AllBatchItems[i];

                if (shouldRender is false || batchItem.IsEmpty())
                {
                    continue;
                }

                this.gl.BeginGroup($"Update Character Data - TextureID({batchItem.TextureId}) - BatchItem({i})");

                if (!fontTextureIsBound)
                {
                    this.gl.ActiveTexture(GLTextureUnit.Texture1);
                    this.gl.BindTexture(GLTextureTarget.Texture2D, batchItem.TextureId);
                    fontTextureIsBound = true;
                }

                this.fontBuffer.UpdateData(batchItem, i);

                totalItemsToRender += 1;

                this.gl.EndGroup();
            }

            // Only render the amount of elements for the amount of batch items to render.
            // 6 = the number of vertices per quad and each batch is a quad. batchAmountToRender is the total quads to render
            if (totalItemsToRender > 0)
            {
                var totalElements = 6u * totalItemsToRender;

                this.gl.BeginGroup($"Render {totalElements} Font Elements");
                this.gl.DrawElements(GLPrimitiveType.Triangles, totalElements, GLDrawElementsType.UnsignedInt, IntPtr.Zero);
                this.gl.EndGroup();
            }

            // Empty the batch items
            this.fontBatchService.EmptyBatch();

            this.gl.EndGroup();
        }

        /// <summary>
        /// Constructs a string of glyphs to be rendered as a result of an array of <see cref="SpriteBatchItem"/>s.
        /// </summary>
        /// <param name="textPos">The position to render the text.</param>
        /// <param name="charMetrics">The glyph metrics of the characters in the text.</param>
        /// <param name="hasKerning">True if the font has kerning to take into account.</param>
        /// <param name="textureId">The ID of the font texture atlas.</param>
        /// <param name="origin">The origin to rotate the text around.</param>
        /// <param name="size">The size of the text.</param>
        /// <param name="angle">The angle of the text.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="atlasWidth">The width of the font texture atlas.</param>
        /// <param name="atlasHeight">The height of the font texture atlas.</param>
        /// <returns>The list of glyphs that make up the string as sprite batch items.</returns>
        private IEnumerable<SpriteBatchItem> BuildGlyphString(
            Vector2 textPos,
            GlyphMetrics[] charMetrics,
            bool hasKerning,
            uint textureId,
            Vector2 origin,
            float size,
            float angle,
            Color color,
            float atlasWidth,
            float atlasHeight)
        {
            var result = new List<SpriteBatchItem>();

            var leftGlyphIndex = 0u;
            var facePtr = this.freeTypeInvoker.GetFace();

            for (var i = 0; i < charMetrics.Length; i++)
            {
                var currentCharMetric = charMetrics[i].ApplySize(size);

                if (hasKerning && leftGlyphIndex != 0 && currentCharMetric.CharIndex != 0)
                {
                    // TODO: Check the perf for curiosity reasons
                    var delta = this.freeTypeInvoker.FT_Get_Kerning(
                        facePtr,
                        leftGlyphIndex,
                        currentCharMetric.CharIndex,
                        (uint)FT_Kerning_Mode.FT_KERNING_DEFAULT);

                    var kerning = (float)(delta.x.ToInt32() >> 6);

                    textPos.X += (int)kerning.ApplySize(size);
                }

                // Create the source rect and take the size into account
                var srcRect = currentCharMetric.GlyphBounds;
                srcRect.Width = srcRect.Width <= 0 ? 1 : srcRect.Width;
                srcRect.Height = srcRect.Height <= 0 ? 1 : srcRect.Height;

                var glyphHalfWidth = currentCharMetric.GlyphWidth / 2f;
                var glyphHalfHeight = currentCharMetric.GlyphHeight / 2f;

                // Calculate the height offset
                var heightOffset = currentCharMetric.GlyphHeight - currentCharMetric.HoriBearingY;

                // Adjust for characters that have a negative horizontal bearing Y
                // For example, the '_' character
                if (currentCharMetric.HoriBearingY < 0)
                {
                    heightOffset += currentCharMetric.HoriBearingY;
                }

                // Create the destination rect
                RectangleF destRect = default;
                destRect.X = textPos.X + glyphHalfWidth;
                destRect.Y = textPos.Y - glyphHalfHeight + heightOffset;
                destRect.Width = atlasWidth;
                destRect.Height = atlasHeight;

                var newPosition = destRect.GetPosition().RotateAround(origin, angle);

                destRect.X = newPosition.X;
                destRect.Y = newPosition.Y;

                // Only render characters that are not a space (32 char code)
                if (currentCharMetric.Glyph != ' ')
                {
                    var itemToAdd = default(SpriteBatchItem);

                    itemToAdd.SrcRect = srcRect;
                    itemToAdd.DestRect = destRect;
                    itemToAdd.Size = size;
                    itemToAdd.Angle = angle;
                    itemToAdd.TintColor = color;
                    itemToAdd.Effects = RenderEffects.None;
                    itemToAdd.ViewPortSize = new SizeF(RenderSurfaceWidth, RenderSurfaceHeight);
                    itemToAdd.TextureId = textureId;

                    result.Add(itemToAdd);
                }

                // Horizontally advance to the next glyph
                // Get the difference between the old glyph width
                // and the glyph width with the size applied
                textPos.X += currentCharMetric.HorizontalAdvance.ApplySize(size);

                leftGlyphIndex = currentCharMetric.CharIndex;
            }

            return result.ToArray();
        }

        /// <summary>
        /// Setup all of the caching for the properties that need caching.
        /// </summary>
        private void SetupPropertyCaches()
        {
            this.cachedUIntProps.Add(
                nameof(RenderSurfaceWidth),
                new CachedValue<uint>(
                    0,
                    () => (uint)this.glExtensions.GetViewPortSize().Width,
                    (value) =>
                    {
                        var viewPortSize = this.glExtensions.GetViewPortSize();

                        this.glExtensions.SetViewPortSize(new Size((int)value, viewPortSize.Height));
                    }));

            this.cachedUIntProps.Add(
                nameof(RenderSurfaceHeight),
                new CachedValue<uint>(
                    0,
                    () => (uint)this.glExtensions.GetViewPortSize().Height,
                    (value) =>
                    {
                        var viewPortSize = this.glExtensions.GetViewPortSize();

                        this.glExtensions.SetViewPortSize(new Size(viewPortSize.Width, (int)value));
                    }));

            this.cachedClearColor = new CachedValue<Color>(
                Color.CornflowerBlue,
                () =>
                {
                    var colorValues = new float[4];
                    this.gl.GetFloat(GLGetPName.ColorClearValue, colorValues);

                    var red = colorValues[0].MapValue(0, 1, 0, 255);
                    var green = colorValues[1].MapValue(0, 1, 0, 255);
                    var blue = colorValues[2].MapValue(0, 1, 0, 255);
                    var alpha = colorValues[3].MapValue(0, 1, 0, 255);

                    return Color.FromArgb((byte)alpha, (byte)red, (byte)green, (byte)blue);
                },
                (value) =>
                {
                    var red = value.R.MapValue(0f, 255f, 0f, 1f);
                    var green = value.G.MapValue(0f, 255f, 0f, 1f);
                    var blue = value.B.MapValue(0f, 255f, 0f, 1f);
                    var alpha = value.A.MapValue(0f, 255f, 0f, 1f);

                    this.gl.ClearColor(red, green, blue, alpha);
                });
        }
    }
}

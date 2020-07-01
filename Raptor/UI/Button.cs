﻿// <copyright file="Button.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace Raptor.UI
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Numerics;
    using Raptor.Content;
    using Raptor.Graphics;
    using Raptor.Input;

    /// <summary>
    /// A button that can be clicked and execute functionality.
    /// </summary>
    public class Button : IControl
    {
        private readonly Mouse mouse;
        private Rectangle rect = default;
        private bool isMouseDown;

        /// <summary>
        /// Initializes a new instance of the <see cref="Button"/> class.
        /// </summary>
        /// <param name="mouseOverTexture">The texture to be displayed when the mouse is over the button.</param>
        /// <param name="mouseNotOverTexture">The texture to be displayed when the mouse is not in the down position or over the button.</param>
        /// <param name="mouseDownTexture">The texture to be displayed when the mouse is in the down position.</param>
        public Button(Texture mouseOverTexture, Texture mouseNotOverTexture, Texture mouseDownTexture)
        {
            MouseOverTexture = mouseOverTexture;
            MouseNotOverTexture = mouseNotOverTexture;
            MouseDownTexture = mouseDownTexture;

            this.mouse = new Mouse();
        }

        /// <summary>
        /// Occurs when the button has been clicked.
        /// </summary>
        public event EventHandler<EventArgs>? Click;

        /// <summary>
        /// Gets or sets the position of the <see cref="Button"/> on the screen.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Gets the width of the <see cref="Button"/>.
        /// </summary>
        public uint Width
        {
            get
            {
                if (MouseOverTexture == null || MouseNotOverTexture == null)
                    return 0;

                return MouseOverTexture.Width > MouseNotOverTexture.Width ?
                    MouseOverTexture.Width :
                    MouseNotOverTexture.Width;
            }
        }

        /// <summary>
        /// Gets the height of the <see cref="Button"/>.
        /// </summary>
        public uint Height
        {
            get
            {
                if (MouseOverTexture == null || MouseNotOverTexture == null)
                    return 0;

                return MouseOverTexture.Height > MouseNotOverTexture.Height ?
                    MouseOverTexture.Height :
                    MouseNotOverTexture.Height;
            }
        }

        /// <summary>
        /// Gets or sets the texture when the mouse is over the <see cref="Button"/>.
        /// </summary>
        public Texture MouseOverTexture { get; set; }

        /// <summary>
        /// Gets or sets the texture when the mouse is not over the <see cref="Button"/>.
        /// </summary>
        public Texture MouseNotOverTexture { get; set; }

        /// <summary>
        /// Gets or sets the texture when the left mouse button is
        /// in the down position over the button.
        /// </summary>
        public Texture MouseDownTexture { get; set; }

        /// <summary>
        /// Gets a value indicating whether gets a value indicating if the mouse is hovering over the button.
        /// </summary>
        public bool IsMouseOver { get; private set; }

        /// <summary>
        /// Gets or sets the text of the button.
        /// </summary>
        public RenderText? ButtonText { get; set; }

        /// <summary>
        /// Initializes the <see cref="Button"/>.
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// Loads the content for the <see cref="Button"/>.
        /// </summary>
        /// <param name="contentLoader">The loader used to laod the content.</param>
        public void LoadContent(ContentLoader contentLoader)
        {
        }

        /// <summary>
        /// Updates the <see cref="Button"/>.
        /// </summary>
        /// <param name="engineTime">The game engine time.</param>
        public void Update(FrameTime engineTime)
        {
            ProcessMouse();

            this.rect.X = (int)(Position.X - (Width / 2f));
            this.rect.Y = (int)(Position.Y - (Height / 2f));
            this.rect.Width = (int)Width;
            this.rect.Height = (int)Height;
        }

        /// <summary>
        /// Renders the <see cref="Button"/> to the screen.
        /// </summary>
        /// <param name="renderer">Renders the <see cref="Button"/>.</param>
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Exception message only used in method.")]
        public void Render(object renderer)
        {
            // if (renderer is null)
            //    throw new ArgumentNullException(nameof(renderer), "The renderer must not be null.");

            // if (_isMouseDown && MouseDownTexture != null)
            // {
            //    renderer.Render(MouseDownTexture, Position.X, Position.Y);
            // }
            // else
            // {
            //    if (IsMouseOver && MouseOverTexture != null)
            //    {
            //        renderer.Render(MouseOverTexture, Position.X, Position.Y);
            //    }
            //    else
            //    {
            //        if (MouseNotOverTexture != null)
            //            renderer.Render(MouseNotOverTexture, Position.X, Position.Y, 0);
            //    }

            // }

            // var textPosition = new Vector2()
            // {
            //    X = Position.X - (ButtonText is null ? 0 : ButtonText.Width / 2f),
            //    Y = Position.Y - (ButtonText is null ? 0 :  ButtonText.Height / 2f)
            // };

            // if(ButtonText != null)
            //    renderer.Render(ButtonText, textPosition, Color.FromArgb(255, 0, 0, 0));
        }

        /// <summary>
        /// Processes any mouse input interaction with the <see cref="Button"/>.
        /// </summary>
        private void ProcessMouse()
        {
            this.mouse.UpdateCurrentState();

            IsMouseOver = this.rect.Contains(this.mouse.X, this.mouse.Y);

            this.isMouseDown = IsMouseOver && this.mouse.IsButtonDown(InputButton.LeftButton);

            if (this.isMouseDown)
                Click?.Invoke(this, new EventArgs());

            this.mouse.UpdatePreviousState();
        }
    }
}

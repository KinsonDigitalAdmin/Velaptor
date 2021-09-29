﻿// <copyright file="TestRenderTextScene.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace VelaptorTesting.Scenes
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Velaptor.Content;
    using Velaptor.Factories;
    using Velaptor.Graphics;
    using VelaptorTesting.Core;

    /// <summary>
    /// Used to test out if text is properly being rendered to the screen.
    /// </summary>
    public class TestRenderTextScene : SceneBase
    {
        private const string TextToRender = "If can you see this text, then text rendering is working correctly.";
        private readonly ILoader<IFont>? fontLoader;
        private Dictionary<char, int>? glyphWidths;
        private IFont? font;
        private int textWidth;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRenderTextScene"/> class.
        /// </summary>
        /// <param name="contentLoader">Loads content for the scene.</param>
        public TestRenderTextScene(IContentLoader contentLoader)
            : base(contentLoader)
                => this.fontLoader = ContentLoaderFactory.CreateFontLoader();

        /// <summary>
        /// Loads the content for the scene.
        /// </summary>
        public override void Load()
        {
            this.font = this.fontLoader.Load("TimesNewRoman");
            this.glyphWidths = new Dictionary<char, int>(this.font.Metrics.Select(m => new KeyValuePair<char, int>(m.Glyph, m.GlyphWidth)));
            this.textWidth = TextToRender.Select(character => this.glyphWidths[character]).Sum();

            base.Load();
        }

        /// <summary>
        /// Renders the scene.
        /// </summary>
        /// <param name="spriteBatch">Renders sprites to the screen.</param>
        public override void Render(ISpriteBatch spriteBatch)
        {
            var xPos = (int)((MainWindow.WindowWidth / 2f) - (this.textWidth / 2f));
            var yPos = MainWindow.WindowHeight / 2;

            spriteBatch.Render(this.font, TextToRender, xPos, yPos, Color.White);

            base.Render(spriteBatch);
        }
    }
}
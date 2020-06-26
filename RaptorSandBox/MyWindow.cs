using FileIO.File;
using Raptor;
using Raptor.Content;
using Raptor.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace RaptorSandBox
{
    public class MyWindow : Window
    {
        private ITexture? linkTexture;
        private ITexture? dungeonTexture;
        private AtlasRegionRectangle[] atlasData;
        private ISpriteBatch? spriteBatch;


        public MyWindow() : base(1020, 800)
        {
        }


        public override void OnLoad()
        {
            if (ContentLoader is null)
                throw new NullReferenceException($"The ContentLoader must not be null.");

            this.spriteBatch = RaptorFactory.CreateSpriteBatch(Width, Height);

            this.dungeonTexture = ContentLoader.LoadTexture("dungeon.png");
            this.linkTexture = ContentLoader.LoadTexture("Link.png");

            base.OnLoad();
        }


        public override void OnUpdate(FrameTime frameTime)
        {
            base.OnUpdate(frameTime);
        }


        public override void OnDraw(FrameTime frameTime)
        {
            this.spriteBatch?.BeginBatch();

            //this.spriteBatch?.Render(this.dungeonTexture, 0, 0);
            this.spriteBatch?.Render(this.linkTexture, 400, 400);

            this.spriteBatch?.EndBatch();

            base.OnDraw(frameTime);
        }


        public override void OnResize()
        {
            base.OnResize();
        }
    }
}

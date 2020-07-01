﻿namespace RaptorTests.Graphics
{
    using System.Collections.Generic;
    using Raptor.Graphics;
    using Raptor.OpenGL;
    using Silk.NET.OpenGL;
    using Xunit;
    using Moq;
    using System.Drawing;

    /// <summary>
    /// Tests the <see cref="Texture"/> class.
    /// </summary>
    public class TextureTests
    {
        private readonly Mock<IGLInvoker> mockGL;
        private readonly byte[] pixelData;
        private readonly uint textureID = 1234;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureTests"/> class.
        /// </summary>
        public TextureTests()
        {
            var byteData = new List<byte>();

            //Rows
            for (int row = 0; row < 3; row++)
            {
                //Columns
                for (int col = 0; col < 2; col++)
                {
                    //If the first row
                    switch (row)
                    {
                        case 0://Row 1
                            byteData.AddRange(ToByteArray(Color.Red));
                            break;
                        case 1://Row 2
                            byteData.AddRange(ToByteArray(Color.Green));
                            break;
                        case 2://Row 3
                            byteData.AddRange(ToByteArray(Color.Blue));
                            break;
                    }
                }
            }

            this.pixelData = byteData.ToArray();

            this.mockGL = new Mock<IGLInvoker>();
            this.mockGL.Setup(m => m.GenTexture()).Returns(this.textureID);
        }

        [Fact]
        public void Ctor_WhenInvoked_UploadsTextureDataToGPU()
        {
            //Act
            var texture = new Texture(this.mockGL.Object, "test-texture.png", this.pixelData, 2, 3);

            //Assert
            this.mockGL.Verify(m => m.ObjectLabel(ObjectIdentifier.Texture, this.textureID, 0, "test-texture.png"), Times.Once());
            this.mockGL.Verify(m => m.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear), Times.Once());

            this.mockGL.Verify(m => m.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int)TextureMinFilter.Linear), Times.Once());

            this.mockGL.Verify(m => m.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureWrapS,
                (int)TextureWrapMode.ClampToEdge), Times.Once());

            this.mockGL.Verify(m => m.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureWrapT,
                (int)TextureWrapMode.ClampToEdge), Times.Once());

            this.mockGL.Verify(m => m.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelFormat.Rgba,
                2,
                3,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                pixelData));
        }

        [Fact]
        public void Dispose_WhenUnmanagedResourcesIsNotDisposed_DisposesOfUnmanagedResources()
        {
            //Arrange
            var texture = new Texture(this.mockGL.Object, "test-texture", pixelData, 2, 3);

            //Act
            texture.Dispose();
            texture.Dispose();

            //Assert
            this.mockGL.Verify(m => m.DeleteTexture(this.textureID), Times.Once());
        }

        private byte[] ToByteArray(Color clr)
        {
            return new[] { clr.A, clr.R, clr.G, clr.B };
        }
    }
}

﻿using FileIO.Core;
using Moq;
using Raptor.Graphics;
using Raptor.OpenGL;
using RaptorTests.Helpers;
using System.Drawing;
using System;
using Xunit;
using Silk.NET.OpenGL;
using System.Numerics;

namespace RaptorTests.Graphics
{
    public class SpriteBatchTests
    {
        private readonly Mock<ITexture> mockTextureOne;
        private readonly Mock<ITexture> mockTextureTwo;
        private readonly Mock<IGLInvoker> mockGL;
        private readonly Mock<IShaderProgram> mockShader;
        private readonly Mock<IGPUBuffer> mockBuffer;
        private readonly Mock<ITextFile> mockTextFile;

        public SpriteBatchTests()
        {
            this.mockTextureOne = new Mock<ITexture>();
            this.mockTextureTwo = new Mock<ITexture>();
            this.mockTextureTwo.SetupGet(p => p.ID).Returns(1);

            this.mockGL = new Mock<IGLInvoker>();
            this.mockGL.Setup(m => m.ShaderCompileSuccess(It.IsAny<uint>())).Returns(true);
            this.mockGL.Setup(m => m.LinkProgramSuccess(It.IsAny<uint>())).Returns(true);

            this.mockShader = new Mock<IShaderProgram>();

            this.mockBuffer = new Mock<IGPUBuffer>();

            this.mockTextFile = new Mock<ITextFile>();
        }

        [Fact]
        public void Ctor_WhenInvokedWithNullGLInvoker_ThrowsException()
        {
            //Act & Assert
            AssertHelpers.ThrowsWithMessage<ArgumentNullException>(() =>
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var buffer = new SpriteBatch(null, this.mockShader.Object, this.mockBuffer.Object);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }, $"The '{nameof(IGLInvoker)}' must not be null. (Parameter 'gl')");
        }

        [Fact]
        public void Ctor_WhenInvokedWithNullShader_ThrowsException()
        {
            //Act & Assert
            AssertHelpers.ThrowsWithMessage<ArgumentNullException>(() =>
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var buffer = new SpriteBatch(this.mockGL.Object, null, this.mockBuffer.Object);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }, $"The '{nameof(IShaderProgram)}' must not be null. (Parameter 'shader')");
        }

        [Fact]
        public void Ctor_WhenInvokedWithNullGPUBuffer_ThrowsException()
        {
            //Act & Assert
            AssertHelpers.ThrowsWithMessage<ArgumentNullException>(() =>
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                var buffer = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }, $"The '{nameof(IGPUBuffer)}' must not be null. (Parameter 'gpuBuffer')");
        }

        [Fact]
        public void Ctor_WhenInvoked_SetsUpShaderProgram()
        {
            //Act
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object);

            //Assert
            this.mockShader.Verify(m => m.UseProgram(), Times.Once());
        }

        [Fact]
        public void Ctor_WhenInvoked_EnablesBlending()
        {
            //Act
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object);

            //Assert
            this.mockGL.Verify(m => m.Enable(EnableCap.Blend), Times.Once());
        }

        [Fact]
        public void Ctor_WhenInvoked_SetsUpBlending()
        {
            //Act
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object);

            //Assert
            this.mockGL.Verify(m => m.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha), Times.Once());
        }

        [Fact]
        public void Ctor_WhenInvoked_SetsUpClearColor()
        {
            //Act
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object);

            //Assert
            this.mockGL.Verify(m => m.ClearColor(0.2f, 0.3f, 0.3f, 1.0f), Times.Once());
        }

        [Fact]
        public void Ctor_WhenInvoked_SetsTextureUnitToSlot0()
        {
            //Act
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object);

            //Assert
            this.mockGL.Verify(m => m.ActiveTexture(TextureUnit.Texture0), Times.Once());
        }

        [Fact]
        public void Ctor_WhenInvoked_GetTransformLocation()
        {
            //Act
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object);

            //Assert
            this.mockGL.Verify(m => m.GetUniformLocation(It.IsAny<uint>(), "uTransform"), Times.Once());
        }

        [Fact]
        public void Render_WhenUsingOverloadWithFourParamsAndWithoutCallingBeginFirst_ThrowsException()
        {
            //Arrange
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object);

            //Act & Assert
            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                batch.Render(this.mockTextureOne.Object, 10, 20);
            }, $"The '{nameof(SpriteBatch.BeginBatch)}()' method must be invoked first before the '{nameof(SpriteBatch.Render)}()' method.");
        }

        [Fact]
        public void Render_WhenUsingOverloadWithFourParamsAndNullTexture_ThrowsException()
        {
            //Arrange
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object);
            
            //Act & Assert
            AssertHelpers.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                batch.BeginBatch();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                batch.Render(null, 10, 20);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            }, "The texture must not be null. (Parameter 'texture')");
        }

        [Fact]
        public void Render_WhenUsingOverloadWithFourParams_RendersBatch()
        {
            //Arrange
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object)
            {
                BatchSize = 1
            };

            batch.BeginBatch();

            //Act
            batch.Render(
                this.mockTextureOne.Object,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Color>());

            batch.EndBatch();
            batch.EndBatch();

            //Assert
            AssertBatchRendered(1, 1, 1, 1);
        }

        [Fact]
        public void Render_WhenExcedingBatchSize_RendersBatchTwoTimes()
        {
            //Arrange
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object)
            {
                BatchSize = 1
            };
            batch.BeginBatch();

            //Act
            batch.Render(
                this.mockTextureOne.Object,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Color>());

            batch.Render(
                this.mockTextureOne.Object,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Color>());

            //Assert
            AssertBatchRendered(1, 2, 1, 1);
        }

        [Fact]
        public void Render_WhenUsingOverloadWithSixParamsAndWithoutCallingBeginFirst_ThrowsException()
        {
            //Arrange
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object);

            //Act & Assert
            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                var srcRect = new Rectangle(1, 2, 3, 4);
                var destRect = new Rectangle(5, 6, 7, 8);
                var tintClr = Color.FromArgb(11, 22, 33, 44);

                batch.Render(this.mockTextureOne.Object, srcRect, destRect, 0.5f, 90, tintClr);
            }, $"The '{nameof(SpriteBatch.BeginBatch)}()' method must be invoked first before the '{nameof(SpriteBatch.Render)}()' method.");
        }

        [Fact]
        public void Render_WhenUsingOverloadWithSixParamsAndWithNullTexture_ThrowsException()
        {
            //Arrange
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object);

            //Act & Assert
            AssertHelpers.ThrowsWithMessage<ArgumentNullException>(() =>
            {
                batch.BeginBatch();
                batch.Render(null, It.IsAny<Rectangle>(), It.IsAny<Rectangle>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<Color>());
            }, "The texture must not be null. (Parameter 'texture')");
        }

        [Fact]
        public void Render_WhenSwitchingTextures_RendersBatch()
        {
            //Arrange
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object)
            {
                RenderSurfaceWidth = 10,
                RenderSurfaceHeight = 20,
                BatchSize = 1
            };


            //Act
            //WARNING - Changing these values will change the trans matrix
            var srcRect = new Rectangle(1, 2, 3, 4);
            var destRect = new Rectangle(5, 6, 7, 8);
            var tintClr = Color.FromArgb(11, 22, 33, 44);

            batch.BeginBatch();
            batch.Render(this.mockTextureOne.Object, srcRect, destRect, 0.5f, 90, tintClr);
            batch.Render(this.mockTextureTwo.Object, srcRect, destRect, 0.5f, 90, tintClr);
            var transMatrix = new Matrix4x4()
            {
                M11 = -6.5567085E-09f,
                M12 = 0.15f,
                M13 = 0f,
                M14 = 0f,

                M21 = -0.1f,
                M22 = -4.371139E-09F,
                M23 = 0f,
                M24 = 0.39999998f,

                M31 = 0f,
                M32 = 0f,
                M33 = 1f,
                M34 = 0f,

                M41 = 0f,
                M42 = 0f,
                M43 = 0f,
                M44 = 1f
            };

            //Assert
            AssertBatchRendered(1, 1, 1, 2, transMatrix);
        }

        [Fact]
        public void Render_WhenBatchIsFull_RendersBatch()
        {
            //Arrange
            var batchSize = 2;
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object)
            {
                RenderSurfaceWidth = 10,
                RenderSurfaceHeight = 20,
                BatchSize = batchSize
            };

            //Act
            batch.BeginBatch();
            batch.Render(this.mockTextureOne.Object, It.IsAny<int>(), It.IsAny<int>());
            batch.Render(this.mockTextureOne.Object, It.IsAny<int>(), It.IsAny<int>());
            batch.Render(this.mockTextureOne.Object, It.IsAny<int>(), It.IsAny<int>());

            var transMatrix = new Matrix4x4()
            {
                M11 = -6.5567085E-09f,
                M12 = 0.15f,
                M13 = 0f,
                M14 = 0f,

                M21 = -0.1f,
                M22 = -4.371139E-09F,
                M23 = 0f,
                M24 = 0.39999998f,

                M31 = 0f,
                M32 = 0f,
                M33 = 1f,
                M34 = 0f,

                M41 = 0f,
                M42 = 0f,
                M43 = 0f,
                M44 = 1f
            };

            //Assert
            AssertBatchRendered(2, batchSize, 1, 1);
        }

        [Fact]
        public void Render_WhenBatchIsNotFull_OnlyRendersRequiredItems()
        {
            //Arrange
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object)
            {
                BatchSize = 2
            };

            //Act
            batch.BeginBatch();

            batch.Render(
                this.mockTextureOne.Object,
                It.IsAny<Rectangle>(),
                It.IsAny<Rectangle>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<Color>());

            batch.EndBatch();

            //Assert
            AssertBatchRendered(1, 1, 1, 1);
        }

        [Fact]
        public void Dispose_WhenInvoked_DisposesOfMangedResources()
        {
            //Arrange
            var batch = new SpriteBatch(this.mockGL.Object, this.mockShader.Object, this.mockBuffer.Object);

            //Act
            batch.Dispose();
            batch.Dispose();

            //Assert
            this.mockShader.Verify(m => m.Dispose(), Times.Once());
            this.mockBuffer.Verify(m => m.Dispose(), Times.Once());
        }

        /// <summary>
        /// Assserts that a single batch was rendered the given amount of <paramref name="totalBatchUpdates"/>.
        /// </summary>
        /// <param name="totalItemsInBatch">The total amount of textures to be expected in the batch.</param>
        /// <param name="totalBatchUpdates">The total amount of batch data updates.</param>
        private void AssertBatchRendered(uint totalItemsInBatch, int totalBatchUpdates, int totalTextureBinds, int totalDrawCalls)
        {
            AssertBatchRendered(totalItemsInBatch, totalBatchUpdates, totalTextureBinds, totalDrawCalls, new Matrix4x4());
        }

        /// <summary>
        /// Assserts that a single batch was rendered the given amount of <paramref name="totalBatchUpdates"/>.
        /// </summary>
        /// <param name="totalItemsInBatch">The total amount of textures to be expected in the batch.</param>
        /// <param name="totalBatchUpdates">The total amount of batch data updates.</param>
        /// <param name="transform">The transform that was sent to the GPU.  An empty transform means any transform data would assert true.</param>
        private unsafe void AssertBatchRendered(uint totalItemsInBatch, int totalBatchUpdates, int totalTextureBinds, int totalDrawCalls, Matrix4x4 transform)
        {
            this.mockGL.Verify(m => m.BindTexture(TextureTarget.Texture2D, It.IsAny<uint>()),
                Times.Exactly(totalTextureBinds),
                "Did not bind texture");

            if (transform.IsEmpty())
            {
                //Verify with any transform
                AssertTransformUpdate(totalBatchUpdates);
            }
            else
            {
                //Verify with given transform
                AssertTransformUpdate(totalBatchUpdates, transform);
            }

            //Invoked in the GPUBuffer.UpdateQuad() method
            this.mockBuffer.Verify(m => m.UpdateQuad(It.IsAny<int>(),
                                                     It.IsAny<Rectangle>(),
                                                     It.IsAny<int>(),
                                                     It.IsAny<int>(),
                                                     It.IsAny<Color>()),
                Times.Exactly(totalBatchUpdates),
                "Quad was not updated on GPU.");

            this.mockGL.Verify(m => m.DrawElements(PrimitiveType.Triangles, 6 * totalItemsInBatch,
                                                   DrawElementsType.UnsignedInt, IntPtr.Zero.ToPointer()),
                Times.Exactly(totalDrawCalls),
                $"Expected total draw calls of {totalDrawCalls} not reached.");
        }

        delegate void MockUniformMatrix4(int location, bool transpose, ref Matrix4x4 matrix);

        private void AssertTransformUpdate(int times)
        {
            //Verify with any transform
            this.mockGL.Verify(m => m.UniformMatrix4x4(It.IsAny<int>(), true, It.IsAny<Matrix4x4>()),
                Times.Exactly(times),
                "Transformation matrix not updated on GPU");
        }

        private void AssertTransformUpdate(int times, Matrix4x4 transform)
        {
            //Verify with given transform
            this.mockGL.Verify(m => m.UniformMatrix4x4(0, true, transform),
                Times.Exactly(times),
                "Transformation matrix not updated on GPU");
        }
    }
}

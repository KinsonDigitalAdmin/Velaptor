﻿using Raptor.OpenGL;
using System.Numerics;
using Xunit;

namespace RaptorTests.OpenGL
{
    public class VertexDataTests
    {
        [Fact]
        public void NotEqualsOperator_WhenInvoked_ReturnsCorrectResult()
        {
            //Arrange
            var dataA = new VertexData()
            {
                Vertex = new Vector3(1, 2, 3),
                TextureCoord = new Vector2(4, 5),
                TintColor = new Vector4(6, 7, 8, 9),
                TransformIndex = 10
            };
            var dataB = new VertexData()
            {
                Vertex = new Vector3(11, 22, 33),
                TextureCoord = new Vector2(44, 55),
                TintColor = new Vector4(66, 77, 88, 99),
                TransformIndex = 10
            };


            //Act
            var actual = dataA != dataB;

            //Assert
            Assert.True(actual);
        }

        [Fact]
        public void EqualsOperator_WhenInvoked_ReturnsCorrectResult()
        {
            //Arrange
            var dataA = new VertexData()
            {
                Vertex = new Vector3(1, 2, 3),
                TextureCoord = new Vector2(4, 5),
                TintColor = new Vector4(6, 7, 8, 9),
                TransformIndex = 10
            };
            var dataB = new VertexData()
            {
                Vertex = new Vector3(1, 2, 3),
                TextureCoord = new Vector2(4, 5),
                TintColor = new Vector4(6, 7, 8, 9),
                TransformIndex = 10
            };

            //Act
            var actual = dataA == dataB;

            //Assert
            Assert.True(actual);
        }

        [Theory]
        [ClassData(typeof(VertexDataTestData))]
        public void Equals_WhenInvoked_ReturnsCorrectResult(Vector3 vertex, Vector2 textureCoord, Vector4 tintClr, int transformIndex, bool expected)
        {
            //Arrange
            var dataA = new VertexData()
            {
                Vertex = new Vector3(1, 2, 3),
                TextureCoord = new Vector2(4, 5),
                TintColor = new Vector4(6, 7, 8, 9),
                TransformIndex = 10
            };
            var dataB = new VertexData()
            {
                Vertex = vertex,
                TextureCoord = textureCoord,
                TintColor = tintClr,
                TransformIndex = transformIndex
            };

            //Act
            var actual = dataA.Equals(dataB);

            //Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Equals_WhenUsingOverloadWithObjectParamWithObjectOfDifferentType_ReturnsFalse()
        {
            //Arrange
            var dataA = new VertexData()
            {
                Vertex = new Vector3(1, 2, 3),
                TextureCoord = new Vector2(4, 5),
                TintColor = new Vector4(6, 7, 8, 9),
                TransformIndex = 10
            };
            var dataB = new object();

            //Act
            var actual = dataA.Equals(dataB);

            //Assert
            Assert.False(actual);
        }

        [Fact]
        public void Equals_WhenUsingOverloadWithObjectParamWithObjectOfSameType_ReturnsTrue()
        {
            //Arrange
            var dataA = new VertexData()
            {
                Vertex = new Vector3(1, 2, 3),
                TextureCoord = new Vector2(4, 5),
                TintColor = new Vector4(6, 7, 8, 9),
                TransformIndex = 10
            };
            object dataB = new VertexData()
            {
                Vertex = new Vector3(1, 2, 3),
                TextureCoord = new Vector2(4, 5),
                TintColor = new Vector4(6, 7, 8, 9),
                TransformIndex = 10
            };

            //Act
            var actual = dataA.Equals(dataB);

            //Assert
            Assert.True(actual);
        }
    }
}

﻿// <copyright file="CachedValueTests.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace VelaptorTests
{
    using Moq;
    using Velaptor;
    using Xunit;

    public class CachedValueTests
    {
        #region Constructor Tests
        [Fact]
        public void Ctor_WhenInvokedWithCachingOn_SetsValueInternally()
        {
            // Arrange
            var externalSystemValue = 0;
            var cachedValue = new CachedValue<int>(
                1234,
                () => externalSystemValue,
                (value) => externalSystemValue = value,
                true);

            // Act
            var actual = cachedValue.GetValue();

            // Assert
            Assert.Equal(1234, actual);
            Assert.Equal(0, externalSystemValue);
        }

        [Fact]
        public void Ctor_WhenInvokedWithCachingOff_SetsValueExternally()
        {
            // Arrange
            var externalSystemValue = 0;
            var cachedValue = new CachedValue<int>(
                1234,
                () => externalSystemValue,
                (value) => externalSystemValue = value,
                false);

            // Act
            var actual = cachedValue.GetValue();

            // Assert
            Assert.Equal(1234, actual);
        }
        #endregion

        #region Prop Tests
        [Fact]
        public void IsCaching_WhenGoingFromCachingOffToOn_ReturnsRecorrectResult()
        {
            // Arrange
            var externalSystemValue = 0;
            var cachedValue = new CachedValue<int>(
                1234,
                () => externalSystemValue,
                (value) => externalSystemValue = value,
                false);

            // Act
            cachedValue.IsCaching = true;
            var actual = cachedValue.GetValue();

            // Assert
            Assert.Equal(1234, actual);
        }

        [Fact]
        public void IsCaching_WhenGoingFromCachingOnToOff_ReturnsCorrectResult()
        {
            // Arrange
            var externalSystemValue = 0;
            var cachedValue = new CachedValue<int>(
                1234,
                () => externalSystemValue,
                (value) => externalSystemValue = value,
                true);

            // Act
            cachedValue.IsCaching = false;
            var actual = cachedValue.GetValue();

            // Assert
            Assert.Equal(1234, actual);
        }
        #endregion

        #region Method Tests
        [Fact]
        public void GetValue_WhileCachingValue_ReturnsCorrectResult()
        {
            // Arrange
            var cachedValue = new CachedValue<int>(
                1234,
                () => It.IsAny<int>(),
                (_) => { });

            // Act
            var actual = cachedValue.GetValue();

            // Assert
            Assert.Equal(1234, actual);
        }

        [Fact]
        public void GetValue_WhileNotCachingValue_ReturnsCorrectResult()
        {
            // Arrange
            var cachedValue = new CachedValue<int>(
                5678,
                () => 1234,
                (_) => { })
            {
                IsCaching = false,
            };

            // Act
            var actual = cachedValue.GetValue();

            // Assert
            Assert.Equal(1234, actual);
        }

        [Fact]
        public void SetValue_WhileCaching_ReturnsCorrectResult()
        {
            // Arrange
            var cachedValue = new CachedValue<int>(
                5678,
                () => 0,
                (_) => { });

            // Act
            cachedValue.SetValue(1234);
            cachedValue.SetValue(5678);
            var actual = cachedValue.GetValue();

            // Assert
            Assert.Equal(5678, actual);
        }

        [Fact]
        public void SetValue_WhileNotCachingValue_InvokesOnResolve()
        {
            // Arrange
            var actual = 0;
            var cachedValue = new CachedValue<int>(
                5678,
                () => 0,
                (value) => actual = value)
            {
                IsCaching = false,
            };

            // Act
            cachedValue.SetValue(1234);

            // Assert
            Assert.Equal(1234, actual);
        }
        #endregion
    }
}

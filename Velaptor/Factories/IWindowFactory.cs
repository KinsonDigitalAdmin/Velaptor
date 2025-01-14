﻿// <copyright file="IWindowFactory.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace Velaptor.Factories
{
    using SILKWindow = Silk.NET.Windowing.IWindow;

    /// <summary>
    /// Creates a native window.
    /// </summary>
    internal interface IWindowFactory
    {
        /// <summary>
        /// Creates a <see cref="Silk"/> specific window.
        /// </summary>
        /// <returns>The window instance.</returns>
        public SILKWindow CreateSilkWindow();
    }
}

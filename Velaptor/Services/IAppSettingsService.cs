﻿// <copyright file="IAppSettingsService.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace Velaptor.Services;

/// <summary>
/// Provides application settings management.
/// </summary>
internal interface IAppSettingsService
{
    /// <summary>
    /// Gets the width of the window.
    /// </summary>
    uint WindowWidth { get; }

    /// <summary>
    /// Gets the height of the window.
    /// </summary>
    uint WindowHeight { get; }
}
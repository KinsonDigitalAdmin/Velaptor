﻿// <copyright file="OpenGLContextReactable.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

namespace Velaptor.Reactables
{
    // ReSharper disable RedundantNameQualifier
    using Velaptor.Reactables.Core;
    using Velaptor.Reactables.ReactableData;

    // ReSharper restore RedundantNameQualifier

    /// <summary>
    /// Creates a reactable to send push notifications of OpenGL events.
    /// </summary>
    internal class OpenGLContextReactable : Reactable<GLContextData>
    {
        /// <summary>
        /// Sends a push notification that the OpenGL context has been created.
        /// </summary>
        /// <param name="data">The data to send with the push notification.</param>
        public override void PushNotification(GLContextData data)
        {
            /* Work from the end to the beginning of the list
               just in case the reactable is disposed(removed)
               in the OnNext() method.
             */
            for (var i = Reactors.Count - 1; i >= 0; i--)
            {
                Reactors[i].OnNext(data);
            }
        }
    }
}

﻿// <copyright file="AudioDeviceManager.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

#pragma warning disable CA1303 // Do not pass literals as localized parameters
namespace Raptor.Audio
{
    using System;
    using System.Collections.Generic;
    using System.Data;
#if DEBUG
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
#endif
    using System.Linq;
    using OpenToolkit.Audio.OpenAL;
    using Raptor.Audio.Exceptions;
    using Raptor.OpenAL;

    /// <summary>
    /// Manages audio devices on the system using OpenAL.
    /// </summary>
    internal sealed class AudioDeviceManager : IAudioDeviceManager
    {
        private const string DeviceNamePrefix = "OpenAL Soft on "; // All device names returned are prefixed with this
        private static readonly string IsDisposedExceptionMessage = $"The '{nameof(AudioDeviceManager)}' has not been initialized.\nInvoked the '{nameof(AudioDeviceManager.InitDevice)}()' to initialize the device manager.";
        private static readonly Dictionary<int, SoundSource> SoundSources = new Dictionary<int, SoundSource>();
        private static readonly List<SoundState> ContinuePlaybackCache = new List<SoundState>();
        private static AudioDeviceManager instance = new AudioDeviceManager();
        private static ALDevice device;
        private static ALContext context;
        private static ALContextAttributes? attributes;
        private static IALInvoker? alInvoker;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioDeviceManager"/> class.
        /// </summary>
        private AudioDeviceManager()
        {
        }

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? DeviceChanged;

        /// <inheritdoc/>
        public bool IsInitialized => !AudioIsNull() &&
            !(alInvoker is null);

        /// <inheritdoc/>
        public string[] DeviceNames
        {
            get
            {
                if (!IsInitialized)
                    throw new AudioDeviceManagerNotInitializedException(IsDisposedExceptionMessage);

                var result = alInvoker?.GetString(device, AlcGetStringList.AllDevicesSpecifier)
                    .Select(n => n.Replace(DeviceNamePrefix, string.Empty, StringComparison.Ordinal)).ToArray();

                return result ?? Array.Empty<string>();
            }
        }

        /// <inheritdoc/>
        public void InitDevice(string? name = null)
        {
            var nameResult = name != null ? $"{DeviceNamePrefix}{name}" : name;

            if (device.Handle == IntPtr.Zero)
                device = alInvoker?.OpenDevice(nameResult) ?? ALDevice.Null;

            if (attributes is null)
                attributes = new ALContextAttributes();

            if (context.Handle == IntPtr.Zero)
                context = alInvoker?.CreateContext(device, attributes) ?? ALContext.Null;

            var setCurrentResult = alInvoker?.MakeContextCurrent(context);

            if (setCurrentResult == null || !(bool)setCurrentResult)
                throw new SettingContextCurrentException();
        }

        /// <inheritdoc/>
        public (int srcId, int bufferId) InitSound()
        {
            if (!IsInitialized)
                throw new AudioDeviceManagerNotInitializedException(IsDisposedExceptionMessage);

            SoundSource soundSrc;
            soundSrc.SampleRate = 0;
            soundSrc.TotalSeconds = -1f;

            soundSrc.SourceId = alInvoker?.GenSource() ?? 0;
            var bufferId = alInvoker?.GenBuffer() ?? 0;

            SoundSources.Add(soundSrc.SourceId, soundSrc);

            return (soundSrc.SourceId, bufferId);
        }

        /// <inheritdoc/>
        public void ChangeDevice(string name)
        {
            if (!IsInitialized)
                throw new AudioDeviceManagerNotInitializedException(IsDisposedExceptionMessage);

            if (!DeviceNames.Contains(name))
                throw new AudioDeviceDoesNotExistException("The audio device does not exist.", name);

            var availableDevices = DeviceNames;

            CacheSoundSources();

            DestroyDevice();
            InitDevice(name);

            SoundSources.Clear();

            DeviceChanged?.Invoke(this, new EventArgs());

            // Reset all of the states such as if playing or paused and the current time position
            foreach (var cachedState in ContinuePlaybackCache)
            {
                // Set the current position of the sound
                SetTimePosition(cachedState.SourceId, cachedState.TimePosition, cachedState.TotalSeconds);

                // Set the state of the sound
                switch (cachedState.PlaybackState)
                {
                    case PlaybackState.Playing:
                        alInvoker?.SourcePlay(cachedState.SourceId);
                        break;
                    case PlaybackState.Paused:
                        alInvoker?.SourceStop(cachedState.SourceId);
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public void UpdateSoundSource(SoundSource soundSrc)
        {
            if (!IsInitialized)
                throw new AudioDeviceManagerNotInitializedException(IsDisposedExceptionMessage);

            if (!SoundSources.Keys.Contains(soundSrc.SourceId))
                throw new SoundSourceDoesNotExistException($"The sound source with the source id '{soundSrc.SourceId}' does not exist.");

            SoundSources[soundSrc.SourceId] = soundSrc;
        }

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public void Dispose()
        {
            SoundSources?.Clear();

            ContinuePlaybackCache?.Clear();

            device = ALDevice.Null;
            context = ALContext.Null;
            attributes = null;
            alInvoker = null;

            instance = new AudioDeviceManager();
        }

        /// <summary>
        /// Gets an instance of the <see cref="AudioDeviceManager"/> singleton.
        /// </summary>
        /// <param name="alInvoker">The OpenAL invoker used to make OpenAL calls.</param>
        /// <returns>A singleton of the <see cref="AudioDeviceManager"/>.</returns>
        internal static AudioDeviceManager GetInstance(IALInvoker alInvoker)
        {
            if (alInvoker is null)
                throw new ArgumentNullException(nameof(alInvoker), "Parameter must not be null.");

            if (instance.IsInitialized)
                return instance;

            if (AudioDeviceManager.alInvoker is null)
            {
                AudioDeviceManager.alInvoker = alInvoker;
                AudioDeviceManager.alInvoker.ErrorCallback = ErrorCallback;
            }

            return instance;
        }

        /// <summary>
        /// Destroys the current audio device.  To use another audio device, the
        /// <see cref="AudioDeviceManager.InitDevice(string?)"/> will have to be invoked again.
        /// </summary>
        private static void DestroyDevice()
        {
            if (context != ALContext.Null)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                alInvoker.MakeContextCurrent(ALContext.Null);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                alInvoker.DestroyContext(context);
            }

            context = ALContext.Null;

            if (device != ALDevice.Null)
                alInvoker?.CloseDevice(device);

            device = ALDevice.Null;

            attributes = null;
        }

        /// <summary>
        /// Caches all of the current sound sources that are currently playing or paused.
        /// These cached sounds sources are the state of the sounds and is used to bring
        /// the state of the sounds back to where they were before changing to another audio device.
        /// </summary>
        private static void CacheSoundSources()
        {
            // Create a cache of all the songs currently playing and record the current playback position
            // Cache only if the sound was currently playing or paused

            // Guarentee that the cache is clear
            ContinuePlaybackCache.Clear();

            foreach (var soundSrcKVP in SoundSources)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var sourceState = alInvoker.GetSourceState(soundSrcKVP.Value.SourceId);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                if (sourceState != ALSourceState.Playing && sourceState != ALSourceState.Paused)
                    continue;
                SoundState soundState;
                soundState.SourceId = soundSrcKVP.Value.SourceId;
                soundState.PlaybackState = default;
                soundState.TimePosition = GetCurrentTimePosition(soundSrcKVP.Value.SourceId, soundSrcKVP.Value.SampleRate);
                soundState.TotalSeconds = soundSrcKVP.Value.TotalSeconds;

                if (sourceState == ALSourceState.Playing)
                {
                    soundState.PlaybackState = PlaybackState.Playing;
                }
                else if (sourceState == ALSourceState.Paused)
                {
                    soundState.PlaybackState = PlaybackState.Paused;
                }

                ContinuePlaybackCache.Add(soundState);
            }
        }

        /// <summary>
        /// Returns a value indicating if the audio device and context are null.
        /// </summary>
        /// <returns>True if the device and context are null.</returns>
        private static bool AudioIsNull() => device == ALDevice.Null && context == ALContext.Null && attributes is null;

        /// <summary>
        /// Gets the current position of the sound in the value of seconds.
        /// </summary>
        /// <param name="srcId">The OpenAL source id.</param>
        /// <param name="sampleRate">The sample rate of the sound.</param>
        /// <returns>The position in seconds.</returns>
        private static float GetCurrentTimePosition(int srcId, float sampleRate)
        {
            var sampleOffset = alInvoker?.GetSource(srcId, ALGetSourcei.SampleOffset) ?? 0;

            return sampleOffset / sampleRate;
        }

        /// <summary>
        /// Sets the time position of the sound to the given <paramref name="seconds"/> value.
        /// </summary>
        /// <param name="srcId">The OpenAL source id.</param>
        /// <param name="seconds">The position in seconds.</param>
        /// <param name="totalSeconds">The total seconds of the sound.</param>
        private static void SetTimePosition(int srcId, float seconds, float totalSeconds)
        {
            // Prevent negative number
            seconds = seconds < 0f ? 0.0f : seconds;

            seconds = seconds > totalSeconds ? totalSeconds : seconds;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            alInvoker.Source(srcId, ALSourcef.SecOffset, seconds);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        /// <summary>
        /// Invoked when there is an OpenAL specific error.
        /// </summary>
        /// <param name="errorMsg">The error message from OpenAL.</param>
        [ExcludeFromCodeCoverage]
        private static void ErrorCallback(string errorMsg) => Debugger.Break();
    }
}
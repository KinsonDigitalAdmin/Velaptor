﻿// <copyright file="SoundTests.cs" company="KinsonDigital">
// Copyright (c) KinsonDigital. All rights reserved.
// </copyright>

#pragma warning disable IDE0017 // Simplify object initialization
namespace RaptorTests.Audio
{
    using System;
    using Castle.DynamicProxy.Tokens;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Moq;
    using OpenToolkit.Audio.OpenAL;
    using Raptor.Audio;
    using Raptor.Content;
    using Raptor.OpenAL;
    using RaptorTests.Helpers;
    using Xunit;

    /// <summary>
    /// Tests the <see cref="Sound"/> class.
    /// </summary>
    public class SoundTests : IDisposable
    {
        private readonly Mock<IAudioDeviceManager> mockAudioManager;
        private readonly Mock<ISoundDecoder<float>> mockOggDecoder;
        private readonly Mock<ISoundDecoder<byte>> mockMp3Decoder;
        private readonly Mock<IContentSource> mockContentSrc;
        private readonly Mock<IALInvoker> mockALInvoker;
        private readonly string soundContentName = "sound";
        private readonly string oggContentFilePath;
        private readonly string mp3ContentFilePath;
        private readonly float[] oggBufferData = new float[] { 11f, 22f, 33f, 44f };
        private readonly int srcId = 1234;
        private readonly int bufferId = 5678;
        private Sound? sound;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundTests"/> class.
        /// </summary>
        public SoundTests()
        {
            this.oggContentFilePath = @$"C:\temp\Content\Graphics\{this.soundContentName}.ogg";
            this.mp3ContentFilePath = @$"C:\temp\Content\Graphics\{this.soundContentName}.mp3";

            this.mockALInvoker = new Mock<IALInvoker>();
            this.mockALInvoker.Setup(m => m.GenSource()).Returns(this.srcId);
            this.mockALInvoker.Setup(m => m.GenBuffer()).Returns(this.bufferId);

            this.mockAudioManager = new Mock<IAudioDeviceManager>();
            this.mockAudioManager.Setup(m => m.InitSound()).Returns((this.srcId, this.bufferId));

            this.mockOggDecoder = new Mock<ISoundDecoder<float>>();
            this.mockOggDecoder.Setup(m => m.LoadData(this.oggContentFilePath)).Returns(() =>
            {
                return new SoundData<float>()
                {
                    BufferData = oggBufferData,
                    Channels = 2,
                    Format = AudioFormat.Stereo16,
                    SampleRate = 44100,
                    TotalSeconds = 10,
                };
            });

            this.mockMp3Decoder = new Mock<ISoundDecoder<byte>>();

            this.mockContentSrc = new Mock<IContentSource>();
            this.mockContentSrc.Setup(m => m.GetContentPath(ContentType.Sounds, "sound"))
                .Returns(@"C:\temp\Content\Graphics\sound.ogg");
        }

        #region Constructor Tests
        [Fact]
        public void Ctor_WhenInvoked_UsesCorrectContentType()
        {
            // Act
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Assert
            this.mockContentSrc.Verify(m => m.GetContentPath(ContentType.Sounds, this.soundContentName), Times.Once());
        }

        [Theory]
        [InlineData(AudioFormat.Mono8, ALFormat.Mono8)]
        [InlineData(AudioFormat.Mono16, ALFormat.Mono16)]
        [InlineData(AudioFormat.Mono32Float, ALFormat.MonoFloat32Ext)]
        [InlineData(AudioFormat.Stereo8, ALFormat.Stereo8)]
        [InlineData(AudioFormat.Stereo16, ALFormat.Stereo16)]
        [InlineData(AudioFormat.StereoFloat32, ALFormat.StereoFloat32Ext)]
        public void Ctor_WhenUsingOggSound_UploadsBufferData(AudioFormat format, ALFormat expected)
        {
            // Act
            this.mockOggDecoder.Setup(m => m.LoadData(this.oggContentFilePath))
                .Returns(() =>
                {
                    SoundData<float> result;
                    result.TotalSeconds = 200f;
                    result.Format = format;
                    result.Channels = 2;
                    result.SampleRate = 44100;
                    result.BufferData = new[] { 1f, 2f };

                    return result;
                });
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Assert
            this.mockOggDecoder.Verify(m => m.LoadData(this.oggContentFilePath), Times.Once());
            this.mockALInvoker.Verify(m => m.BufferData(this.bufferId, expected, new[] { 1f, 2f }, 8, 44100), Times.Once());
        }

        [Fact]
        public void Ctor_WhenUsingUnknownFormat_ThrowsException()
        {
            // Act
            this.mockOggDecoder.Setup(m => m.LoadData(this.oggContentFilePath))
                .Returns(() =>
                {
                    SoundData<float> result;
                    result.TotalSeconds = 200f;
                    result.Format = default;
                    result.Channels = 2;
                    result.SampleRate = 44100;
                    result.BufferData = new[] { 1f, 2f };

                    return result;
                });

            // Act & Assert
            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);
            }, "Invalid or unknown audio format.");
        }

        [Fact]
        public void Ctor_WhenUsingMp3Sound_UploadsBufferData()
        {
            // Act
            this.mockContentSrc.Setup(m => m.GetContentPath(ContentType.Sounds, "sound"))
                .Returns(@"C:\temp\Content\Graphics\sound.mp3");
            this.mockMp3Decoder.Setup(m => m.LoadData(this.mp3ContentFilePath))
                .Returns(() =>
                {
                    SoundData<byte> result;
                    result.TotalSeconds = 200f;
                    result.Format = AudioFormat.Stereo16;
                    result.Channels = 2;
                    result.SampleRate = 44100;
                    result.BufferData = new byte[] { 1, 2 };

                    return result;
                });
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Assert
            this.mockMp3Decoder.Verify(m => m.LoadData(this.mp3ContentFilePath), Times.Once());
            this.mockALInvoker.Verify(m => m.BufferData(this.bufferId, ALFormat.Stereo16, new byte[] { 1, 2 }, 2, 44100), Times.Once());
        }

        [Fact]
        public void Ctor_WhenUsingUnsupportedFileType_ThrowsException()
        {
            // Arrange
            this.mockContentSrc.Setup(m => m.GetContentPath(ContentType.Sounds, "sound"))
                .Returns(@"C:\temp\Content\Graphics\sound.wav");

            // Act & Assert
            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                this.sound = new Sound("sound", this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);
            }, "The file extension '.wav' is not supported file type.");
        }
        #endregion

        #region Prop Tests
        [Fact]
        public void ContentName_WhenGettingValue_ReturnsCorrectValue()
        {
            // Act
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Assert
            Assert.Equal("sound", this.sound.ContentName);
        }

        [Fact]
        public void IsLooping_WhenGettingValueWhileDisposed_ThrowsException()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act & Assert
            this.sound.Dispose();

            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                _ = this.sound.IsLooping;
            }, "The sound is disposed.  You must create another sound instance.");
        }

        [Fact]
        public void IsLooping_WhenGettingValue_GetsSoundLoopingValue()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act
            _ = this.sound.IsLooping;

            // Assert
            this.mockALInvoker.Verify(m => m.GetSource(this.srcId, ALSourceb.Looping), Times.Once());
        }

        [Fact]
        public void IsLooping_WhenSettingValueWhileDisposed_ThrowsException()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act & Assert
            this.sound.Dispose();

            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                this.sound.IsLooping = true;
            }, "The sound is disposed.  You must create another sound instance.");
        }

        [Fact]
        public void IsLooping_WhenSettingValue_SetsSoundLoopingSetting()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act
            this.sound.IsLooping = true;

            // Assert
            this.mockALInvoker.Verify(m => m.Source(this.srcId, ALSourceb.Looping, true), Times.Once());
        }

        [Fact]
        public void Volume_WhenGettingValueWhileDisposed_ThrowsException()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act & Assert
            this.sound.Dispose();

            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                _ = this.sound.Volume;
            }, "The sound is disposed.  You must create another sound instance.");
        }

        [Fact]
        public void Volume_WhenGettingValue_GetsSoundVolume()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act
            _ = this.sound.Volume;

            // Assert
            this.mockALInvoker.Verify(m => m.GetSource(this.srcId, ALSourcef.Gain), Times.Once());
        }

        [Fact]
        public void Volume_WhenSettingValueWhileDisposed_ThrowsException()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act & Assert
            this.sound.Dispose();

            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                this.sound.Volume = 0.5f;
            }, "The sound is disposed.  You must create another sound instance.");
        }

        [Theory]
        [InlineData(0.5f, 0.00499999989f)]
        [InlineData(50f, 0.5f)]
        [InlineData(-5f, 0f)]
        [InlineData(142f, 1f)]
        public void Volume_WhenSettingValue_SetsSoundVolume(float volume, float expected)
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act
            this.sound.Volume = volume;

            // Assert
            this.mockALInvoker.Verify(m => m.Source(this.srcId, ALSourcef.Gain, expected), Times.Once());
        }

        [Fact]
        public void CurrentTimePosition_WhenDisposed_ThrowsException()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act & Assert
            this.sound.Dispose();

            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                _ = this.sound.TimePosition;
            }, "The sound is disposed.  You must create another sound instance.");
        }

        [Fact]
        public void CurrentTimePosition_WhenGettingValue_GetsSoundTimePosition()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act
            _ = this.sound.TimePosition;

            // Assert
            this.mockALInvoker.Verify(m => m.GetSource(this.srcId, ALGetSourcei.SampleOffset), Times.Once());
        }
        #endregion

        #region Method Tests
        [Fact]
        public void Play_WhenDisposed_ThrowsException()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act & Assert
            this.sound.Dispose();

            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                this.sound.PlaySound();
            }, "The sound is disposed.  You must create another sound instance.");
        }

        [Fact]
        public void Play_WhenInvoked_PlaysSound()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act
            this.sound.PlaySound();

            // Assert
            this.mockALInvoker.Verify(m => m.SourcePlay(this.srcId), Times.Once());
        }

        [Fact]
        public void Pause_WhenDisposed_ThrowsException()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act & Assert
            this.sound.Dispose();

            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                this.sound.PauseSound();
            }, "The sound is disposed.  You must create another sound instance.");
        }

        [Fact]
        public void Pause_WhenInvoked_PausesSound()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act
            this.sound.PauseSound();

            // Assert
            this.mockALInvoker.Verify(m => m.SourcePause(this.srcId), Times.Once());
        }

        [Fact]
        public void Stop_WhenDisposed_ThrowsException()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act & Assert
            this.sound.Dispose();

            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                this.sound.StopSound();
            }, "The sound is disposed.  You must create another sound instance.");
        }

        [Fact]
        public void Stop_WhenInvoked_StopsSound()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act
            this.sound.StopSound();

            // Assert
            this.mockALInvoker.Verify(m => m.SourceStop(this.srcId), Times.Once());
        }

        [Fact]
        public void Reset_WhenDisposed_ThrowsException()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act & Assert
            this.sound.Dispose();

            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                this.sound.Reset();
            }, "The sound is disposed.  You must create another sound instance.");
        }

        [Fact]
        public void Reset_WhenInvoked_ResetsSound()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act
            this.sound.Reset();

            // Assert
            this.mockALInvoker.Verify(m => m.SourceRewind(this.srcId), Times.Once());
        }

        [Fact]
        public void SetTimePosition_WhenDisposed_ThrowsException()
        {
            // Arrange
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act & Assert
            this.sound.Dispose();

            AssertHelpers.ThrowsWithMessage<Exception>(() =>
            {
                this.sound.SetTimePosition(5);
            }, "The sound is disposed.  You must create another sound instance.");
        }

        [Theory]
        [InlineData(123f, 123f)]
        [InlineData(-2f, 0f)]
        [InlineData(300f, 123f)]
        public void SetTimePosition_WithInvoked_SetsTimePosition(float seconds, float expected)
        {
            // Arrange
            this.mockOggDecoder.Setup(m => m.LoadData(this.oggContentFilePath))
                .Returns(() =>
                {
                    SoundData<float> result;
                    result.TotalSeconds = 123f;
                    result.Format = AudioFormat.Stereo16;
                    result.Channels = 2;
                    result.SampleRate = 441000;
                    result.BufferData = new[] { 1f, 2f };

                    return result;
                });
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act
            this.sound.SetTimePosition(seconds);

            // Assert
            this.mockALInvoker.Verify(m => m.Source(this.srcId, ALSourcef.SecOffset, expected), Times.Once());
        }

        [Fact]
        public void Sound_WhenChangingAudoDevice_ReinitializesSound()
        {
            // Arrange
            // Simulate an audio device change so the event is invoked inside of the sound class
            this.mockAudioManager.Setup(m => m.ChangeDevice(It.IsAny<string>()))
                .Callback<string>((name) =>
                {
                    this.mockAudioManager.Raise(manager => manager.DeviceChanged += (sender, e) => { }, new EventArgs());
                });

            this.mockOggDecoder.Setup(m => m.LoadData(this.oggContentFilePath))
                .Returns(() =>
                {
                    SoundData<float> result;
                    result.TotalSeconds = 200f;
                    result.Format = AudioFormat.Stereo16;
                    result.Channels = 2;
                    result.SampleRate = 44100;
                    result.BufferData = new[] { 1f, 2f };

                    return result;
                });
            this.sound = new Sound(this.soundContentName, this.mockALInvoker.Object, this.mockAudioManager.Object, this.mockOggDecoder.Object, this.mockMp3Decoder.Object, this.mockContentSrc.Object);

            // Act
            this.mockAudioManager.Object.ChangeDevice(It.IsAny<string>());

            // Assert
            // NOTE: The first invoke is during Sound construction, the second is when changing audio devices
            this.mockOggDecoder.Verify(m => m.LoadData(this.oggContentFilePath), Times.Exactly(2));
            this.mockALInvoker.Verify(m => m.BufferData(this.bufferId, ALFormat.Stereo16, new[] { 1f, 2f }, 8, 44100), Times.Exactly(2));
        }
        #endregion

        public void Dispose()
        {
            this.sound?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
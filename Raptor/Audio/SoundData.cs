﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Raptor.Audio
{
    public struct SoundData<T>
    {
        public T[] BufferData;

        public int SampleRate;

        public int Channels;

        public AudioFormat Format;

        public float TotalSeconds;
    }
}
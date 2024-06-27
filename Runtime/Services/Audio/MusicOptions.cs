//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.App.Services
{
    [Serializable]
    public struct MusicOptions
    {
        public float Volume;
        public float DelaySec;
        public float FadeDurationSec;

        public static MusicOptions Default => new()
        {
            Volume = 1f,
            DelaySec = 0f,
            FadeDurationSec = 0f
        };
    }
}

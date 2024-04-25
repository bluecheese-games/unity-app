//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

namespace BlueCheese.Unity.App.Services
{
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

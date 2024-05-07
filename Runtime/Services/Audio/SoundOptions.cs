//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.Unity.App.Services
{
    [Serializable]
    public struct SoundOptions
    {
        public float Volume;
        public float Delay;
        public bool Loop;
        public float Pitch;
        public SpacialOptions Spacial;

        public static SoundOptions Default => new()
        {
            Volume = 1f,
            Delay = 0f,
            Pitch = 1f,
            Spacial = SpacialOptions.Default,
        };

        [Serializable]
        public struct SpacialOptions
        {
            public bool IsSpacialized;
            public Vector3 Position;
            public Transform Target;
            public float MinDistance;
            public float MaxDistance;
            public AudioRolloffMode RolloffMode;

            public static SpacialOptions Default => new()
            {
                IsSpacialized = false,
                Position = default,
                Target = default,
                MinDistance = 1f,
                MaxDistance = 10f,
                RolloffMode = AudioRolloffMode.Logarithmic,
            };
        }
    }
}

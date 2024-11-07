//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using NaughtyAttributes;
using System;
using UnityEngine;

namespace BlueCheese.App
{
    [Serializable]
    public struct SoundOptions
    {
        [Range(0f, 1f)]
        public float Volume;
        [Min(0f)]
        public float Delay;
        public bool Loop;
		[MinMaxSlider(0, 3)]
		public Vector2 Pitch;
        public SpacialOptions Spacial;

        [HideInInspector]
        public bool _isInitialized;

        public static SoundOptions Default => new()
        {
            Volume = 1f,
            Delay = 0f,
            Pitch = new(1f, 1f),
            Spacial = SpacialOptions.Default,
        };

        [Serializable]
        public struct SpacialOptions
        {
            public bool IsSpacialized;
            public Transform Target;
            public float MinDistance;
            public float MaxDistance;
            public AudioRolloffMode RolloffMode;

            public static SpacialOptions Default => new()
            {
                IsSpacialized = false,
                Target = default,
                MinDistance = 1f,
                MaxDistance = 10f,
                RolloffMode = AudioRolloffMode.Logarithmic,
            };
        }
    }
}

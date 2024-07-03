//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.App
{
    public class DefaultRandomService : IRandomService
    {
        private Random _random;

        public float Value
        {
            get
            {
                return (float)GetRandom().NextDouble();
            }
        }

        private Random GetRandom() => _random ??= new Random();

        public void Init(int seed) => _random = new Random(seed);

        public float Next(float min, float max) => min + (Value * (max - min));

        public int Next(int min, int max) => GetRandom().Next(min, max);
    }
}

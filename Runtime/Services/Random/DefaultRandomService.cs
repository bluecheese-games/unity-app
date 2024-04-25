using System;

namespace BlueCheese.Unity.App.Services
{
    public class DefaultRandomService : IRandomService
    {
        private Random _random;

        public float Value
        {
            get
            {
                return (float)_random.NextDouble();
            }
        }

        private Random GetRandom() => _random ??= new Random();

        public void Init(int seed) => _random = new Random(seed);

        public float Next(float min, float max) => min + (Value * (max - min));

        public int Next(int min, int max) => GetRandom().Next(min, max);
    }
}

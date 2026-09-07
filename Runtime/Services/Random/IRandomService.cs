namespace BlueCheese.App
{
    public interface IRandomService
    {
        void Init(int seed);
        float Next(float min, float max);
        int Next(int min, int max);
        float Value { get; }
    }
}

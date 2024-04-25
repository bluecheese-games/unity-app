namespace BlueCheese.Unity.App.Services
{
    public interface IRandomService
    {
        void Init(int seed);
        float Next(float min, float max);
        int Next(int min, int max);
        float Value { get; }
    }
}

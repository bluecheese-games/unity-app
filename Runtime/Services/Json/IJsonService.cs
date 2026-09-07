namespace BlueCheese.App
{
    public interface IJsonService
    {
        string Serialize<T>(T obj);
        T Deserialize<T>(string str);
    }
}

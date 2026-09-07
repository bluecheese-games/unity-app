using UnityEngine;

namespace BlueCheese.App
{
    public class JsonUtilityService : IJsonService
    {
        public string Serialize<T>(T obj) => JsonUtility.ToJson(obj);

        public T Deserialize<T>(string str) => JsonUtility.FromJson<T>(str);
    }
}

//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using Unity.Plastic.Newtonsoft.Json;

namespace BlueCheese.App
{
    public class NewtonSoftJsonService : IJsonService
    {
        public T Deserialize<T>(string str) => JsonConvert.DeserializeObject<T>(str);

        public string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj);
    }
}

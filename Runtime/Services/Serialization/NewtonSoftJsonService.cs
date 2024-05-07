//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using Unity.Plastic.Newtonsoft.Json;

namespace BlueCheese.Unity.App.Services
{
    public class NewtonSoftJsonService : ISerializationService
    {
        public T JsonDeserialize<T>(string str) => JsonConvert.DeserializeObject<T>(str);

        public string JsonSerialize<T>(T obj) => JsonConvert.SerializeObject(obj);
    }
}

//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App.Services
{
    public class JsonUtilityService : IJsonService
    {
        public string Serialize<T>(T obj) => JsonUtility.ToJson(obj);

        public T Deserialize<T>(string str) => JsonUtility.FromJson<T>(str);
    }
}

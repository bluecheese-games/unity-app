//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.Unity.App.Services
{
    public class JsonUtilityService : ISerializationService
    {
        public string JsonSerialize<T>(T obj) => JsonUtility.ToJson(obj);

        public T JsonDeserialize<T>(string str) => JsonUtility.FromJson<T>(str);
    }
}

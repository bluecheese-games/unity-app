//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.Unity.App.Services
{
    public class PlayerPrefsService : ILocalStorageService
    {
        private readonly ISerializationService _serializationService;

        public PlayerPrefsService(ISerializationService serializationService)
        {
            _serializationService = serializationService;
        }

        public void WriteValue<T>(string key, T value = default)
        {
            switch (value)
            {
                case int intValue:
                    PlayerPrefs.SetInt(key, intValue);
                    break;
                case float floatValue:
                    PlayerPrefs.SetFloat(key, floatValue);
                    break;
                case string stringValue:
                    PlayerPrefs.SetString(key, stringValue);
                    break;
                default:
                    PlayerPrefs.SetString(key, _serializationService.JsonSerialize(value));
                    break;
            }
        }

        public T ReadValue<T>(string key, T defaultValue = default)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return defaultValue;
            }

            return Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.Int32 => (T)Convert.ChangeType(PlayerPrefs.GetInt(key), typeof(T)),
                TypeCode.Single => (T)Convert.ChangeType(PlayerPrefs.GetFloat(key), typeof(T)),
                TypeCode.String => (T)Convert.ChangeType(PlayerPrefs.GetString(key), typeof(T)),
                _ => _serializationService.JsonDeserialize<T>(PlayerPrefs.GetString(key)),
            };
        }
    }
}

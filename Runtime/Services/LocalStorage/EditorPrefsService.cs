//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using UnityEditor;

namespace BlueCheese.App
{
    public class EditorPrefsService : ILocalStorageService
    {
        private readonly IJsonService _json;

        public EditorPrefsService(IJsonService json)
        {
            _json = json;
        }

        public void WriteValue<T>(string key, T value = default)
        {
            switch (value)
            {
                case int intValue:
                    EditorPrefs.SetInt(key, intValue);
                    break;
                case float floatValue:
					EditorPrefs.SetFloat(key, floatValue);
                    break;
                case string stringValue:
					EditorPrefs.SetString(key, stringValue);
                    break;
                default:
					EditorPrefs.SetString(key, _json.Serialize(value));
                    break;
            }
        }

        public T ReadValue<T>(string key, T defaultValue = default)
        {
            if (!EditorPrefs.HasKey(key))
            {
                return defaultValue;
            }

            return Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.Int32 => (T)Convert.ChangeType(EditorPrefs.GetInt(key), typeof(T)),
                TypeCode.Single => (T)Convert.ChangeType(EditorPrefs.GetFloat(key), typeof(T)),
                TypeCode.String => (T)Convert.ChangeType(EditorPrefs.GetString(key), typeof(T)),
                _ => _json.Deserialize<T>(EditorPrefs.GetString(key)),
            };
        }
    }
}

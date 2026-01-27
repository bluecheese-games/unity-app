//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;

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
#if UNITY_EDITOR
			switch (value)
			{
				case int intValue:
					UnityEditor.EditorPrefs.SetInt(key, intValue);
					break;
				case float floatValue:
					UnityEditor.EditorPrefs.SetFloat(key, floatValue);
					break;
				case string stringValue:
					UnityEditor.EditorPrefs.SetString(key, stringValue);
					break;
				default:
					UnityEditor.EditorPrefs.SetString(key, _json.Serialize(value));
					break;
			}
#endif
		}

		public T ReadValue<T>(string key, T defaultValue = default)
		{
#if UNITY_EDITOR
			if (!UnityEditor.EditorPrefs.HasKey(key))
			{
				return defaultValue;
			}

			return Type.GetTypeCode(typeof(T)) switch
			{
				TypeCode.Int32 => (T)Convert.ChangeType(UnityEditor.EditorPrefs.GetInt(key), typeof(T)),
				TypeCode.Single => (T)Convert.ChangeType(UnityEditor.EditorPrefs.GetFloat(key), typeof(T)),
				TypeCode.String => (T)Convert.ChangeType(UnityEditor.EditorPrefs.GetString(key), typeof(T)),
				_ => _json.Deserialize<T>(UnityEditor.EditorPrefs.GetString(key)),
			};
#else
            return defaultValue;
#endif
		}
	}
}

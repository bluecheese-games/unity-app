//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App.Services
{
    public class ConfigRegistry
    {
        private static readonly Dictionary<string, ConfigItem> _items = new();

        public ConfigRegistry(params ConfigAsset[] assets)
        {
            foreach (var asset in assets)
            {
                LoadAsset(asset);
            }
        }

        private void LoadAsset(ConfigAsset asset) => AddItems(asset.Items);

        private void AddItems(params ConfigItem[] items)
        {
            foreach (var item in items)
            {
                AddItem(item);
            }
        }

        private void AddItem(ConfigItem item)
        {
            if (!_items.ContainsKey(item.Key))
            {
                _items[item.Key] = item;
            }
        }

        public static string GetStringValue(string key, string defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.StringValue;
        }

        public static int GetIntValue(string key, int defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.IntValue;
        }

        public static float GetFloatValue(string key, float defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.FloatValue;
        }

        public static bool GetBoolValue(string key, bool defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.BoolValue;
        }

        public static Object GetObjectValue(string key, Object defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.ObjectValue;
        }

        public static void SetStringValue(string key, string value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.String;
            item.StringValue = value;
        }

        public static void SetIntValue(string key, int value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.Int;
            item.IntValue = value;
        }

        public static void SetFloatValue(string key, float value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.Float;
            item.FloatValue = value;
        }

        public static void SetBoolValue(string key, bool value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.Boolean;
            item.BoolValue = value;
        }

        public static void SetObjectValue(string key, Object value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.Object;
            item.ObjectValue = value;
        }

        public static ConfigItem GetItem(string key, bool createIfNotExists = false)
        {
            if (!_items.ContainsKey(key))
            {
                if (createIfNotExists == false)
                {
                    return null;
                }
                _items[key] = new ConfigItem(key, ConfigItem.ValueType.String);
            }
            return _items[key];
        }
    }
}

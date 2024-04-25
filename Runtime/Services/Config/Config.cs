//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.Unity.App.Services
{
    public partial class Config
    {
        public const string ConfigRessourceFolder = "Config";

        private readonly Dictionary<string, ConfigItem> _items = new Dictionary<string, ConfigItem>();

        public Config(params ConfigAsset[] assets)
        {
            foreach (var asset in assets)
            {
                LoadAsset(asset);
            }
        }

        public void LoadAsset(ConfigAsset asset) => AddItems(asset.Items);

        public void AddItems(params ConfigItem[] items)
        {
            foreach (var item in items)
            {
                AddItem(item);
            }
        }

        public void AddItem(ConfigItem item)
        {
            if (!_items.ContainsKey(item.Key))
            {
                _items[item.Key] = item;
            }
        }

        public string GetStringValue(string key, string defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.StringValue;
        }

        public int GetIntValue(string key, int defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.IntValue;
        }

        public float GetFloatValue(string key, float defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.FloatValue;
        }

        public bool GetBoolValue(string key, bool defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.BoolValue;
        }

        public Object GetObjectValue(string key, Object defaultValue = default)
        {
            var item = GetItem(key);
            return item == null ? defaultValue : item.ObjectValue;
        }

        public void SetStringValue(string key, string value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.String;
            item.StringValue = value;
        }

        public void SetIntValue(string key, int value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.Int;
            item.IntValue = value;
        }

        public void SetFloatValue(string key, float value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.Float;
            item.FloatValue = value;
        }

        public void SetBoolValue(string key, bool value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.Boolean;
            item.BoolValue = value;
        }

        public void SetObjectValue(string key, Object value)
        {
            var item = GetItem(key, true);
            item.Type = ConfigItem.ValueType.Object;
            item.ObjectValue = value;
        }

        public ConfigItem GetItem(string key, bool createIfNotExists = false)
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

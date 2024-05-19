//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using NaughtyAttributes;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Services
{
    [CreateAssetMenu(fileName = "ConfigManager", menuName = "Config/Manager")]
    public class ConfigAssetsManager : ScriptableObject
    {
        public string GeneratedScriptsFolder = "Assets/Scripts/Configs";

        public ConfigAsset[] Configs;

        [Button]
        private void FindConfigs()
        {
            Configs = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(ConfigAsset)))
                .Select(guid => AssetDatabase.LoadAssetAtPath<ConfigAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();
        }

        [Button]
        private void GenerateCode()
        {
            foreach (var config in Configs)
            {
                ConfigCodeGen.Generate(config, GeneratedScriptsFolder);
            }
        }
    }
}

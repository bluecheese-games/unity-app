//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.IO;
using UnityEngine;

namespace BlueCheese.Unity.App.Services
{
    [CreateAssetMenu(fileName = "Config_New", menuName = "Config/Asset")]
    public class ConfigAsset : ScriptableObject
    {
        public ConfigItem[] Items;
        public string GeneratedFolder = "";

        public string GeneratedFilePath => Path.Combine(Application.dataPath, GeneratedFolder, name + ".cs");

        public bool GeneratedFileExists => File.Exists(GeneratedFilePath);

        private void OnValidate()
        {
            UpdateGenFolder();

            foreach (var item in Items)
            {
                item.Cleanup();
            }
        }

        private void OnEnable()
        {
            UpdateGenFolder();
        }

        private void UpdateGenFolder()
        {
            if (GeneratedFolder == "")
            {
                IAssetService assetService = new AssetService();
                var assets = assetService.LoadAssetsFromResources<ConfigAsset>(Config.ConfigRessourceFolder);
                if (assets.Length > 0 && assets[0] != this)
                {
                    GeneratedFolder = assets[0].GeneratedFolder;
                }
            }
        }
    }
}

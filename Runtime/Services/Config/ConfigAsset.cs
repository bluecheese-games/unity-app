//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.IO;
using UnityEngine;

namespace BlueCheese.App.Services
{
    [CreateAssetMenu(fileName = "Config_New", menuName = "Config/Asset")]
    public class ConfigAsset : ScriptableObject
    {
        public ConfigItem[] Items;

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
            /*if (GeneratedFolder == "")
            {
                IAssetService assetService = new AssetService();
                var assets = assetService.LoadAssetsFromResources<ConfigAsset>(Config.ConfigRessourceFolder);
                if (assets.Length > 0 && assets[0] != this)
                {
                    GeneratedFolder = assets[0].GeneratedFolder;
                }
            }*/
        }
    }
}

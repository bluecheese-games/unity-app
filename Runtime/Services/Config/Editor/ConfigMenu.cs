//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

namespace BlueCheese.Unity.App.Services.Editor
{
    public static class ConfigMenu
    {
        [UnityEditor.MenuItem(itemName: "Config/Edit")]
        private static void EditConfigData()
        {
            UnityEditor.EditorUtility.FocusProjectWindow();
            IAssetService assetService = new AssetService();
            var assets = assetService.LoadAssetsFromResources<ConfigAsset>(Config.ConfigRessourceFolder);
            UnityEditor.AssetDatabase.OpenAsset(assets[0]);
        }
    }
}

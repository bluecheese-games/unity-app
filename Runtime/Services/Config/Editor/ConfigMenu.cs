//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App.Services.Editor
{
    public static class ConfigMenu
    {
        [UnityEditor.MenuItem(itemName: "Config/Edit")]
        private static void EditConfigData()
        {
            UnityEditor.EditorUtility.FocusProjectWindow();
            IAssetService assetService = new AssetService();
            var assets = assetService.LoadAssetsFromResources<ConfigAsset>("Configs");
            UnityEditor.AssetDatabase.OpenAsset(assets[0]);
        }
    }
}

//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.Unity.App.Services
{
    public class ConfigService : IConfigService
    {
        private readonly IAssetService _assetService;

        private bool _isInitialized;

        public Config Config { get; private set; }

        public ConfigService(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            // load all config assets from resources
            var assets = _assetService.LoadAssetsFromResources<ConfigAsset>(Config.ConfigRessourceFolder);
            Config = new Config(assets);

            _isInitialized = true;
        }
    }
}

//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App.Services
{
    public class ConfigService : IConfigService
    {
        private readonly IAssetService _assetService;

        private bool _isInitialized;

        public ConfigRegistry Config { get; private set; }

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
            var assetsManager = _assetService.FindAssetInResources<ConfigAssetsManager>();
            Config = new ConfigRegistry(assetsManager.ConfigAssets);

            _isInitialized = true;
        }
    }
}

//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App
{
    public static class DefaultServicesInstaller
    {
        public static UnityApp.Builder RegisterDefaultServices(this UnityApp.Builder builder)
        {
            builder.ServiceContainer.Register<IAudioService, AudioService>()
                .WithOptions(() => new AudioService.Options()
                {
                    AudioBankResourcePath = "Audio",
                });
            builder.ServiceContainer.Register<IRemoteConfigService, DefaultRemoteConfigService>();
            builder.ServiceContainer.Register<ILocalStorageService, PlayerPrefsService>();
            builder.ServiceContainer.Register<ICacheService, MemoryCacheService>();
            builder.ServiceContainer.Register<ISceneService, UnitySceneService>();
            builder.ServiceContainer.Register<IUIService, UIService>();
            builder.ServiceContainer.Register<IInputService, DefaultInputService>();
            builder.ServiceContainer.Register<IAssetLoaderService, AssetService>();
            builder.ServiceContainer.Register<IAssetFinderService, AssetService>();
            builder.ServiceContainer.Register<IJsonService, JsonUtilityService>();
            builder.ServiceContainer.Register<IClockService, UnityClockService>();
            builder.ServiceContainer.Register<IHttpService, HttpService>();
            builder.ServiceContainer.Register<IHttpClient, UnityWebRequestHttpClient>();
            builder.ServiceContainer.Register<IGameObjectService, GameObjectService>();
            builder.ServiceContainer.Register<IGameObjectPoolService, GameObjectPoolService>();
            builder.ServiceContainer.Register<IErrorHandlingService, DefaultErrorHandlingService>();
            builder.ServiceContainer.Register<ITrackingService, DebugTrackingService>();
            builder.ServiceContainer.Register<IRandomService, DefaultRandomService>().AsTransient();
            builder.ServiceContainer.Register<ILocalizationService, LocalizationService>()
                .WithOptions(() => LocalizationService.Options.FromResourcesOrDefault());
            builder.ServiceContainer.Register<ITranslationService, TranslationService>();
            builder.ServiceContainer.Register(typeof(ILogger<>), typeof(UnityLogger<>));
            return builder;
        }
    }
}

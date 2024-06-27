//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App.Services
{
    public static class DefaultServicesRegistrar
    {
        public static UnityApp.Builder RegisterDefaultServices(this UnityApp.Builder builder)
        {
            builder.ServiceContainer.Register<IAudioService, DefaultAudioService>();
            builder.ServiceContainer.Register<IRemoteConfigService, DefaultRemoteConfigService>();
            builder.ServiceContainer.Register<ILocalStorageService, PlayerPrefsService>();
            builder.ServiceContainer.Register<ICacheService, MemoryCacheService>();
            builder.ServiceContainer.Register<ISceneService, UnitySceneService>();
            builder.ServiceContainer.Register<IUIService, UIService>();
            builder.ServiceContainer.Register<IInputService, DefaultInputService>();
            builder.ServiceContainer.Register<IAssetService, AssetService>();
            builder.ServiceContainer.Register<IJsonService, JsonUtilityService>();
            builder.ServiceContainer.Register<IClockService, UnityClockService>();
            builder.ServiceContainer.Register<IHttpService, UnityHttpService>();
            builder.ServiceContainer.Register<IGameObjectService, GameObjectService>();
            builder.ServiceContainer.Register<IPoolService, DefaultPoolService>();
            builder.ServiceContainer.Register<IErrorHandlingService, DefaultErrorHandlingService>();
            builder.ServiceContainer.Register<ITrackingService, DebugTrackingService>();
            builder.ServiceContainer.Register<IRandomService, DefaultRandomService>().AsTransient();
            builder.ServiceContainer.Register(typeof(ILogger<>), typeof(UnityLogger<>));
            return builder;
        }
    }
}

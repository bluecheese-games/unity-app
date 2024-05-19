//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.App.Services;
using BlueCheese.Core.Services;
using UnityEngine;

namespace BlueCheese.App
{
    public class UnityApp
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void ReloadDomain()
        {
            ServiceContainer.Default.Reset();
        }

        public ServiceContainer ServiceContainer { get; private set; }

        private UnityApp() { }

        public void Start()
        {
            ServiceContainer.Startup();
            Application.quitting += Stop;
        }

        public void Stop()
        {
            Application.quitting -= Stop;
            ServiceContainer.Shutdown();
        }

        public class Builder
        {
            private readonly UnityApp _app = new();
            public ServiceContainer ServiceContainer => _app.ServiceContainer;

            public Builder()
            {
                _app.ServiceContainer = ServiceContainer.Default;
            }

            public Builder(ServiceContainer serviceContainer)
            {
                _app.ServiceContainer = serviceContainer;
            }

            public Builder RegisterDefaultServices()
            {
                _app.ServiceContainer.Register<IConfigService, ConfigService>();
                _app.ServiceContainer.Register<IAudioService, DefaultAudioService>();
                _app.ServiceContainer.Register<ILocalStorageService, PlayerPrefsService>();
                _app.ServiceContainer.Register<ISceneService, UnitySceneService>();
                _app.ServiceContainer.Register<IUIService, UIService>();
                _app.ServiceContainer.Register<IInputService, DefaultInputService>();
                _app.ServiceContainer.Register<IAssetService, AssetService>();
                _app.ServiceContainer.Register<ISerializationService, JsonUtilityService>();
                _app.ServiceContainer.Register<IClockService, UnityClockService>();
                _app.ServiceContainer.Register<IAPIService, APIService>();
                _app.ServiceContainer.Register<IGameObjectService, GameObjectService>();
                _app.ServiceContainer.Register<IPoolService, DefaultPoolService>();
                _app.ServiceContainer.Register<IErrorHandlingService, DefaultErrorHandlingService>();
                _app.ServiceContainer.Register<IRandomService, DefaultRandomService>().AsTransient();
                _app.ServiceContainer.Register(typeof(ILogger<>), typeof(UnityLogger<>));
                return this;
            }

            public UnityApp Build() => _app;
        }
    }
}
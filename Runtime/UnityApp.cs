//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
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

        public void Run()
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

            public UnityApp Build() => _app;
        }
    }
}
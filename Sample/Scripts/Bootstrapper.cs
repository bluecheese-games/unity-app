//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.App.Services;
using BlueCheese.Core.ServiceLocator;
using UnityEngine;

namespace BlueCheese.App.Sample
{
    public static class Bootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Execute()
        {
            var builder = new UnityApp.Builder()
                .UseEnvironment(Environment.Development)
                .RegisterDefaultServices();
            RegisterAppServices(builder.ServiceContainer);
            var app = builder.Build();
            app.Run();
        }

        private static void RegisterAppServices(ServiceContainer container)
        {
            // Register your app services here
        }
    }
}
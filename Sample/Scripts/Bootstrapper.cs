//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using UnityEngine;

namespace BlueCheese.App.Sample
{
    public class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private AudioBank _audioBank;

        private void Awake()
        {
            // Make sure this is called before all other scripts
            // => Use the Unity Script Execution Order
            // => Or put it in a static method with the RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad) attribute
            var builder = new UnityApp.Builder()
                .UseEnvironment(Environment.Development)
                .RegisterDefaultServices();
            RegisterAppServices(builder.ServiceContainer);
            var app = builder.Build();
            app.Run();
        }

        private void RegisterAppServices(ServiceContainer container)
        {
			// Register your app services here
			container.Register<IAudioService, AudioService>()
                .WithOptions(() => new AudioService.Options
				{
                    AudioBanks = new[] { _audioBank }
				});
		}
    }
}
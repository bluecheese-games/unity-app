//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;
using System;

namespace BlueCheese.App
{
	public static class EditorServiceLocator
	{
		private static ServiceContainer _editorServiceContainer;

		private static ServiceContainer EditorServiceContainer
		{
			get
			{
				try
				{
					_editorServiceContainer ??= InitializeEditorContainer();
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogError("Failed to initialize the editor service container : " + e);
				}
				return _editorServiceContainer;
			}
		}

		private static ServiceContainer InitializeEditorContainer()
		{
			var container = new ServiceContainer();

			// Register the services that are specific to the editor
			container.Register<IAssetLoaderService, AssetService>();
			container.Register<IAssetFinderService, AssetService>();
			container.Register<IGameObjectService, GameObjectService>();
			container.Register<IJsonService, JsonUtilityService>();
			container.Register<ILocalStorageService, EditorPrefsService>();
			container.Register(typeof(ILogger<>), typeof(UnityLogger<>));
			container.Register<IHttpService, HttpService>();
			container.Register<IHttpClient, UnityWebRequestHttpClient>();
			container.Register<ILocalizationService, LocalizationService>();
			container.Configure<LocalizationService.Settings>((options) =>
			{
				var editorOptions = LocalizationService.Settings.FromResourcesOrDefault();
				options.DefaultLanguage = editorOptions.DefaultLanguage;
				options.SupportedLanguages = editorOptions.SupportedLanguages;
			});
			container.Register<ITranslationService, EditorTranslationService>();
			container.Register<EditorTranslationService>();
			container.Register<IApp, EditorApp>();
			container.Register<EditorAudioService>();

			container.Initialize();
			return container;
		}

		/// <summary>
		/// Resolve and return a service that was registered in this container.
		/// </summary>
		/// <typeparam name="TService">
		/// The Type of the service to resolve.
		/// Use the exact Type used to register the service.
		/// </typeparam>
		/// <returns>A service instance</returns>
		public static TService Get<TService>()
		{
			return EditorServiceContainer.Resolve<TService>();
		}
	}
}

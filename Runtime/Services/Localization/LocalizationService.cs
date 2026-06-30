//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;
using BlueCheese.Core.Signals;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
	public class LocalizationService : ILocalizationService
	{
		private const string _currentLanguageKey = "CurrentLanguage";

		private readonly ILocalStorageService _localStorage;
		private readonly Settings _settings;

		public LocalizationService(ILocalStorageService localStorage, IOptions<Settings> settings)
		{
			_localStorage = localStorage;
			_settings = settings.Value ?? Settings.Default;
		}

		public Language DeviceLanguage { get; private set; }
		public Language DefaultLanguage { get; private set; }
		public Language CurrentLanguage { get; private set; }

		public IReadOnlyList<Language> SupportedLanguages => _settings.SupportedLanguages;

		public void Initialize()
		{
			DeviceLanguage = LangUtilities.GetLanguage(Application.systemLanguage);
			DefaultLanguage = _settings.DefaultLanguage;

			// Load current language from local storage
			var backupValue = IsSupported(DeviceLanguage) ? DeviceLanguage.ToString() : DefaultLanguage.ToString();
			CurrentLanguage = Enum.Parse<Language>(_localStorage.ReadValue(_currentLanguageKey, backupValue));
		}

		private bool IsSupported(Language language)
		{
			if (_settings.SupportedLanguages == null || _settings.SupportedLanguages.Count == 0)
			{
				// If no supported languages are specified, all languages are considered supported
				return true;
			}
			return _settings.SupportedLanguages.Contains(language);
		}

		public void SetCurrentLanguage(Language language)
		{
			if (language != CurrentLanguage)
			{
				CurrentLanguage = language;
				_localStorage.WriteValue(_currentLanguageKey, CurrentLanguage.ToString());
				SignalAPI.Publish(new ChangeLanguageSignal(CurrentLanguage));
			}
		}

		[Serializable]
		public class Settings
		{
			public Language DefaultLanguage = Language.English;
			public List<Language> SupportedLanguages;

			public static Settings Default => new();

			public static Settings FromResourcesOrDefault(string path = "LocalizationSettings")
			{
				var settings = Resources.Load<LocalizationSettingsAsset>(path);
				return settings != null ? settings.Options : Default;
			}
		}
	}

	public readonly struct ChangeLanguageSignal
	{
		public ChangeLanguageSignal(Language language) => Language = language;

		public readonly Language Language { get; }
	}
}

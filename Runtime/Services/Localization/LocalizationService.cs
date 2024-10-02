using BlueCheese.Core.ServiceLocator;
using Core.Signals;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
	public class LocalizationService : ILocalizationService
	{
		private const string _currentLanguageKey = "CurrentLanguage";

		private readonly ILocalStorageService _localStorage;
		private readonly Options _options;

		public LocalizationService(ILocalStorageService localStorage, Options options)
		{
			_localStorage = localStorage;
			_options = options;
		}

		public SystemLanguage DeviceLanguage { get; private set; }
		public SystemLanguage DefaultLanguage { get; private set; }
		public SystemLanguage CurrentLanguage { get; private set; }

		public void Initialize()
		{
			DeviceLanguage = Application.systemLanguage;
			DefaultLanguage = _options.DefaultLanguage;

			// Load current language from local storage
			var backupValue = IsSupported(DeviceLanguage) ? DeviceLanguage.ToString() : DefaultLanguage.ToString();
			CurrentLanguage = Enum.Parse<SystemLanguage>(_localStorage.ReadValue(_currentLanguageKey, backupValue));
		}

		private bool IsSupported(SystemLanguage language)
		{
			if (_options.SupportedLanguages == null || _options.SupportedLanguages.Count == 0)
			{
				// If no supported languages are specified, all languages are considered supported
				return true;
			}
			return _options.SupportedLanguages.Contains(language);
		}

		public void SetCurrentLanguage(SystemLanguage language)
		{
			if (language != CurrentLanguage)
			{
				CurrentLanguage = language;
				_localStorage.WriteValue(_currentLanguageKey, CurrentLanguage.ToString());
				SignalAPI.Publish(new ChangeLanguageSignal(CurrentLanguage));
			}
		}

		[Serializable]
		public class Options : IOptions
		{
			public SystemLanguage DefaultLanguage = SystemLanguage.English;
			public List<SystemLanguage> SupportedLanguages;

			public static Options Default = new();

			public static Options FromResourcesOrDefault(string path = "LocalizationSettings")
			{
				var settings = Resources.Load<LocalizationSettingsAsset>(path);
				return settings != null ? settings.Options : Default;
			}
		}
	}

	public readonly struct ChangeLanguageSignal
	{
		public ChangeLanguageSignal(SystemLanguage language) => Language = language;

		public readonly SystemLanguage Language { get; }
	}
}

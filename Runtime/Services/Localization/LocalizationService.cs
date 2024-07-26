using BlueCheese.Core.ServiceLocator;
using Core.Signals;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BlueCheese.App
{
	public class LocalizationService : ILocalizationService
	{
		private const string _currentLocaleKey = "CurrentLocale";

		private readonly ILocalStorageService _localStorage;
		private readonly Options _options;

		public LocalizationService(ILocalStorageService localStorage, Options options)
		{
			_localStorage = localStorage;
			_options = options;
		}

		public Locale DeviceLocale { get; private set; }
		public Locale DefaultLocale { get; private set; }
		public Locale CurrentLocale { get; private set; }

		public void Initialize()
		{
			DeviceLocale = new Locale(Application.systemLanguage, RegionInfo.CurrentRegion.TwoLetterISORegionName);
			DefaultLocale = _options.DefaultLocale;
			CurrentLocale = _localStorage.ReadValue<string>(_currentLocaleKey, IsSupported(DeviceLocale) ? DeviceLocale : DefaultLocale);
		}

		private bool IsSupported(Locale locale)
		{
			if (_options.SupportedLocales == null || _options.SupportedLocales.Count == 0)
			{
				// If no supported locales are specified, all locales are considered supported
				return true;
			}
			return _options.SupportedLocales.Contains(locale);
		}

		public void SetCurrentLocale(Locale locale)
		{
			if (locale != CurrentLocale)
			{
				CurrentLocale = locale;
				_localStorage.WriteValue(_currentLocaleKey, CurrentLocale.ToString());
				SignalAPI.Publish(new ChangeLocaleSignal(CurrentLocale));
			}
		}

		public class Options : IOptions
		{
			public Locale DefaultLocale = "en-US";
			public List<Locale> SupportedLocales;
		}
	}

	public readonly struct ChangeLocaleSignal
	{
		public ChangeLocaleSignal(Locale locale) => Locale = locale;

		public readonly Locale Locale { get; }
	}
}

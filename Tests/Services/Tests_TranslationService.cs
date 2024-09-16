//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.App;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BlueCheese.Tests.Services
{
	public class Tests_LocalizationService
	{
		private ILocalStorageService _localStorage;
		private LocalizationService.Options _options;
		private LocalizationService _localizationService;

		[SetUp]
		public void SetUp()
		{
			_localStorage = new FakeLocalStorageService();
			_options = new LocalizationService.Options
			{
				DefaultLocale = new Locale(SystemLanguage.English, "US"),
				SupportedLocales = new List<Locale>
				{
					new(SystemLanguage.English, "US"),
					new(SystemLanguage.French, "FR")
				}
			};
			_localizationService = new LocalizationService(_localStorage, _options);
			_localizationService.Initialize();
		}

		[Test]
		public void SetCurrentLocale_UpdatesCurrentLocale()
		{
			// Arrange
			var expectedLocale = new Locale(SystemLanguage.Spanish, "ES");

			// Act
			_localizationService.SetCurrentLocale(expectedLocale);

			// Assert
			Assert.AreEqual(expectedLocale, _localizationService.CurrentLocale);
			Assert.AreEqual(expectedLocale.ToString(), _localStorage.ReadValue<string>("CurrentLocale"));
		}

		[Test]
		public void Initialize_SetsDeviceLocale()
		{
			// Arrange
			var expectedDeviceLocale = new Locale(Application.systemLanguage, RegionInfo.CurrentRegion.TwoLetterISORegionName);

			// Act
			_localizationService.Initialize();

			// Assert
			Assert.AreEqual(expectedDeviceLocale, _localizationService.DeviceLocale);
		}

		[Test]
		public void Initialize_SetsDefaultLocale()
		{
			// Arrange
			var expectedDefaultLocale = _options.DefaultLocale;

			// Act
			_localizationService.Initialize();

			// Assert
			Assert.AreEqual(expectedDefaultLocale, _localizationService.DefaultLocale);
		}

		[Test]
		public void Initialize_SetsCurrentLocaleToDeviceLocale_WhenDeviceLocaleIsSupported()
		{
			// Arrange
			var expectedCurrentLocale = _localizationService.DeviceLocale;

			// Act
			_localizationService.Initialize();

			// Assert
			Assert.AreEqual(expectedCurrentLocale, _localizationService.CurrentLocale);
		}

		[Test]
		public void Initialize_SetsCurrentLocaleToDefaultLocale_WhenDeviceLocaleIsNotSupported()
		{
			// Arrange
			_options.SupportedLocales = new List<Locale>
			{
				new(SystemLanguage.Spanish, "ES")
			};
			_localizationService = new LocalizationService(_localStorage, _options);
			var expectedCurrentLocale = _options.DefaultLocale;

			// Act
			_localizationService.Initialize();

			// Assert
			Assert.AreEqual(expectedCurrentLocale, _localizationService.CurrentLocale);
		}
	}

	[TestFixture]
	public class Tests_TranslationTable
	{
		[Test]
		public void TryGet_ReturnsTrueAndSingularTranslation_WhenKeyExists()
		{
			// Arrange
			var translationTable = new TranslationTable();
			var translations = new Dictionary<string, string>
			{
				{ "hello", "Bonjour" }
			};
			translationTable.Add(translations);
			var key = new TranslationKey("hello");

			// Act
			var result = translationTable.TryGet(key, out var singular, out var plural);

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual("Bonjour", singular);
			Assert.AreEqual("Bonjour", plural);
		}

		[Test]
		public void TryGet_ReturnsTrueAndPluralTranslation_WhenPluralKeyExists()
		{
			// Arrange
			var translationTable = new TranslationTable();
			var translations = new Dictionary<string, string>
			{
				{ "apples", "pomme" },
				{ "apples_plural", "pommes" }
			};
			translationTable.Add(translations);
			var key = new TranslationKey("apples", new string[] { "2" }, "apples_plural");

			// Act
			var result = translationTable.TryGet(key, out var singular, out var plural);

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual("pomme", singular);
			Assert.AreEqual("pommes", plural);
		}

		[Test]
		public void TryGet_ReturnsTrueAndPluralTranslation_WhenPluralKeyExists_UsingStringInt()
		{
			// Arrange
			var translationTable = new TranslationTable();
			var translations = new Dictionary<string, string>
			{
				{ "apples", "pomme" },
				{ "apples_plural", "pommes" }
			};
			translationTable.Add(translations);
			var key = new TranslationKey("apples", new string[] { "2" }, "apples_plural");

			// Act
			var result = translationTable.TryGet(key, out var singular, out var plural);

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual("pomme", singular);
			Assert.AreEqual("pommes", plural);
		}

		[Test]
		public void TryGet_ReturnsFalseAndNullTranslations_WhenKeyDoesNotExist()
		{
			// Arrange
			var translationTable = new TranslationTable();
			var key = new TranslationKey("goodbye");

			// Act
			var result = translationTable.TryGet(key, out var singular, out var plural);

			// Assert
			Assert.IsFalse(result);
			Assert.IsNull(singular);
			Assert.IsNull(plural);
		}
	}

	[TestFixture]
	public class TranslationServiceTests
	{
		private ILocalizationService _localization;
		private IAssetLoaderService _assetLoader;
		private TranslationService _translationService;

		[SetUp]
		public void SetUp()
		{
			_localization = new MockLocalizationService();
			_assetLoader = new FakeAssetLoaderService();
			_translationService = new TranslationService(_localization, _assetLoader);
		}

		[Test]
		public void Translate_ReturnsSingularTranslation_WhenPluralValueIs1()
		{
			// Arrange
			var locale = new Locale(SystemLanguage.English, "US");
			var translations = new Dictionary<string, string>
			{
				{ "apples", "{0} apple" },
				{ "apples_plural", "{0} apples" }
			};
			_localization.SetCurrentLocale(locale);
			_translationService.AddTranslations(locale, translations);
			var key = new TranslationKey("apples", new string[] { "1" }, "apples_plural");

			// Act
			var translation = _translationService.Translate(key);

			// Assert
			Assert.AreEqual("1 apple", translation);
		}

		[Test]
		public void Translate_ReturnsPluralTranslation_WhenPluralValueIsGreaterThan1()
		{
			// Arrange
			var locale = new Locale(SystemLanguage.English, "US");
			var translations = new Dictionary<string, string>
			{
				{ "apples", "{0} apple" },
				{ "apples_plural", "{0} apples" }
			};
			_localization.SetCurrentLocale(locale);
			_translationService.AddTranslations(locale, translations);
			var key = new TranslationKey("apples", new string[] { "2" }, "apples_plural");

			// Act
			var translation = _translationService.Translate(key);

			// Assert
			Assert.AreEqual("2 apples", translation);
		}

		[Test]
		public void Translate_ReturnsTranslation_WithStringKey()
		{
			// Arrange
			var locale = new Locale(SystemLanguage.English, "US");
			var translations = new Dictionary<string, string>
			{
				{ "apple", "I love apples" }
			};
			_localization.SetCurrentLocale(locale);
			_translationService.AddTranslations(locale, translations);

			// Act
			var translation = _translationService.Translate("apple");

			// Assert
			Assert.AreEqual("I love apples", translation);
		}

		private class MockLocalizationService : ILocalizationService
		{
			private Locale _currentLocale;

			public Locale CurrentLocale => _currentLocale;

			public Locale DeviceLocale => throw new System.NotImplementedException();

			public Locale DefaultLocale => throw new System.NotImplementedException();

			public void Initialize() { }

			public void SetCurrentLocale(Locale locale)
			{
				_currentLocale = locale;
			}
		}
	}

	[TestFixture]
	public class Tests_Locale
	{
		[Test]
		public void Constructor_SetsLanguageAndCountryCode()
		{
			// Arrange
			var language = SystemLanguage.French;
			var countryCode = "FR";

			// Act
			var locale = new Locale(language, countryCode);

			// Assert
			Assert.AreEqual(language, locale.Language);
			Assert.AreEqual(countryCode, locale.CountryCode);
		}

		[Test]
		public void Constructor_WhenCountryCodeIsNull()
		{
			// Arrange
			var language = SystemLanguage.French;

			// Act
			var locale = new Locale(language);

			// Assert
			Assert.AreEqual(language, locale.Language);
			Assert.IsNull(locale.CountryCode);
		}

		[Test]
		public void Constructor_WhenCountryCodeIsEmpty()
		{
			// Arrange
			var language = SystemLanguage.French;

			// Act
			var locale = new Locale(language, "");

			// Assert
			Assert.AreEqual(language, locale.Language);
			Assert.IsNull(locale.CountryCode);
		}

		[Test]
		public void Constructor_WhenCountryCodeIsWhitespace()
		{
			// Arrange
			var language = SystemLanguage.French;

			// Act
			var locale = new Locale(language, " ");

			// Assert
			Assert.AreEqual(language, locale.Language);
			Assert.IsNull(locale.CountryCode);
		}

		[Test]
		public void LanguageCode_ReturnsCorrectCode()
		{
			// Arrange
			var language = SystemLanguage.French;
			var countryCode = "FR";
			var locale = new Locale(language, countryCode);
			var expectedLanguageCode = "fr";

			// Act
			var languageCode = locale.LanguageCode;

			// Assert
			Assert.AreEqual(expectedLanguageCode, languageCode);
		}

		[Test]
		public void ToString_ReturnsCorrectFormat()
		{
			// Arrange
			var language = SystemLanguage.French;
			var countryCode = "FR";
			var locale = new Locale(language, countryCode);
			var expectedString = "fr-FR";

			// Act
			var stringRepresentation = locale.ToString();

			// Assert
			Assert.AreEqual(expectedString, stringRepresentation);
		}

		[Test]
		public void ImplicitConversion_StringToLocale()
		{
			// Arrange
			var localeString = "fr-FR";
			var expectedLanguage = SystemLanguage.French;
			var expectedCountryCode = "FR";

			// Act
			Locale locale = localeString;

			// Assert
			Assert.AreEqual(expectedLanguage, locale.Language);
			Assert.AreEqual(expectedCountryCode, locale.CountryCode);
		}

		[Test]
		public void ImplicitConversion_StringToLocale_WithoutCountryCode()
		{
			// Arrange
			var localeString = "fr";
			var expectedLanguage = SystemLanguage.French;

			// Act
			Locale locale = localeString;

			// Assert
			Assert.AreEqual(expectedLanguage, locale.Language);
			Assert.IsNull(locale.CountryCode);
		}

		[Test]
		public void ImplicitConversion_LocaleToString()
		{
			// Arrange
			var language = SystemLanguage.French;
			var countryCode = "FR";
			var locale = new Locale(language, countryCode);
			var expectedString = "fr-FR";

			// Act
			string stringRepresentation = locale;

			// Assert
			Assert.AreEqual(expectedString, stringRepresentation);
		}

		[Test]
		public void Locale_Equality()
		{
			// Arrange
			var locale1 = new Locale(SystemLanguage.French, "FR");
			var locale2 = new Locale(SystemLanguage.French, "FR");
			var locale3 = new Locale(SystemLanguage.English, "US");
			var locale4 = new Locale(SystemLanguage.French, "CA");
			var locale5 = new Locale(SystemLanguage.French);

			var localFromString1 = (Locale)"fr-FR";
			var localFromString2 = (Locale)"fr";

			var localFromLanguage = (Locale)SystemLanguage.French;

			// Act / Assert
			Assert.AreEqual(locale1, locale2);
			Assert.AreNotEqual(locale1, locale3);
			Assert.AreNotEqual(locale1, locale4);
			Assert.AreNotEqual(locale1, locale5);
			Assert.AreEqual(locale1, localFromString1);
			Assert.AreEqual(locale5, localFromString2);
			Assert.AreNotEqual(locale1, localFromLanguage);
			Assert.AreEqual(locale5 , localFromLanguage);
			Assert.AreEqual(localFromLanguage, localFromString2);

			Assert.IsTrue(locale1 == locale2);
			Assert.IsFalse(locale1 != locale2);
			Assert.IsTrue(locale1 == "fr-FR");
			Assert.IsFalse(locale1 != "fr-FR");
			Assert.IsTrue(locale5 == "fr");
			Assert.IsFalse(locale5 != "fr");
			Assert.IsTrue(locale5 == SystemLanguage.French);
			Assert.IsFalse(locale5 != SystemLanguage.French);
			Assert.IsTrue(localFromLanguage == "fr");
			Assert.IsFalse(localFromLanguage != "fr");
			Assert.IsTrue(localFromLanguage == locale5);
			Assert.IsFalse(localFromLanguage != locale5);
		}
	}
}
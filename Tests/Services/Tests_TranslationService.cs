//
// Copyright (c) 2025 BlueCheese Games All rights reserved
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
				DefaultLanguage = Language.English,
				SupportedLanguages = new List<Language>
				{
					Language.English,
					Language.French
				}
			};
			_localizationService = new LocalizationService(_localStorage, _options);
			_localizationService.Initialize();
		}

		[Test]
		public void SetCurrentLocale_UpdatesCurrentLocale()
		{
			// Arrange
			var expectedLanguage = Language.Spanish;

			// Act
			_localizationService.SetCurrentLanguage(expectedLanguage);

			// Assert
			Assert.AreEqual(expectedLanguage, _localizationService.CurrentLanguage);
			Assert.AreEqual(expectedLanguage.ToString(), _localStorage.ReadValue<string>("CurrentLanguage"));
		}

		[Test]
		public void Initialize_SetsDeviceLocale()
		{
			// Arrange
			var expectedDeviceLanguage = LangUtilities.GetLanguage(Application.systemLanguage);

			// Act
			_localizationService.Initialize();

			// Assert
			Assert.AreEqual(expectedDeviceLanguage, _localizationService.DeviceLanguage);
		}

		[Test]
		public void Initialize_SetsDefaultLocale()
		{
			// Arrange
			var expectedDefaultLanguage = _options.DefaultLanguage;

			// Act
			_localizationService.Initialize();

			// Assert
			Assert.AreEqual(expectedDefaultLanguage, _localizationService.DefaultLanguage);
		}

		[Test]
		public void Initialize_SetsCurrentLocaleToDeviceLocale_WhenDeviceLocaleIsSupported()
		{
			// Arrange
			var expectedCurrentLanguage = _localizationService.DeviceLanguage;

			// Act
			_localizationService.Initialize();

			// Assert
			Assert.AreEqual(expectedCurrentLanguage, _localizationService.CurrentLanguage);
		}

		[Test]
		public void Initialize_SetsCurrentLocaleToDefaultLocale_WhenDeviceLocaleIsNotSupported()
		{
			// Arrange
			_options.SupportedLanguages = new List<Language>
			{
				Language.Spanish
			};
			_localizationService = new LocalizationService(_localStorage, _options);
			var expectedCurrentLanguage = _options.DefaultLanguage;

			// Act
			_localizationService.Initialize();

			// Assert
			Assert.AreEqual(expectedCurrentLanguage, _localizationService.CurrentLanguage);
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
			var language = Language.English;
			var translations = new Dictionary<string, string>
			{
				{ "apples", "{0} apple" },
				{ "apples_plural", "{0} apples" }
			};
			_localization.SetCurrentLanguage(language);
			_translationService.AddTranslations(language, translations);
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
			var language = Language.English;
			var translations = new Dictionary<string, string>
			{
				{ "apples", "{0} apple" },
				{ "apples_plural", "{0} apples" }
			};
			_localization.SetCurrentLanguage(language);
			_translationService.AddTranslations(language, translations);
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
			var language = Language.English;
			var translations = new Dictionary<string, string>
			{
				{ "apple", "I love apples" }
			};
			_localization.SetCurrentLanguage(language);
			_translationService.AddTranslations(language, translations);

			// Act
			var translation = _translationService.Translate("apple");

			// Assert
			Assert.AreEqual("I love apples", translation);
		}

		private class MockLocalizationService : ILocalizationService
		{
			private Language _currentLanguage;

			public Language CurrentLanguage => _currentLanguage;

			public Language DeviceLanguage => throw new System.NotImplementedException();

			public Language DefaultLanguage => throw new System.NotImplementedException();

			public IReadOnlyList<Language> SupportedLanguages => throw new System.NotImplementedException();

			public void Initialize() { }

			public void SetCurrentLanguage(Language language)
			{
				_currentLanguage = language;
			}
		}
	}
}
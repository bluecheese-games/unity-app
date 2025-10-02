//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.App
{
	public class TranslationService : ITranslationService
	{
		public const string TranslationsResourcePath = "Translations";

		protected readonly ILocalizationService _localization;
		protected readonly IAssetLoaderService _assetLoader;
		protected readonly Dictionary<Language, TranslationTable> _translations = new();

		public TranslationService(ILocalizationService localization, IAssetLoaderService assetLoader)
		{
			_localization = localization;
			_assetLoader = assetLoader;
		}

		public void Initialize()
		{
			var collection = AssetBank.GetAssetByType<TranslationTableCollection>();
			if (collection != null)
			{
				foreach (var tableAsset in collection.Items)
				{
					AddTranslations(tableAsset);
				}
			}

			Translator.Initialize(this);
		}

		private void AddTranslations(ITranslationTableAsset tableAsset)
		{
			if (tableAsset == null) return;
			foreach (var language in tableAsset.Languages)
			{
				AddTranslations(language, tableAsset.GetTranslations(language));
			}
		}

		public void AddTranslations(Language language, IDictionary<string, string> translations)
		{
			if (!_translations.TryGetValue(language, out var table))
			{
				table = new TranslationTable();
				_translations[language] = table;
			}
			table.Add(translations);
		}

		public string Translate(TranslationKey key)
		{
			if (TryTranslate(_localization.CurrentLanguage, key, out var translation))
			{
				// Found translation for current language
				return translation;
			}
			else if (TryTranslate(_localization.DefaultLanguage, key, out translation))
			{
				// Found translation for default language
				return translation;
			}

			// No translation found, return key
			return key.Key;
		}

		public bool HasTranslation(TranslationKey key)
		{
			if (!_translations.TryGetValue(_localization.CurrentLanguage, out var table))
			{
				return false;
			}
			return table.HasTranslation(key);
		}

		private bool TryTranslate(Language language, TranslationKey key, out string translation)
		{
			if (_translations.TryGetValue(language, out var table))
			{
				if (table.TryGet(key, out var singular, out var plural))
				{
					translation = key.Format(singular, plural);
					return true;
				}
			}
			translation = null;
			return false;
		}
	}
}

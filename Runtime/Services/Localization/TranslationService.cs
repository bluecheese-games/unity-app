using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
	public class TranslationService : ITranslationService
	{
		protected readonly ILocalizationService _localization;
		protected readonly IAssetLoaderService _assetLoader;
		protected readonly Dictionary<SystemLanguage, TranslationTable> _translations = new();

		public TranslationService(ILocalizationService localization, IAssetLoaderService assetLoader)
		{
			_localization = localization;
			_assetLoader = assetLoader;
		}

		public void Initialize()
		{
			var translationAssets = _assetLoader.LoadAssetsFromResources<TranslationTableAsset>("Translations");
			foreach (var translationAsset in translationAssets)
			{
				translationAsset.Load(this);
			}

			Translator.Initialize(this);
		}

		public void AddTranslations(SystemLanguage language, Dictionary<string, string> translations)
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

		private bool TryTranslate(SystemLanguage language, TranslationKey key, out string translation)
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

	public class EditorTranslationService : TranslationService
	{
		public EditorTranslationService(ILocalizationService localization, IAssetLoaderService assetLoader) : base(localization, assetLoader) { }

		public IReadOnlyList<string> GetAllKeys()
		{
			// Get all keys from all translation tables, combine, remove duplicates, sort by key and return
			var keys = new HashSet<string>();
			foreach (var table in _translations.Values)
			{
				foreach (var key in table.GetKeys())
				{
					keys.Add(key);
				}
			}
			var sortedKeys = new List<string>(keys);
			sortedKeys.Sort();
			return sortedKeys;
		}
	}
}

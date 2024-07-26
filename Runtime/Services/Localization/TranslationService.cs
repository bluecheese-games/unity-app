using System.Collections.Generic;

namespace BlueCheese.App
{
	public class TranslationService : ITranslationService
	{
		private readonly ILocalizationService _localization;
		private readonly IAssetLoaderService _assetLoader;
		private readonly Dictionary<Locale, TranslationTable> _translations = new();

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
		}

		public void AddTranslations(Locale locale, Dictionary<string, string> translations)
		{
			if (!_translations.TryGetValue(locale, out var table))
			{
				table = new TranslationTable();
				_translations[locale] = table;
			}
			table.Add(translations);
		}

		public string Translate(TranslationKey key)
		{
			if (TryTranslate(_localization.CurrentLocale, key, out var translation))
			{
				// Found translation for current locale
				return translation;
			}
			else if (TryTranslate(_localization.CurrentLocale.Language, key, out translation))
			{
				// Found translation for current language
				return translation;
			}
			else if (TryTranslate(_localization.DefaultLocale, key, out translation))
			{
				// Found translation for default locale
				return translation;
			}
			else if (TryTranslate(_localization.DefaultLocale.Language, key, out translation))
			{
				// Found translation for default language
				return translation;
			}

			// No translation found, return key
			return key.Key;
		}

		private bool TryTranslate(Locale locale, TranslationKey key, out string translation)
		{
			if (_translations.TryGetValue(locale, out var table))
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

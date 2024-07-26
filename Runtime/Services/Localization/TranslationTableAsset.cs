using BlueCheese.App.Editor;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueCheese.App
{
	[CreateAssetMenu(fileName = "TranslationTable", menuName = "Localization/Translation Table")]
	public class TranslationTableAsset : ScriptableObject
	{
		[SerializeField] private LanguageData[] _languages;
		[SerializeField] private string[] _keys;
		[SerializeField] private Translations[] _translations;

		public void Load(ITranslationService translationService)
		{
			for (int langIndex = 0; langIndex < _languages.Length; langIndex++)
			{
				if (langIndex >= _translations.Length) continue;

				LanguageData lang = _languages[langIndex];
				var translations = new Dictionary<string, string>();
				for (int keyIndex = 0; keyIndex < _keys.Length; keyIndex++)
				{
					if (keyIndex >= _translations.Length) continue;

					var key = _keys[keyIndex];
					var value = _translations[langIndex][keyIndex];
					translations.Add(key, value);
				}
				translationService.AddTranslations(lang.ToLocale(), translations);
			}
		}

		private void OnValidate()
		{
			if (_languages != null && _languages.Length > 0)
			{
				for (int i = 0; i < _languages.Length; i++)
				{
					LanguageData locale = _languages[i];
					string name = $"{locale.Language} ({(string.IsNullOrEmpty(locale.CountryCode) ? "All" : locale.CountryCode)})";
					if (i == 0) name += " (Default)";
					locale.Name = name;
				}
			}
			if (_translations != null && _translations.Length > 0)
			{
				for (int i = 0; i < _translations.Length; i++)
				{
					if (i >= _languages.Length) continue;

					Translations translations = _translations[i];
					translations.Lang = _languages[i].ToLocale();
				}
			}
		}

		private void Refresh()
		{
			ITranslationService translationService = EditorServices.Get<ITranslationService>();
			Load(translationService);
		}

		[Serializable]
		public class LanguageData
		{
			[HideInInspector]
			public string Name;

			public SystemLanguage Language = SystemLanguage.English;
			public string CountryCode;

			public Locale ToLocale() => new(Language, CountryCode);
		}

		[Serializable]
		public class Translations
		{
			[HideInInspector]
			public string Lang;

			public string[] Entries;

			public string this[int index] => index < Entries.Length ? Entries[index] : null;
		}
	}
}

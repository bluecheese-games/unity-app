//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.App.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.App
{
	[CreateAssetMenu(fileName = "TranslationTable", menuName = "Localization/Translation Table")]
	public class TranslationTableAsset : ScriptableObject
	{
		[SerializeField] private List<string> _keys;
		[SerializeField] private List<Translations> _translations;

		public void Load(ITranslationService translationService)
		{
			for (int langIndex = 0; langIndex < Languages.Count; langIndex++)
			{
				if (langIndex >= _translations.Count) continue;

				Language lang = Languages[langIndex];
				var translations = new Dictionary<string, string>();
				for (int keyIndex = 0; keyIndex < _keys.Count; keyIndex++)
				{
					if (keyIndex >= _translations.Count) continue;

					var key = _keys[keyIndex];
					var value = _translations[langIndex][keyIndex];
					translations.Add(key, value);
				}
				translationService.AddTranslations(lang, translations);
			}
		}

		[ContextMenu("Refresh")]
		public void Refresh()
		{
			ITranslationService translationService = EditorServices.Get<ITranslationService>();
			Load(translationService);
		}

		public List<Language> Languages => _translations.Select(t => t.Language).ToList();

		public List<string> Keys => _keys;

		private Translations GetTranslations(Language language)
		{
			return _translations.FirstOrDefault(t => t.Language == language);
		} 

		public string GetTranslation(Language language, string key)
		{
			int keyIndex = _keys.IndexOf(key);
			if (keyIndex == -1) return null;
			return GetTranslations(language)[keyIndex];
		}

		public void SetTranslation(Language language, string key, string value)
		{
			int keyIndex = _keys.IndexOf(key);
			if (keyIndex == -1) return;
			GetTranslations(language)[keyIndex] = value;
		}

		public void AddLanguage(Language language)
		{
			_translations.Add(new Translations(language, _keys.Count));
		}

		public void RemoveLanguage(Language language)
		{
			int langIndex = Languages.IndexOf(language);
			if (langIndex == -1) return;
			Languages.RemoveAt(langIndex);
			_translations.RemoveAt(langIndex);
		}

		public void AddKey(string keyToAdd)
		{
			_keys.Add(keyToAdd);
			foreach (var translation in _translations)
			{
				translation.Add("");
			}
		}

		public void SetKey(int keyIndex, string newKey)
		{
			_keys[keyIndex] = newKey;
		}

		public bool ContainsKey(string searchText)
		{
			foreach (var key in _keys)
			{
				if (key.Contains(searchText)) return true;
			}
			return false;
		}

		public bool ContainsTranslation(string searchText)
		{
			foreach (var translation in _translations)
			{
				foreach (var item in translation.Items)
				{
					if (item.Contains(searchText)) return true;
				}
			}
			return false;
		}

		[Serializable]
		public class Translations
		{
			public Language Language;
			public List<string> Items;

			public Translations(Language language, int capacity)
			{
				Language = language;
				Items = new List<string>(capacity);
				for (int i = 0; i < capacity; i++)
				{
					Items.Add("");
				}
			}

			public string this[int index]
			{
				get => Items[index];
				set => Items[index] = value;
			}

			public void Add(string value)
			{
				Items.Add(value);
			}
		}
	}
}

//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.App
{

	[CreateAssetMenu(fileName = "TranslationTable", menuName = "Localization/Translation Table")]
	public class TranslationTableAsset : ScriptableObject, ITranslationTableAsset
	{
		[SerializeField] private List<Language> _languages;
		[SerializeField] private List<TranslationItem> _items;
		[SerializeField] private long _lastModified;

#if UNITY_EDITOR
		public void Validate()
		{
			_languages ??= new List<Language>();
			_items ??= new List<TranslationItem>();

			if (_languages.Count == 0)
			{
				var defaultLanguage = EditorServices.Get<ILocalizationService>().DefaultLanguage;
				AddLanguage(defaultLanguage);
				UnityEditor.EditorUtility.SetDirty(this);
				UnityEditor.AssetDatabase.SaveAssets();
			}
		}
#endif

		public string Name => name;

		public IDictionary<string, string> GetTranslations(Language language)
		{
			var translations = new Dictionary<string, string>();
			foreach (var item in _items)
			{
				if (item.TryGetTranslation(language, out var translation))
				{
					translations[item.Key] = translation.Value;
				}
			}
			return translations;
		}

		public List<Language> Languages => _languages;

		public List<string> Keys => _items?.Select(i => i.Key).ToList();

		public List<TranslationItem> Items => _items;

		public DateTime LastModified
		{
			get => new(_lastModified);
			set => _lastModified = value.Ticks;
		}

		public int Count(TranslationStatus status) => _items.Count(i => i.Status == status);

		public string GetTranslation(string key, Language language)
		{
			var item = GetItem(key);
			if (item != null && item.TryGetTranslation(language, out var translation))
			{
				return translation.Value;
			}
			return string.Empty;
		}

		private TranslationItem GetItem(string key) => _items.FirstOrDefault(i => i.Key == key);

		public bool IsLanguageSupported(Language language) => Languages.Contains(language);

		public void SetTranslation(Language language, string key, string value)
		{
			if (!IsLanguageSupported(language))
			{
				throw new ArgumentException($"Language {language} is not supported.");
			}

			var item = GetItem(key);
			item ??= AddItem(key);
			item.SetTranslation(language, value);
			LastModified = DateTime.UtcNow;
		}

		public void EditKey(string existingKey, string newKey)
		{
			if (existingKey == newKey) return; // No change
			var item = GetItem(existingKey);
			if (item == null)
			{
				throw new ArgumentException($"Key {existingKey} does not exist.");
			}
			if (GetItem(newKey) != null)
			{
				throw new ArgumentException($"Key {newKey} already exists.");
			}
			item.EditKey(newKey);
			LastModified = DateTime.UtcNow;
		}

		public void AddLanguage(Language language)
		{
			_languages ??= new List<Language>();
			if (_languages.Contains(language)) return;
			_languages.Add(language);
			foreach (var item in _items)
			{
				item.SetTranslation(language, "");
			}
			LastModified = DateTime.UtcNow;
		}

		public void RemoveLanguage(Language language)
		{
			_languages.Remove(language);
			foreach (var item in _items)
			{
				item.RemoveTranslation(language);
			}
			LastModified = DateTime.UtcNow;
		}

		public TranslationItem AddItem(string keyToAdd)
		{
			keyToAdd = NormalizeKey(keyToAdd);
			var item = GetItem(keyToAdd);
			if (item != null) return item; // Key already exists

			item = TranslationItem.Create(keyToAdd);
			foreach (var language in Languages)
			{
				item.SetTranslation(language, "");
			}
			_items.Add(item);
			LastModified = DateTime.UtcNow;
			return item;
		}

		public bool TryAddItem(TranslationItem item, bool addMissingLanguages = true)
		{
			if (GetItem(item.Key) != null) return false; // Key already exists

			if (addMissingLanguages)
			{
				// Add any missing languages to the table
				foreach (var translation in item.Translations)
				{
					if (!IsLanguageSupported(translation.Language))
					{
						AddLanguage(translation.Language);
					}
				}
			}

			_items.Add(item);
			LastModified = DateTime.UtcNow;
			return true;
		}

		public void RemoveKey(string key)
		{
			_items.RemoveAll(i => i.Key == key);
			LastModified = DateTime.UtcNow;
		}

		public bool ContainsKey(string searchText)
		{
			foreach (var item in _items)
			{
				if (item.Key.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)) return true;
			}
			return false;
		}

		public bool ContainsTranslation(string searchText)
		{
			foreach (var item in _items)
			{
				foreach (var translation in item.Translations)
				{
					if (translation.Value.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)) return true;
				}
			}
			return false;
		}

		private string NormalizeKey(string key) => key.Trim();

		[Serializable]
		public class TranslationItem
		{
			public string Key;
			public List<Translation> Translations;
			public long LastModified;
			public long LastValidated;
			public TranslationStatus Status;

			private TranslationItem()
			{
				Translations = new List<Translation>();
				Status = TranslationStatus.Validated;
			}

			private TranslationItem(string key)
			{
				Key = key;
				Translations = new List<Translation>();
				SetModified();
			}

			private void SetModified()
			{
				LastModified = DateTime.UtcNow.Ticks;
				Status = TranslationStatus.Modified;
			}

			public static TranslationItem Create(string key)
			{
				return new TranslationItem(key);
			}

			public void EditKey(string newKey)
			{
				if (Key != newKey)
				{
					Key = newKey;
					SetModified();
				}
			}

			public void SetTranslation(Language language, string value)
			{
				if (TryGetTranslation(language, out var translation))
				{
					if (translation.Value == value) return; // No change
					translation.Value = value;
				}
				else
				{
					Translations.Add(Translation.Create(language, value));
				}
				SetModified();
			}

			public bool TryGetTranslation(Language language, out Translation translation)
			{
				translation = Translations?.FirstOrDefault(t => t.Language == language);
				return translation is not null && translation.IsValid;
			}

			public void RemoveTranslation(Language language)
			{
				Translations.RemoveAll(t => t.Language == language);
				SetModified();
			}

			public void ValidateTranslations()
			{
				Status = TranslationStatus.Validated;
				LastValidated = DateTime.UtcNow.Ticks;
			}

			public TranslationItem Clone() => new(Key)
			{
				LastModified = LastModified,
				LastValidated = LastValidated,
				Status = Status,
				Translations = Translations.Select(t => Translation.Create(t.Language, t.Value)).ToList()
			};

			[Serializable]
			public class Translation
			{
				public Language Language;
				public string Value;

				public bool IsValid => Language != Language.Unknown && Value is not null;

				private Translation(Language language, string value)
				{
					Language = language;
					Value = value;
				}

				public static Translation Create(Language language, string value)
				{
					return new Translation(language, value);
				}

				public static bool operator ==(Translation a, Translation b) => a.Language == b.Language && a.Value == b.Value;

				public static bool operator !=(Translation a, Translation b) => !(a == b);

				public override bool Equals(object obj) => obj is Translation other && this == other;

				public override int GetHashCode() => HashCode.Combine(Language, Value);

				public static implicit operator string(Translation t) => t.Value;
			}
		}
	}
}

//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueCheese.App
{
	// Decorator for TranslationService that adds editor-specific functionalities
	public class EditorTranslationService : TranslationService
	{
		public EditorTranslationService(ILocalizationService localization, IAssetLoaderService assetLoader) : base(localization, assetLoader) { }

		private List<TranslationTableAsset> _translationTableAssets = null;

		public string[] GetAllKeys()
		{
			if (_translations.Count == 0)
			{
				Refresh();
			}

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
			return sortedKeys.ToArray();
		}

		public List<TranslationTableAsset> TranslationTableAssets
		{
			get
			{
				if (_translationTableAssets == null)
				{
					Refresh();
				}
				return _translationTableAssets;
			}
		}

		private List<TranslationTableAsset> FindAssets()
		{
			var assets = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(TranslationTableAsset)}")
				.Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
				.Select(UnityEditor.AssetDatabase.LoadAssetAtPath<TranslationTableAsset>)
				.OrderBy(asset => asset.Name);
			return assets.ToList();
		}

		public void Refresh()
		{
			_translationTableAssets = FindAssets();
			_translations.Clear();
			foreach (var asset in _translationTableAssets)
			{
				asset.Validate();
				foreach (var language in asset.Languages)
				{
					AddTranslations(language, asset.GetTranslations(language));
				}
			}
		}

		public Language DefaultLanguage => _localization.DefaultLanguage;

		public Language CurrentLanguage => _localization.CurrentLanguage;
	}
}

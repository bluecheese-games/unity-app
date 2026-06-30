//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace BlueCheese.App
{
	/// <summary>
	/// Editor-only helpers that locate localization assets in the project.
	/// Centralizes the AssetDatabase queries shared by the localization tooling
	/// (services, windows, property drawers) so they stay consistent.
	/// </summary>
	public static class TranslationAssetFinder
	{
		public static List<TranslationTableAsset> FindAllTables()
		{
			return AssetDatabase.FindAssets($"t:{nameof(TranslationTableAsset)}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<TranslationTableAsset>)
				.Where(asset => asset != null)
				.OrderBy(asset => asset.Name)
				.ToList();
		}

		public static HashSet<string> GetAllKeys()
		{
			var keys = new HashSet<string>();
			foreach (var table in FindAllTables())
			{
				foreach (var key in table.Keys)
				{
					if (!string.IsNullOrEmpty(key))
					{
						keys.Add(key);
					}
				}
			}
			return keys;
		}

		public static Language FindDefaultLanguage(Language fallback = Language.English)
		{
			var guid = AssetDatabase.FindAssets($"t:{nameof(LocalizationSettingsAsset)}").FirstOrDefault();
			if (!string.IsNullOrEmpty(guid))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettingsAsset>(path);
				if (settings != null)
				{
					return settings.DefaultLanguage;
				}
			}
			return fallback;
		}
	}
}
#endif

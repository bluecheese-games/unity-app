//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.App.Editor;
using BlueCheese.Core;
using BlueCheese.Core.Utils.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App
{
	[CustomEditor(typeof(TranslationTableCollection))]
	public class TranslationTableCollectionEditor : CollectionEditor
	{
		private SerializedProperty _itemsProperty;

		private string _searchText;

		override protected void OnEnable()
		{
			base.OnEnable();
			_itemsProperty = serializedObject.FindProperty("_items");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			DrawTranslationTables();
			EditorGUILayout.Space();
			DrawDuplicates();
			serializedObject.ApplyModifiedProperties();
		}

		private void DrawTranslationTables()
		{
			EditorGUIHelper.DrawTitle("Translation Tables");

			var translationTables = new List<ITranslationTableAsset>();
			for (int i = 0; i < _itemsProperty.arraySize; i++)
			{
				var itemProp = _itemsProperty.GetArrayElementAtIndex(i);
				if (itemProp.objectReferenceValue is ITranslationTableAsset table)
				{
					translationTables.Add(table);
				}
			}

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent(EditorIcon.Search), GUILayout.Width(20));
			_searchText = EditorGUILayout.TextField("Search key or translation", _searchText);
			EditorGUILayout.EndHorizontal();
			bool foundAny = false;
			foreach (var table in translationTables)
			{
				if (!string.IsNullOrEmpty(_searchText) &&
					!table.ContainsKey(_searchText) &&
					!table.ContainsTranslation(_searchText))
				{
					continue;
				}

				foundAny = true;
				int keyCount = table.Keys.Count;
				TranslationTableAsset tableAsset = null;
				if (table is TranslationTableAsset asset) tableAsset = asset;

				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField($"{table.Name}", EditorStyles.boldLabel);
				if (GUILayout.Button("Open", GUILayout.Width(100)))
				{
					table.Open();
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField($"Key Count: {keyCount}");
				var lastModifiedStyle = new GUIStyle(EditorStyles.label)
				{
					alignment = TextAnchor.MiddleRight
				};
				if (tableAsset != null)
				{
					EditorGUILayout.LabelField($"Modified: {tableAsset.LastModified.ToLocalTime().TimeAgo()}", lastModifiedStyle, GUILayout.ExpandWidth(true), GUILayout.MinWidth(200));
				}
				EditorGUILayout.EndHorizontal();
				if (tableAsset != null && keyCount > 0)
				{
					int validatedCount = tableAsset.Count(TranslationStatus.Validated);
					float progress = validatedCount / keyCount;
					EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, $"Validated: {validatedCount}/{keyCount} ({progress:P0})");
				}

				EditorGUILayout.EndVertical();
			}
			if (!foundAny)
			{
				if (!string.IsNullOrEmpty(_searchText))
				{
					EditorGUILayout.LabelField("No match");
				}
				else
				{
					EditorGUILayout.LabelField("No translation tables found. Create one by right clicking in a resources folder");
				}
			}
		}

		private void DrawDuplicates()
		{
			var assetFinder = EditorServices.Get<IAssetFinderService>();
			var translationTables = assetFinder
				.FindAssetsInResources<ScriptableObject>()
				.OfType<ITranslationTableAsset>()
				.ToList();
			var duplicateKeys = translationTables
				.SelectMany(t => t.Keys)
				.GroupBy(k => k)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();
			if (duplicateKeys.Count == 0)
			{
				return;
			}
			EditorGUILayout.HelpBox($"Found {duplicateKeys.Count} duplicate keys", MessageType.Warning);
			EditorGUILayout.BeginVertical("box");
			foreach (var key in duplicateKeys)
			{
				EditorGUILayout.LabelField(key, EditorStyles.boldLabel);
				foreach (var table in translationTables)
				{
					if (table.Keys.Contains(key))
					{
						EditorGUILayout.LabelField($"> In {table.Name}");
					}
				}
			}
			EditorGUILayout.EndVertical();
		}
	}
}

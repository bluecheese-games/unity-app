//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	public class TranslationTableWindow : EditorWindow
	{
		public static void Open(TranslationTableAsset asset)
		{
			var window = GetWindow<TranslationTableWindow>("Translation Table");
			window._asset = asset;
			window.Show();
		}

		private TranslationTableAsset _asset;
		private Vector2 _scrollPosition;
		private int _languageToAddIndex;
		private string _keyToAdd;
		private bool _needsRefresh;
		private string _filterText;

		private int _columnWidth = 200;

		private void OnGUI()
		{
			if (_asset == null)
			{
				EditorGUILayout.LabelField("No asset selected");
				return;
			}

			_columnWidth = Mathf.Max(200, (int)((position.width - 28) / (_asset.Languages.Count + 2)));

			_needsRefresh = false;
			DrawFilter();
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
			DrawHeader();
			DrawLines();
			EditorGUILayout.EndScrollView();
			if (_needsRefresh)
			{
				RefreshAsset();
			}
		}

		private void DrawFilter()
		{
			_filterText = EditorGUIHelper.DrawTextfieldWithIcon(_filterText, EditorIcon.Search);
			EditorGUILayout.Separator();
		}

		private void DrawHeader()
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.Width(_columnWidth));
			foreach (var language in _asset.Languages)
			{
				EditorGUILayout.LabelField(language.ToString(), EditorStyles.boldLabel, GUILayout.Width(_columnWidth - 33));
				if (GUILayout.Button(new GUIContent(EditorIcon.Trash), GUILayout.Width(30)))
				{
					if (EditorUtility.DisplayDialog("Delete Language", $"Are you sure you want to delete translations for the language {language}?", "Yes", "No"))
					{
						Undo.RecordObject(_asset, "Delete Language");
						_asset.RemoveLanguage(language);
						_needsRefresh = true;
					}
				}
			}

			var enumStyle = new GUIStyle(EditorStyles.popup)
			{
				fixedHeight = 20
			};

			var localizationService = EditorServices.Get<ILocalizationService>();
			List<string> languageNames = localizationService.SupportedLanguages
				.Except(_asset.Languages)
				.Select(x => x.ToString())
				.ToList();

			languageNames.Insert(0, "Add Language...");

			if (languageNames.Count == 0)
			{
				EditorGUILayout.EndHorizontal();
				return;
			}

			_languageToAddIndex = EditorGUILayout.Popup(_languageToAddIndex, languageNames.ToArray(), enumStyle, GUILayout.Width(_columnWidth - 33));

			if(_languageToAddIndex == 0)
			{
				GUI.enabled = false;
			}
			if (GUILayout.Button(new GUIContent(EditorIcon.Plus), GUILayout.Width(30)))
			{
				Language languageToAdd = Enum.Parse<Language>(languageNames[_languageToAddIndex]);
				if (_asset.Languages.Contains(languageToAdd))
				{
					EditorUtility.DisplayDialog("Error", "Language already exists", "Ok");
				}
				else
				{
					Undo.RecordObject(_asset, "Add Language");
					_asset.AddLanguage(languageToAdd);
					_needsRefresh = true;
					_languageToAddIndex = 0;
				}
			}
			GUI.enabled = true;

			EditorGUILayout.EndHorizontal();
		}

		private void DrawLines()
		{
			for (int keyIndex = 0; keyIndex < _asset.Keys.Count; keyIndex++)
			{
				string key = _asset.Keys[keyIndex];

				EditorGUILayout.BeginHorizontal();
				string newKey = EditorGUILayout.TextField(_asset.Keys[keyIndex], GUILayout.Width(_columnWidth));
				if (newKey != _asset.Keys[keyIndex])
				{
					if (_asset.Keys.Contains(newKey))
					{
						EditorUtility.DisplayDialog("Error", "Key already exists", "Ok");
					}
					else
					{
						Undo.RecordObject(_asset, "Edit Key");
						_asset.SetKey(keyIndex, newKey);
						_needsRefresh = true;
					}
				}
				for (int langIndex = 0; langIndex < _asset.Languages.Count; langIndex++)
				{
					var language = _asset.Languages[langIndex];
					var translation = _asset.GetTranslation(language, _asset.Keys[keyIndex]);
					var newTranslation = EditorGUILayout.TextField(translation ?? "", GUILayout.Width(_columnWidth));
					if (translation != newTranslation)
					{
						Undo.RecordObject(_asset, "Edit Translation");
						_asset.SetTranslation(language, _asset.Keys[keyIndex], newTranslation);
						_needsRefresh = true;
					}
				}
				GUI.enabled = false;
				EditorGUILayout.TextField("", GUILayout.Width(_columnWidth));
				GUI.enabled = true;
				EditorGUILayout.EndHorizontal();
			}

			// draw separator
			EditorGUILayout.Separator();

			EditorGUILayout.BeginHorizontal();
			_keyToAdd = EditorGUILayout.TextField(_keyToAdd, GUILayout.Width(_columnWidth));
			if (GUILayout.Button("Add Key", GUILayout.Width(_columnWidth)))
			{
				if (_asset.Keys.Contains(_keyToAdd))
				{
					EditorUtility.DisplayDialog("Error", "Key already exists", "Ok");
				}
				else
				{
					Undo.RecordObject(_asset, "Add Key");
					_asset.AddKey(_keyToAdd);
					_needsRefresh = true;
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private void RefreshAsset()
		{
			EditorUtility.SetDirty(_asset);
			_asset.Refresh();

			foreach (var localizedText in FindObjectsOfType<LocalizedText>())
			{
				localizedText.UpdateText();
			}
			SceneView.RepaintAll();
		}
	}
}

//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core;
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
		private string _keyToRemove;
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
			DrawItems();
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

			if (languageNames.Count == 0)
			{
				GUI.enabled = false;
			}

			languageNames.Insert(0, "Add Language...");

			if (languageNames.Count > 0)
			{
				languageNames.Add("All missing languages");
			}

			_languageToAddIndex = EditorGUILayout.Popup(_languageToAddIndex, languageNames.ToArray(), enumStyle, GUILayout.Width(_columnWidth - 33));

			if (_languageToAddIndex == 0)
			{
				GUI.enabled = false;
			}
			if (GUILayout.Button(new GUIContent(EditorIcon.Plus), GUILayout.Width(30)))
			{
				if (_languageToAddIndex == languageNames.Count - 1)
				{
					// Add all missing
					var missingLanguages = localizationService.SupportedLanguages.Except(_asset.Languages).ToList();
					Undo.RecordObject(_asset, "Add Languages");
					foreach (var lang in missingLanguages)
					{
						_asset.AddLanguage(lang);
					}
					_needsRefresh = true;
					_languageToAddIndex = 0;
					return;
				}

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

		private void DrawItems()
		{
			foreach (var item in _asset.Items)
			{
				if (!string.IsNullOrWhiteSpace(_filterText) && !item.Key.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
				{
					bool containsTranslation = false;
					foreach (var lang in _asset.Languages)
					{
						var translation = _asset.GetTranslation(item.Key, lang);
						if (!string.IsNullOrWhiteSpace(translation) && translation.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
						{
							containsTranslation = true;
							break;
						}
					}
					if (!containsTranslation)
					{
						continue;
					}
				}

				EditorGUILayout.BeginHorizontal();
				string newKey = EditorGUILayout.TextField(item.Key, GUILayout.Width(_columnWidth));
				if (newKey != item.Key)
				{
					if (_asset.ContainsKey(newKey))
					{
						EditorUtility.DisplayDialog("Error", "Key already exists", "Ok");
					}
					else
					{
						Undo.RecordObject(_asset, "Edit Key");
						_asset.EditKey(item.Key, newKey);
						_needsRefresh = true;
					}
				}
				for (int langIndex = 0; langIndex < _asset.Languages.Count; langIndex++)
				{
					var language = _asset.Languages[langIndex];
					var translation = _asset.GetTranslation(item.Key, language);
					var newTranslation = EditorGUILayout.TextField(translation ?? "", GUILayout.Width(_columnWidth));
					if (translation != newTranslation)
					{
						Undo.RecordObject(_asset, "Edit Translation");
						_asset.SetTranslation(language, item.Key, newTranslation);
						_needsRefresh = true;
					}
				}

				if (item.Status == TranslationStatus.Validated)
				{
					GUI.color = Color.green;
					string validatedTime = new DateTime(item.LastValidated).ToLocalTime().TimeAgo();
					EditorGUILayout.LabelField(new GUIContent(EditorIcon.Valid, $"Validated {validatedTime}"), GUILayout.Width(20));
					GUI.color = Color.white;
				}

				if (item.Status == TranslationStatus.Modified)
				{
					GUI.color = Color.yellow;
					string modifiedTime = new DateTime(item.LastModified).ToLocalTime().TimeAgo();
					EditorGUILayout.LabelField(new GUIContent(EditorIcon.Warning, $"Modified {modifiedTime}"), GUILayout.Width(20));
					GUI.color = Color.white;

					if (GUILayout.Button(new GUIContent("Validate", EditorIcon.Valid, "Validate Translations"), GUILayout.Width(76)))
					{
						Undo.RecordObject(_asset, "Validate Translations");
						item.ValidateTranslations();
						_needsRefresh = true;
					}
				}
				GUILayout.FlexibleSpace();
				if (GUILayout.Button(new GUIContent(EditorIcon.Trash), GUILayout.Width(30)))
				{
					if (EditorUtility.DisplayDialog("Delete Key", $"Are you sure you want to delete the key '{item.Key}' and all its translations?", "Yes", "No"))
					{
						_keyToRemove = item.Key;
					}
				}
				EditorGUILayout.EndHorizontal();
			}

			// draw separator
			EditorGUILayout.Separator();

			EditorGUILayout.BeginHorizontal();
			_keyToAdd = EditorGUILayout.TextField(_keyToAdd, GUILayout.Width(_columnWidth));
			GUI.enabled = !string.IsNullOrWhiteSpace(_keyToAdd) && !_asset.ContainsKey(_keyToAdd);
			if (GUILayout.Button("Add Key", GUILayout.Width(_columnWidth)))
			{
				Undo.RecordObject(_asset, "Add Key");
				_asset.AddItem(_keyToAdd);
				_needsRefresh = true;
			}
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();

			if (!string.IsNullOrWhiteSpace(_keyToRemove))
			{
				Undo.RecordObject(_asset, "Remove Key");
				_asset.RemoveKey(_keyToRemove);
				_keyToRemove = null;
				_needsRefresh = true;
			}
		}

		private void RefreshAsset()
		{
			EditorUtility.SetDirty(_asset);
			foreach (var localizedText in FindObjectsOfType<LocalizedText>())
			{
				localizedText.UpdateText();
			}
			SceneView.RepaintAll();
		}
	}
}

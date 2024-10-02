using System;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings.Switch;

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
		private SystemLanguage _languageToAdd;
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
			var searchIcon = EditorGUIUtility.Load("d_Search Icon") as Texture2D;
			var clearIcon = EditorGUIUtility.Load("CrossIcon") as Texture2D;
			var style = new GUIStyle(EditorStyles.toolbar)
			{
				stretchWidth = true,
				fixedHeight = 28
			};

			EditorGUILayout.BeginHorizontal(style);
			EditorGUILayout.LabelField(new GUIContent(searchIcon), GUILayout.Width(30), GUILayout.Height(24));
			_filterText = EditorGUILayout.TextField(_filterText, GUILayout.ExpandWidth(true), GUILayout.Height(22));
			/*if (GUILayout.Button(new GUIContent(clearIcon), GUILayout.Width(30)))
			{
				_filterText = "";
			}*/
			EditorGUILayout.EndHorizontal();
		}

		private void DrawHeader()
		{
			var deleteIcon = EditorGUIUtility.Load("TreeEditor.Trash") as Texture2D;
			var addIcon = EditorGUIUtility.Load("d_Toolbar Plus") as Texture2D;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.Width(_columnWidth));
			foreach (var language in _asset.Languages)
			{
				EditorGUILayout.LabelField(language.ToString(), EditorStyles.boldLabel, GUILayout.Width(_columnWidth - 33));
				if (GUILayout.Button(new GUIContent(deleteIcon), GUILayout.Width(30)))
				{
					if (EditorUtility.DisplayDialog("Delete Language", $"Are you sure you want to delete the language {language}?", "Yes", "No"))
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

			_languageToAdd = (SystemLanguage)EditorGUILayout.EnumPopup(_languageToAdd, enumStyle, GUILayout.Width(_columnWidth - 33));
			if (GUILayout.Button(new GUIContent(addIcon), GUILayout.Width(30)))
			{
				if (_asset.Languages.Contains(_languageToAdd))
				{
					EditorUtility.DisplayDialog("Error", "Language already exists", "Ok");
				}
				else
				{
					Undo.RecordObject(_asset, "Add Language");
					_asset.AddLanguage(_languageToAdd);
					_needsRefresh = true;
				}
			}

			EditorGUILayout.EndHorizontal();
		}

		private void DrawLines()
		{
			for (int keyIndex = 0; keyIndex < _asset.Keys.Count; keyIndex++)
			{
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

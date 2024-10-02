using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomEditor(typeof(LocalizationSettingsAsset))]
	public class LocalizationSettingsEditor : UnityEditor.Editor
	{
		private SerializedProperty _defaultLanguageProperty;
		private SerializedProperty _supportedLanguagesProperty;

		private LocalizationSettingsAsset _settings => (LocalizationSettingsAsset)target;

		private SystemLanguage _newLanguage;
		private string _searchText;

		private void OnEnable()
		{
			_defaultLanguageProperty = serializedObject.FindProperty(nameof(_settings.DefaultLanguage));
			_supportedLanguagesProperty = serializedObject.FindProperty(nameof(_settings.SupportedLanguages));
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawSupportedLanguages();
			EditorGUILayout.Space();
			DrawTranslationTables();

			if (serializedObject.ApplyModifiedProperties())
			{
				RefreshTexts();
			}
		}

		private static void DrawTitle(string title)
		{
			var titleStyle = new GUIStyle(EditorStyles.helpBox)
			{
				alignment = TextAnchor.MiddleCenter,
				fontSize = 18,
				fontStyle = FontStyle.Bold
			};

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(title, titleStyle);
			EditorGUILayout.EndHorizontal();
		}

		private void DrawSupportedLanguages()
		{
			DrawTitle("Supported Languages");
			SystemLanguage currentLanguage = EditorServices.Get<ILocalizationService>().CurrentLanguage;

			var trashIcon = EditorGUIUtility.IconContent("TreeEditor.Trash").image;
			var addIcon = EditorGUIUtility.IconContent("Toolbar Plus").image;
			float columnsWidth = (EditorGUIUtility.currentViewWidth - 70) / 3;
			var enumPopupStyle = new GUIStyle(EditorStyles.popup)
			{
				fixedHeight = 20
			};

			// Add defalut language in supported languages if it is not already there
			if (!_settings.SupportedLanguages.Contains(_settings.DefaultLanguage))
			{
				_supportedLanguagesProperty.InsertArrayElementAtIndex(0);
				_supportedLanguagesProperty.GetArrayElementAtIndex(0).enumValueIndex = (int)_newLanguage;
			}

			EditorGUILayout.BeginVertical("box");
			foreach (var language in _settings.SupportedLanguages)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(language.ToString(), EditorStyles.boldLabel, GUILayout.Width(columnsWidth));
				if (language != _settings.DefaultLanguage)
				{
					if (GUILayout.Button("Set as default", GUILayout.Width(columnsWidth)))
					{
						_defaultLanguageProperty.enumValueIndex = (int)language;
					}
				}
				else
				{
					EditorGUILayout.LabelField("[Default]", GUILayout.Width(columnsWidth));
				}

				if (language != currentLanguage)
				{
					if (GUILayout.Button("Set as current", GUILayout.Width(columnsWidth)))
					{
						EditorServices.Get<ILocalizationService>().SetCurrentLanguage(language);
						RefreshTexts();
					}
				}
				else
				{
					EditorGUILayout.LabelField("[Current]", GUILayout.Width(columnsWidth));
				}

				if (GUILayout.Button(trashIcon, GUILayout.Width(30)))
				{
					int index = _settings.SupportedLanguages.IndexOf(language);
					_supportedLanguagesProperty.DeleteArrayElementAtIndex(index);
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginHorizontal();
			_newLanguage = (SystemLanguage)EditorGUILayout.EnumPopup(_newLanguage, enumPopupStyle, GUILayout.Width(columnsWidth));
			GUI.enabled = _newLanguage != SystemLanguage.Unknown && !_settings.SupportedLanguages.Contains(_newLanguage);
			if (GUILayout.Button(new GUIContent(addIcon, "Add language"), GUILayout.Width(30)))
			{
				_supportedLanguagesProperty.InsertArrayElementAtIndex(_supportedLanguagesProperty.arraySize);
				_supportedLanguagesProperty.GetArrayElementAtIndex(_supportedLanguagesProperty.arraySize - 1).enumValueIndex = (int)_newLanguage;
			}
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();
		}

		private void RefreshTexts()
		{
			foreach (var localizedText in FindObjectsOfType<LocalizedText>())
			{
				localizedText.UpdateText();
			}
			SceneView.RepaintAll();
		}

		private void DrawTranslationTables()
		{
			DrawTitle("Translation Tables");

			var searchIcon = EditorGUIUtility.IconContent("Search Icon").image;
			var assetFinder = EditorServices.Get<IAssetFinderService>();
			var translationTables = assetFinder.FindAssetsInResources<TranslationTableAsset>();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent(searchIcon), GUILayout.Width(20));
			_searchText = EditorGUILayout.TextField("Search key or translation", _searchText);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginVertical("box");
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
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField($"{table.name} [{table.Keys.Count}]", EditorStyles.boldLabel);
				if (GUILayout.Button("Open", GUILayout.Width(100)))
				{
					TranslationTableWindow.Open(table);
				}
				EditorGUILayout.EndHorizontal();
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
			EditorGUILayout.EndVertical();
		}
	}
}

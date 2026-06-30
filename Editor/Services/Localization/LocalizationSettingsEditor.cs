//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Editor;
using System;
using System.Linq;
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

		private int _newLanguageIndex;
		private string _searchText;

		[MenuItem("Tools/Localization/Localization Settings")]
		public static void OpenSettings()
		{
			var guid = AssetDatabase.FindAssets($"t:{nameof(LocalizationSettingsAsset)}").FirstOrDefault();
			LocalizationSettingsAsset settings = string.IsNullOrEmpty(guid)
				? null
				: AssetDatabase.LoadAssetAtPath<LocalizationSettingsAsset>(AssetDatabase.GUIDToAssetPath(guid));

			if (settings == null)
			{
				if (!EditorUtility.DisplayDialog("Localization Settings", "No LocalizationSettings asset found.\nCreate one in Assets/Resources?", "Create", "Cancel"))
				{
					return;
				}
				if (!AssetDatabase.IsValidFolder("Assets/Resources"))
				{
					AssetDatabase.CreateFolder("Assets", "Resources");
				}
				settings = CreateInstance<LocalizationSettingsAsset>();
				AssetDatabase.CreateAsset(settings, "Assets/Resources/LocalizationSettings.asset");
				AssetDatabase.SaveAssets();
			}

			Selection.activeObject = settings;
			EditorGUIUtility.PingObject(settings);
		}

		private void OnEnable()
		{
			_defaultLanguageProperty = serializedObject.FindProperty(nameof(_settings.DefaultLanguage));
			_supportedLanguagesProperty = serializedObject.FindProperty(nameof(_settings.SupportedLanguages));
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawSupportedLanguages();

			if (serializedObject.ApplyModifiedProperties())
			{
				RefreshTexts();
			}
		}

		private void DrawSupportedLanguages()
		{
			EditorGUIHelper.DrawTitle("Supported Languages");
			Language currentLanguage = EditorServiceLocator.Get<ILocalizationService>().CurrentLanguage;

			float columnsWidth = (EditorGUIUtility.currentViewWidth - 70) / 3;
			var enumPopupStyle = new GUIStyle(EditorStyles.popup)
			{
				fixedHeight = 20
			};

			// Add default language in supported languages if it is not already there
			if (!_settings.SupportedLanguages.Contains(_settings.DefaultLanguage))
			{
				_supportedLanguagesProperty.InsertArrayElementAtIndex(0);
				_supportedLanguagesProperty.GetArrayElementAtIndex(0).enumValueIndex = (int)_settings.DefaultLanguage;
			}

			EditorGUILayout.BeginVertical("box");
			for (int i = 0; i < _supportedLanguagesProperty.arraySize; i++)
			{
				var language = (Language)_supportedLanguagesProperty.GetArrayElementAtIndex(i).enumValueIndex;
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
						EditorServiceLocator.Get<ILocalizationService>().SetCurrentLanguage(language);
						RefreshTexts();
					}
				}
				else
				{
					EditorGUILayout.LabelField("[Current]", GUILayout.Width(columnsWidth));
				}

				if (GUILayout.Button(EditorIcon.Trash, GUILayout.Width(30)))
				{
					_supportedLanguagesProperty.DeleteArrayElementAtIndex(i);
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginHorizontal();
			string[] languageNames = Enum.GetValues(typeof(Language))
				.Cast<Language>()
				.Except(_settings.SupportedLanguages)
				.Except(new[] { Language.Unknown })
				.Select(x => x.ToString())
				.Distinct()
				.ToArray();

			_newLanguageIndex = EditorGUILayout.Popup(_newLanguageIndex, languageNames, enumPopupStyle, GUILayout.Width(columnsWidth));
			if (GUILayout.Button(new GUIContent(EditorIcon.Plus, "Add language"), GUILayout.Width(30)))
			{
				string languageName = languageNames[_newLanguageIndex];
				Language newLanguage = Enum.Parse<Language>(languageName);
				_supportedLanguagesProperty.InsertArrayElementAtIndex(_supportedLanguagesProperty.arraySize);
				_supportedLanguagesProperty.GetArrayElementAtIndex(_supportedLanguagesProperty.arraySize - 1).enumValueIndex = (int)newLanguage;
				_newLanguageIndex = 0;
			}
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
	}
}

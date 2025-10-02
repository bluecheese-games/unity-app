//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomPropertyDrawer(typeof(TranslationKey))]
	public class TranslationKeyPropertyDrawer : PropertyDrawer
	{
		private int _selectedTableIndex = 0;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (EditorGUI.PropertyField(position, property, label))
			{
				DrawTranslationKeyProperty(property);
			}
		}

		private void DrawTranslationKeyProperty(SerializedProperty property)
		{
			var translationService = EditorServices.Get<EditorTranslationService>();
			var keys = translationService.GetAllKeys();
			bool isValid = keys.Contains(property.FindPropertyRelative("_key").stringValue);

			EditorGUILayout.BeginVertical("box");
			DrawKey(property, keys);

			if (isValid)
			{
				DrawPluralKey(property);
				EditorGUI.indentLevel++;
				DrawParameters(property);
				EditorGUI.indentLevel--;
			}
			else
			{
				DrawCreateKey(property, translationService.TranslationTableAssets);
			}
			EditorGUILayout.EndVertical();
		}

		private void DrawKey(SerializedProperty property, string[] keys)
		{
			var keyProperty = property.FindPropertyRelative("_key");
			EditorGUIHelper.DrawSearchableKeyProperty(keyProperty, new GUIContent("Key"), keys);
		}

		private void DrawPluralKey(SerializedProperty property)
		{
			var pluralKeyProperty = property.FindPropertyRelative("_pluralKey");

			if (!string.IsNullOrEmpty(pluralKeyProperty.stringValue))
			{
				var keys = EditorServices.Get<EditorTranslationService>().GetAllKeys();
				EditorGUIHelper.DrawSearchableKeyProperty(pluralKeyProperty, new GUIContent("PluralKey"), keys);
			}
			else
			{
				if (GUILayout.Button("Add Plural Form"))
				{
					var keyProperty = property.FindPropertyRelative("_key");
					var parametersProperty = property.FindPropertyRelative("_parameters");
					pluralKeyProperty.stringValue = keyProperty.stringValue + ".plural";
					if (parametersProperty.arraySize == 0)
					{
						parametersProperty.arraySize = 1;
						parametersProperty.GetArrayElementAtIndex(0).stringValue = "0";
					}
				}
			}
		}

		private void DrawParameters(SerializedProperty property)
		{
			var parametersProperty = property.FindPropertyRelative("_parameters");
			if (parametersProperty.arraySize > 0)
			{
				EditorGUILayout.PropertyField(parametersProperty, new GUIContent("Parameters"), true);
			}
			else
			{
				if (GUILayout.Button("Add Parameters"))
				{
					parametersProperty.arraySize = 1;
				}
			}
		}

		private void DrawCreateKey(SerializedProperty property, List<TranslationTableAsset> translationTableAssets)
		{
			var keyProperty = property.FindPropertyRelative("_key");
			if (keyProperty.stringValue == "")
			{
				return;
			}
			if (translationTableAssets.Count == 0)
			{
				EditorGUILayout.HelpBox("No Translation Table Assets found in Resources folder. Create one first.", MessageType.Info);
				return;
			}
			// Show a popup to select which translation table to add the key to
			string[] tableNames = translationTableAssets.Select(t => t.Name).ToArray();
			EditorGUILayout.BeginHorizontal();
			var enumPopupStyle = new GUIStyle(EditorStyles.popup)
			{
				fixedHeight = 20
			};
			_selectedTableIndex = EditorGUILayout.Popup("Add Key To Table", _selectedTableIndex, tableNames, enumPopupStyle);
			if (GUILayout.Button(EditorIcon.Plus, GUILayout.Width(30)))
			{
				var table = translationTableAssets[_selectedTableIndex];
				if (table != null)
				{
					Undo.RecordObject(table, "Add Translation Key");
					var item = table.AddItem(keyProperty.stringValue);
					var translationService = EditorServices.Get<EditorTranslationService>();
					var language = translationService.DefaultLanguage;
					var localizedText = (LocalizedText)property.serializedObject.targetObject;
					var tmpText = localizedText.GetComponent<TMPro.TextMeshProUGUI>();
					var textValue = tmpText.text;

					item.SetTranslation(language, textValue);
					translationService.Refresh();

					EditorUtility.SetDirty(table);
				}
			}
			EditorGUILayout.EndHorizontal();
		}
	}
}
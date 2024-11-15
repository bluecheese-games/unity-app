//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils.Editor;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomPropertyDrawer(typeof(TranslationKey))]
	public class TranslationKeyPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (EditorGUI.PropertyField(position, property, label))
			{
				DrawTranslationKeyProperty(property);
			}
		}

		private void DrawTranslationKeyProperty(SerializedProperty property)
		{
			var keys = EditorServices.Get<EditorTranslationService>().GetAllKeys();

			EditorGUILayout.BeginVertical("box");
			DrawKey(property, keys);
			DrawPluralKey(property);
			EditorGUI.indentLevel++;
			DrawParameters(property);
			EditorGUI.indentLevel--;
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
	}
}
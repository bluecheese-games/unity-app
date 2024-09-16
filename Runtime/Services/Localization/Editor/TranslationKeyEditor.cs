using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomPropertyDrawer(typeof(TranslationKey))]
	public class TranslationKeyPropertyDrawer : PropertyDrawer
	{
		private string _keyToSet = null;
		private string _pluralKeyToSet = null;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (EditorGUI.PropertyField(position, property, label))
			{
				DrawTranslationKeyProperty(property);
			}
		}

		private void DrawTranslationKeyProperty(SerializedProperty property)
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUI.indentLevel++;
			DrawKey(property);
			DrawPluralKey(property);
			DrawParameters(property);
			EditorGUI.indentLevel--;
			EditorGUILayout.EndVertical();
		}

		private void DrawKey(SerializedProperty property)
		{
			var keyProperty = property.FindPropertyRelative("_key");
			if (!string.IsNullOrEmpty(_keyToSet))
			{
				keyProperty.stringValue = _keyToSet;
				_keyToSet = null;
			}

			bool keyExists = EditorServices.Get<ITranslationService>().Translate(keyProperty.stringValue) != keyProperty.stringValue;

			EditorGUILayout.BeginHorizontal();
			var searchIcon = EditorGUIUtility.Load("Search On Icon") as Texture2D;
			var existIcon = EditorGUIUtility.Load("d_Valid") as Texture2D;
			EditorGUILayout.PropertyField(keyProperty, new GUIContent("Key"));
			var icon = keyExists ? existIcon : searchIcon;
			if (keyExists) GUI.color = Color.green;
			if (GUILayout.Button(new GUIContent(icon), GUILayout.Width(40), GUILayout.Height(20)))
			{
				// Search for key
				SearchTranslationKeyWindow.Open(key => _keyToSet = key);
			}
			EditorGUILayout.EndHorizontal();
			GUI.color = Color.white;
		}

		private void DrawPluralKey(SerializedProperty property)
		{
			var pluralKeyProperty = property.FindPropertyRelative("_pluralKey");
			if (!string.IsNullOrEmpty(_pluralKeyToSet))
			{
				pluralKeyProperty.stringValue = _pluralKeyToSet;
				_pluralKeyToSet = null;
			}

			var searchIcon = EditorGUIUtility.Load("Search On Icon") as Texture2D;
			var existIcon = EditorGUIUtility.Load("d_Valid") as Texture2D;
			if (!string.IsNullOrEmpty(pluralKeyProperty.stringValue))
			{
				bool keyExists = EditorServices.Get<ITranslationService>().Translate(pluralKeyProperty.stringValue) != pluralKeyProperty.stringValue;

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(pluralKeyProperty, new GUIContent("Plural Key"));
				var icon = keyExists ? existIcon : searchIcon;
				if (keyExists) GUI.color = Color.green;
				if (GUILayout.Button(new GUIContent(icon), GUILayout.Width(40), GUILayout.Height(20)))
				{
					// Search for key
					SearchTranslationKeyWindow.Open(key => _pluralKeyToSet = key);
				}
				EditorGUILayout.EndHorizontal();
				GUI.color = Color.white;
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
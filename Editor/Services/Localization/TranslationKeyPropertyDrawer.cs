//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Editor;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomPropertyDrawer(typeof(TranslationKey))]
	public class TranslationKeyPropertyDrawer : PropertyDrawer
	{
		private const float Spacing = 2f;
		private const float OpenButtonWidth = 28f;

		private static string[] GetKeys() => EditorServiceLocator.Resolve<EditorTranslationService>().GetAllKeys();

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float line = EditorGUIUtility.singleLineHeight;
			var key = property.FindPropertyRelative("_key").stringValue;
			bool valid = !string.IsNullOrEmpty(key) && GetKeys().Contains(key);

			float height = line; // key selector line (always shown)

			if (valid && property.isExpanded)
			{
				height += Spacing + line; // plural key row
				var parameters = property.FindPropertyRelative("_parameters");
				height += Spacing + (parameters.arraySize > 0 ? EditorGUI.GetPropertyHeight(parameters, true) : line);
			}
			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			float line = EditorGUIUtility.singleLineHeight;
			var keyProperty = property.FindPropertyRelative("_key");
			var key = keyProperty.stringValue;
			var keys = GetKeys();
			bool valid = !string.IsNullOrEmpty(key) && keys.Contains(key);

			BuildChoicesWithNone(keys, out var choices, out var choiceLabels);

			// Offered in the search dropdown when the typed text matches no existing key.
			void CreateNew(string newKey) => CreateKey(property, newKey);

			var firstLine = new Rect(position.x, position.y, position.width, line);

			// Valid key: a foldout arrow (to reveal plural/parameters) at the far left; the label/field
			// are indented one step so the arrow has room. The field itself is always drawn by the helper
			// through PrefixLabel, so it aligns exactly with the other inspector fields.
			if (valid)
			{
				var arrowRect = new Rect(firstLine.x, firstLine.y, 14f, line);
				property.isExpanded = EditorGUI.Foldout(arrowRect, property.isExpanded, GUIContent.none, toggleOnLabelClick: true);
			}
			else
			{
				property.isExpanded = false;
			}

			int previousIndent = EditorGUI.indentLevel;
			if (valid)
			{
				EditorGUI.indentLevel++;
			}
			DrawKeyFieldWithOpen(firstLine, keyProperty, label, choices, choiceLabels, keys, CreateNew);
			EditorGUI.indentLevel = previousIndent;

			if (valid && property.isExpanded)
			{
				float y = position.y + line + Spacing;
				EditorGUI.indentLevel++;
				y = DrawPluralKey(position, y, property, keys);
				DrawParameters(position, y, property);
				EditorGUI.indentLevel--;
			}
		}

		private static void BuildChoicesWithNone(string[] keys, out string[] choices, out string[] labels)
		{
			choices = new string[keys.Length + 1];
			labels = new string[keys.Length + 1];
			choices[0] = string.Empty;
			labels[0] = "None";
			for (int i = 0; i < keys.Length; i++)
			{
				choices[i + 1] = keys[i];
				labels[i + 1] = keys[i];
			}
		}

		private float DrawPluralKey(Rect position, float y, SerializedProperty property, string[] keys)
		{
			float line = EditorGUIUtility.singleLineHeight;
			var pluralKeyProperty = property.FindPropertyRelative("_pluralKey");
			var rowRect = new Rect(position.x, y, position.width, line);

			if (!string.IsNullOrEmpty(pluralKeyProperty.stringValue))
			{
				// Include a "None" entry so the plural key can be cleared (removes the plural form).
				BuildChoicesWithNone(keys, out var choices, out var choiceLabels);
				DrawKeyFieldWithOpen(rowRect, pluralKeyProperty, new GUIContent("Plural Key"), choices, choiceLabels, keys, onCreateNew: null);
			}
			else if (GUI.Button(EditorGUI.IndentedRect(rowRect), "Add Plural Form"))
			{
				pluralKeyProperty.stringValue = property.FindPropertyRelative("_key").stringValue + ".plural";
				var parameters = property.FindPropertyRelative("_parameters");
				if (parameters.arraySize == 0)
				{
					parameters.arraySize = 1;
					parameters.GetArrayElementAtIndex(0).stringValue = "0";
				}
			}
			return y + line + Spacing;
		}

		private float DrawParameters(Rect position, float y, SerializedProperty property)
		{
			float line = EditorGUIUtility.singleLineHeight;
			var parameters = property.FindPropertyRelative("_parameters");

			if (parameters.arraySize > 0)
			{
				float height = EditorGUI.GetPropertyHeight(parameters, true);
				EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), parameters, new GUIContent("Parameters"), true);
				return y + height + Spacing;
			}

			var rowRect = new Rect(position.x, y, position.width, line);
			if (GUI.Button(EditorGUI.IndentedRect(rowRect), "Add Parameters"))
			{
				parameters.arraySize = 1;
			}
			return y + line + Spacing;
		}

		// Creates a new key from the search dropdown. Picks the table directly if there is only one,
		// otherwise lets the user choose via a context menu.
		private void CreateKey(SerializedProperty property, string newKey)
		{
			newKey = newKey?.Trim();
			if (string.IsNullOrEmpty(newKey))
			{
				return;
			}

			var tables = EditorServiceLocator.Resolve<EditorTranslationService>().GetTranslationTableAssets();
			if (tables == null || tables.Count == 0)
			{
				EditorUtility.DisplayDialog("Create Key", "Create a Translation Table first.", "OK");
				return;
			}

			if (tables.Count == 1)
			{
				CreateKeyInTable(property, tables[0], newKey);
				return;
			}

			var menu = new GenericMenu();
			foreach (var candidate in tables)
			{
				var table = candidate;
				menu.AddItem(new GUIContent($"Create in {table.Name}"), false, () => CreateKeyInTable(property, table, newKey));
			}
			menu.ShowAsContext();
		}

		private void CreateKeyInTable(SerializedProperty property, TranslationTableAsset table, string newKey)
		{
			if (table == null)
			{
				return;
			}
			Undo.RecordObject(table, "Add Translation Key");
			var item = table.AddItem(newKey);
			var service = EditorServiceLocator.Resolve<EditorTranslationService>();
			item.SetTranslation(service.DefaultLanguage, GetSourceText(property));
			service.Refresh();
			EditorUtility.SetDirty(table);

			var keyProperty = property.FindPropertyRelative("_key");
			keyProperty.stringValue = newKey;
			keyProperty.serializedObject.ApplyModifiedProperties();
		}

		// Draws the searchable key field, plus a small icon button (when the key exists) to open the
		// containing table in the Translation Editor.
		private static void DrawKeyFieldWithOpen(Rect rect, SerializedProperty keyProperty, GUIContent label, string[] choices, string[] choiceLabels, string[] validKeys, System.Action<string> onCreateNew)
		{
			var key = keyProperty.stringValue;
			bool canOpen = !string.IsNullOrEmpty(key) && validKeys.Contains(key);

			var fieldRect = canOpen ? new Rect(rect.x, rect.y, rect.width - OpenButtonWidth, rect.height) : rect;
			EditorGUIHelper.DrawSearchableKeyProperty(fieldRect, keyProperty, label, choices, choiceLabels, onCreateNew: onCreateNew);

			if (canOpen)
			{
				var buttonRect = new Rect(fieldRect.xMax + 2f, rect.y, OpenButtonWidth - 2f, rect.height);
				var previousIconSize = EditorGUIUtility.GetIconSize();
				EditorGUIUtility.SetIconSize(new Vector2(16, 16));
				if (GUI.Button(buttonRect, new GUIContent(EditorIcon.Open, "Open in Translation Editor")))
				{
					OpenTableForKey(key);
				}
				EditorGUIUtility.SetIconSize(previousIconSize);
			}
		}

		private static void OpenTableForKey(string key)
		{
			var table = EditorServiceLocator.Resolve<EditorTranslationService>()
				.GetTranslationTableAssets()
				.FirstOrDefault(t => t != null && t.Keys != null && t.Keys.Contains(key));
			if (table != null)
			{
				TranslationTableWindow.Open(table, key);
			}
		}

		// Robustly resolves the source text to seed the default-language translation.
		private static string GetSourceText(SerializedProperty property)
		{
			if (property.serializedObject.targetObject is LocalizedText localizedText)
			{
				if (property.serializedObject.FindProperty("_text")?.objectReferenceValue is TMP_Text referenced)
				{
					return referenced.text;
				}
				var component = localizedText.GetComponentInChildren<TMP_Text>(true);
				if (component != null)
				{
					return component.text;
				}
			}
			return string.Empty;
		}
	}
}

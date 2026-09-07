using BlueCheese.Core.Editor;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomPropertyDrawer(typeof(TranslationKey))]
	public class TranslationKeyPropertyDrawer : PropertyDrawer
	{
		private const float Spacing = 2f;
		private const float OpenButtonWidth = 28f;
		private const float AIButtonWidth = 24f;

		// Keyed by target instance id so the button can only fire once per component while a request
		// is in flight (a drawer instance may be reused across different targets/repaints).
		private static readonly HashSet<int> _aiBusyInstanceIds = new();

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
			EditorGUI.BeginProperty(position, label, property);

			float line = EditorGUIUtility.singleLineHeight;
			var keyProperty = property.FindPropertyRelative("_key");
			var key = keyProperty.stringValue;
			var keys = GetKeys();
			bool valid = !string.IsNullOrEmpty(key) && keys.Contains(key);

			BuildChoicesWithNone(keys, out var choices, out var choiceLabels);

			// Offered in the search dropdown when the typed text matches no existing key.
			void CreateNew(string newKey) => CreateKey(property, newKey);

			var firstLine = new Rect(position.x, position.y, position.width, line);

			// Draw the prefix with LabelField (EditorGUI.PrefixLabel renders it detached from its field in
			// this drawer). LabelField puts "Translation Key" on the correct row, at the same x as the other
			// component prefixes (Script, Text, ...). The field rect mirrors what PrefixLabel returns — the
			// label column plus Unity's internal 2px gap — so the field lines up with the other values.
			const float prefixPaddingRight = 2f;
			float labelWidth = EditorGUIUtility.labelWidth;
			var labelRect = new Rect(firstLine.x, firstLine.y, labelWidth, line);
			// The AI button always claims space at the row's right edge, whether or not a key is set yet.
			var aiButtonRect = new Rect(firstLine.xMax - AIButtonWidth, firstLine.y, AIButtonWidth, line);
			var fieldRect = new Rect(firstLine.x + labelWidth + prefixPaddingRight, firstLine.y, firstLine.width - labelWidth - prefixPaddingRight - AIButtonWidth - Spacing, line);

			EditorGUI.LabelField(labelRect, label);

			if (valid)
			{
				// A foldout arrow in the free space at the end of the label column toggles the plural key /
				// parameters. It sits just before the field, so it neither indents the prefix nor shortens
				// the key selector.
				const float arrowWidth = 13f;
				var arrowRect = new Rect(labelRect.xMax - arrowWidth, firstLine.y, arrowWidth, line);
				property.isExpanded = EditorGUI.Foldout(arrowRect, property.isExpanded, GUIContent.none, toggleOnLabelClick: true);
			}
			else
			{
				property.isExpanded = false;
			}

			DrawKeyFieldWithOpen(fieldRect, keyProperty, GUIContent.none, choices, choiceLabels, keys, CreateNew);
			DrawAIButton(aiButtonRect, property, keyProperty);

			if (valid && property.isExpanded)
			{
				float y = position.y + line + Spacing;
				EditorGUI.indentLevel++;
				y = DrawPluralKey(position, y, property, keys);
				DrawParameters(position, y, property);
				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
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

		// AI: tries to match an existing key (using maximum context) or propose new key candidates.
		private static void DrawAIButton(Rect rect, SerializedProperty property, SerializedProperty keyProperty)
		{
			int instanceId = property.serializedObject.targetObject.GetInstanceID();
			bool busy = _aiBusyInstanceIds.Contains(instanceId);

			using (new EditorGUI.DisabledScope(busy))
			{
				if (GUI.Button(rect, new GUIContent(busy ? "…" : "✨", "AI: find or create a matching translation key")))
				{
					OnClickAIMatch(property, keyProperty, instanceId);
				}
			}
		}

		private static void OnClickAIMatch(SerializedProperty property, SerializedProperty keyProperty, int instanceId)
		{
			var settings = AITranslationSettings.GetOrNull();
			if (settings == null)
			{
				if (EditorUtility.DisplayDialog("AI Translation", "No AI Translation Settings found. Create one now?", "Create", "Cancel"))
				{
					AITranslationSettings.Open();
				}
				return;
			}
			if (settings.Provider == AITranslationProviderKind.None)
			{
				EditorUtility.DisplayDialog("AI Translation", "No AI provider selected. Choose one in the AI Translation Settings.", "Ok");
				AITranslationSettings.Open();
				return;
			}
			if (string.IsNullOrEmpty(AITranslationSettings.GetApiKey(settings.Provider)))
			{
				EditorUtility.DisplayDialog("AI Translation", $"Set your {settings.Provider} API key in the AI Translation Settings first.", "Ok");
				AITranslationSettings.Open();
				return;
			}
			if (property.serializedObject.targetObject is not Component component)
			{
				return;
			}

			var service = EditorServiceLocator.Resolve<EditorTranslationService>();
			var defaultLanguage = service.DefaultLanguage;
			var sourceText = GetSourceText(property);
			var existingKeys = AIKeyMatchContextBuilder.BuildExistingKeysGlossary(service, defaultLanguage);
			var request = AIKeyMatchContextBuilder.Build(component, sourceText, defaultLanguage, existingKeys, settings);

			// The SerializedProperty/SerializedObject must NOT be captured across this async call: Unity
			// may rebuild or dispose them before the response arrives, and touching a stale one throws
			// from native code even after a null-check. Keep only the plain component reference (whose
			// `== null` check is safe) and the property path, and rebuild a fresh SerializedObject later.
			var keyPropertyPath = keyProperty.propertyPath;

			_aiBusyInstanceIds.Add(instanceId);
			AIProviders.Create(settings).MatchKey(request, result =>
			{
				_aiBusyInstanceIds.Remove(instanceId);
				InternalEditorUtility.RepaintAllViews();
				HandleMatchResult(component, keyPropertyPath, result, settings, defaultLanguage);
			});
		}

		private static void HandleMatchResult(Component component, string keyPropertyPath, AIKeyMatchResult result, AITranslationSettings settings, Language defaultLanguage)
		{
			if (component == null)
			{
				return; // the inspected object was destroyed/unloaded while the request was in flight
			}

			if (!result.Success)
			{
				EditorUtility.DisplayDialog("AI Key Match", "Failed:\n" + result.Error, "Ok");
				return;
			}

			if (!string.IsNullOrEmpty(result.MatchedKey))
			{
				var service = EditorServiceLocator.Resolve<EditorTranslationService>();
				var table = service.GetTranslationTableAssets().FirstOrDefault(t => t != null && t.Keys != null && t.Keys.Contains(result.MatchedKey));
				var defaultText = table != null ? table.GetTranslation(result.MatchedKey, defaultLanguage) : string.Empty;

				var message = $"Key: {result.MatchedKey}\nDefault text: \"{defaultText}\"";
				if (!string.IsNullOrEmpty(result.MatchReason))
				{
					message += $"\n\nReason: {result.MatchReason}";
				}

				if (EditorUtility.DisplayDialog("AI Key Match", message, "Use this key", "Cancel"))
				{
					ApplyKey(component, keyPropertyPath, result.MatchedKey);
				}
				return;
			}

			if (result.NewKeySuggestions.Count == 0)
			{
				EditorUtility.DisplayDialog("AI Key Match", "The AI did not return a match or any new key suggestion.", "Ok");
				return;
			}

			AIKeySuggestionWindow.Open(component, keyPropertyPath, result.NewKeySuggestions, settings, defaultLanguage);
		}

		// Rebuilds a fresh SerializedObject from the still-live component rather than reusing a
		// SerializedProperty captured before the async AI round-trip (see the comment in OnClickAIMatch).
		private static void ApplyKey(Component component, string keyPropertyPath, string key)
		{
			var serializedObject = new SerializedObject(component);
			var property = serializedObject.FindProperty(keyPropertyPath);
			if (property == null)
			{
				return;
			}
			property.stringValue = key;
			serializedObject.ApplyModifiedProperties();
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

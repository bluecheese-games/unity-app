//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Popup shown when the AI found no confident existing key match for a <see cref="TranslationKey"/>
	/// field: lets the user pick/edit one of the AI-proposed new keys, choose the destination table, and
	/// optionally auto-translate it into every other supported language on creation.
	/// </summary>
	public class AIKeySuggestionWindow : EditorWindow
	{
		// The originating field is identified by component + property path rather than a held
		// SerializedProperty/SerializedObject: this window stays open (and later fires an async
		// translate call) well past the frame it was opened in, and Unity may rebuild or dispose a
		// held SerializedObject in the meantime — touching a stale one throws from native code even
		// after a null-check. A fresh SerializedObject is built from the component just-in-time instead.
		private string _keyPropertyPath;
		private Component _sourceComponent;
		private List<NewKeySuggestion> _suggestions;
		private AITranslationSettings _settings;
		private Language _defaultLanguage;

		private List<TranslationTableAsset> _tables;
		private int _tableIndex;
		private int _candidateIndex;
		private string _key;
		private string _defaultText;
		private bool _translateAllLanguages = true;
		private bool _creating;

		private Label _statusLabel;
		private Button _createButton;

		public static void Open(Component sourceComponent, string keyPropertyPath, List<NewKeySuggestion> suggestions, AITranslationSettings settings, Language defaultLanguage)
		{
			var window = CreateInstance<AIKeySuggestionWindow>();
			window.titleContent = new GUIContent("AI Key Suggestion");
			window.minSize = new Vector2(480, 340);
			window._keyPropertyPath = keyPropertyPath;
			window._sourceComponent = sourceComponent;
			window._suggestions = suggestions;
			window._settings = settings;
			window._defaultLanguage = defaultLanguage;
			window._tables = TranslationAssetFinder.FindAllTables();
			window.ApplyCandidate(0);
			window.ShowUtility();
		}

		private void ApplyCandidate(int index)
		{
			_candidateIndex = Mathf.Clamp(index, 0, _suggestions.Count - 1);
			var candidate = _suggestions[_candidateIndex];
			_key = candidate.Key;
			_defaultText = candidate.DefaultText;
		}

		private void CreateGUI()
		{
			var root = rootVisualElement;
			root.style.flexGrow = 1;
			root.style.paddingLeft = 10;
			root.style.paddingRight = 10;
			root.style.paddingTop = 8;
			root.style.paddingBottom = 8;

			root.Add(new Label("No confident existing key match — create a new one") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13, marginBottom = 6, whiteSpace = WhiteSpace.Normal } });

			if (_sourceComponent != null)
			{
				root.Add(new Label($"On: {_sourceComponent.gameObject.name} ({_sourceComponent.GetType().Name})") { style = { opacity = 0.6f, marginBottom = 8 } });
			}

			var candidateChoices = _suggestions.Select(s => $"{s.Key}  —  {s.DefaultText}").ToList();
			var candidateDropdown = new DropdownField("Suggestion", candidateChoices, _candidateIndex) { style = { marginBottom = 6 } };
			root.Add(candidateDropdown);

			var keyField = new TextField("Key") { value = _key, style = { marginBottom = 4 } };
			root.Add(keyField);

			var textField = new TextField("Default text") { value = _defaultText, multiline = true, style = { marginBottom = 8, whiteSpace = WhiteSpace.Normal } };
			root.Add(textField);

			candidateDropdown.RegisterValueChangedCallback(_ =>
			{
				ApplyCandidate(candidateDropdown.index);
				keyField.SetValueWithoutNotify(_key);
				textField.SetValueWithoutNotify(_defaultText);
			});
			keyField.RegisterValueChangedCallback(evt => _key = evt.newValue);
			textField.RegisterValueChangedCallback(evt => _defaultText = evt.newValue);

			if (_tables.Count > 1)
			{
				var tableDropdown = new DropdownField("Table", _tables.Select(t => t.Name).ToList(), _tableIndex) { style = { marginBottom = 8 } };
				tableDropdown.RegisterValueChangedCallback(_ => _tableIndex = tableDropdown.index);
				root.Add(tableDropdown);
			}
			else if (_tables.Count == 0)
			{
				root.Add(new Label("No Translation Table found in the project — create one first.") { style = { color = new Color(0.9f, 0.5f, 0.5f), whiteSpace = WhiteSpace.Normal, marginBottom = 8 } });
			}

			var translateToggle = new Toggle("Also translate into all languages") { value = _translateAllLanguages, style = { marginBottom = 10 } };
			translateToggle.RegisterValueChangedCallback(evt => _translateAllLanguages = evt.newValue);
			root.Add(translateToggle);

			_statusLabel = new Label { style = { opacity = 0.7f, marginBottom = 4, display = DisplayStyle.None } };
			root.Add(_statusLabel);

			var footer = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, marginTop = 4 } };
			footer.Add(new Button(Close) { text = "Cancel", style = { marginRight = 4 } });

			_createButton = new Button(Create) { text = "Create", style = { minWidth = 90 } };
			_createButton.SetEnabled(_tables.Count > 0);
			_createButton.style.backgroundColor = new Color(0.25f, 0.6f, 0.3f);
			_createButton.style.color = Color.white;
			footer.Add(_createButton);

			root.Add(footer);
		}

		private void Create()
		{
			if (_creating || _tables.Count == 0)
			{
				return;
			}

			var key = _key?.Trim();
			if (string.IsNullOrEmpty(key))
			{
				EditorUtility.DisplayDialog("AI Key Suggestion", "Key cannot be empty.", "Ok");
				return;
			}

			var table = _tables[Mathf.Clamp(_tableIndex, 0, _tables.Count - 1)];
			if (table.Keys != null && table.Keys.Contains(key))
			{
				EditorUtility.DisplayDialog("AI Key Suggestion", $"Key '{key}' already exists in '{table.Name}'. Choose a different key.", "Ok");
				return;
			}

			Undo.RecordObject(table, "AI Create Translation Key");
			var item = table.AddItem(key);
			item.SetTranslation(_defaultLanguage, _defaultText ?? string.Empty, aiTranslated: true);
			EditorUtility.SetDirty(table);

			if (_sourceComponent != null)
			{
				var serializedObject = new SerializedObject(_sourceComponent);
				var property = serializedObject.FindProperty(_keyPropertyPath);
				if (property != null)
				{
					property.stringValue = key;
					serializedObject.ApplyModifiedProperties();
				}
			}

			var service = EditorServiceLocator.Resolve<EditorTranslationService>();
			service.Refresh();

			var targets = table.Languages.Where(l => l != _defaultLanguage).ToList();
			if (!_translateAllLanguages || _settings == null || targets.Count == 0)
			{
				Close();
				return;
			}

			_creating = true;
			_createButton.SetEnabled(false);
			_statusLabel.style.display = DisplayStyle.Flex;
			_statusLabel.text = "✨ Translating…";

			var references = _sourceComponent != null
				? new List<TranslationKeyReferenceFinder.Reference> { BuildReference(_sourceComponent) }
				: new List<TranslationKeyReferenceFinder.Reference>();

			var request = AITranslationContextBuilder.Build(table, item, _defaultLanguage, targets, references, _settings);
			AIProviders.Create(_settings).Translate(request, result =>
			{
				if (result.Success)
				{
					Undo.RecordObject(table, "AI Translate");
					AITranslationApplier.ApplyMostProbable(table, item, _defaultLanguage, result);
					EditorUtility.SetDirty(table);
					service.Refresh();
				}
				else
				{
					Debug.LogWarning($"[AI Key Suggestion] Translation failed for '{key}': {result.Error}");
				}
				Close();
			});
		}

		private static TranslationKeyReferenceFinder.Reference BuildReference(Component component)
		{
			var (assetPath, isScene) = ResolveAssetContext(component);
			return new TranslationKeyReferenceFinder.Reference(
				assetPath,
				isScene,
				TranslationKeyReferenceFinder.GetHierarchyPath(component.transform),
				component.GetType().Name,
				false,
				TranslationKeyReferenceFinder.EstimateMaxChars(component),
				TranslationKeyReferenceFinder.GatherSiblingTexts(component));
		}

		private static (string path, bool isScene) ResolveAssetContext(Component component)
		{
			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null)
			{
				return (stage.assetPath, false);
			}
			if (component.gameObject.scene.IsValid())
			{
				return (component.gameObject.scene.path, true);
			}
			return (AssetDatabase.GetAssetPath(component.gameObject), false);
		}
	}
}

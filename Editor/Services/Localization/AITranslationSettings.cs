using BlueCheese.Core.Editor;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Editor-only, project-shared configuration for AI translation. API keys are intentionally
	/// NOT stored here (they live in EditorPrefs, per machine and per provider, so they never get committed).
	/// </summary>
	public class AITranslationSettings : ScriptableObject
	{
		private const string ApiKeyPrefFormat = "BlueCheese.AITranslation.{0}.ApiKey";
		private const string DefaultAssetPath = "Assets/AITranslationSettings.asset";

		[Tooltip("Which AI backend to use. Each provider keeps its own model and API key.")]
		public AITranslationProviderKind Provider = AITranslationProviderKind.None;

		public string AnthropicModel = "claude-sonnet-4-5";
		public string GeminiModel = "gemini-2.5-flash";

		[Tooltip("Free-form guidelines: tone, formality, brand terms, forbidden words, style…")]
		[TextArea(4, 12)] public string Guidelines;

		public string ProjectName;
		[TextArea(2, 5)] public string ProjectInfo;

		[Tooltip("Include where/how the key is used (scenes, prefabs, hierarchy). More precise but scans the project.")]
		public bool IncludeUsageContext = true;

		[Tooltip("Ask the AI to respect the UI character budget estimated from the text field size.")]
		public bool EnforceMaxChars = true;

		[Tooltip("How many validated translations to send as a consistency glossary.")]
		public int GlossarySampleSize = 20;

		[Tooltip("How many alternative translations the AI proposes per language. The most likely is applied automatically; you can pick another in the detail panel.")]
		[Range(1, 5)] public int AlternativesCount = 1;

		/// <summary>Model id for the currently selected provider.</summary>
		public string GetModel() => Provider switch
		{
			AITranslationProviderKind.Anthropic => AnthropicModel,
			AITranslationProviderKind.Gemini => GeminiModel,
			_ => string.Empty,
		};

		public static string ModelFieldName(AITranslationProviderKind kind) => kind switch
		{
			AITranslationProviderKind.Gemini => nameof(GeminiModel),
			_ => nameof(AnthropicModel),
		};

		public static string GetApiKey(AITranslationProviderKind kind) => EditorPrefs.GetString(string.Format(ApiKeyPrefFormat, kind), string.Empty);
		public static void SetApiKey(AITranslationProviderKind kind, string value) => EditorPrefs.SetString(string.Format(ApiKeyPrefFormat, kind), value ?? string.Empty);

		public static AITranslationSettings GetOrNull()
		{
			var guid = AssetDatabase.FindAssets($"t:{nameof(AITranslationSettings)}").FirstOrDefault();
			return string.IsNullOrEmpty(guid)
				? null
				: AssetDatabase.LoadAssetAtPath<AITranslationSettings>(AssetDatabase.GUIDToAssetPath(guid));
		}

		[MenuItem("Tools/Localization/AI Translation Settings")]
		public static void Open()
		{
			var settings = GetOrNull();
			if (settings == null)
			{
				settings = CreateInstance<AITranslationSettings>();
				AssetDatabase.CreateAsset(settings, DefaultAssetPath);
				AssetDatabase.SaveAssets();
			}
			Selection.activeObject = settings;
			EditorGUIUtility.PingObject(settings);
		}
	}

	[CustomEditor(typeof(AITranslationSettings))]
	public class AITranslationSettingsEditor : UnityEditor.Editor
	{
		private bool _testing;
		private bool _testOk;
		private string _testResult;

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var providerProp = serializedObject.FindProperty(nameof(AITranslationSettings.Provider));
			EditorGUILayout.PropertyField(providerProp);
			var provider = (AITranslationProviderKind)providerProp.enumValueIndex;

			if (provider == AITranslationProviderKind.None)
			{
				EditorGUILayout.HelpBox("Select an AI provider to enable AI translation.", MessageType.Info);
			}
			else
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField($"{provider} configuration", EditorStyles.boldLabel);

				// Model: pick from presets (searchable) and/or type a custom id — both edit the same field.
				var modelProp = serializedObject.FindProperty(AITranslationSettings.ModelFieldName(provider));
				EditorGUIHelper.DrawSearchableKeyProperty(modelProp, new GUIContent("Model (preset)"), AIModelCatalog.For(provider));
				EditorGUILayout.PropertyField(modelProp, new GUIContent("Model (custom)"));

				var currentKey = AITranslationSettings.GetApiKey(provider);
				var newKey = EditorGUILayout.PasswordField("API Key", currentKey);
				if (newKey != currentKey)
				{
					AITranslationSettings.SetApiKey(provider, newKey);
					currentKey = newKey;
					_testResult = null;
				}
				EditorGUILayout.HelpBox("API key is stored locally in EditorPrefs (per machine, per provider) — never committed.", MessageType.Info);

				using (new EditorGUI.DisabledScope(_testing || string.IsNullOrEmpty(currentKey)))
				{
					if (GUILayout.Button(_testing ? "Testing…" : "Test API"))
					{
						RunTest();
					}
				}
				if (!string.IsNullOrEmpty(_testResult))
				{
					EditorGUILayout.HelpBox(_testResult, _testOk ? MessageType.Info : MessageType.Error);
				}
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Context", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AITranslationSettings.Guidelines)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AITranslationSettings.ProjectName)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AITranslationSettings.ProjectInfo)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AITranslationSettings.IncludeUsageContext)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AITranslationSettings.EnforceMaxChars)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AITranslationSettings.GlossarySampleSize)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AITranslationSettings.AlternativesCount)));

			serializedObject.ApplyModifiedProperties();
		}

		private void RunTest()
		{
			// Persist edits (model choice) so the provider is built from current values.
			serializedObject.ApplyModifiedProperties();

			var provider = AIProviders.Create((AITranslationSettings)target);
			if (provider == null)
			{
				_testOk = false;
				_testResult = "No provider selected.";
				return;
			}

			_testing = true;
			_testResult = null;
			provider.TestConnection((ok, message) =>
			{
				_testing = false;
				_testOk = ok;
				_testResult = ok ? message : $"Failed: {message}";
				Repaint();
			});
		}
	}
}

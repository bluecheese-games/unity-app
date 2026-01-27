//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BlueCheese.App;
using BlueCheese.Core.Editor;

namespace BlueCheese.Tools.Editor
{
	public class MissingLocalizationKeysWindow : EditorWindow
	{
		private const string Title = "Missing Localization Keys";
		private const string PrefsKeyIgnored = "BlueCheese.MissingKeys.Ignored"; // pipe-separated ids

		[Serializable]
		private class WorkItem
		{
			public enum ActionType { Ignore, AddTo }

			public string SourcePath;             // Asset path of the scene or prefab
			public bool IsScene;                // true => scene, false => prefab
			public string ObjectPath;             // Hierarchy path for context
			public UnityEngine.Object PingTarget; // Asset to select
			public string Key;                    // Missing key (exists on object but not in any table asset)
			public string DefaultText;            // TMP_Text text to seed default language
			public ActionType Action = ActionType.AddTo;
			public TranslationTableAsset Destination; // Destination when Action == AddTo

			public string PersistentId => $"{SourcePath}|{ObjectPath}|{Key}";
		}

		[Serializable]
		private class WarningItem
		{
			public string SourcePath;
			public bool IsScene;
			public string ObjectPath;
			public UnityEngine.Object PingTarget;
			public string Reason;   // e.g., "No key", "Invalid key (contains whitespace)"
		}

		private readonly List<string> _searchFolders = new();
		private readonly List<WorkItem> _work = new();
		private readonly List<WarningItem> _warnings = new();
		private HashSet<string> _ignored = new(StringComparer.Ordinal);
		private bool _showIgnored = false;

		private Vector2 _scroll;

		private TranslationTableAsset[] _allTableAssets;
		private string[] _allTableAssetNames;

		private Language _defaultLanguage = Language.English;

		// Bulk toolbar state
		private TranslationTableAsset _bulkDestination;
		private int _bulkDestinationIndex = 0;

		[MenuItem("Tools/Localization/Missing Keys Scanner")]
		public static void Open() => GetWindow<MissingLocalizationKeysWindow>(Title).Show();

		private void OnEnable()
		{
			if (_searchFolders.Count == 0) _searchFolders.Add("Assets");
			LoadIgnored();
			RefreshTranslationAssetsCache();
			_defaultLanguage = FindDefaultLanguage();

			// Initialize bulk destination
			if (_allTableAssets != null && _allTableAssets.Length > 0)
			{
				_bulkDestination = _allTableAssets[0];
				_bulkDestinationIndex = 0;
			}
		}

		private void RefreshTranslationAssetsCache()
		{
			var guids = AssetDatabase.FindAssets($"t:{nameof(TranslationTableAsset)}");
			_allTableAssets = guids
				.Select(g => AssetDatabase.LoadAssetAtPath<TranslationTableAsset>(AssetDatabase.GUIDToAssetPath(g)))
				.Where(a => a != null)
				.OrderBy(a => a.name)
				.ToArray();

			_allTableAssetNames = _allTableAssets.Select(a => a.name).ToArray();

			// keep bulk destination index in sync if possible
			if (_bulkDestination != null)
			{
				var idx = Array.IndexOf(_allTableAssets, _bulkDestination);
				_bulkDestinationIndex = Mathf.Max(0, idx);
				if (idx < 0 && _allTableAssets.Length > 0)
				{
					_bulkDestinationIndex = 0;
					_bulkDestination = _allTableAssets[0];
				}
			}
		}

		private Language FindDefaultLanguage()
		{
			var guid = AssetDatabase.FindAssets($"t:{nameof(LocalizationSettingsAsset)}").FirstOrDefault();
			if (!string.IsNullOrEmpty(guid))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettingsAsset>(path);
				if (settings != null) return settings.DefaultLanguage;
			}
			return Language.English;
		}

		private void LoadIgnored()
		{
			_ignored.Clear();
			var raw = EditorPrefs.GetString(PrefsKeyIgnored, "");
			if (string.IsNullOrEmpty(raw)) return;

			foreach (var token in raw.Split('|'))
			{
				if (!string.IsNullOrWhiteSpace(token))
					_ignored.Add(token);
			}
		}

		private void SaveIgnored()
		{
			var raw = string.Join("|", _ignored);
			EditorPrefs.SetString(PrefsKeyIgnored, raw);
		}

		private static HashSet<string> GetAllExistingKeysFromAllAssets()
		{
			var all = new HashSet<string>(StringComparer.Ordinal);
			foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(TranslationTableAsset)}"))
			{
				var asset = AssetDatabase.LoadAssetAtPath<TranslationTableAsset>(AssetDatabase.GUIDToAssetPath(guid));
				if (!asset) continue;

				// Read serialized keys; avoids depending on internals
				var so = new SerializedObject(asset);
				var items = so.FindProperty("_items");
				if (items is { isArray: true })
				{
					for (int i = 0; i < items.arraySize; i++)
					{
						var el = items.GetArrayElementAtIndex(i);
						var key = el.FindPropertyRelative("Key")?.stringValue;
						if (!string.IsNullOrEmpty(key))
							all.Add(key);
					}
				}
			}
			return all;
		}

		private void Scan()
		{
			_work.Clear();
			_warnings.Clear();
			RefreshTranslationAssetsCache();
			_defaultLanguage = FindDefaultLanguage();

			var knownKeys = GetAllExistingKeysFromAllAssets();
			var searchIn = _searchFolders.ToArray();

			var prefabGuids = AssetDatabase.FindAssets("t:Prefab", searchIn);
			var sceneGuids = AssetDatabase.FindAssets("t:Scene", searchIn);

			foreach (var guid in prefabGuids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				ScanPrefab(path, knownKeys);
			}

			foreach (var guid in sceneGuids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				ScanScene(path, knownKeys);
			}

			_work.Sort((a, b) =>
			{
				int c = string.CompareOrdinal(a.SourcePath, b.SourcePath);
				if (c != 0) return c;
				c = string.CompareOrdinal(a.ObjectPath, b.ObjectPath);
				if (c != 0) return c;
				return string.CompareOrdinal(a.Key, b.Key);
			});

			_warnings.Sort((a, b) =>
			{
				int c = string.CompareOrdinal(a.SourcePath, b.SourcePath);
				if (c != 0) return c;
				c = string.CompareOrdinal(a.ObjectPath, b.ObjectPath);
				if (c != 0) return c;
				return string.CompareOrdinal(a.Reason, b.Reason);
			});

			Repaint();
		}

		private void ScanPrefab(string path, HashSet<string> knownKeys)
		{
			var root = PrefabUtility.LoadPrefabContents(path);
			try
			{
				foreach (var lt in root.GetComponentsInChildren<LocalizedText>(true))
					CategorizeLocalizedText(lt, path, isScene: false, knownKeys);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		private void ScanScene(string path, HashSet<string> knownKeys)
		{
			var opened = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
			try
			{
				var all = Resources.FindObjectsOfTypeAll<LocalizedText>()
					.Where(o => o != null && o.gameObject.scene == opened);
				foreach (var lt in all)
					CategorizeLocalizedText(lt, path, isScene: true, knownKeys);
			}
			finally
			{
				EditorSceneManager.CloseScene(opened, removeScene: true);
			}
		}

		private void CategorizeLocalizedText(LocalizedText lt, string sourcePath, bool isScene, HashSet<string> knownKeys)
		{
			var so = new SerializedObject(lt);
			var tk = so.FindProperty("_translationKey");
			var keyProp = tk?.FindPropertyRelative("_key");
			string key = keyProp?.stringValue ?? string.Empty;

			// Prefer the referenced TMP_Text, else component
			TMP_Text text = null;
			var textProp = so.FindProperty("_text");
			if (textProp != null && textProp.objectReferenceValue is TMP_Text tmpRef)
				text = tmpRef;
			if (text == null) text = lt.GetComponent<TMP_Text>();
			string defaultText = text != null ? text.text : string.Empty;

			// Build row identity info
			string objectPath = GetHierarchyPath(lt.transform);
			var pingTarget = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(sourcePath);

			// Determine invalid/empty key warnings (no add actions)
			if (string.IsNullOrWhiteSpace(key))
			{
				_warnings.Add(new WarningItem
				{
					SourcePath = sourcePath,
					IsScene = isScene,
					ObjectPath = objectPath,
					PingTarget = pingTarget,
					Reason = "No key"
				});
				return;
			}
			if (ContainsWhitespaceOrControl(key))
			{
				_warnings.Add(new WarningItem
				{
					SourcePath = sourcePath,
					IsScene = isScene,
					ObjectPath = objectPath,
					PingTarget = pingTarget,
					Reason = "Invalid key (contains whitespace)"
				});
				return;
			}

			// Otherwise, treat as candidate missing key if it's not present in any table asset
			if (!knownKeys.Contains(key))
			{
				var item = new WorkItem
				{
					SourcePath = sourcePath,
					IsScene = isScene,
					ObjectPath = objectPath,
					PingTarget = pingTarget,
					Key = key,
					DefaultText = defaultText
				};

				if (!_showIgnored && _ignored.Contains(item.PersistentId)) return;

				_work.Add(item);
			}
		}

		private static bool ContainsWhitespaceOrControl(string s)
		{
			for (int i = 0; i < s.Length; i++)
				if (char.IsWhiteSpace(s[i]) || char.IsControl(s[i]))
					return true;
			return false;
		}

		private static string GetHierarchyPath(Transform t)
		{
			var parts = new List<string>();
			while (t != null)
			{
				parts.Add(t.name);
				t = t.parent;
			}
			parts.Reverse();
			return string.Join("/", parts);
		}

		private void OnGUI()
		{
			DrawHeader();

			EditorGUILayout.Space();

			using (new EditorGUILayout.HorizontalScope())
			{
				// Big & colored Scan button
				var prevBg = GUI.backgroundColor;
				GUI.backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.20f, 0.55f, 0.95f) : new Color(0.25f, 0.50f, 1f);
				if (GUILayout.Button(new GUIContent("   Scan"), GUILayout.Height(32), GUILayout.Width(120)))
					Scan();
				GUI.backgroundColor = prevBg;

				GUILayout.FlexibleSpace();

				if (GUILayout.Button(new GUIContent(" Clear Results"), GUILayout.Height(26)))
				{
					_work.Clear();
					_warnings.Clear();
				}
			}

			EditorGUILayout.Space(8);

			// Bulk toolbar (only when we have actionable rows and table assets)
			if (_work.Count > 0 && _allTableAssets.Length > 0)
				DrawBulkToolbar();

			// Warnings first (no actions)
			DrawWarnings();

			// Actionable results
			DrawResults();

			GUILayout.FlexibleSpace();

			using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
			{
				GUILayout.FlexibleSpace();

				using (new EditorGUI.DisabledScope(_work.Count == 0))
				{
					// Colored only when there is work
					var prevBg = GUI.backgroundColor;
					if (_work.Count > 0)
						GUI.backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.70f, 0.35f) : new Color(0.30f, 0.75f, 0.40f);

					if (GUILayout.Button(new GUIContent(" Process"), GUILayout.Width(140), GUILayout.Height(28)))
						Process();

					GUI.backgroundColor = prevBg;
				}
			}
		}

		private void DrawHeader()
		{
			EditorGUILayout.LabelField("Search Folders", EditorStyles.boldLabel);

			for (int i = 0; i < _searchFolders.Count; i++)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					// big, flexible path column
					EditorGUILayout.LabelField(_searchFolders[i], GUILayout.ExpandWidth(true));

					if (GUILayout.Button(new GUIContent(" Select", EditorIcon.Select), GUILayout.Width(90)))
					{
						var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_searchFolders[i]);
						if (obj) Selection.activeObject = obj;
					}

					if (GUILayout.Button("Remove", GUILayout.Width(70)))
					{
						_searchFolders.RemoveAt(i);
						i--;
					}
				}
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Add Folder...", GUILayout.Width(120)))
				{
					var start = _searchFolders.LastOrDefault() ?? "Assets";
					var abs = EditorUtility.OpenFolderPanel("Select folder to scan", start, "");
					if (!string.IsNullOrEmpty(abs))
					{
						var project = Directory.GetParent(Application.dataPath)!.FullName.Replace('\\', '/');
						var absNorm = abs.Replace('\\', '/');
						if (absNorm.StartsWith(project, StringComparison.OrdinalIgnoreCase))
						{
							var rel = absNorm.Substring(project.Length + 1);
							if (!_searchFolders.Contains(rel))
								_searchFolders.Add(rel);
						}
						else
						{
							EditorUtility.DisplayDialog(Title, "Folder must be inside this Unity project.", "OK");
						}
					}
				}

				GUILayout.FlexibleSpace();

				_showIgnored = EditorGUILayout.ToggleLeft("Show ignored", _showIgnored, GUILayout.Width(120));

				if (GUILayout.Button("Default: Assets/", GUILayout.Width(140)))
				{
					_searchFolders.Clear();
					_searchFolders.Add("Assets");
				}
			}
		}

		private void DrawBulkToolbar()
		{
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("Bulk Actions", EditorStyles.boldLabel);

				using (new EditorGUILayout.HorizontalScope())
				{
					// Default destination popup
					EditorGUILayout.LabelField("Default destination", GUILayout.Width(130));
					_bulkDestinationIndex = EditorGUILayout.Popup(_bulkDestinationIndex, _allTableAssetNames, GUILayout.Width(260));
					if (_bulkDestinationIndex >= 0 && _bulkDestinationIndex < _allTableAssets.Length)
						_bulkDestination = _allTableAssets[_bulkDestinationIndex];

					GUILayout.Space(12);

					if (GUILayout.Button("Set all to Ignore", GUILayout.Width(150)))
						SetAllToIgnore();

					GUILayout.Space(8);

					using (new EditorGUI.DisabledScope(_bulkDestination == null))
					{
						if (GUILayout.Button("Set all to Add to", GUILayout.Width(150)))
							SetAllToAddTo(_bulkDestination);
					}

					GUILayout.Space(8);

					using (new EditorGUI.DisabledScope(_bulkDestination == null))
					{
						if (GUILayout.Button("Fill empty destinations", GUILayout.Width(170)))
							FillEmptyDestinations(_bulkDestination);
					}

					GUILayout.FlexibleSpace();
				}
			}
		}

		private void DrawWarnings()
		{
			if (_warnings.Count == 0)
				return;

			EditorGUILayout.Space(6);
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				// Header
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField($"Warnings ({_warnings.Count})", EditorStyles.boldLabel);
				}

				// Group and render
				foreach (var group in _warnings.GroupBy(w => w.SourcePath))
				{
					EditorGUILayout.Space(4);
					using (new EditorGUILayout.HorizontalScope())
					{
						var prevIcon = EditorGUIUtility.GetIconSize();
						EditorGUIUtility.SetIconSize(new Vector2(16f, 16f));
						var icon = group.First().IsScene ? EditorIcon.Scene : EditorIcon.Prefab;
						var gc = new GUIContent(Path.GetFileName(group.Key), icon, group.Key);
						GUILayout.Label(gc, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
						EditorGUIUtility.SetIconSize(prevIcon);

						if (GUILayout.Button(new GUIContent(" Select", EditorIcon.Select), GUILayout.Width(90)))
						{
							var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(group.Key);
							if (obj) Selection.activeObject = obj;
						}
					}

					// Column headers
					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.Space(8);
						GUILayout.Label("Object", EditorStyles.miniBoldLabel, GUILayout.Width(340));
						GUILayout.Label("Reason", EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
					}

					foreach (var item in group)
					{
						using (new EditorGUILayout.HorizontalScope())
						{
							GUILayout.Space(8);
							EditorGUILayout.LabelField(item.ObjectPath, GUILayout.Width(340));

							// Yellow-ish help style for reason
							var style = new GUIStyle(EditorStyles.label);
							style.normal.textColor = EditorGUIUtility.isProSkin ? new Color(1f, 0.85f, 0.25f) : new Color(0.7f, 0.45f, 0f);
							EditorGUILayout.LabelField(item.Reason, style, GUILayout.ExpandWidth(true));
						}
					}
				}
			}
		}

		private void DrawResults()
		{
			EditorGUILayout.LabelField($"Results ({_work.Count})", EditorStyles.boldLabel);

			if (_work.Count == 0)
			{
				EditorGUILayout.HelpBox("No missing keys found in the scanned folders.", MessageType.Info);
				return;
			}

			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			foreach (var group in _work.GroupBy(w => w.SourcePath))
			{
				EditorGUILayout.Space(6);
				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						// Ensure fixed icon size for the label (16x16)
						var prevIcon = EditorGUIUtility.GetIconSize();
						EditorGUIUtility.SetIconSize(new Vector2(16f, 16f));
						var icon = group.First().IsScene ? EditorIcon.Scene : EditorIcon.Prefab;
						var gc = new GUIContent(Path.GetFileName(group.Key), icon, group.Key);

						// Wide, flexible label for asset path/title
						GUILayout.Label(gc, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

						// Restore icon size
						EditorGUIUtility.SetIconSize(prevIcon);

						if (GUILayout.Button(new GUIContent(" Select", EditorIcon.Select), GUILayout.Width(90)))
						{
							var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(group.Key);
							if (obj) Selection.activeObject = obj;
						}
					}

					// Column headers
					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.Space(8);
						GUILayout.Label("Object", EditorStyles.miniBoldLabel, GUILayout.Width(320));
						GUILayout.Label("Key", EditorStyles.miniBoldLabel, GUILayout.Width(220));
						GUILayout.Label("Default Text", EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
						GUILayout.Label("Action", EditorStyles.miniBoldLabel, GUILayout.Width(95));
						GUILayout.Label("Destination", EditorStyles.miniBoldLabel, GUILayout.Width(240));
					}

					foreach (var item in group)
					{
						using (new EditorGUILayout.HorizontalScope())
						{
							GUILayout.Space(8);

							// Object path � wider now
							EditorGUILayout.LabelField(item.ObjectPath, GUILayout.Width(320));

							// Key
							EditorGUILayout.LabelField(item.Key, EditorStyles.miniBoldLabel, GUILayout.Width(220));

							// Default TMP text (read-only)
							using (new EditorGUI.DisabledScope(true))
							{
								EditorGUILayout.TextField(item.DefaultText ?? string.Empty, GUILayout.ExpandWidth(true));
							}

							// Action
							item.Action = (WorkItem.ActionType)EditorGUILayout.EnumPopup(item.Action, GUILayout.Width(95));

							// Destination (when AddTo)
							using (new EditorGUI.DisabledScope(item.Action != WorkItem.ActionType.AddTo))
							{
								int idx = Array.IndexOf(_allTableAssets, item.Destination);
								if (idx < 0) idx = _bulkDestinationIndex; // fall back to bulk selection
								int next = EditorGUILayout.Popup(idx, _allTableAssetNames, GUILayout.Width(240));
								if (next >= 0 && next < _allTableAssets.Length)
									item.Destination = _allTableAssets[next];
							}
						}
					}
				}
			}

			EditorGUILayout.EndScrollView();
		}

		private void SetAllToIgnore()
		{
			foreach (var wi in _work)
				wi.Action = WorkItem.ActionType.Ignore;
			Repaint();
		}

		private void SetAllToAddTo(TranslationTableAsset dest)
		{
			foreach (var wi in _work)
			{
				wi.Action = WorkItem.ActionType.AddTo;
				wi.Destination = dest;
			}
			Repaint();
		}

		private void FillEmptyDestinations(TranslationTableAsset dest)
		{
			foreach (var wi in _work)
			{
				if (wi.Action == WorkItem.ActionType.AddTo && wi.Destination == null)
					wi.Destination = dest;
			}
			Repaint();
		}

		private void Process()
		{
			int added = 0;
			int newlyIgnored = 0;

			// Persist ignores
			foreach (var item in _work)
			{
				if (item.Action == WorkItem.ActionType.Ignore)
				{
					if (_ignored.Add(item.PersistentId))
						newlyIgnored++;
				}
			}
			if (newlyIgnored > 0) SaveIgnored();

			// Add keys per destination, seeding default language
			foreach (var item in _work)
			{
				if (item.Action != WorkItem.ActionType.AddTo) continue;
				if (item.Destination == null) continue;

				var created = CreateItemForAdd(item.Key, _defaultLanguage, item.DefaultText ?? string.Empty);
				if (created != null && item.Destination.TryAddItem(created, addMissingLanguages: true))
					added++;
			}

			if (added > 0)
				AssetDatabase.SaveAssets();

			EditorUtility.DisplayDialog(Title, $"Added {added} key(s). Ignored {newlyIgnored}.", "OK");
			Scan(); // refresh after processing (respects "Show ignored")
		}

		private static TranslationTableAsset.TranslationItem CreateItemForAdd(string key, Language defaultLanguage, string defaultText)
		{
			if (string.IsNullOrWhiteSpace(key)) return null;

			var item = TranslationTableAsset.TranslationItem.Create(key);
			item.SetTranslation(defaultLanguage, defaultText ?? string.Empty); // seed default language
			return item;
		}
	}
}
#endif

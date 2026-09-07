using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlueCheese.App;
using BlueCheese.Core.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.Tools.Editor
{
	/// <summary>
	/// UI Toolkit scanner that finds every <see cref="TranslationKey"/> used on any component whose key is
	/// missing from every table (or invalid), and lets you add the keys to a chosen table (seeding the
	/// default-language text) or ignore them.
	/// </summary>
	public class MissingLocalizationKeysWindow : EditorWindow
	{
		private const string Title = "Missing Localization Keys";
		private const string PrefsKeyIgnored = "BlueCheese.MissingKeys.Ignored";

		private class WorkItem
		{
			public string SourcePath;
			public bool IsScene;
			public string ObjectPath;
			public string Key;
			public string DefaultText;
			public bool Ignore;                       // true => ignore, false => add to Destination
			public TranslationTableAsset Destination;

			public string PersistentId => $"{SourcePath}|{ObjectPath}|{Key}";
		}

		private class WarningItem
		{
			public string SourcePath;
			public bool IsScene;
			public string ObjectPath;
			public string Reason;
		}

		private readonly List<string> _searchFolders = new();
		private readonly List<WorkItem> _work = new();
		private readonly List<WarningItem> _warnings = new();
		private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
		private HashSet<string> _ignored = new(StringComparer.Ordinal);
		private bool _showIgnored;
		private string _filter = string.Empty;
		private bool _hasScanned;

		private TranslationTableAsset[] _allTableAssets = Array.Empty<TranslationTableAsset>();
		private string[] _allTableAssetNames = Array.Empty<string>();
		private Language _defaultLanguage = Language.English;
		private TranslationTableAsset _bulkDestination;

		// UI references
		private VisualElement _foldersContainer;
		private Label _summaryLabel;
		private VisualElement _bulkBar;
		private ScrollView _resultsScroll;
		private Button _processButton;

		[MenuItem("Tools/Localization/Missing Keys Scanner")]
		public static void Open()
		{
			var window = GetWindow<MissingLocalizationKeysWindow>();
			window.titleContent = new GUIContent(Title);
			window.minSize = new Vector2(720, 480);
			window.Show();
		}

		#region Lifecycle

		private void OnEnable()
		{
			titleContent = new GUIContent(Title);
			if (_searchFolders.Count == 0)
			{
				_searchFolders.Add("Assets");
			}
			LoadIgnored();
			RefreshTableCache();
		}

		private void CreateGUI()
		{
			var root = rootVisualElement;
			root.style.flexGrow = 1;

			root.Add(BuildToolbar());
			_foldersContainer = new VisualElement { style = { flexShrink = 0, marginTop = 4 } };
			root.Add(_foldersContainer);

			_summaryLabel = new Label { style = { flexShrink = 0, marginLeft = 6, marginTop = 4, marginBottom = 2, opacity = 0.8f } };
			root.Add(_summaryLabel);

			_bulkBar = new VisualElement { style = { flexShrink = 0 } };
			root.Add(_bulkBar);

			_resultsScroll = new ScrollView { style = { flexGrow = 1, marginTop = 4 } };
			root.Add(_resultsScroll);

			root.Add(BuildFooter());

			RefreshFolders();
			RefreshBulkBar();
			RefreshResults();
			RefreshSummary();
			UpdateProcessButton();
		}

		#endregion

		#region Chrome

		private VisualElement BuildToolbar()
		{
			var bar = new VisualElement { style = { flexShrink = 0, flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 6, paddingRight = 6, paddingTop = 6, paddingBottom = 4 } };

			var scan = new Button(Scan) { text = "Scan", style = { height = 26, width = 110, unityFontStyleAndWeight = FontStyle.Bold } };
			scan.style.backgroundColor = new Color(0.22f, 0.5f, 0.85f);
			scan.style.color = Color.white;
			bar.Add(scan);

			var clear = new Button(ClearResults) { text = "Clear", style = { height = 26, marginLeft = 4 } };
			bar.Add(clear);

			var searchIcon = new Image { image = EditorIcon.Search, style = { width = 16, height = 16, marginLeft = 10, marginRight = 2 } };
			bar.Add(searchIcon);
			var filter = new TextField { value = _filter, tooltip = "Filter results by object, key or text", style = { flexGrow = 1, marginRight = 8 } };
			filter.RegisterValueChangedCallback(evt => { _filter = evt.newValue; RefreshResults(); });
			bar.Add(filter);

			var showIgnored = new Toggle("Show ignored") { value = _showIgnored, style = { flexShrink = 0 } };
			showIgnored.RegisterValueChangedCallback(evt =>
			{
				_showIgnored = evt.newValue;
				if (_hasScanned) Scan(); // re-scan so ignored rows appear/disappear
			});
			bar.Add(showIgnored);

			return bar;
		}

		private void RefreshFolders()
		{
			_foldersContainer.Clear();

			var box = new VisualElement { style = { paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 4, marginLeft = 4, marginRight = 4, backgroundColor = new Color(0, 0, 0, 0.12f), borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } };

			var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
			header.Add(new Label("Search folders") { style = { unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 } });
			header.Add(new Button(AddFolder) { text = "Add folder…", style = { marginRight = 4 } });
			header.Add(new Button(() => { _searchFolders.Clear(); _searchFolders.Add("Assets"); RefreshFolders(); }) { text = "Reset (Assets)" });
			box.Add(header);

			foreach (var folder in _searchFolders.ToList())
			{
				var captured = folder;
				var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 2 } };
				row.Add(new Label(captured) { tooltip = captured, style = { flexGrow = 1, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis } });
				row.Add(new Button(() => PingAsset(captured)) { text = "Select", style = { width = 70, marginRight = 4 } });
				var remove = new Button(() => { _searchFolders.Remove(captured); if (_searchFolders.Count == 0) _searchFolders.Add("Assets"); RefreshFolders(); }) { text = "×", style = { width = 24 } };
				row.Add(remove);
				box.Add(row);
			}

			_foldersContainer.Add(box);
		}

		private void RefreshBulkBar()
		{
			_bulkBar.Clear();
			if (_work.Count == 0)
			{
				_bulkBar.style.display = DisplayStyle.None;
				return;
			}
			_bulkBar.style.display = DisplayStyle.Flex;

			var bar = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexWrap = Wrap.Wrap, paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 4 } };
			bar.Add(new Label("Bulk:") { style = { marginRight = 6, opacity = 0.7f } });

			var hasTables = _allTableAssets.Length > 0;
			if (hasTables)
			{
				var destination = new DropdownField { choices = _allTableAssetNames.ToList(), style = { width = 220, marginRight = 6 } };
				destination.index = Mathf.Max(0, Array.IndexOf(_allTableAssets, _bulkDestination));
				destination.RegisterValueChangedCallback(_ =>
				{
					if (destination.index >= 0 && destination.index < _allTableAssets.Length)
					{
						_bulkDestination = _allTableAssets[destination.index];
					}
				});
				bar.Add(destination);

				bar.Add(new Button(() =>
				{
					foreach (var w in VisibleWork()) { w.Ignore = false; w.Destination = _bulkDestination; }
					RefreshResults();
					UpdateProcessButton();
				}) { text = "Assign all to table", style = { marginRight = 4 } });
			}
			else
			{
				bar.Add(new Label("No translation table found — create one to add keys.") { style = { opacity = 0.7f, marginRight = 8 } });
			}

			bar.Add(new Button(() =>
			{
				foreach (var w in VisibleWork()) { w.Ignore = true; w.Destination = null; }
				RefreshResults();
				UpdateProcessButton();
			}) { text = "Ignore all" });

			_bulkBar.Add(bar);
		}

		private VisualElement BuildFooter()
		{
			var footer = new VisualElement { style = { flexShrink = 0, flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, alignItems = Align.Center, paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 6, borderTopWidth = 1, borderTopColor = new Color(0, 0, 0, 0.3f) } };
			_processButton = new Button(Process) { text = "Process", style = { width = 150, height = 28 } };
			footer.Add(_processButton);
			return footer;
		}

		#endregion

		#region Results UI

		private IEnumerable<WorkItem> VisibleWork() => _work.Where(PassesFilter);

		private bool PassesFilter(WorkItem w)
		{
			if (string.IsNullOrWhiteSpace(_filter))
			{
				return true;
			}
			return Contains(w.Key) || Contains(w.ObjectPath) || Contains(w.DefaultText) || Contains(Path.GetFileName(w.SourcePath));

			bool Contains(string s) => !string.IsNullOrEmpty(s) && s.Contains(_filter, StringComparison.OrdinalIgnoreCase);
		}

		private void RefreshResults()
		{
			_resultsScroll.Clear();

			if (!_hasScanned)
			{
				_resultsScroll.Add(Hint("Choose folders and press Scan."));
				return;
			}

			// Warnings
			var warnings = _warnings.Where(w => string.IsNullOrWhiteSpace(_filter) || (w.ObjectPath?.Contains(_filter, StringComparison.OrdinalIgnoreCase) ?? false) || Path.GetFileName(w.SourcePath).Contains(_filter, StringComparison.OrdinalIgnoreCase)).ToList();
			if (warnings.Count > 0)
			{
				var warnBox = Section($"Warnings ({warnings.Count})", new Color(0.5f, 0.4f, 0.1f, 0.18f));
				foreach (var group in warnings.GroupBy(w => w.SourcePath))
				{
					warnBox.Add(AssetHeader(group.Key, group.First().IsScene, group.Select(w => w.ObjectPath)));
					foreach (var item in group)
					{
						var warnItem = item;
						var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 12 } };
						row.Add(ObjectLink(warnItem.ObjectPath, $"Select GameObject in the hierarchy:\n{warnItem.ObjectPath}", () => SelectReference(warnItem.SourcePath, warnItem.IsScene, warnItem.ObjectPath)));
						row.Add(new Label(warnItem.Reason) { style = { width = 240, color = new Color(0.95f, 0.8f, 0.35f) } });
						warnBox.Add(row);
					}
				}
				_resultsScroll.Add(warnBox);
			}

			// Missing keys
			var work = VisibleWork().ToList();
			if (work.Count == 0 && warnings.Count == 0)
			{
				_resultsScroll.Add(Hint(_work.Count == 0 && _warnings.Count == 0
					? "No missing keys found in the scanned folders. 🎉"
					: "No result matches the filter."));
				return;
			}

			if (work.Count == 0)
			{
				return;
			}

			var box = Section($"Missing keys ({work.Count})", new Color(0, 0, 0, 0.12f));
			foreach (var group in work.GroupBy(w => w.SourcePath))
			{
				box.Add(AssetHeader(group.Key, group.First().IsScene, group.Select(w => w.ObjectPath)));

				var columns = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingLeft = 12, opacity = 0.6f } };
				columns.Add(new Label("Object") { style = { flexGrow = 1, flexBasis = 0 } });
				columns.Add(new Label("Key") { style = { width = 200 } });
				columns.Add(new Label("Default text") { style = { flexGrow = 1, flexBasis = 0 } });
				columns.Add(new Label("Action") { style = { width = 224 } });
				box.Add(columns);

				foreach (var item in group)
				{
					box.Add(BuildWorkRow(item));
				}
			}
			_resultsScroll.Add(box);
		}

		private VisualElement BuildWorkRow(WorkItem item)
		{
			var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 12, paddingTop = 1, paddingBottom = 1 } };

			row.Add(ObjectLink(item.ObjectPath, $"Select GameObject in the hierarchy:\n{item.ObjectPath}", () => SelectReference(item.SourcePath, item.IsScene, item.ObjectPath)));
			row.Add(new Label(item.Key) { tooltip = item.Key, style = { width = 200, unityFontStyleAndWeight = FontStyle.Bold, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis } });

			var defaultText = new TextField { value = item.DefaultText ?? string.Empty, isReadOnly = true, style = { flexGrow = 1, flexBasis = 0 } };
			row.Add(defaultText);

			// One dropdown merges the action + destination: "Ignore" or a table name.
			var choices = new List<string> { "Ignore" };
			choices.AddRange(_allTableAssetNames);
			var action = new DropdownField { choices = choices, style = { width = 220, marginLeft = 4 } };
			action.index = ChoiceIndex(item);
			action.RegisterValueChangedCallback(evt =>
			{
				int idx = action.index;
				if (idx <= 0)
				{
					item.Ignore = true;
					item.Destination = null;
				}
				else
				{
					item.Ignore = false;
					item.Destination = _allTableAssets[idx - 1];
				}
				UpdateProcessButton();
			});
			row.Add(action);

			return row;
		}

		private int ChoiceIndex(WorkItem item)
		{
			if (item.Ignore || item.Destination == null)
			{
				return 0;
			}
			int idx = Array.IndexOf(_allTableAssets, item.Destination);
			return idx >= 0 ? idx + 1 : 0;
		}

		private VisualElement AssetHeader(string sourcePath, bool isScene, IEnumerable<string> objectPaths)
		{
			var paths = objectPaths.ToList();
			var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 6 } };
			header.Add(new Image { image = isScene ? EditorIcon.Scene : EditorIcon.Prefab, style = { width = 16, height = 16, marginRight = 4 } });
			header.Add(new Label(Path.GetFileName(sourcePath)) { tooltip = sourcePath, style = { unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 } });
			header.Add(new Button(() => SelectReferences(sourcePath, isScene, paths))
			{
				text = "Select",
				tooltip = "Select the GameObject(s) in the open scene/prefab, or ping the asset in the Project",
				style = { width = 70 },
			});
			return header;
		}

		private static VisualElement Section(string title, Color background)
		{
			var box = new VisualElement { style = { marginLeft = 4, marginRight = 4, marginBottom = 6, paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 6, backgroundColor = background, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } };
			box.Add(new Label(title) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
			return box;
		}

		private static Label Hint(string text) => new(text) { style = { whiteSpace = WhiteSpace.Normal, opacity = 0.6f, marginLeft = 8, marginTop = 8 } };

		private void RefreshSummary()
		{
			if (!_hasScanned)
			{
				_summaryLabel.text = string.Empty;
				return;
			}
			_summaryLabel.text = $"{_work.Count} missing key(s) • {_warnings.Count} warning(s) • {_ignored.Count} ignored";
		}

		private void UpdateProcessButton()
		{
			int toAdd = _work.Count(w => !w.Ignore && w.Destination != null);
			int toIgnore = _work.Count(w => w.Ignore);
			_processButton.SetEnabled(_work.Count > 0);
			_processButton.text = _work.Count == 0 ? "Process" : $"Process ({toAdd} add, {toIgnore} ignore)";
		}

		#endregion

		#region Scan

		private void ClearResults()
		{
			_work.Clear();
			_warnings.Clear();
			_hasScanned = false;
			RefreshResults();
			RefreshBulkBar();
			RefreshSummary();
			UpdateProcessButton();
		}

		private void Scan()
		{
			_work.Clear();
			_warnings.Clear();
			_seen.Clear();
			RefreshTableCache();
			_defaultLanguage = TranslationAssetFinder.FindDefaultLanguage();

			var knownKeys = TranslationAssetFinder.GetAllKeys();
			var searchIn = _searchFolders.ToArray();
			var prefabGuids = AssetDatabase.FindAssets("t:Prefab", searchIn);
			var sceneGuids = AssetDatabase.FindAssets("t:Scene", searchIn);
			int total = prefabGuids.Length + sceneGuids.Length;
			int done = 0;

			try
			{
				foreach (var guid in prefabGuids)
				{
					var path = AssetDatabase.GUIDToAssetPath(guid);
					if (EditorUtility.DisplayCancelableProgressBar(Title, $"Scanning {Path.GetFileName(path)}", total == 0 ? 1f : (float)done / total))
					{
						break;
					}
					ScanPrefab(path, knownKeys);
					done++;
				}
				foreach (var guid in sceneGuids)
				{
					var path = AssetDatabase.GUIDToAssetPath(guid);
					if (EditorUtility.DisplayCancelableProgressBar(Title, $"Scanning {Path.GetFileName(path)}", total == 0 ? 1f : (float)done / total))
					{
						break;
					}
					ScanScene(path, knownKeys);
					done++;
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			_work.Sort(CompareWork);
			_warnings.Sort(CompareWarning);
			_hasScanned = true;

			// Default: already-ignored items stay ignored; the rest target the bulk destination when a table exists.
			foreach (var item in _work)
			{
				if (_ignored.Contains(item.PersistentId) || _bulkDestination == null)
				{
					item.Ignore = true;
					item.Destination = null;
				}
				else
				{
					item.Ignore = false;
					item.Destination = _bulkDestination;
				}
			}

			RefreshBulkBar();
			RefreshResults();
			RefreshSummary();
			UpdateProcessButton();
		}

		private static int CompareWork(WorkItem a, WorkItem b)
		{
			int c = string.CompareOrdinal(a.SourcePath, b.SourcePath);
			if (c != 0) return c;
			c = string.CompareOrdinal(a.ObjectPath, b.ObjectPath);
			return c != 0 ? c : string.CompareOrdinal(a.Key, b.Key);
		}

		private static int CompareWarning(WarningItem a, WarningItem b)
		{
			int c = string.CompareOrdinal(a.SourcePath, b.SourcePath);
			if (c != 0) return c;
			c = string.CompareOrdinal(a.ObjectPath, b.ObjectPath);
			return c != 0 ? c : string.CompareOrdinal(a.Reason, b.Reason);
		}

		private void ScanPrefab(string path, HashSet<string> knownKeys)
		{
			var root = PrefabUtility.LoadPrefabContents(path);
			try
			{
				foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
				{
					ScanComponent(component, path, isScene: false, knownKeys);
				}
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		private void ScanScene(string path, HashSet<string> knownKeys)
		{
			var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
			bool wasOpen = scene.IsValid() && scene.isLoaded;
			if (!wasOpen)
			{
				scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
			}
			try
			{
				foreach (var root in scene.GetRootGameObjects())
				{
					foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
					{
						ScanComponent(component, path, isScene: true, knownKeys);
					}
				}
			}
			finally
			{
				if (!wasOpen)
				{
					EditorSceneManager.CloseScene(scene, removeScene: true);
				}
			}
		}

		// Walks every serialized property of the component and categorizes each TranslationKey it finds
		// (including keys nested in arrays, lists or sub-structs), not just LocalizedText components.
		private void ScanComponent(MonoBehaviour component, string sourcePath, bool isScene, HashSet<string> knownKeys)
		{
			if (component == null)
			{
				return; // missing script
			}

			var so = new SerializedObject(component);
			var iterator = so.GetIterator();
			bool enterChildren = true;
			while (iterator.NextVisible(enterChildren))
			{
				enterChildren = true;
				if (iterator.propertyType == SerializedPropertyType.Generic && !iterator.isArray && iterator.type == nameof(TranslationKey))
				{
					CategorizeTranslationKey(iterator, component, sourcePath, isScene, knownKeys);
					enterChildren = false; // skip the key's own children (_key, _pluralKey, _parameters)
				}
			}
		}

		private void CategorizeTranslationKey(SerializedProperty keyProperty, MonoBehaviour component, string sourcePath, bool isScene, HashSet<string> knownKeys)
		{
			var key = keyProperty.FindPropertyRelative("_key")?.stringValue ?? string.Empty;
			string objectPath = GetHierarchyPath(component.transform);

			if (string.IsNullOrWhiteSpace(key))
			{
				AddWarning(sourcePath, isScene, objectPath, "No key");
				return;
			}
			if (ContainsWhitespaceOrControl(key))
			{
				AddWarning(sourcePath, isScene, objectPath, "Invalid key (whitespace)");
				return;
			}
			if (knownKeys.Contains(key))
			{
				return; // already translated
			}

			var item = new WorkItem { SourcePath = sourcePath, IsScene = isScene, ObjectPath = objectPath, Key = key, DefaultText = ResolveDefaultText(component) };
			if (!_showIgnored && _ignored.Contains(item.PersistentId))
			{
				return;
			}
			if (_seen.Add(item.PersistentId))
			{
				_work.Add(item);
			}
		}

		private void AddWarning(string sourcePath, bool isScene, string objectPath, string reason)
		{
			if (_seen.Add($"W|{sourcePath}|{objectPath}|{reason}"))
			{
				_warnings.Add(new WarningItem { SourcePath = sourcePath, IsScene = isScene, ObjectPath = objectPath, Reason = reason });
			}
		}

		// Best-effort default-language text to seed a new key: the component's driven text field (e.g.
		// LocalizedText._text) when present, otherwise the nearest TMP_Text.
		private static string ResolveDefaultText(MonoBehaviour component)
		{
			var so = new SerializedObject(component);
			if (so.FindProperty("_text")?.objectReferenceValue is TMP_Text referenced)
			{
				return referenced.text;
			}
			var tmp = component.GetComponentInChildren<TMP_Text>(true);
			return tmp != null ? tmp.text : string.Empty;
		}

		#endregion

		#region Process

		private void Process()
		{
			int added = 0;
			int newlyIgnored = 0;
			bool ignoredChanged = false;

			foreach (var item in _work)
			{
				if (item.Ignore)
				{
					if (_ignored.Add(item.PersistentId))
					{
						newlyIgnored++;
						ignoredChanged = true;
					}
				}
				else if (_ignored.Remove(item.PersistentId))
				{
					// The user un-ignored this row (picked a table) — drop the stale entry.
					ignoredChanged = true;
				}
			}
			if (ignoredChanged)
			{
				SaveIgnored();
			}

			foreach (var item in _work)
			{
				if (item.Ignore || item.Destination == null)
				{
					continue;
				}
				var created = TranslationTableAsset.TranslationItem.Create(item.Key);
				created.SetTranslation(_defaultLanguage, item.DefaultText ?? string.Empty);
				if (item.Destination.TryAddItem(created, addMissingLanguages: true))
				{
					added++;
				}
			}

			if (added > 0)
			{
				AssetDatabase.SaveAssets();
			}

			EditorUtility.DisplayDialog(Title, $"Added {added} key(s). Ignored {newlyIgnored}.", "OK");
			Scan();
		}

		#endregion

		#region Helpers

		private void RefreshTableCache()
		{
			_allTableAssets = TranslationAssetFinder.FindAllTables().ToArray();
			_allTableAssetNames = _allTableAssets.Select(a => a.Name).ToArray();
			if (_bulkDestination == null || Array.IndexOf(_allTableAssets, _bulkDestination) < 0)
			{
				_bulkDestination = _allTableAssets.FirstOrDefault();
			}
		}

		private void AddFolder()
		{
			var start = _searchFolders.LastOrDefault() ?? "Assets";
			var absolute = EditorUtility.OpenFolderPanel("Select a folder to scan", start, string.Empty);
			if (string.IsNullOrEmpty(absolute))
			{
				return;
			}
			var project = Directory.GetParent(Application.dataPath)!.FullName.Replace('\\', '/');
			var normalized = absolute.Replace('\\', '/');
			if (!normalized.StartsWith(project, StringComparison.OrdinalIgnoreCase))
			{
				EditorUtility.DisplayDialog(Title, "The folder must be inside this Unity project.", "OK");
				return;
			}
			var relative = normalized.Substring(project.Length + 1);
			if (!_searchFolders.Contains(relative))
			{
				_searchFolders.Add(relative);
				RefreshFolders();
			}
		}

		private static void PingAsset(string assetPath)
		{
			var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
			if (asset != null)
			{
				Selection.activeObject = asset;
				EditorGUIUtility.PingObject(asset);
			}
		}

		// A flat, left-aligned "link" button used to make an object path clickable.
		private static Button ObjectLink(string text, string tooltip, Action onClick)
		{
			return new Button(onClick)
			{
				text = text,
				tooltip = tooltip,
				style =
				{
					flexGrow = 1, flexBasis = 0,
					unityTextAlign = TextAnchor.MiddleLeft,
					backgroundColor = Color.clear,
					borderTopWidth = 0, borderBottomWidth = 0, borderLeftWidth = 0, borderRightWidth = 0,
					marginTop = 0, marginBottom = 0, marginLeft = 0, marginRight = 0,
					paddingLeft = 2, paddingRight = 2,
					overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis,
				},
			};
		}

		// Selects the GameObject that uses the key when its scene/prefab is currently open, so the user
		// lands directly on it in the hierarchy. Falls back to pinging the asset otherwise.
		private static void SelectReference(string sourcePath, bool isScene, string objectPath)
		{
			var roots = GetOpenRoots(sourcePath, isScene);
			var go = roots != null ? FindByPath(roots, objectPath) : null;
			if (go != null)
			{
				Selection.activeGameObject = go;
				EditorGUIUtility.PingObject(go);
				return;
			}
			PingAsset(sourcePath);
		}

		// Selects every listed GameObject that resolves in the open scene/prefab; pings the asset if none
		// resolve (e.g. the scene/prefab is not currently open).
		private static void SelectReferences(string sourcePath, bool isScene, List<string> objectPaths)
		{
			var roots = GetOpenRoots(sourcePath, isScene);
			if (roots != null)
			{
				var found = objectPaths
					.Select(p => FindByPath(roots, p))
					.Where(g => g != null)
					.Distinct()
					.Cast<UnityEngine.Object>()
					.ToArray();
				if (found.Length > 0)
				{
					Selection.objects = found;
					EditorGUIUtility.PingObject(found[0]);
					return;
				}
			}
			PingAsset(sourcePath);
		}

		// Returns the root GameObjects of the source when it is currently open (a loaded scene, or a prefab
		// open in Prefab Mode), otherwise null.
		private static GameObject[] GetOpenRoots(string sourcePath, bool isScene)
		{
			if (isScene)
			{
				var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(sourcePath);
				return scene.IsValid() && scene.isLoaded ? scene.GetRootGameObjects() : null;
			}

			var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null && stage.prefabContentsRoot != null && NormalizePath(stage.assetPath) == NormalizePath(sourcePath))
			{
				return new[] { stage.prefabContentsRoot };
			}
			return null;
		}

		private static string NormalizePath(string path) => string.IsNullOrEmpty(path) ? path : path.Replace('\\', '/');

		private static GameObject FindByPath(GameObject[] roots, string objectPath)
		{
			if (string.IsNullOrEmpty(objectPath) || roots == null)
			{
				return null;
			}
			var parts = objectPath.Split('/');
			var current = roots.FirstOrDefault(r => r != null && r.name == parts[0]);
			for (int i = 1; i < parts.Length && current != null; i++)
			{
				current = FindChildByName(current.transform, parts[i]);
			}
			return current;
		}

		private static GameObject FindChildByName(Transform parent, string name)
		{
			foreach (Transform child in parent)
			{
				if (child.name == name)
				{
					return child.gameObject;
				}
			}
			return null;
		}

		private void LoadIgnored()
		{
			_ignored.Clear();
			var raw = EditorPrefs.GetString(PrefsKeyIgnored, string.Empty);
			if (string.IsNullOrEmpty(raw))
			{
				return;
			}
			foreach (var token in raw.Split('|'))
			{
				if (!string.IsNullOrWhiteSpace(token))
				{
					_ignored.Add(token);
				}
			}
		}

		private void SaveIgnored() => EditorPrefs.SetString(PrefsKeyIgnored, string.Join("|", _ignored));

		private static bool ContainsWhitespaceOrControl(string s)
		{
			foreach (var c in s)
			{
				if (char.IsWhiteSpace(c) || char.IsControl(c))
				{
					return true;
				}
			}
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

		#endregion
	}
}

using BlueCheese.Core;
using BlueCheese.Core.Editor;
using BlueCheese.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using TItem = BlueCheese.App.TranslationTableAsset.TranslationItem;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// UI Toolkit editor window to author <see cref="TranslationTableAsset"/>s.
	/// Several tables can be open at once, one per tab. Within a table the list shows the
	/// key, its status icon and its default-language translation (read-only); selecting a
	/// row opens a side panel to edit it. Languages are managed from the top toolbar and
	/// the selection/add-key bars are pinned to the bottom so they stay reachable.
	/// </summary>
	public class TranslationTableWindow : EditorWindow
	{
		private const string WindowTitle = "Translation Editor";
		private const string AllStatuses = "All";

		[MenuItem("Tools/Localization/Translation Editor")]
		public static void OpenEditor()
		{
			var window = GetWindow<TranslationTableWindow>();
			window.titleContent = new GUIContent(WindowTitle);
			window.minSize = new Vector2(1200, 640);
			window.RebuildUI();
			window.Show();
		}

		public static void Open(TranslationTableAsset asset, string selectKey = null)
		{
			var window = GetWindow<TranslationTableWindow>();
			window.titleContent = new GUIContent(WindowTitle);
			window.minSize = new Vector2(1200, 640);
			window.OpenTable(asset, selectKey);
			window.Show();
		}

		/// <summary>Per-tab state for one open table.</summary>
		private class TableTab
		{
			public TranslationTableAsset Asset;
			public string SearchText = string.Empty;
			public string StatusFilter = AllStatuses;
			public readonly HashSet<string> SelectedKeys = new();
			public TItem EditingItem;
		}

		private readonly List<TableTab> _tabs = new();
		private TableTab _active;

		// Persisted across domain reloads so open tabs survive recompiles (transient
		// per-tab state such as search/selection is intentionally not restored).
		[SerializeField] private List<TranslationTableAsset> _persistedAssets = new();
		[SerializeField] private int _persistedActiveIndex;

		private IReadOnlyList<Language> _supportedLanguages = Array.Empty<Language>();
		private Language _defaultLanguage = Language.English;

		// Search text for the empty-state table list (filters by key/translation content).
		private string _tableListSearch = string.Empty;

		private readonly List<TItem> _filtered = new();

		// UI references (rebuilt for the active tab)
		private VisualElement _languagesBar;
		private VisualElement _bulkBar;
		private VisualElement _detailPanel;
		private ListView _listView;
		private Toggle _selectAllToggle;
		private Label _countLabel;
		private Label _keyHeaderLabel;
		private TextField _keyField;
		private Label _keyLoader;

		// Resizable "Key" column width, persisted per user.
		private const string KeyColumnWidthPref = "BlueCheese.TranslationEditor.KeyColumnWidth";
		private const float MinColumnWidth = 80f;
		private float _keyColumnWidth = 240f;

		// Inline reference-scan state.
		private bool _scanning;
		private EditorApplication.CallbackFunction _scanStep;

		// Transient AI alternatives for the item currently shown in the detail panel.
		private TItem _alternativesItem;
		private readonly Dictionary<Language, List<string>> _alternatives = new();

		// Bulk "Translate selected with AI" progress state.
		private bool _bulkTranslating;
		private int _bulkTotal;
		private int _bulkDone;
		private string _bulkCurrentKey;                       // key currently being translated (row spinner)
		private int _bulkSpinnerFrame;
		private EditorApplication.CallbackFunction _bulkSpinnerTick;

		private TranslationTableAsset Asset => _active.Asset;

		#region Lifecycle

		private void OnEnable()
		{
			titleContent = new GUIContent(WindowTitle);
			Undo.undoRedoPerformed += OnUndoRedo;
			_keyColumnWidth = Mathf.Max(MinColumnWidth, EditorPrefs.GetFloat(KeyColumnWidthPref, 240f));

			// Restore tabs after a domain reload.
			if (_tabs.Count == 0 && _persistedAssets != null)
			{
				foreach (var asset in _persistedAssets)
				{
					if (asset != null)
					{
						_tabs.Add(new TableTab { Asset = asset });
					}
				}
				if (_tabs.Count > 0)
				{
					_active = _tabs[Mathf.Clamp(_persistedActiveIndex, 0, _tabs.Count - 1)];
				}
			}
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= OnUndoRedo;
			StopScan();
			StopBulkSpinner();
			ClearAlternatives();
		}

		private void ClearAlternatives()
		{
			_alternativesItem = null;
			_alternatives.Clear();
		}

		private void CreateGUI() => RebuildUI();

		public void OpenTable(TranslationTableAsset asset, string selectKey = null)
		{
			if (asset == null)
			{
				return;
			}
			var tab = _tabs.FirstOrDefault(t => t.Asset == asset);
			if (tab == null)
			{
				tab = new TableTab { Asset = asset };
				_tabs.Add(tab);
			}
			_active = tab;

			// Optionally select a specific key: clear filters so its row is visible, then edit it.
			if (!string.IsNullOrEmpty(selectKey))
			{
				asset.Validate();
				var item = asset.Items.FirstOrDefault(i => i.Key == selectKey);
				if (item != null)
				{
					tab.SearchText = string.Empty;
					tab.StatusFilter = AllStatuses;
					tab.EditingItem = item;
				}
			}

			RebuildUI();

			if (!string.IsNullOrEmpty(selectKey) && _active.EditingItem != null && _listView != null)
			{
				int index = _filtered.IndexOf(_active.EditingItem);
				if (index >= 0)
				{
					_listView.ScrollToItem(index);
				}
			}
		}

		private void OnUndoRedo()
		{
			if (_active == null || _listView == null)
			{
				return;
			}
			// Drop the edited item if the undo removed it from the table.
			if (_active.EditingItem != null && !Asset.Items.Contains(_active.EditingItem))
			{
				_active.EditingItem = null;
			}
			RefreshLanguages();
			RefreshList();
			ShowDetail();
			RefreshBulkBar();
		}

		#endregion

		#region Build

		private void RebuildUI()
		{
			StopScan();
			ClearAlternatives();

			var root = rootVisualElement;
			root.Clear();
			root.style.flexGrow = 1;

			// Drop tabs whose asset was destroyed/unloaded.
			_tabs.RemoveAll(t => t.Asset == null);
			if (_active != null && !_tabs.Contains(_active))
			{
				_active = _tabs.FirstOrDefault();
			}
			SyncPersistence();

			// No tab open: show the project's tables to pick from.
			if (_active == null)
			{
				root.Add(BuildTableList());
				return;
			}

			root.Add(BuildTabBar());

			Asset.Validate();
			var localization = EditorServiceLocator.Resolve<ILocalizationService>();
			_supportedLanguages = localization.SupportedLanguages ?? Array.Empty<Language>();
			_defaultLanguage = localization.DefaultLanguage;

			root.Add(BuildToolbar());

			_languagesBar = new VisualElement { style = { flexShrink = 0, paddingLeft = 6, paddingRight = 6, paddingBottom = 4 } };
			root.Add(_languagesBar);

			// Main area: list (left) + edit panel (right).
			var main = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row } };

			var left = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column } };
			left.Add(BuildListHeader());

			_listView = new ListView
			{
				fixedItemHeight = 22,
				selectionType = SelectionType.Single,
				makeItem = MakeRow,
				bindItem = BindRow,
				itemsSource = _filtered,
				style = { flexGrow = 1 },
			};
			_listView.selectionChanged += OnRowSelectionChanged;
			left.Add(_listView);
			main.Add(left);

			_detailPanel = new VisualElement
			{
				style =
				{
					flexShrink = 0,
					width = 520,
					flexDirection = FlexDirection.Column,
					borderLeftWidth = 1,
					borderLeftColor = new Color(0, 0, 0, 0.3f),
				},
			};
			main.Add(_detailPanel);

			root.Add(main);

			root.Add(BuildFooter());

			RefreshLanguages();
			RefreshList();
			RefreshBulkBar();
			ShowDetail();
		}

		private VisualElement BuildTabBar()
		{
			var bar = new VisualElement { style = { flexShrink = 0, flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, paddingLeft = 4, paddingTop = 4, borderBottomWidth = 1, borderBottomColor = new Color(0, 0, 0, 0.4f) } };
			foreach (var tab in _tabs)
			{
				bar.Add(BuildTab(tab));
			}
			return bar;
		}

		private VisualElement BuildTab(TableTab tab)
		{
			bool isActive = tab == _active;
			var element = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row, alignItems = Align.Center,
					paddingLeft = 8, paddingRight = 4, paddingTop = 3, paddingBottom = 3, marginRight = 2,
					backgroundColor = isActive ? new Color(0.24f, 0.24f, 0.24f) : new Color(0, 0, 0, 0.18f),
					borderTopLeftRadius = 4, borderTopRightRadius = 4,
				},
			};
			element.RegisterCallback<PointerDownEvent>(evt =>
			{
				if (evt.button == 0)
				{
					SetActive(tab);
				}
			});

			var label = new Label(tab.Asset != null ? tab.Asset.name : "(missing)");
			if (isActive)
			{
				label.style.unityFontStyleAndWeight = FontStyle.Bold;
			}
			element.Add(label);

			var close = new Button { text = "×", tooltip = "Close tab", style = { width = 18, marginLeft = 4, paddingLeft = 0, paddingRight = 0, backgroundColor = Color.clear, borderTopWidth = 0, borderBottomWidth = 0, borderLeftWidth = 0, borderRightWidth = 0 } };
			close.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation()); // don't activate the tab when closing it
			close.clicked += () => CloseTab(tab);
			element.Add(close);

			return element;
		}

		private void SetActive(TableTab tab)
		{
			if (_active == tab)
			{
				return;
			}
			_active = tab;
			RebuildUI();
		}

		private void CloseTab(TableTab tab)
		{
			int index = _tabs.IndexOf(tab);
			_tabs.Remove(tab);
			if (_active == tab)
			{
				_active = _tabs.Count == 0 ? null : _tabs[Mathf.Clamp(index, 0, _tabs.Count - 1)];
			}
			RebuildUI();
		}

		// Empty-state view: lists every table in the project (mirrors the collection inspector),
		// with a search field that filters tables by key/translation content.
		private VisualElement BuildTableList()
		{
			var view = new ScrollView { style = { flexGrow = 1, paddingLeft = 10, paddingRight = 10, paddingTop = 10 } };
			view.Add(new Label("Translation Tables") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 15, marginBottom = 6 } });

			var tables = TranslationAssetFinder.FindAllTables();

			var searchRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
			searchRow.Add(new Image { image = EditorIcon.Search, style = { width = 16, height = 16, marginRight = 2 } });
			var search = new TextField { value = _tableListSearch, tooltip = "Search key or translation", style = { flexGrow = 1 } };
			searchRow.Add(search);
			view.Add(searchRow);

			var cards = new VisualElement();
			view.Add(cards);

			void Refresh()
			{
				cards.Clear();
				bool any = false;
				foreach (var table in tables)
				{
					if (!string.IsNullOrEmpty(_tableListSearch) && !table.ContainsKey(_tableListSearch) && !table.ContainsTranslation(_tableListSearch))
					{
						continue;
					}
					any = true;
					cards.Add(BuildTableCard(table));
				}
				if (!any)
				{
					cards.Add(new Label(string.IsNullOrEmpty(_tableListSearch)
						? "No translation tables found. Create one via Assets ▸ Create ▸ Localization ▸ Translation Table."
						: "No table matches the search.") { style = { whiteSpace = WhiteSpace.Normal, opacity = 0.6f } });
				}
			}

			search.RegisterValueChangedCallback(evt => { _tableListSearch = evt.newValue; Refresh(); });
			Refresh();
			return view;
		}

		private VisualElement BuildTableCard(TranslationTableAsset table)
		{
			var card = new VisualElement
			{
				style =
				{
					marginBottom = 6, paddingLeft = 8, paddingRight = 8, paddingTop = 6, paddingBottom = 6,
					backgroundColor = new Color(0, 0, 0, 0.12f),
					borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
				},
			};
			card.RegisterCallback<PointerDownEvent>(evt => { if (evt.button == 0) OpenTable(table); });

			var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
			headerRow.Add(new Label(table.Name) { style = { unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 } });
			headerRow.Add(new Button(() => OpenTable(table)) { text = "Open", style = { width = 80 } });
			card.Add(headerRow);

			int keyCount = table.Keys?.Count ?? 0;
			var infoRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 2 } };
			infoRow.Add(new Label($"Key Count: {keyCount}") { style = { flexGrow = 1, opacity = 0.8f } });
			infoRow.Add(new Label($"Modified: {table.LastModified.TimeAgo()}") { style = { opacity = 0.6f } });
			card.Add(infoRow);

			if (keyCount > 0)
			{
				int validated = table.Count(TranslationStatus.Validated);
				float ratio = (float)validated / keyCount;
				card.Add(new ProgressBar { title = $"Validated: {validated}/{keyCount} ({ratio:P0})", value = ratio * 100f, style = { marginTop = 4 } });
			}

			return card;
		}

		private VisualElement BuildToolbar()
		{
			var bar = new VisualElement { style = { flexShrink = 0, flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 4 } };

			var searchIcon = new Image { image = EditorIcon.Search, style = { width = 16, height = 16, marginRight = 2 } };
			bar.Add(searchIcon);

			var search = new TextField { value = _active.SearchText, style = { flexGrow = 1, marginRight = 6 } };
			search.RegisterValueChangedCallback(evt => { _active.SearchText = evt.newValue; RefreshList(); });
			bar.Add(search);

			var statusChoices = new List<string> { AllStatuses };
			statusChoices.AddRange(Enum.GetNames(typeof(TranslationStatus)));
			var status = new DropdownField("Status", statusChoices, statusChoices.IndexOf(_active.StatusFilter)) { style = { width = 230, marginRight = 6 } };
			status.RegisterValueChangedCallback(evt => { _active.StatusFilter = evt.newValue; RefreshList(); });
			bar.Add(status);

			_countLabel = new Label { style = { unityTextAlign = TextAnchor.MiddleRight, opacity = 0.7f, minWidth = 70 } };
			bar.Add(_countLabel);

			return bar;
		}

		private VisualElement BuildListHeader()
		{
			var header = new VisualElement { style = { flexShrink = 0, flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 4, paddingTop = 2, paddingBottom = 2, borderBottomWidth = 1, borderBottomColor = new Color(0, 0, 0, 0.3f) } };

			_selectAllToggle = new Toggle { tooltip = "Select / deselect all", style = { width = 18 } };
			_selectAllToggle.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue)
				{
					foreach (var item in _filtered) _active.SelectedKeys.Add(item.Key);
				}
				else
				{
					foreach (var item in _filtered) _active.SelectedKeys.Remove(item.Key);
				}
				_listView.RefreshItems();
				RefreshBulkBar();
			});
			header.Add(_selectAllToggle);

			header.Add(new Label { style = { width = 20 } }); // status icon column
			_keyHeaderLabel = new Label("Key") { style = { width = _keyColumnWidth, flexShrink = 0, unityFontStyleAndWeight = FontStyle.Bold } };
			header.Add(_keyHeaderLabel);
			header.Add(BuildColumnResizer());
			header.Add(new Label($"Default ({LangUtilities.GetLanguageCode(_defaultLanguage)})") { style = { flexGrow = 1, flexBasis = 0, unityFontStyleAndWeight = FontStyle.Bold } });
			header.Add(new Label { style = { width = 16, flexShrink = 0 } }); // AI marker column

			return header;
		}

		// Draggable handle between the "Key" and "Default" columns; persists the chosen width.
		private VisualElement BuildColumnResizer()
		{
			var resizer = new VisualElement
			{
				// alignSelf=Stretch is essential: in a Center-aligned row an empty element would
				// otherwise have 0 height and be impossible to grab.
				style =
				{
					width = 6, flexShrink = 0, marginLeft = 1, marginRight = 1,
					alignSelf = Align.Stretch, minHeight = 16,
					backgroundColor = new Color(1f, 1f, 1f, 0.15f),
				},
			};
			resizer.tooltip = "Drag to resize the Key column";

			bool dragging = false;
			resizer.RegisterCallback<PointerDownEvent>(evt =>
			{
				dragging = true;
				resizer.CapturePointer(evt.pointerId);
				evt.StopPropagation();
			});
			resizer.RegisterCallback<PointerMoveEvent>(evt =>
			{
				if (!dragging)
				{
					return;
				}
				_keyColumnWidth = Mathf.Max(MinColumnWidth, _keyColumnWidth + evt.deltaPosition.x);
				ApplyKeyColumnWidth();
			});
			resizer.RegisterCallback<PointerUpEvent>(evt =>
			{
				if (!dragging)
				{
					return;
				}
				dragging = false;
				resizer.ReleasePointer(evt.pointerId);
				EditorPrefs.SetFloat(KeyColumnWidthPref, _keyColumnWidth);
			});
			return resizer;
		}

		private void ApplyKeyColumnWidth()
		{
			if (_keyHeaderLabel != null)
			{
				_keyHeaderLabel.style.width = _keyColumnWidth;
			}
			_listView?.RefreshItems();
		}

		private VisualElement BuildFooter()
		{
			// Pinned bottom area: selection (bulk) bar above the add-key row.
			var footer = new VisualElement { style = { flexShrink = 0, borderTopWidth = 1, borderTopColor = new Color(0, 0, 0, 0.3f) } };

			_bulkBar = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 2 } };
			footer.Add(_bulkBar);

			var addRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 6, paddingRight = 6, paddingTop = 2, paddingBottom = 4 } };

			var keyField = new TextField { style = { flexGrow = 1, marginRight = 6 } };
			var defField = new TextField { style = { flexGrow = 1, marginRight = 6 } };
			var addButton = IconButton("Add Key", EditorIcon.Plus, null);

			void Commit()
			{
				var key = keyField.value?.Trim();
				if (string.IsNullOrEmpty(key))
				{
					return;
				}
				if (Asset.Items.Any(i => i.Key == key))
				{
					EditorUtility.DisplayDialog("Error", "Key already exists", "Ok");
					return;
				}
				Undo.RecordObject(Asset, "Add Key");
				var item = Asset.AddItem(key);
				var defaultText = defField.value;
				if (!string.IsNullOrEmpty(defaultText))
				{
					Asset.SetTranslation(_defaultLanguage, key, defaultText);
				}
				keyField.value = string.Empty;
				defField.value = string.Empty;
				_active.EditingItem = item;
				AfterMutation();
				RefreshList();
				ShowDetail();
				int index = _filtered.IndexOf(item);
				if (index >= 0) _listView.ScrollToItem(index);
				keyField.Focus();
			}

			addButton.clicked += Commit;
			void OnEnter(KeyDownEvent evt)
			{
				if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
				{
					Commit();
					evt.StopPropagation();
				}
			}
			keyField.RegisterCallback<KeyDownEvent>(OnEnter);
			defField.RegisterCallback<KeyDownEvent>(OnEnter);

			addRow.Add(new Label("New key") { style = { marginRight = 6, opacity = 0.7f } });
			addRow.Add(keyField);
			addRow.Add(new Label($"Default ({LangUtilities.GetLanguageCode(_defaultLanguage)})") { style = { marginRight = 6, opacity = 0.7f } });
			addRow.Add(defField);
			addRow.Add(addButton);
			footer.Add(addRow);

			return footer;
		}

		#endregion

		#region List rows

		private VisualElement MakeRow()
		{
			var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 4 } };

			var toggle = new Toggle { name = "sel", style = { width = 18 } };
			toggle.RegisterValueChangedCallback(evt =>
			{
				if (row.userData is not TItem item)
				{
					return;
				}
				if (evt.newValue) _active.SelectedKeys.Add(item.Key);
				else _active.SelectedKeys.Remove(item.Key);
				RefreshBulkBar();
				UpdateSelectAllToggle();
			});
			row.Add(toggle);

			row.Add(new Image { name = "status", style = { width = 16, height = 16, marginLeft = 2, marginRight = 2, flexShrink = 0 } });
			row.Add(new Label { name = "key", style = { width = _keyColumnWidth, flexShrink = 0, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis, unityTextOverflowPosition = TextOverflowPosition.End } });
			row.Add(new VisualElement { style = { width = 8, flexShrink = 0 } }); // matches the header resizer footprint
			row.Add(new Label { name = "def", style = { flexGrow = 1, flexBasis = 0, opacity = 0.7f, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis } });
			row.Add(new Label { name = "ai", style = { width = 16, flexShrink = 0, unityTextAlign = TextAnchor.MiddleCenter } });

			return row;
		}

		private void BindRow(VisualElement element, int index)
		{
			var item = _filtered[index];
			element.userData = item;
			element.Q<Toggle>("sel").SetValueWithoutNotify(_active.SelectedKeys.Contains(item.Key));

			var statusIcon = element.Q<Image>("status");
			statusIcon.image = StatusIcon(item.Status);
			statusIcon.tintColor = StatusColor(item.Status);
			statusIcon.tooltip = StatusTooltip(item);

			var keyLabel = element.Q<Label>("key");
			keyLabel.style.width = _keyColumnWidth; // keep in sync when the column is resized
			keyLabel.text = item.Key;
			element.Q<Label>("def").text = Asset.GetTranslation(item.Key, _defaultLanguage);

			var aiLabel = element.Q<Label>("ai");
			if (_bulkTranslating && item.Key == _bulkCurrentKey)
			{
				aiLabel.text = _spinner[_bulkSpinnerFrame % _spinner.Length];
				aiLabel.tooltip = "Translating…";
			}
			else
			{
				bool anyAI = item.Translations.Any(t => t.AITranslated);
				aiLabel.text = anyAI ? "✨" : string.Empty;
				aiLabel.tooltip = anyAI ? "Contains AI-translated text" : null;
			}
		}

		private void OnRowSelectionChanged(IEnumerable<object> selection)
		{
			StopScan();
			ClearAlternatives();
			_active.EditingItem = selection.FirstOrDefault() as TItem;
			ShowDetail();
		}

		#endregion

		#region Refresh

		private bool MatchesFilters(TItem item)
		{
			if (_active.StatusFilter != AllStatuses && item.Status.ToString() != _active.StatusFilter)
			{
				return false;
			}
			var search = _active.SearchText;
			if (string.IsNullOrWhiteSpace(search))
			{
				return true;
			}
			if (item.Key.Contains(search, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			foreach (var language in Asset.Languages)
			{
				var translation = Asset.GetTranslation(item.Key, language);
				if (!string.IsNullOrEmpty(translation) && translation.Contains(search, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		private void RefreshList()
		{
			_filtered.Clear();
			foreach (var item in Asset.Items)
			{
				if (MatchesFilters(item))
				{
					_filtered.Add(item);
				}
			}
			_listView.itemsSource = _filtered;
			_listView.RefreshItems();
			_countLabel.text = $"{_filtered.Count} / {Asset.Items.Count}";
			UpdateSelectAllToggle();
			SyncSelection();
		}

		// Keep the ListView highlight in sync with the edited item without firing selectionChanged.
		private void SyncSelection()
		{
			int index = _active.EditingItem == null ? -1 : _filtered.IndexOf(_active.EditingItem);
			if (index >= 0)
			{
				_listView.SetSelectionWithoutNotify(new[] { index });
			}
			else
			{
				_listView.SetSelectionWithoutNotify(Array.Empty<int>());
			}
		}

		private void UpdateSelectAllToggle()
		{
			if (_selectAllToggle == null)
			{
				return;
			}
			bool allSelected = _filtered.Count > 0 && _filtered.All(i => _active.SelectedKeys.Contains(i.Key));
			_selectAllToggle.SetValueWithoutNotify(allSelected);
		}

		private void RefreshLanguages()
		{
			_languagesBar.Clear();

			var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexWrap = Wrap.Wrap } };
			row.Add(new Label("Languages:") { style = { marginRight = 6, opacity = 0.7f } });

			foreach (var language in Asset.Languages)
			{
				row.Add(BuildLanguagePill(language));
			}

			var add = new Button { text = "Add Language  ▾", style = { marginLeft = 4 } };
			add.clicked += () => ShowAddLanguageMenu(add.worldBound);
			row.Add(add);

			_languagesBar.Add(row);
		}

		private VisualElement BuildLanguagePill(Language language)
		{
			var pill = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginRight = 4, marginBottom = 2, paddingLeft = 6, paddingRight = 2, borderTopLeftRadius = 8, borderTopRightRadius = 8, borderBottomLeftRadius = 8, borderBottomRightRadius = 8, backgroundColor = new Color(0, 0, 0, 0.18f) } };
			pill.Add(new Label(language.ToString()));

			var remove = new Button { text = "×", tooltip = $"Remove {language}", style = { width = 18, marginLeft = 2, paddingLeft = 0, paddingRight = 0, backgroundColor = Color.clear, borderTopWidth = 0, borderBottomWidth = 0, borderLeftWidth = 0, borderRightWidth = 0 } };
			remove.clicked += () =>
			{
				if (EditorUtility.DisplayDialog("Delete Language", $"Remove all translations for {language}?", "Yes", "No"))
				{
					Undo.RecordObject(Asset, "Remove Language");
					Asset.RemoveLanguage(language);
					AfterMutation();
					RefreshLanguages();
					RefreshList();
					ShowDetail();
				}
			};
			pill.Add(remove);
			return pill;
		}

		private void RefreshBulkBar()
		{
			_bulkBar.Clear();
			int count = _active.SelectedKeys.Count;
			if (count == 0)
			{
				_bulkBar.style.display = DisplayStyle.None;
				return;
			}
			_bulkBar.style.display = DisplayStyle.Flex;

			if (_bulkTranslating)
			{
				_bulkBar.Add(new Label($"✨ Translating {_bulkDone}/{_bulkTotal}…") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
				return;
			}

			_bulkBar.Add(new Label($"{count} selected") { style = { marginRight = 8, unityFontStyleAndWeight = FontStyle.Bold } });

			var translate = new Button(TranslateSelectedWithAI) { text = "✨ Translate", style = { marginRight = 4 } };
			var validate = IconButton("Validate", EditorIcon.Valid, ValidateSelected);
			validate.style.marginRight = 4;
			var delete = IconButton("Delete", EditorIcon.Trash, DeleteSelected);
			delete.style.marginRight = 4;
			var move = IconButton("Move to  ▾", EditorIcon.Open, null);
			move.clicked += () => ShowMoveMenu(_active.SelectedKeys.ToArray(), move.worldBound);

			_bulkBar.Add(translate);
			_bulkBar.Add(validate);
			_bulkBar.Add(delete);
			_bulkBar.Add(move);
		}

		#endregion

		#region Detail panel

		private void ShowDetail()
		{
			_detailPanel.Clear();

			if (_active.EditingItem == null)
			{
				_keyField = null;
				_keyLoader = null;
				_detailPanel.Add(new Label("Select a key in the list to translate it.") { style = { whiteSpace = WhiteSpace.Normal, opacity = 0.6f, marginTop = 8 } });
				return;
			}

			var item = _active.EditingItem;

			// Scrollable area: key + translations (can grow tall without hiding the docked details).
			var scroll = new ScrollView { style = { flexGrow = 1, paddingLeft = 8, paddingRight = 8, paddingTop = 6 } };

			scroll.Add(new Label("Edit Key") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14, marginBottom = 6 } });

			// Key (rename on focus out) with an inline scan loader to its right.
			var keyRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
			_keyField = new TextField("Key") { value = item.Key, isReadOnly = _scanning, style = { flexGrow = 1 } };
			_keyField.RegisterCallback<FocusOutEvent>(_ => CommitRename(item, _keyField));
			keyRow.Add(_keyField);
			_keyLoader = new Label { tooltip = "Searching references…", style = { marginLeft = 6, minWidth = 64, unityTextAlign = TextAnchor.MiddleLeft, display = _scanning ? DisplayStyle.Flex : DisplayStyle.None } };
			keyRow.Add(_keyLoader);
			scroll.Add(keyRow);

			scroll.Add(new Label("Translations") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8, marginBottom = 2 } });

			foreach (var language in Asset.Languages)
			{
				var captured = language;

				// AI-translated: show a selector (in place of the field) with a reset button.
				if (_alternativesItem == item && _alternatives.TryGetValue(language, out var options))
				{
					var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };

					var picker = new DropdownField(language.ToString()) { choices = options, style = { flexGrow = 1, flexBasis = 0, minWidth = 0 } };
					int current = options.IndexOf(Asset.GetTranslation(item.Key, language));
					picker.index = current >= 0 ? current : 0;
					picker.RegisterValueChangedCallback(evt =>
					{
						Undo.RecordObject(Asset, "Choose AI Alternative");
						Asset.SetTranslation(captured, item.Key, evt.newValue, aiTranslated: true);
						AfterMutation();
						RefreshList();
					});
					row.Add(picker);

					var reset = new Button(() => ResetTranslation(item, captured))
					{
						text = "×",
						tooltip = "Reset: clear the translation and discard alternatives",
						style = { width = 20, marginLeft = 4 },
					};
					row.Add(reset);

					scroll.Add(row);
					continue;
				}

				// Regular editable field.
				var fieldRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.FlexStart } };
				var field = new TextField(language.ToString()) { multiline = true, value = Asset.GetTranslation(item.Key, language), style = { flexGrow = 1, flexBasis = 0, minWidth = 0 } };
				field.style.whiteSpace = WhiteSpace.Normal;
				field.RegisterCallback<FocusOutEvent>(_ => CommitTranslation(item, captured, field));
				fieldRow.Add(field);
				if (item.TryGetTranslation(language, out var translation) && translation.AITranslated)
				{
					fieldRow.Add(new Label("✨") { tooltip = "AI-translated (edit to clear)", style = { marginLeft = 4, marginTop = 2 } });
				}
				scroll.Add(fieldRow);
			}

			_detailPanel.Add(scroll);

			// Docked bottom: status/dates and actions stay visible regardless of translation length.
			var footer = new VisualElement { style = { flexShrink = 0, paddingLeft = 8, paddingRight = 8, paddingTop = 4, paddingBottom = 6, borderTopWidth = 1, borderTopColor = new Color(0, 0, 0, 0.3f) } };
			footer.Add(BuildDetailInfo(item));
			footer.Add(BuildDetailActions(item));
			_detailPanel.Add(footer);
		}

		private VisualElement BuildDetailInfo(TItem item)
		{
			var box = new VisualElement();

			var statusRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
			statusRow.Add(new Label("Status:") { style = { width = 70, opacity = 0.7f } });
			statusRow.Add(new Image { image = StatusIcon(item.Status), tintColor = StatusColor(item.Status), style = { width = 16, height = 16, marginRight = 4 } });
			statusRow.Add(new Label(item.Status.ToString()) { style = { unityFontStyleAndWeight = FontStyle.Bold, color = StatusColor(item.Status) } });
			box.Add(statusRow);

			box.Add(InfoLine("Modified:", TimeAgoOrDash(item.LastModified)));
			box.Add(InfoLine("Validated:", TimeAgoOrDash(item.LastValidated)));
			return box;
		}

		private static VisualElement InfoLine(string label, string value)
		{
			var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
			row.Add(new Label(label) { style = { width = 70, opacity = 0.7f } });
			row.Add(new Label(value));
			return row;
		}

		private VisualElement BuildDetailActions(TItem item)
		{
			var container = new VisualElement { style = { marginTop = 10 } };

			var translate = new Button(() => TranslateWithAI(item)) { text = "✨ Translate with AI" };
			translate.style.marginBottom = 4;
			container.Add(translate);

			var references = IconButton("Find References", EditorIcon.Link, () => ScanAndListReferences(item.Key));
			references.style.marginBottom = 4;
			container.Add(references);

			var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

			var validate = IconButton("Validate", EditorIcon.Valid, () =>
			{
				Undo.RecordObject(Asset, "Validate Translations");
				item.ValidateTranslations();
				ClearAlternatives(); // lock in the chosen AI alternatives (selectors become plain fields)
				AfterMutation();
				RefreshList();
				ShowDetail();
			});
			validate.style.flexGrow = 1;
			validate.style.marginRight = 4;
			if (item.Status == TranslationStatus.Modified)
			{
				validate.style.color = StatusColor(TranslationStatus.Modified); // yellow "Validate" text
			}
			else if (item.Status == TranslationStatus.Validated)
			{
				var validateIcon = validate.Q<Image>();
				if (validateIcon != null)
				{
					validateIcon.tintColor = StatusColor(TranslationStatus.Validated); // green check
				}
			}

			var move = IconButton("Move", EditorIcon.Open, null);
			move.style.flexGrow = 1;
			move.style.marginRight = 4;
			move.clicked += () => ShowMoveMenu(new[] { item.Key }, move.worldBound);

			var delete = IconButton("Delete", EditorIcon.Trash, () =>
			{
				if (EditorUtility.DisplayDialog("Delete Key", $"Delete key '{item.Key}' and all its translations?", "Yes", "No"))
				{
					Undo.RecordObject(Asset, "Remove Key");
					_active.SelectedKeys.Remove(item.Key);
					Asset.RemoveKey(item.Key);
					_active.EditingItem = null;
					AfterMutation();
					RefreshList();
					RefreshBulkBar();
					ShowDetail();
				}
			});
			delete.style.flexGrow = 1;
			delete.style.color = new Color(0.9f, 0.5f, 0.5f);

			row.Add(validate);
			row.Add(move);
			row.Add(delete);

			container.Add(row);
			return container;
		}

		private void CommitRename(TItem item, TextField keyField)
		{
			var newKey = keyField.value?.Trim();
			if (string.IsNullOrEmpty(newKey) || newKey == item.Key)
			{
				keyField.SetValueWithoutNotify(item.Key);
				return;
			}
			if (Asset.Items.Any(i => i.Key == newKey))
			{
				EditorUtility.DisplayDialog("Error", "Key already exists", "Ok");
				keyField.SetValueWithoutNotify(item.Key);
				return;
			}

			Undo.RecordObject(Asset, "Edit Key");
			var oldKey = item.Key;
			Asset.EditKey(oldKey, newKey);
			if (_active.SelectedKeys.Remove(oldKey))
			{
				_active.SelectedKeys.Add(newKey);
			}
			AfterMutation();
			RefreshList();

			PromptPropagateRename(oldKey, newKey);
		}

		private void CommitTranslation(TItem item, Language language, TextField field)
		{
			if (Asset.GetTranslation(item.Key, language) == field.value)
			{
				return;
			}
			Undo.RecordObject(Asset, "Edit Translation");
			Asset.SetTranslation(language, item.Key, field.value);
			AfterMutation();
			RefreshList();
			ShowDetail(); // refresh status / timestamps
		}

		#endregion

		#region Bulk actions

		private void ValidateSelected()
		{
			Undo.RecordObject(Asset, "Validate Translations");
			foreach (var key in _active.SelectedKeys)
			{
				Asset.Items.FirstOrDefault(i => i.Key == key)?.ValidateTranslations();
			}
			AfterMutation();
			RefreshList();
			ShowDetail();
		}

		private void DeleteSelected()
		{
			int count = _active.SelectedKeys.Count;
			if (!EditorUtility.DisplayDialog("Delete Keys", $"Delete {count} selected key(s) and all their translations?", "Yes", "No"))
			{
				return;
			}
			Undo.RecordObject(Asset, "Remove Keys");
			foreach (var key in _active.SelectedKeys.ToArray())
			{
				Asset.RemoveKey(key);
				if (_active.EditingItem != null && _active.EditingItem.Key == key)
				{
					_active.EditingItem = null;
				}
			}
			_active.SelectedKeys.Clear();
			AfterMutation();
			RefreshList();
			RefreshBulkBar();
			ShowDetail();
		}

		private void ShowMoveMenu(string[] keys, Rect anchor)
		{
			var menu = new GenericMenu();
			var others = TranslationAssetFinder.FindAllTables().Where(t => t != Asset).ToList();
			if (others.Count == 0)
			{
				menu.AddDisabledItem(new GUIContent("No other table"));
			}
			else
			{
				foreach (var table in others)
				{
					var target = table;
					menu.AddItem(new GUIContent(target.Name), false, () => MoveKeys(keys, target));
				}
			}
			menu.DropDown(anchor);
		}

		private void MoveKeys(string[] keys, TranslationTableAsset target)
		{
			Undo.RecordObject(Asset, "Move Keys");
			Undo.RecordObject(target, "Move Keys");

			var failed = new List<string>();
			foreach (var key in keys)
			{
				var item = Asset.Items.FirstOrDefault(i => i.Key == key);
				if (item == null)
				{
					continue;
				}
				if (target.TryAddItem(item.Clone()))
				{
					Asset.RemoveKey(key);
					_active.SelectedKeys.Remove(key);
					if (_active.EditingItem != null && _active.EditingItem.Key == key)
					{
						_active.EditingItem = null;
					}
				}
				else
				{
					failed.Add(key);
				}
			}

			EditorUtility.SetDirty(target);
			AfterMutation();
			RefreshList();
			RefreshBulkBar();
			ShowDetail();

			if (failed.Count > 0)
			{
				EditorUtility.DisplayDialog("Move Keys", $"These keys already exist in '{target.Name}' and were not moved:\n{string.Join(", ", failed)}", "Ok");
			}
		}

		#endregion

		#region Language menu

		private void ShowAddLanguageMenu(Rect anchor)
		{
			var missing = _supportedLanguages.Except(Asset.Languages).ToList();
			var menu = new GenericMenu();

			if (missing.Count == 0)
			{
				menu.AddDisabledItem(new GUIContent("No language to add"));
			}
			else
			{
				foreach (var language in missing)
				{
					var captured = language;
					menu.AddItem(new GUIContent(captured.ToString()), false, () => AddLanguages(new[] { captured }));
				}
				menu.AddSeparator(string.Empty);
				menu.AddItem(new GUIContent("All missing languages"), false, () => AddLanguages(missing.ToArray()));
			}

			menu.DropDown(anchor);
		}

		private void AddLanguages(Language[] languages)
		{
			Undo.RecordObject(Asset, "Add Language");
			foreach (var language in languages)
			{
				Asset.AddLanguage(language);
			}
			AfterMutation();
			RefreshLanguages();
			RefreshList();
			ShowDetail();
		}

		#endregion

		#region References

		private static readonly string[] _spinner = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };

		// Scans every prefab and scene for TranslationKey references to <paramref name="key"/>,
		// one asset per editor tick so the UI stays responsive. Instead of a ProcessQueueWindow it
		// shows an inline spinner next to the (read-only) Key field, then calls <paramref name="onDone"/>.
		private void ScanReferencesInline(string key, Action<List<TranslationKeyReferenceFinder.Reference>> onDone)
		{
			if (_scanning)
			{
				return;
			}

			var results = new List<TranslationKeyReferenceFinder.Reference>();
			var work = new Queue<Action>();
			foreach (var path in TranslationKeyReferenceFinder.FindPrefabPaths())
			{
				var prefabPath = path;
				work.Enqueue(() => TranslationKeyReferenceFinder.ScanPrefab(prefabPath, key, results));
			}
			foreach (var path in TranslationKeyReferenceFinder.FindScenePaths())
			{
				var scenePath = path;
				work.Enqueue(() => TranslationKeyReferenceFinder.ScanScene(scenePath, key, results));
			}

			if (work.Count == 0)
			{
				onDone(results);
				return;
			}

			int total = work.Count;
			int done = 0;
			SetScanning(true);

			_scanStep = () =>
			{
				if (work.Count > 0)
				{
					try { work.Dequeue().Invoke(); }
					catch (Exception e) { Debug.LogError(e); }
					done++;
					if (_keyLoader != null)
					{
						_keyLoader.text = $"{_spinner[done % _spinner.Length]}  {done}/{total}";
					}
					return;
				}

				StopScan();
				onDone(results);
			};
			EditorApplication.update += _scanStep;
		}

		private void SetScanning(bool on)
		{
			_scanning = on;
			if (_keyField != null)
			{
				_keyField.isReadOnly = on;
			}
			if (_keyLoader != null)
			{
				_keyLoader.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}

		// Cancels any in-progress scan and clears the loader (no onDone callback fires).
		private void StopScan()
		{
			if (_scanStep != null)
			{
				EditorApplication.update -= _scanStep;
				_scanStep = null;
			}
			SetScanning(false);
		}

		private void ScanAndListReferences(string key)
		{
			ScanReferencesInline(key, refs =>
			{
				if (refs.Count == 0)
				{
					EditorUtility.DisplayDialog("References", $"No references to '{key}' found in the project.", "Ok");
					return;
				}
				TranslationKeyReferencesWindow.OpenList(key, refs);
			});
		}

		private void PromptPropagateRename(string oldKey, string newKey)
		{
			bool search = EditorUtility.DisplayDialog(
				"Update references",
				$"Search the project for references to '{oldKey}' and update them to '{newKey}'?",
				"Search", "Skip");
			if (!search)
			{
				return;
			}

			ScanReferencesInline(oldKey, refs =>
			{
				if (refs.Count == 0)
				{
					EditorUtility.DisplayDialog("Update references", $"No references to '{oldKey}' found in the project.", "Ok");
					return;
				}
				TranslationKeyReferencesWindow.OpenRenameConfirm(oldKey, newKey, refs, () => ApplyRename(oldKey, newKey, refs));
			});
		}

		private void ApplyRename(string oldKey, string newKey, List<TranslationKeyReferenceFinder.Reference> refs)
		{
			// Make sure no unsaved scene work gets bundled into our edits when we save scenes.
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				return;
			}

			var assets = refs
				.GroupBy(r => r.AssetPath)
				.Select(g => (Path: g.Key, g.First().IsScene))
				.ToList();

			var queue = new ProcessQueue();
			foreach (var asset in assets)
			{
				var path = asset.Path;
				bool isScene = asset.IsScene;
				queue.EnqueueAction(() =>
				{
					if (isScene)
					{
						TranslationKeyReferenceFinder.RenameInScene(path, oldKey, newKey);
					}
					else
					{
						TranslationKeyReferenceFinder.RenameInPrefab(path, oldKey, newKey);
					}
				}, $"{(isScene ? "Scene" : "Prefab")}: {Path.GetFileName(path)}");
			}

			ProcessQueueWindow.Open(queue, $"Updating references → '{newKey}'", () =>
			{
				AssetDatabase.SaveAssets();
				AfterMutation();
			}, autoClose: false, autoStart: true);
		}

		#endregion

		#region AI translation

		private void TranslateWithAI(TItem item)
		{
			if (_scanning)
			{
				return;
			}

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

			var allTargets = Asset.Languages.Where(l => l != _defaultLanguage).ToList();
			if (allTargets.Count == 0)
			{
				EditorUtility.DisplayDialog("AI Translation", "No target languages to translate into (only the default language exists).", "Ok");
				return;
			}

			// Default to filling only the empty languages; offer to re-translate everything otherwise.
			var targets = allTargets.Where(l => string.IsNullOrEmpty(Asset.GetTranslation(item.Key, l))).ToList();
			if (targets.Count == 0)
			{
				if (!EditorUtility.DisplayDialog("AI Translation", "All target languages already have a translation.\nRe-translate all of them?", "Re-translate", "Cancel"))
				{
					return;
				}
				targets = allTargets;
			}

			void Proceed(List<TranslationKeyReferenceFinder.Reference> references)
			{
				var request = AITranslationContextBuilder.Build(Asset, item, _defaultLanguage, targets, references, settings);
				SetScanning(true);
				if (_keyLoader != null)
				{
					_keyLoader.text = "✨ Translating…";
				}
				AIProviders.Create(settings).Translate(request, result =>
				{
					SetScanning(false);
					if (!result.Success)
					{
						EditorUtility.DisplayDialog("AI Translation", "Failed:\n" + result.Error, "Ok");
						return;
					}
					ApplyAIResult(item, result);
				});
			}

			if (settings.IncludeUsageContext)
			{
				ScanReferencesInline(item.Key, Proceed);
			}
			else
			{
				Proceed(new List<TranslationKeyReferenceFinder.Reference>());
			}
		}

		private void ApplyAIResult(TItem item, AITranslationResult result)
		{
			Undo.RecordObject(Asset, "AI Translate");

			// Fill the default language too when it was empty and the AI proposed a source.
			if (Asset.IsLanguageSupported(_defaultLanguage)
				&& string.IsNullOrEmpty(Asset.GetTranslation(item.Key, _defaultLanguage))
				&& !string.IsNullOrEmpty(result.GeneratedSource))
			{
				Asset.SetTranslation(_defaultLanguage, item.Key, result.GeneratedSource, aiTranslated: true);
			}

			// Apply the most probable option now; keep the extra options as transient alternatives to pick from.
			_alternatives.Clear();
			_alternativesItem = item;
			foreach (var entry in result.Entries)
			{
				// The default language is the source, never a target: ignore it even if the model echoes it,
				// so we don't overwrite the authored source or show alternatives for it.
				if (entry.Language == _defaultLanguage || !Asset.IsLanguageSupported(entry.Language) || entry.Options.Count == 0)
				{
					continue;
				}
				Asset.SetTranslation(entry.Language, item.Key, entry.Options[0], aiTranslated: true);
				_alternatives[entry.Language] = entry.Options;
			}

			AfterMutation();
			RefreshList();
			ShowDetail();
		}

		// Translates every selected key with AI (sequentially), applying the most probable option per
		// language. No alternatives UI here — bulk auto-applies; single-key editing keeps the picker.
		private void TranslateSelectedWithAI()
		{
			if (_bulkTranslating || _scanning)
			{
				return;
			}

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

			var allTargets = Asset.Languages.Where(l => l != _defaultLanguage).ToList();
			if (allTargets.Count == 0)
			{
				EditorUtility.DisplayDialog("AI Translation", "No target languages to translate into.", "Ok");
				return;
			}

			var items = _active.SelectedKeys
				.Select(key => Asset.Items.FirstOrDefault(i => i.Key == key))
				.Where(item => item != null)
				.ToList();
			if (items.Count == 0)
			{
				return;
			}

			if (!EditorUtility.DisplayDialog("AI Translation",
				$"Translate {items.Count} selected key(s) with {settings.Provider}?\nOne request is sent per key; the most likely translation is applied to empty languages.",
				"Translate", "Cancel"))
			{
				return;
			}

			// One project scan for all selected keys (usage context incl. the hierarchy path of each field).
			Dictionary<string, List<TranslationKeyReferenceFinder.Reference>> refsByKey = null;
			if (settings.IncludeUsageContext)
			{
				try
				{
					refsByKey = TranslationKeyReferenceFinder.FindReferences(
						items.Select(i => i.Key),
						(progress, path) => EditorUtility.DisplayProgressBar("AI Translation", $"Scanning usage… {System.IO.Path.GetFileName(path)}", progress));
				}
				finally
				{
					EditorUtility.ClearProgressBar();
				}
			}

			_bulkTranslating = true;
			_bulkTotal = items.Count;
			_bulkDone = 0;
			RefreshBulkBar();
			StartBulkSpinner();
			TranslateNextSelected(items, 0, AIProviders.Create(settings), settings, allTargets, refsByKey);
		}

		private void StartBulkSpinner()
		{
			if (_bulkSpinnerTick != null)
			{
				return;
			}
			_bulkSpinnerTick = () =>
			{
				int frame = (int)(EditorApplication.timeSinceStartup * 10) % _spinner.Length;
				if (frame != _bulkSpinnerFrame)
				{
					_bulkSpinnerFrame = frame;
					_listView?.RefreshItems(); // animate the spinner on the current row
				}
			};
			EditorApplication.update += _bulkSpinnerTick;
		}

		private void StopBulkSpinner()
		{
			if (_bulkSpinnerTick != null)
			{
				EditorApplication.update -= _bulkSpinnerTick;
				_bulkSpinnerTick = null;
			}
		}

		private void TranslateNextSelected(List<TItem> items, int index, IAITranslationProvider provider, AITranslationSettings settings, List<Language> allTargets, Dictionary<string, List<TranslationKeyReferenceFinder.Reference>> refsByKey)
		{
			if (index >= items.Count)
			{
				_bulkTranslating = false;
				_bulkCurrentKey = null;
				StopBulkSpinner();
				RefreshBulkBar();
				RefreshList();
				ShowDetail();
				EditorUtility.DisplayDialog("AI Translation", $"Translated {items.Count} key(s).", "Ok");
				return;
			}

			var item = items[index];
			var targets = allTargets.Where(l => string.IsNullOrEmpty(Asset.GetTranslation(item.Key, l))).ToList();
			if (targets.Count == 0)
			{
				_bulkDone++;
				RefreshBulkBar();
				TranslateNextSelected(items, index + 1, provider, settings, allTargets, refsByKey);
				return;
			}

			_bulkCurrentKey = item.Key; // row shows the spinner while this key is translated
			RefreshList();

			var references = refsByKey != null && refsByKey.TryGetValue(item.Key, out var found)
				? found
				: new List<TranslationKeyReferenceFinder.Reference>();
			var request = AITranslationContextBuilder.Build(Asset, item, _defaultLanguage, targets, references, settings);
			provider.Translate(request, result =>
			{
				if (result.Success)
				{
					ApplyMostProbable(item, result);
				}
				else
				{
					Debug.LogWarning($"[AI Translation] '{item.Key}' failed: {result.Error}");
				}
				_bulkDone++;
				RefreshBulkBar();
				RefreshList();
				TranslateNextSelected(items, index + 1, provider, settings, allTargets, refsByKey);
			});
		}

		// Applies the most probable option per target language (used by bulk translation, no alternatives UI).
		private void ApplyMostProbable(TItem item, AITranslationResult result)
		{
			Undo.RecordObject(Asset, "AI Translate");
			AITranslationApplier.ApplyMostProbable(Asset, item, _defaultLanguage, result);
			AfterMutation();
		}

		// Clears a translation and discards its AI alternatives (no validation), then shows an empty editable field.
		private void ResetTranslation(TItem item, Language language)
		{
			Undo.RecordObject(Asset, "Reset Translation");
			if (Asset.IsLanguageSupported(language))
			{
				Asset.SetTranslation(language, item.Key, string.Empty, aiTranslated: false);
			}
			_alternatives.Remove(language);
			AfterMutation();
			RefreshList();
			ShowDetail();
		}

		#endregion

		#region Helpers

		private static Button IconButton(string text, Texture2D icon, Action onClick)
		{
			var button = onClick != null ? new Button(onClick) : new Button();
			button.style.flexDirection = FlexDirection.Row;
			button.style.alignItems = Align.Center;
			button.style.justifyContent = Justify.Center;
			if (icon != null)
			{
				button.Add(new Image { image = icon, style = { width = 14, height = 14, marginRight = 4, flexShrink = 0 } });
			}
			button.Add(new Label(text));
			return button;
		}

		private void SyncPersistence()
		{
			_persistedAssets = _tabs.Select(t => t.Asset).ToList();
			_persistedActiveIndex = _active == null ? 0 : _tabs.IndexOf(_active);
		}

		private void AfterMutation()
		{
			EditorUtility.SetDirty(Asset);

			// Rebuild the editor translation cache (reads the in-memory, possibly unsaved assets) and
			// point the Translator at it, so LocalizedText in open scenes reflect the edit immediately.
			var translationService = EditorServiceLocator.Resolve<EditorTranslationService>();
			translationService.Refresh();
			Translator.Initialize(translationService);

			foreach (var localizedText in FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				localizedText.UpdateText();
			}
			SceneView.RepaintAll();
		}

		private static string TimeAgoOrDash(long ticks)
			=> ticks <= 0 ? "—" : new DateTimeOffset(ticks, TimeSpan.Zero).TimeAgo();

		private static Texture2D StatusIcon(TranslationStatus status) => status switch
		{
			TranslationStatus.Validated => EditorIcon.Valid,
			TranslationStatus.Modified => EditorIcon.Edit,
			TranslationStatus.ToRemove => EditorIcon.Trash,
			_ => null,
		};

		private static string StatusTooltip(TItem item) => item.Status switch
		{
			TranslationStatus.Validated => $"Validated {TimeAgoOrDash(item.LastValidated)}",
			TranslationStatus.Modified => $"Modified {TimeAgoOrDash(item.LastModified)}",
			TranslationStatus.ToRemove => "Marked for removal",
			_ => item.Status.ToString(),
		};

		private static Color StatusColor(TranslationStatus status) => status switch
		{
			TranslationStatus.Validated => new Color(0.4f, 0.8f, 0.45f),
			TranslationStatus.Modified => new Color(0.95f, 0.8f, 0.25f),
			TranslationStatus.ToRemove => new Color(0.9f, 0.45f, 0.45f),
			_ => Color.gray,
		};

		#endregion
	}
}

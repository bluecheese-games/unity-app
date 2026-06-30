//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Reference = BlueCheese.App.Editor.TranslationKeyReferenceFinder.Reference;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Popup that lists every project reference to a localization key (grouped by asset, with
	/// links to ping them). Doubles as the confirmation screen before propagating a key rename.
	/// </summary>
	public class TranslationKeyReferencesWindow : EditorWindow
	{
		private string _key;
		private string _newKey;
		private bool _confirmMode;
		private List<Reference> _refs;
		private Action _onApply;

		public static void OpenList(string key, List<Reference> refs)
		{
			var window = CreateInstance<TranslationKeyReferencesWindow>();
			window.titleContent = new GUIContent("Key References");
			window.minSize = new Vector2(560, 360);
			window._key = key;
			window._refs = refs;
			window._confirmMode = false;
			window.ShowUtility();
		}

		public static void OpenRenameConfirm(string oldKey, string newKey, List<Reference> refs, Action onApply)
		{
			var window = CreateInstance<TranslationKeyReferencesWindow>();
			window.titleContent = new GUIContent("Update Key References");
			window.minSize = new Vector2(560, 360);
			window._key = oldKey;
			window._newKey = newKey;
			window._refs = refs;
			window._confirmMode = true;
			window._onApply = onApply;
			window.ShowUtility();
		}

		private void CreateGUI()
		{
			var root = rootVisualElement;
			root.style.flexGrow = 1;
			root.style.paddingLeft = 8;
			root.style.paddingRight = 8;
			root.style.paddingTop = 6;
			root.style.paddingBottom = 6;

			if (_refs == null)
			{
				return;
			}

			int assetCount = _refs.Select(r => r.AssetPath).Distinct().Count();
			string title = _confirmMode
				? $"Update {_refs.Count} reference(s):  '{_key}'  →  '{_newKey}'"
				: $"References to  '{_key}'   ({_refs.Count} in {assetCount} asset(s))";
			root.Add(new Label(title) { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13, marginBottom = 6, whiteSpace = WhiteSpace.Normal } });

			var scroll = new ScrollView { style = { flexGrow = 1 } };
			if (_refs.Count == 0)
			{
				scroll.Add(new Label("No references found in the project.") { style = { opacity = 0.6f, marginTop = 8 } });
			}
			else
			{
				foreach (var group in _refs.GroupBy(r => r.AssetPath))
				{
					scroll.Add(BuildAssetGroup(group.Key, group.ToList()));
				}
			}
			root.Add(scroll);

			root.Add(BuildFooter());
		}

		private VisualElement BuildAssetGroup(string assetPath, List<Reference> refs)
		{
			var box = new VisualElement { style = { marginBottom = 6, paddingBottom = 4, borderBottomWidth = 1, borderBottomColor = new Color(0, 0, 0, 0.25f) } };

			var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
			var icon = refs[0].IsScene ? EditorIcon.Scene : EditorIcon.Prefab;
			headerRow.Add(new Image { image = icon, style = { width = 16, height = 16, marginRight = 4 } });
			headerRow.Add(new Label(Path.GetFileName(assetPath)) { tooltip = assetPath, style = { unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 } });

			var select = new Button(() =>
			{
				var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
				if (asset != null)
				{
					Selection.activeObject = asset;
					EditorGUIUtility.PingObject(asset);
				}
			})
			{ text = "Select", style = { width = 70 } };
			headerRow.Add(select);
			box.Add(headerRow);

			foreach (var reference in refs)
			{
				var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 22 } };
				row.Add(new Label(reference.ObjectPath) { style = { flexGrow = 1, flexBasis = 0, overflow = Overflow.Hidden, textOverflow = TextOverflow.Ellipsis } });
				var field = reference.IsPlural ? "plural key" : "key";
				row.Add(new Label($"{reference.ComponentType} ({field})") { style = { opacity = 0.65f } });
				box.Add(row);
			}

			return box;
		}

		private VisualElement BuildFooter()
		{
			var footer = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, marginTop = 6 } };

			if (_confirmMode)
			{
				footer.Add(new Button(Close) { text = "Cancel", style = { marginRight = 4 } });

				var apply = new Button(() =>
				{
					var callback = _onApply;
					Close();
					callback?.Invoke();
				})
				{ text = "Apply", style = { minWidth = 90 } };
				apply.SetEnabled(_refs.Count > 0);
				apply.style.backgroundColor = new Color(0.25f, 0.6f, 0.3f);
				apply.style.color = Color.white;
				footer.Add(apply);
			}
			else
			{
				footer.Add(new Button(Close) { text = "Close", style = { minWidth = 90 } });
			}

			return footer;
		}
	}
}

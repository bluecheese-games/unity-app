using BlueCheese.Core.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace BlueCheese.App.Editor
{
	// Non-fallback and for the exact type: takes priority over AssetBaseEditor's editorForChildClasses
	// editor, while still inheriting its Name/Tags/LoadMode header (drawn via OnHeaderGUI, unaffected by
	// this class overriding CreateInspectorGUI for the body).
	//
	// UISettings groups every UI-wide concern (Canvas scaling, UIButton's punch/grayscale/click-SFX
	// defaults...) into one asset so a project doesn't accumulate a separate settings asset per UI
	// feature. A plain default inspector would list every section's fields one after another in a single
	// wall; tabs keep each concern visually isolated without needing a separate asset per tab.
	[CustomEditor(typeof(UISettings))]
	public class UISettingsEditor : AssetBaseEditor
	{
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();
			var tabView = new TabView();

			var canvasTab = new Tab { label = "Canvas" };
			AddChildFields(canvasTab, serializedObject.FindProperty(nameof(UISettings.Canvas)));
			tabView.Add(canvasTab);

			var buttonTab = new Tab { label = "Button" };
			var buttonProperty = serializedObject.FindProperty(nameof(UISettings.Button));
			AddFoldoutSection(buttonTab, buttonProperty, nameof(UISettings.ButtonSection.Punch), "Punch animation");
			AddFoldoutSection(buttonTab, buttonProperty, nameof(UISettings.ButtonSection.DisableState), "Disable state");
			AddFoldoutSection(buttonTab, buttonProperty, nameof(UISettings.ButtonSection.Audio), "Audio");
			tabView.Add(buttonTab);

			root.Add(tabView);
			root.Bind(serializedObject);
			return root;
		}

		private static void AddFoldoutSection(VisualElement parent, SerializedProperty sectionParent, string relativePropertyName, string label)
		{
			var foldout = new Foldout { text = label, value = true, style = { marginTop = 4 } };
			AddChildFields(foldout, sectionParent.FindPropertyRelative(relativePropertyName));
			parent.Add(foldout);
		}

		private static void AddChildFields(VisualElement parent, SerializedProperty property)
		{
			foreach (var child in GetDirectChildren(property))
			{
				parent.Add(new PropertyField(child));
			}
		}

		// PropertyField on the section property itself would draw it as a single collapsed foldout
		// (redundant once it's already isolated in its own tab/foldout); this walks just its direct
		// children so each field appears inline instead.
		private static IEnumerable<SerializedProperty> GetDirectChildren(SerializedProperty parent)
		{
			var current = parent.Copy();
			var end = parent.GetEndProperty();
			bool enterChildren = true;
			while (current.NextVisible(enterChildren) && !SerializedProperty.EqualContents(current, end))
			{
				yield return current.Copy();
				enterChildren = false;
			}
		}
	}
}

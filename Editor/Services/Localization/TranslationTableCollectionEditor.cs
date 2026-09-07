using BlueCheese.Core;
using BlueCheese.Core.Editor;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomEditor(typeof(TranslationTableCollection))]
	public class TranslationTableCollectionEditor : CollectionEditor
	{
		private const float Padding = 4f;

		private static GUIStyle _modifiedStyle;

		// Lazily built: EditorStyles isn't safe to touch outside an OnGUI-ish call (e.g. static init).
		private static GUIStyle ModifiedStyle => _modifiedStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space();
			DrawDuplicates();
		}

		protected override float GetItemHeight(SerializedProperty element, int index)
		{
			if (GetTable(element) is not TranslationTableAsset table)
				return base.GetItemHeight(element, index);

			int lineCount = table.Keys.Count > 0 ? 3 : 2;
			return lineCount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + Padding * 2;
		}

		protected override void DrawItem(Rect rect, SerializedProperty element, int index)
		{
			if (GetTable(element) is not TranslationTableAsset table)
			{
				base.DrawItem(rect, element, index);
				return;
			}

			GUI.Box(rect, GUIContent.none);

			var lineHeight = EditorGUIUtility.singleLineHeight;
			var lineRect = new Rect(rect.x + Padding, rect.y + Padding, rect.width - Padding * 2, lineHeight);

			var openRect = new Rect(lineRect.xMax - 100, lineRect.y, 100, lineHeight);
			var nameRect = new Rect(lineRect.x, lineRect.y, lineRect.width - 108, lineHeight);
			EditorGUI.LabelField(nameRect, table.Name, EditorStyles.boldLabel);
			// TranslationTableCollection is an AutoCollection (not user-editable), but the "Open"
			// action edits the table asset itself, not the collection, so it stays enabled.
			if (GUI.Button(openRect, "Open"))
			{
				TranslationTableWindow.Open(table);
			}

			lineRect.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
			int keyCount = table.Keys.Count;
			EditorGUI.LabelField(new Rect(lineRect.x, lineRect.y, lineRect.width / 2, lineHeight), $"Key Count: {keyCount}");
			EditorGUI.LabelField(new Rect(lineRect.x + lineRect.width / 2, lineRect.y, lineRect.width / 2, lineHeight),
				$"Modified: {table.LastModified.TimeAgo()}", ModifiedStyle);

			if (keyCount > 0)
			{
				lineRect.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
				int validatedCount = table.Count(TranslationStatus.Validated);
				float progress = (float)validatedCount / keyCount;
				EditorGUI.ProgressBar(lineRect, progress, $"Validated: {validatedCount}/{keyCount} ({progress:P0})");
			}
		}

		private static ITranslationTableAsset GetTable(SerializedProperty element)
			=> element.objectReferenceValue as ITranslationTableAsset;

		private void DrawDuplicates()
		{
			var assetFinder = EditorServiceLocator.Resolve<IAssetFinderService>();
			var translationTables = assetFinder
				.FindAssetsInResources<ScriptableObject>()
				.OfType<ITranslationTableAsset>()
				.ToList();
			var duplicateKeys = translationTables
				.Where(t => t.Keys != null)
				.SelectMany(t => t.Keys)
				.GroupBy(k => k)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();
			if (duplicateKeys.Count == 0)
			{
				return;
			}
			EditorGUILayout.HelpBox($"Found {duplicateKeys.Count} duplicate keys", MessageType.Warning);
			EditorGUILayout.BeginVertical("box");
			foreach (var key in duplicateKeys)
			{
				EditorGUILayout.LabelField(key, EditorStyles.boldLabel);
				foreach (var table in translationTables)
				{
					if (table.Keys.Contains(key))
					{
						EditorGUILayout.LabelField($"> In {table.Name}");
					}
				}
			}
			EditorGUILayout.EndVertical();
		}
	}
}

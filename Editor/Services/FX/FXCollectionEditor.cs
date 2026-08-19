//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomEditor(typeof(FXCollection))]
	public class FXCollectionEditor : CollectionEditor
	{
		private const float PrewarmToggleWidth = 85f;
		private const float PoolLabelWidth = 32f;
		private const float PoolFieldWidth = 40f;
		private const float Spacing = 4f;

		protected override float GetItemHeight(SerializedProperty element, int index) => EditorGUIUtility.singleLineHeight;

		protected override void DrawItem(Rect rect, SerializedProperty element, int index)
		{
			var fxDefRect = new Rect(rect.x, rect.y,
				rect.width - PrewarmToggleWidth - PoolLabelWidth - PoolFieldWidth - Spacing * 2, rect.height);
			var prewarmRect = new Rect(fxDefRect.xMax + Spacing, rect.y, PrewarmToggleWidth, rect.height);
			var poolLabelRect = new Rect(prewarmRect.xMax + Spacing, rect.y, PoolLabelWidth, rect.height);
			var poolFieldRect = new Rect(poolLabelRect.xMax, rect.y, PoolFieldWidth, rect.height);

			base.DrawItem(fxDefRect, element, index);

			var fxDef = element.objectReferenceValue as FXDef;
			if (fxDef == null)
			{
				return;
			}

			// Prewarm/PrewarmPoolSize belong to the FXDef asset itself, so they stay editable here
			// even though the collection's reference list (this.IsEditable) is not.
			var fxDefSerialized = new SerializedObject(fxDef);
			fxDefSerialized.Update();

			var prewarmProperty = fxDefSerialized.FindProperty(nameof(FXDef.Prewarm));
			var poolSizeProperty = fxDefSerialized.FindProperty(nameof(FXDef.PrewarmPoolSize));

			prewarmProperty.boolValue = EditorGUI.ToggleLeft(prewarmRect, "Prewarm", prewarmProperty.boolValue);

			EditorGUI.LabelField(poolLabelRect, "Pool:");
			using (new EditorGUI.DisabledScope(!prewarmProperty.boolValue))
			{
				poolSizeProperty.intValue = Mathf.Max(1, EditorGUI.IntField(poolFieldRect, poolSizeProperty.intValue));
			}

			fxDefSerialized.ApplyModifiedProperties();
		}
	}
}

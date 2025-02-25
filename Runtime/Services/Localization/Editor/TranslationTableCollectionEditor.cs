//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils.Editor;
using UnityEditor;

namespace BlueCheese.App
{
	[CustomEditor(typeof(TranslationTable))]
	public class TranslationTableCollectionEditor : CollectionEditor
	{
		public override void OnInspectorGUI() => base.OnInspectorGUI();
	}
}

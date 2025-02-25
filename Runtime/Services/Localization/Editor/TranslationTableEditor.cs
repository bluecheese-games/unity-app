//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomEditor(typeof(TranslationTableAsset))]
	public class TranslationTableEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			// Show button to open the Translation Table window
			if (GUILayout.Button("Open Translation Table"))
			{
				TranslationTableWindow.Open((TranslationTableAsset)target);
			}
		}

		[OnOpenAsset]
		public static bool OnOpenAsset(int instanceID, int _)
		{
			var asset = EditorUtility.InstanceIDToObject(instanceID) as TranslationTableAsset;
			if (asset != null)
			{
				TranslationTableWindow.Open(asset);
				return true;
			}
			return false;
		}
	}
}

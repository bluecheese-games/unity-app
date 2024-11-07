//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomPropertyDrawer(typeof(FX))]
	public class FXPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var def = property.FindPropertyRelative("_fxDef");

			EditorGUI.PropertyField(position, def, label);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
}

//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomPropertyDrawer(typeof(SoundFX))]
	public class SoundFXPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var nameProperty = property.FindPropertyRelative("Name");
			var hasOptionsProperty = property.FindPropertyRelative("HasOptions");
			var optionsProperty = property.FindPropertyRelative("Options");
			var optionsInitializedProperty = optionsProperty.FindPropertyRelative("_isInitialized");
			EditorAudioService audioService = EditorServices.Get<EditorAudioService>();

			// Initialize options if not already initialized
			if (!optionsInitializedProperty.boolValue)
			{
				optionsProperty.boxedValue = SoundOptions.Default;
				optionsInitializedProperty.boolValue = true;
			}

			Rect nameRect = new(position.x, position.y, position.width - 115, EditorGUIUtility.singleLineHeight);
			Rect buttonRect = new(position.x + position.width - 110, position.y, 25, EditorGUIUtility.singleLineHeight);
			Rect hasOptionLabelRect = new(position.x + position.width - 65, position.y, 50, EditorGUIUtility.singleLineHeight);
			Rect hasOptionsRect = new(position.x + position.width - 15, position.y, 15, EditorGUIUtility.singleLineHeight);

			EditorGUI.PrefixLabel(position, label);
			EditorGUI.PropertyField(nameRect, nameProperty, label);
			bool isPlaying = audioService.IsPlaying();

			GUI.color = Color.yellow;
			if (isPlaying && GUI.Button(buttonRect, EditorIcon.Stop))
			{
				audioService.StopAll();
			}
			GUI.color = Color.white;
			if (!isPlaying && GUI.Button(buttonRect, EditorIcon.Play))
			{
				audioService.PlaySound((SoundFX)property.boxedValue);
			}

			EditorGUI.LabelField(hasOptionLabelRect, new GUIContent("Options"));
			EditorGUI.PropertyField(hasOptionsRect, hasOptionsProperty, GUIContent.none);

			if (hasOptionsProperty.boolValue)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(optionsProperty);
				EditorGUI.indentLevel--;
			}
		}
	}
}

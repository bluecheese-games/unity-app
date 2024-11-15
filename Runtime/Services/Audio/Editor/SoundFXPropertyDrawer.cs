//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils.Editor;
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
			string[] keys = audioService.AllSounds;

			// Initialize options if not already initialized
			if (!optionsInitializedProperty.boolValue)
			{
				optionsProperty.boxedValue = SoundOptions.Default;
				optionsInitializedProperty.boolValue = true;
			}

			EditorGUIHelper.DrawSearchableKeyProperty(nameProperty, label, keys);
			bool isPlaying = audioService.IsPlaying();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(" ");

			EditorGUILayout.LabelField("Test", GUILayout.Width(30));
			GUI.color = Color.yellow;
			if (isPlaying && GUILayout.Button(EditorIcon.Stop, GUILayout.Width(30)))
			{
				audioService.StopAll();
			}
			GUI.color = Color.white;
			if (!isPlaying && GUILayout.Button(EditorIcon.Play, GUILayout.Width(30)))
			{
				audioService.PlaySound((SoundFX)property.boxedValue);
			}

			EditorGUILayout.Space(10, true);

			EditorGUILayout.LabelField(new GUIContent("Has options"), GUILayout.Width(70));
			EditorGUILayout.PropertyField(hasOptionsProperty, GUIContent.none, GUILayout.Width(70));

			EditorGUILayout.EndHorizontal();

			if (hasOptionsProperty.boolValue)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(optionsProperty);
				EditorGUI.indentLevel--;
			}
		}
	}
}

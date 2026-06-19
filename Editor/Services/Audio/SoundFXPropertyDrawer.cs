//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Editor;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomPropertyDrawer(typeof(SoundFX))]
	public class SoundFXPropertyDrawer : PropertyDrawer
	{
		private const float PlayButtonWidth = 30f;
		private const float PlayButtonSpacing = 2f;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float line = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;

			// SFX field line + "Has options" line
			float height = line + spacing + line;

			if (property.FindPropertyRelative("HasOptions").boolValue)
			{
				var optionsProperty = property.FindPropertyRelative("Options");
				height += spacing + EditorGUI.GetPropertyHeight(optionsProperty, true);
			}

			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var nameProperty = property.FindPropertyRelative("Name");
			var hasOptionsProperty = property.FindPropertyRelative("HasOptions");
			var optionsProperty = property.FindPropertyRelative("Options");
			var optionsInitializedProperty = optionsProperty.FindPropertyRelative("_isInitialized");
			EditorAudioService audioService = EditorServiceLocator.Get<EditorAudioService>();

			// Initialize options if not already initialized
			if (!optionsInitializedProperty.boolValue)
			{
				optionsProperty.boxedValue = SoundOptions.Default;
				optionsInitializedProperty.boolValue = true;
			}

			// Prepend a "None" entry mapping to an empty key so the sound can be cleared.
			string[] sounds = audioService.AllSounds;
			string[] keys = sounds.Prepend(string.Empty).ToArray();
			string[] labels = sounds.Prepend("None").ToArray();

			float line = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;

			// Line 1: SFX selection field + play/stop button on the right
			var fieldRect = new Rect(position.x, position.y, position.width - PlayButtonWidth - PlayButtonSpacing, line);
			var buttonRect = new Rect(position.xMax - PlayButtonWidth, position.y, PlayButtonWidth, line);

			EditorGUIHelper.DrawSearchableKeyProperty(fieldRect, nameProperty, label, keys, labels);

			if (audioService.IsPlaying())
			{
				var prevColor = GUI.color;
				GUI.color = Color.yellow;
				if (GUI.Button(buttonRect, new GUIContent(EditorIcon.Stop, "Stop")))
				{
					audioService.StopAll();
				}
				GUI.color = prevColor;
			}
			else
			{
				using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(nameProperty.stringValue)))
				{
					if (GUI.Button(buttonRect, new GUIContent(EditorIcon.Play, "Play")))
					{
						audioService.PlaySound((SoundFX)property.boxedValue);
					}
				}
			}

			// Line 2: Has options toggle
			var hasOptionsRect = new Rect(position.x, position.y + line + spacing, position.width, line);
			EditorGUI.PropertyField(hasOptionsRect, hasOptionsProperty, new GUIContent("Has options"));

			// Options block
			if (hasOptionsProperty.boolValue)
			{
				var optionsRect = new Rect(position.x, hasOptionsRect.yMax + spacing, position.width, EditorGUI.GetPropertyHeight(optionsProperty, true));
				EditorGUI.indentLevel++;
				EditorGUI.PropertyField(optionsRect, optionsProperty, true);
				EditorGUI.indentLevel--;
			}
		}
	}
}

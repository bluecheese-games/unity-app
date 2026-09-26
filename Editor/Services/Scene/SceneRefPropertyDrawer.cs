using BlueCheese.Core.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomPropertyDrawer(typeof(SceneRef))]
	public class SceneRefPropertyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			// Draw the label, then let the field fill the remaining (narrowed) rect.
			var fieldRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			// Draw the scene reference field
			var nameProperty = property.FindPropertyRelative(nameof(SceneRef.Name));
			var sceneNames = GetSceneNames().ToArray();

			EditorGUIHelper.DrawSearchableKeyProperty(fieldRect, nameProperty, GUIContent.none, sceneNames, sceneNames);

			EditorGUI.EndProperty();
		}

		private IEnumerable<string> GetSceneNames()
		{
			yield return SceneRef.None; // Option for no scene selected

			foreach (var scene in EditorBuildSettings.scenes)
			{
				if (scene.enabled)
				{
					yield return Path.GetFileNameWithoutExtension(scene.path);
				}
			}
		}
	}
}

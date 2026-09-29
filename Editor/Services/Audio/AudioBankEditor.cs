using BlueCheese.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Custom row rendering for <see cref="AudioBank"/>'s items list. The base <see cref="CollectionEditor"/>
	/// draws each element via a plain <c>PropertyField(..., GUIContent.none, includeChildren: true)</c>, which
	/// for a collapsed nested class (the default state of a freshly added <see cref="AudioItem"/>) renders as
	/// nothing but a bare foldout arrow -- no Name/Clip/Volume visible at all until manually expanded item by
	/// item. This draws those three fields inline instead, plus a Play/Stop button (via
	/// <see cref="EditorAudioService.PlayClip"/>, same pattern as <see cref="SoundFXPropertyDrawer"/>) to
	/// preview each entry without entering Play Mode.
	/// </summary>
	[CustomEditor(typeof(AudioBank))]
	public class AudioBankEditor : CollectionEditor
	{
		private const float PlayButtonWidth = 30f;
		private const float Spacing = 4f;

		protected override float GetItemHeight(SerializedProperty element, int index)
		{
			float line = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;
			return line + spacing + line; // Name (+ Play button) line, Clip + Volume line
		}

		protected override void DrawItem(Rect rect, SerializedProperty element, int index)
		{
			var nameProperty = element.FindPropertyRelative(nameof(AudioItem.Name));
			var clipProperty = element.FindPropertyRelative(nameof(AudioItem.Clip));
			var volumeProperty = element.FindPropertyRelative(nameof(AudioItem.Volume));

			float line = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;

			var nameRect = new Rect(rect.x, rect.y, rect.width - PlayButtonWidth - Spacing, line);
			var playRect = new Rect(rect.xMax - PlayButtonWidth, rect.y, PlayButtonWidth, line);

			float secondLineY = rect.y + line + spacing;
			float clipWidth = rect.width * 0.7f - Spacing * 0.5f;
			var clipRect = new Rect(rect.x, secondLineY, clipWidth, line);
			var volumeRect = new Rect(rect.x + clipWidth + Spacing, secondLineY, rect.width - clipWidth - Spacing, line);

			EditorGUI.PropertyField(nameRect, nameProperty, GUIContent.none);
			EditorGUI.PropertyField(clipRect, clipProperty, GUIContent.none);
			EditorGUI.PropertyField(volumeRect, volumeProperty, GUIContent.none);

			DrawPlayButton(playRect, (AudioClip)clipProperty.objectReferenceValue, volumeProperty.floatValue);
		}

		private static void DrawPlayButton(Rect rect, AudioClip clip, float volume)
		{
			var audioService = EditorServiceLocator.Resolve<EditorAudioService>();
			bool isPlaying = audioService.IsPlaying();

			var prevColor = GUI.color;
			if (isPlaying) GUI.color = Color.yellow;

			var icon = isPlaying ? EditorIcon.Stop : EditorIcon.Play;
			using (new EditorGUI.DisabledScope(!isPlaying && clip == null))
			{
				if (GUI.Button(rect, new GUIContent(icon, isPlaying ? "Stop" : "Play")))
				{
					if (isPlaying) audioService.StopAll();
					else audioService.PlayClip(clip, volume);
				}
			}

			GUI.color = prevColor;
		}
	}
}

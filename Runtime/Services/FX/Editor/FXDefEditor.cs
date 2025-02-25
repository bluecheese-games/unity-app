//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	[CustomEditor(typeof(FXDef))]
	public class FXDefEditor : UnityEditor.Editor
	{
		private Color[] _backgroundColors = new[] { Color.black, Color.gray, Color.white };

		public FXDef FXDef => (FXDef)target;

		private GameObject _previewObject;
		private ParticleSystem _particleSystem;
		private Camera _previewCamera;
		private RenderTexture _previewTexture;

		private bool _isPreviewReady = false;
		private float _time = 0;
		private bool _isPlaying = false;
		private bool _restart = true;
		private DateTime _previousTime = DateTime.Now;

		private SerializedProperty _prefabProperty;
		private SerializedProperty _overrideDurationProperty;
		private SerializedProperty _durationProperty;
		private SerializedProperty _scalersProperty;
		private int _selectedScalerIndex = 0;

		private void OnEnable()
		{
			CleanupPreview();
			SetupPreview();
			RestartPreview();

			_prefabProperty = serializedObject.FindProperty(nameof(FXDef.Prefab));
			_overrideDurationProperty = serializedObject.FindProperty(nameof(FXDef.OverrideDuration));
			_durationProperty = serializedObject.FindProperty(nameof(FXDef.Duration));
			_scalersProperty = serializedObject.FindProperty(nameof(FXDef.Scalers));
		}

		private void SetupPreview()
		{
			if (_isPreviewReady)
			{
				return;
			}

			if (FXDef.Prefab == null)
			{
				return;
			}
			if (!FXDef.Prefab.GetComponent<ParticleSystem>())
			{
				return;
			}

			_previewObject = Instantiate(FXDef.Prefab);
			_previewObject.transform.SetPositionAndRotation(Vector3.down * 200, default);
			_previewObject.hideFlags = HideFlags.HideAndDontSave;

			_particleSystem = _previewObject.GetComponent<ParticleSystem>();

			_previewTexture = new RenderTexture(256, 256, 16);

			_previewCamera = new GameObject("Preview Camera").AddComponent<Camera>();
			_previewCamera.enabled = false;
			_previewCamera.targetTexture = _previewTexture;
			_previewCamera.clearFlags = CameraClearFlags.Color;
			_previewCamera.backgroundColor = Color.black;
			_previewCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;

			_isPreviewReady = true;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(_prefabProperty);

			if (FXDef.Prefab == null)
			{
				serializedObject.ApplyModifiedProperties();
				return;
			}

			if (!FXDef.Prefab.GetComponent<ParticleSystem>())
			{
				EditorGUILayout.HelpBox("The prefab must have a ParticleSystem component", MessageType.Error);
				serializedObject.ApplyModifiedProperties();
				return;
			}

			EditorGUILayout.PropertyField(_overrideDurationProperty);
			GUI.enabled = FXDef.OverrideDuration;
			EditorGUILayout.PropertyField(_durationProperty);
			GUI.enabled = true;

			DrawScalers();

			serializedObject.ApplyModifiedProperties();

			if (!_isPreviewReady)
			{
				return;
			}

			var now = DateTime.Now;
			double deltaTime = (now - _previousTime).TotalMilliseconds;
			_previousTime = now;

			if (_isPlaying)
			{
				_time += (float)deltaTime / 1000;
				if (_time > FXDef.Duration)
				{
					if (!_particleSystem.main.loop)
					{
						_time = FXDef.Duration;
						_isPlaying = false;
					}
				}
			}
			_particleSystem.Simulate(_time, true, _restart);
			SceneView.RepaintAll();
		}

		private void DrawScalers()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Scalers", EditorStyles.boldLabel);
			for (int i = 0; i < _scalersProperty.arraySize; i++)
			{
				var scalerProperty = _scalersProperty.GetArrayElementAtIndex(i);
				var typeProperty = scalerProperty.FindPropertyRelative(nameof(FXScaler.type));
				var curveProperty = scalerProperty.FindPropertyRelative(nameof(FXScaler.curve));

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(((FXScaler.Type)typeProperty.enumValueIndex).ToString());
				EditorGUILayout.PropertyField(curveProperty, GUIContent.none);
				if (GUILayout.Button(EditorIcon.Cross, GUILayout.Width(30), GUILayout.Height(18)))
				{
					_scalersProperty.DeleteArrayElementAtIndex(i);
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();

			// Extract current scalers and convert to a HashSet for efficient lookup
			HashSet<FXScaler.Type> currentScalers = FXDef.Scalers.Select(s => s.type).ToHashSet();
			currentScalers.Add(FXScaler.Type.None);

			// Get all possible scalers, excluding 'None', and filter out the current scalers
			List<string> availableScalers = Enum.GetValues(typeof(FXScaler.Type))
				.Cast<FXScaler.Type>()
				.Except(currentScalers)
				.Select(x => x.ToString())
				.ToList();

			// Insert the placeholder at the beginning
			availableScalers.Insert(0, "< Add Scaler >");

			// Convert to array
			var scalers = availableScalers.ToArray();

			_selectedScalerIndex = EditorGUILayout.Popup(_selectedScalerIndex, scalers);
			if (_selectedScalerIndex > 0)
			{
				_scalersProperty.InsertArrayElementAtIndex(_scalersProperty.arraySize);
				var newScalerProperty = _scalersProperty.GetArrayElementAtIndex(_scalersProperty.arraySize - 1);
				var newTypeProperty = newScalerProperty.FindPropertyRelative(nameof(FXScaler.type));
				var newCurveProperty = newScalerProperty.FindPropertyRelative(nameof(FXScaler.curve));
				newTypeProperty.enumValueIndex = (int)Enum.Parse<FXScaler.Type>(scalers[_selectedScalerIndex]);
				newCurveProperty.animationCurveValue = AnimationCurve.Constant(0, 1, 1);
				_selectedScalerIndex = 0;
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}

		public override bool RequiresConstantRepaint() => _isPreviewReady && _isPlaying;

		public override bool HasPreviewGUI() => _isPreviewReady;

		public override void OnInteractivePreviewGUI(Rect rect, GUIStyle background)
		{
			if (!_isPreviewReady) return;

			_particleSystem.transform.position = Vector3.up * 2f;
			_previewObject.layer = 30;

			// Set camera position and render the preview
			var vect = -_particleSystem.transform.forward * 5f + Vector3.up * 2f;
			vect *= FXDef._previewSettings.zoom;
			_previewCamera.backgroundColor = FXDef._previewSettings.backgroundColor;
			_previewCamera.transform.position = _particleSystem.transform.position + vect;
			_previewCamera.transform.LookAt(_particleSystem.transform.position);
			_previewCamera.clearFlags = FXDef._previewSettings.showSkybox ? CameraClearFlags.Skybox : CameraClearFlags.Color;
			_previewCamera.overrideSceneCullingMask = (ulong)(1 << _previewObject.layer);
			_previewCamera.Render();

			// Draw the RenderTexture in the preview area
			GUI.DrawTexture(rect, _previewTexture, ScaleMode.ScaleToFit, false);

			// Header
			GUI.Label(new Rect(rect.xMin, rect.yMin, rect.width, 30), GUIContent.none, EditorStyles.textArea);

			if (GUI.Button(new Rect(rect.xMin + 10, rect.yMin + 5, 30, 20), EditorIcon.Restart))
			{
				RestartPreview();
			}
			if (_isPlaying && GUI.Button(new Rect(rect.xMin + 50, rect.yMin + 5, 30, 20), EditorIcon.Stop))
			{
				StopPreview();
			}
			else if (!_isPlaying && GUI.Button(new Rect(rect.xMin + 50, rect.yMin + 5, 30, 20), EditorIcon.Play))
			{
				PlayPreview();
			}

			GUI.enabled = !_isPlaying;
			_time = GUI.HorizontalSlider(new Rect(rect.xMin + 100, rect.yMin + 5, rect.width - 120, 20), _time, 0f, _particleSystem.main.duration);
			GUI.enabled = true;

			// Footer
			GUI.Label(new Rect(rect.xMin, rect.yMax - 30, rect.width, 30), GUIContent.none, EditorStyles.textArea);
			GUI.Label(new Rect(rect.xMin + 10, rect.yMax - 25, 50, 20), "Zoom");
			FXDef._previewSettings.zoom = GUI.HorizontalSlider(new Rect(rect.xMin + 60, rect.yMax - 25, 100, 20), FXDef._previewSettings.zoom, 0.5f, 3f);

			int index = 0;
			for (; index < _backgroundColors.Length; index++)
			{
				Color color = _backgroundColors[index];
				ShowFooterButton(rect, index, color, EditorIcon.Valid, color == FXDef._previewSettings.backgroundColor, () =>
				{
					FXDef._previewSettings.backgroundColor = color;
				});
			}

			ShowFooterButton(rect, index, Color.white, EditorIcon.Skybox, FXDef._previewSettings.showSkybox, () =>
			{
				FXDef._previewSettings.showSkybox = !FXDef._previewSettings.showSkybox;
			});
		}

		private void ShowFooterButton(Rect rect, int index, Color bgColor, Texture2D icon, bool isSelected, Action onClick)
		{
			if (isSelected)
			{
				GUI.Label(new Rect(rect.xMin + 168 + index * 35, rect.yMax - 27, 34, 24), GUIContent.none, EditorStyles.selectionRect);
			}

			GUI.backgroundColor = bgColor;
			if (GUI.Button(new Rect(rect.xMin + 170 + index * 35, rect.yMax - 25, 30, 20), icon))
			{
				onClick();
			}
			GUI.backgroundColor = Color.white;
		}

		private void RestartPreview()
		{
			_time = 0f;
			_restart = true;
			_isPlaying = true;
		}

		private void PlayPreview()
		{
			_isPlaying = true;
			if (_time >= _particleSystem.main.duration)
			{
				RestartPreview();
			}
		}

		private void StopPreview()
		{
			_isPlaying = false;
		}

		private void OnDisable()
		{
			CleanupPreview();
		}

		private void CleanupPreview()
		{
			_isPreviewReady = false;

			if (_previewCamera != null)
			{
				DestroyImmediate(_previewCamera.gameObject);
				_previewCamera = null;
			}

			if (_previewTexture != null)
			{
				_previewTexture.Release();
				DestroyImmediate(_previewTexture);
				_previewTexture = null;
			}

			if (_previewObject != null)
			{
				DestroyImmediate(_previewObject.gameObject);
				_previewObject = null;
			}
		}
	}
}

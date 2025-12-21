//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Editor;
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
		private readonly Color[] _backgroundColors = new[] { Color.black, Color.gray, Color.white };

		public FXDef FXDef => (FXDef)target;

		private GameObject _previewObject;
		private ParticleSystem _particleSystem;
		private Camera _previewCamera;
		private RenderTexture _previewTexture;

		private bool _isPreviewReady = false;
		private float _time = 0f;
		private bool _isPlaying = false;
		private bool _restart = true;
		private double _lastTicks;

		private SerializedProperty _prefabProperty;
		private SerializedProperty _overrideDurationProperty;
		private SerializedProperty _durationProperty;
		private SerializedProperty _scalersProperty;
		private int _selectedScalerIndex = 0;

		private Vector2Int _rtSize;

		private void OnEnable()
		{
			CleanupPreview();
			SetupPreview();
			RestartPreview();

			_prefabProperty = serializedObject.FindProperty(nameof(FXDef.Prefab));
			_overrideDurationProperty = serializedObject.FindProperty(nameof(FXDef.OverrideDuration));
			_durationProperty = serializedObject.FindProperty(nameof(FXDef.Duration));
			_scalersProperty = serializedObject.FindProperty(nameof(FXDef.Scalers));

			_lastTicks = EditorApplication.timeSinceStartup;
		}

		private void SetupPreview()
		{
			if (_isPreviewReady) return;
			if (FXDef.Prefab == null) return;
			if (!FXDef.Prefab.GetComponent<ParticleSystem>()) return;

			_previewObject = Instantiate(FXDef.Prefab);
			_previewObject.transform.SetPositionAndRotation(Vector3.down * 200f, default);
			_previewObject.hideFlags = HideFlags.HideAndDontSave | HideFlags.NotEditable;

			// Put object and children on a private layer for the preview camera
			int layer = LayerMask.NameToLayer("FXPreview");
			if (layer < 0) layer = 30; // fallback layer
			SetLayerRecursively(_previewObject, layer);

			_particleSystem = _previewObject.GetComponent<ParticleSystem>();

			_previewCamera = new GameObject("Preview Camera").AddComponent<Camera>();
			_previewCamera.enabled = false;
			_previewCamera.clearFlags = CameraClearFlags.Color;
			_previewCamera.backgroundColor = Color.black;
			_previewCamera.cullingMask = 1 << layer;
			_previewCamera.gameObject.hideFlags = HideFlags.HideAndDontSave | HideFlags.NotEditable;

			_isPreviewReady = true;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			// Prefab field with rebuild on change
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(_prefabProperty);
			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
				CleanupPreview();
				SetupPreview();
				RestartPreview();
				return; // avoid using stale state this frame
			}

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
			using (new EditorGUI.DisabledScope(!_overrideDurationProperty.boolValue))
			{
				EditorGUILayout.PropertyField(_durationProperty);
			}

			DrawScalers();

			serializedObject.ApplyModifiedProperties();

			if (!_isPreviewReady || _particleSystem == null)
				return;

			// Time update and simulation
			double now = EditorApplication.timeSinceStartup;
			double deltaTime = now - _lastTicks;
			_lastTicks = now;

			if (_isPlaying)
			{
				_time += (float)deltaTime;
				var main = _particleSystem.main;
				float clampDuration = main.duration > 0f ? main.duration : Mathf.Max(0.01f, FXDef.Duration);
				if (_time > clampDuration)
				{
					if (!main.loop)
					{
						_time = clampDuration;
						_isPlaying = false;
					}
				}
			}

			_particleSystem.Simulate(_time, withChildren: true, restart: _restart);
			_restart = false; // restart only on the first simulate after a reset
			SceneView.RepaintAll();
		}

		private void DrawScalers()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Scalers", EditorStyles.boldLabel);

			// Existing scalers list
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
					// Make it undoable and ensure the change is committed this frame
					Undo.RecordObject(target, "Remove Scaler");
					_scalersProperty.DeleteArrayElementAtIndex(i);
					serializedObject.ApplyModifiedProperties();
					EditorUtility.SetDirty(target);
					GUIUtility.ExitGUI(); // bail to avoid layout issues after structural change
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();

			// Build current type set from SerializedProperty to avoid nulls
			HashSet<FXScaler.Type> currentScalers = new HashSet<FXScaler.Type>();
			for (int i = 0; i < _scalersProperty.arraySize; i++)
			{
				var scalerProperty = _scalersProperty.GetArrayElementAtIndex(i);
				var typeProperty = scalerProperty.FindPropertyRelative(nameof(FXScaler.type));
				currentScalers.Add((FXScaler.Type)typeProperty.enumValueIndex);
			}
			currentScalers.Add(FXScaler.Type.None); // never add 'None'

			var availableScalers = Enum.GetValues(typeof(FXScaler.Type))
				.Cast<FXScaler.Type>()
				.Except(currentScalers)
				.Select(x => x.ToString())
				.ToList();

			availableScalers.Insert(0, "< Add Scaler >");
			var scalers = availableScalers.ToArray();

			_selectedScalerIndex = EditorGUILayout.Popup(_selectedScalerIndex, scalers);
			if (_selectedScalerIndex > 0)
			{
				_scalersProperty.InsertArrayElementAtIndex(_scalersProperty.arraySize);
				var newScalerProperty = _scalersProperty.GetArrayElementAtIndex(_scalersProperty.arraySize - 1);
				newScalerProperty.FindPropertyRelative(nameof(FXScaler.type)).enumValueIndex = (int)Enum.Parse<FXScaler.Type>(scalers[_selectedScalerIndex]);
				newScalerProperty.FindPropertyRelative(nameof(FXScaler.curve)).animationCurveValue = AnimationCurve.Constant(0, 1, 1);
				_selectedScalerIndex = 0;
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}

		public override bool RequiresConstantRepaint() => _isPreviewReady && _isPlaying;

		public override bool HasPreviewGUI() => _isPreviewReady && _particleSystem != null;

		public override void OnInteractivePreviewGUI(Rect rect, GUIStyle background)
		{
			if (!_isPreviewReady || _particleSystem == null) return;

			EnsureRTSize(rect);

			_particleSystem.transform.position = Vector3.up * 2f;
			// Camera placement
			var vect = -_particleSystem.transform.forward * 5f + Vector3.up * 2f;
			vect *= FXDef._previewSettings.zoom;

			_previewCamera.backgroundColor = FXDef._previewSettings.backgroundColor;
			_previewCamera.transform.position = _particleSystem.transform.position + vect;
			_previewCamera.transform.LookAt(_particleSystem.transform.position);
			_previewCamera.clearFlags = FXDef._previewSettings.showSkybox ? CameraClearFlags.Skybox : CameraClearFlags.Color;
			_previewCamera.targetTexture = _previewTexture;
			_previewCamera.Render();

			// Draw the texture
			GUI.DrawTexture(rect, _previewTexture, ScaleMode.ScaleToFit, false);

			// Header bar
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

			using (new EditorGUI.DisabledScope(_isPlaying))
			{
				float sliderMax = Mathf.Max(0.01f, _particleSystem.main.duration);
				_time = GUI.HorizontalSlider(new Rect(rect.xMin + 100, rect.yMin + 5, rect.width - 160, 20), _time, 0f, sliderMax);
			}

			GUI.Label(new Rect(rect.xMax - 55, rect.yMin + 5, 50, 20), $"{_time:0.00}s");

			// Footer bar
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

		private void EnsureRTSize(Rect rect)
		{
			int w = Mathf.Clamp(Mathf.CeilToInt(rect.width), 64, 2048);
			int h = Mathf.Clamp(Mathf.CeilToInt(rect.height), 64, 2048);
			if (_previewTexture == null || _rtSize.x != w || _rtSize.y != h)
			{
				if (_previewTexture != null)
				{
					_previewCamera.targetTexture = null;
					_previewTexture.Release();
					DestroyImmediate(_previewTexture);
				}
				_previewTexture = new RenderTexture(w, h, 16);
				_rtSize = new Vector2Int(w, h);
			}
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
			if (_particleSystem != null && _time >= Mathf.Max(0.01f, _particleSystem.main.duration))
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
				_previewCamera.targetTexture = null;
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

		private static void SetLayerRecursively(GameObject go, int layer)
		{
			go.layer = layer;
			var transforms = go.GetComponentsInChildren<Transform>(true);
			for (int i = 0; i < transforms.Length; i++)
				transforms[i].gameObject.layer = layer;
		}
	}
}
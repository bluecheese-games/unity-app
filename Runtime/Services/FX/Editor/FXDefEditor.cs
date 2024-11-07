//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Editor;
using System;
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
		private float _previewRatio = 0f;

		private void OnEnable()
		{
			Debug.Log("OnEnable");
			CleanupPreview();
			SetupPreview();
			RestartPreview();
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
			DrawDefaultInspector();

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

		public override bool RequiresConstantRepaint() => _isPreviewReady && _isPlaying;

		public override bool HasPreviewGUI() => _isPreviewReady;

		public override void OnInteractivePreviewGUI(Rect rect, GUIStyle background)
		{
			if (!_isPreviewReady) return;

			// Set camera position and render the preview
			var vect = -_particleSystem.transform.forward * 5f + Vector3.up * 2f;
			vect *= FXDef._previewSettings.zoom;
			_previewCamera.backgroundColor = FXDef._previewSettings.backgroundColor;
			_previewCamera.transform.position = _particleSystem.transform.position + vect;
			_previewCamera.transform.LookAt(_particleSystem.transform.position);
			_previewCamera.Render();

			// Draw the RenderTexture in the preview area
			GUI.DrawTexture(rect, _previewTexture, ScaleMode.ScaleToFit, false);

			// Header
			GUI.Label(new Rect(rect.xMin, rect.yMin, rect.width, 30), GUIContent.none, EditorStyles.helpBox);

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
			GUI.Label(new Rect(rect.xMin, rect.yMax - 30, rect.width, 30), GUIContent.none, EditorStyles.helpBox);
			GUI.Label(new Rect(rect.xMin + 10, rect.yMax - 25, 50, 20), "Zoom");
			FXDef._previewSettings.zoom = GUI.HorizontalSlider(new Rect(rect.xMin + 60, rect.yMax - 25, 100, 20), FXDef._previewSettings.zoom, 0.5f, 3f);

			for (int i = 0; i < _backgroundColors.Length; i++)
			{
				Color color = _backgroundColors[i];
				if (color == FXDef._previewSettings.backgroundColor)
				{
					GUI.Label(new Rect(rect.xMin + 168 + i * 35, rect.yMax - 27, 34, 24), GUIContent.none, EditorStyles.selectionRect);
				}

				GUI.backgroundColor = color;
				if (GUI.Button(new Rect(rect.xMin + 170 + i * 35, rect.yMax - 25, 30, 20), EditorIcon.Valid))
				{
					FXDef._previewSettings.backgroundColor = color;
					_previewCamera.backgroundColor = color;
				}
				GUI.backgroundColor = Color.white;
			}
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

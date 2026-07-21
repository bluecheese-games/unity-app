//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Editor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.App.Editor
{
	[CustomEditor(typeof(FXDef))]
	public class FXDefEditor : AssetBaseEditor
	{
		// Guards against huge jumps in simulated FX time if the editor stalls
		// (recompiling, importing assets, dragging a window, etc.).
		private const float MaxDeltaTime = 0.1f;

		// Caps how often the preview actually simulates/renders/repaints. EditorApplication.update
		// is not vsynced and can fire at a very high, uncapped rate; ~30 fps is plenty for a
		// particle preview and avoids pegging a CPU core.
		private const double TargetFrameInterval = 1.0 / 30.0;

		private readonly Color[] _backgroundColors = new[] { Color.black, Color.gray, Color.white };

		public FXDef TargetFXDef => (FXDef)target;

		// Isolated preview scene/camera, replacing the old "instantiate far below the real scene
		// on a custom layer" hack. No project-specific layer setup required, and the preview
		// object never actually exists in the edited scene.
		private PreviewRenderUtility _previewUtility;
		private GameObject _previewObject;
		private ParticleSystem _particleSystem;

		private bool _isPreviewReady = false;
		private float _time = 0f;
		private bool _isPlaying = false;
		private bool _restart = true;
		private double _lastTicks;

		// Set by OnInteractivePreviewGUI, consumed (and reset) by OnEditorUpdate. Lets us skip
		// simulating/repainting entirely while the preview panel isn't actually being drawn
		// (collapsed foldout, inspector not visible, another tab focused, etc.).
		private bool _previewVisibleThisFrame;

		private SerializedProperty _overrideDurationProperty;
		private SerializedProperty _scalersProperty;

		private int _cachedScalerTypesMask = -1;
		private List<string> _cachedScalerOptions;

		protected override void OnEnable()
		{
			base.OnEnable();

			CleanupPreview();
			SetupPreview();
			RestartPreview();

			_overrideDurationProperty = serializedObject.FindProperty(nameof(TargetFXDef.OverrideDuration));
			_scalersProperty = serializedObject.FindProperty(nameof(TargetFXDef.Scalers));

			_lastTicks = EditorApplication.timeSinceStartup;

			EditorApplication.update += OnEditorUpdate;
			PrefabStage.prefabSaved += OnPrefabSaved;
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			EditorApplication.update -= OnEditorUpdate;
			PrefabStage.prefabSaved -= OnPrefabSaved;

			CleanupPreview();
		}

		private void OnPrefabSaved(GameObject savedPrefabRoot)
		{
			// Keeps the preview in sync when the artist tweaks the FX prefab directly from Prefab
			// Mode (curves, emission, etc.) and saves, instead of requiring them to reselect the
			// Prefab field to force a rebuild.
			if (TargetFXDef.Prefab == null || savedPrefabRoot == null)
			{
				return;
			}

			string savedPath = AssetDatabase.GetAssetPath(savedPrefabRoot);
			string ourPath = AssetDatabase.GetAssetPath(TargetFXDef.Prefab);
			if (string.IsNullOrEmpty(savedPath) || savedPath != ourPath)
			{
				return;
			}

			CleanupPreview();
			SetupPreview();
			RestartPreview();
			Repaint();
		}

		private void SetupPreview()
		{
			if (_isPreviewReady) return;
			if (TargetFXDef.Prefab == null) return;
			if (!TargetFXDef.Prefab.GetComponent<ParticleSystem>()) return;

			_previewUtility = new PreviewRenderUtility();
			_previewUtility.camera.nearClipPlane = 0.05f;
			_previewUtility.camera.farClipPlane = 1000f;
			_previewUtility.camera.fieldOfView = 30f;

			_previewObject = UnityEngine.Object.Instantiate(TargetFXDef.Prefab);
			_previewUtility.AddSingleGO(_previewObject);

			_particleSystem = _previewObject.GetComponent<ParticleSystem>();

			_isPreviewReady = true;
		}

		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();

			var prefabField = new PropertyField(serializedObject.FindProperty(nameof(TargetFXDef.Prefab)));
			root.Add(prefabField);

			var missingParticleSystemBox = new HelpBox("The prefab must have a ParticleSystem component", HelpBoxMessageType.Error);
			root.Add(missingParticleSystemBox);

			var overrideDurationField = new PropertyField(_overrideDurationProperty);
			root.Add(overrideDurationField);

			var durationField = new PropertyField(serializedObject.FindProperty(nameof(TargetFXDef.Duration)));
			root.Add(durationField);

			var scalersContainer = new VisualElement();
			root.Add(scalersContainer);

			void RefreshFields()
			{
				bool hasPrefab = TargetFXDef.Prefab != null;
				bool hasParticleSystem = hasPrefab && TargetFXDef.Prefab.GetComponent<ParticleSystem>() != null;

				missingParticleSystemBox.style.display = (hasPrefab && !hasParticleSystem) ? DisplayStyle.Flex : DisplayStyle.None;
				overrideDurationField.style.display = hasParticleSystem ? DisplayStyle.Flex : DisplayStyle.None;
				durationField.style.display = hasParticleSystem ? DisplayStyle.Flex : DisplayStyle.None;
				durationField.SetEnabled(_overrideDurationProperty.boolValue);
				scalersContainer.style.display = hasParticleSystem ? DisplayStyle.Flex : DisplayStyle.None;
			}

			prefabField.RegisterValueChangeCallback(_ =>
			{
				serializedObject.ApplyModifiedProperties();
				CleanupPreview();
				SetupPreview();
				RestartPreview();
				RefreshFields();
				RebuildScalersUI(scalersContainer);
			});

			overrideDurationField.RegisterValueChangeCallback(evt => durationField.SetEnabled(evt.changedProperty.boolValue));

			RefreshFields();
			RebuildScalersUI(scalersContainer);

			return root;
		}

		private void RebuildScalersUI(VisualElement container)
		{
			container.Clear();

			var box = new Box();
			box.Add(new Label("Scalers") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });

			for (int i = 0; i < _scalersProperty.arraySize; i++)
			{
				int index = i; // captured by the remove-button closure below
				var scalerProperty = _scalersProperty.GetArrayElementAtIndex(i);
				var typeProperty = scalerProperty.FindPropertyRelative(nameof(FXScaler.type));
				var curveProperty = scalerProperty.FindPropertyRelative(nameof(FXScaler.curve));

				var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

				row.Add(new Label(((FXScaler.Type)typeProperty.enumValueIndex).ToString())
				{
					style = { width = 100 }
				});

				var curveField = new PropertyField(curveProperty, string.Empty) { style = { flexGrow = 1 } };
				row.Add(curveField);

				var removeButton = new Button(() =>
				{
					Undo.RecordObject(target, "Remove Scaler");
					_scalersProperty.DeleteArrayElementAtIndex(index);
					serializedObject.ApplyModifiedProperties();
					EditorUtility.SetDirty(target);
					InvalidateScalerOptionsCache();
					RebuildScalersUI(container);
				})
				{
					text = "x",
					style = { width = 24 }
				};
				row.Add(removeButton);

				box.Add(row);
			}

			var options = GetAvailableScalerOptions();
			var dropdown = new DropdownField(options, 0);
			dropdown.RegisterValueChangedCallback(evt =>
			{
				if (Enum.TryParse(evt.newValue, out FXScaler.Type type) && type != FXScaler.Type.None)
				{
					_scalersProperty.InsertArrayElementAtIndex(_scalersProperty.arraySize);
					var newElement = _scalersProperty.GetArrayElementAtIndex(_scalersProperty.arraySize - 1);
					newElement.FindPropertyRelative(nameof(FXScaler.type)).enumValueIndex = (int)type;
					newElement.FindPropertyRelative(nameof(FXScaler.curve)).animationCurveValue = AnimationCurve.Constant(0, 1, 1);
					serializedObject.ApplyModifiedProperties();
					InvalidateScalerOptionsCache();
					RebuildScalersUI(container);
				}
				else
				{
					dropdown.SetValueWithoutNotify(options[0]);
				}
			});
			box.Add(dropdown);

			container.Add(box);
		}

		private void InvalidateScalerOptionsCache() => _cachedScalerTypesMask = -1;

		// Recomputes which FXScaler.Type values are still available to add (everything except
		// 'None' and types already present on this FXDef). Cached behind a cheap bitmask of
		// currently-used types so we don't rebuild the list (Enum.GetValues + string alloc) every
		// time this is called; only invalidated when a scaler is actually added or removed.
		private List<string> GetAvailableScalerOptions()
		{
			int mask = 0;
			for (int i = 0; i < _scalersProperty.arraySize; i++)
			{
				var typeProperty = _scalersProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(FXScaler.type));
				mask |= 1 << typeProperty.enumValueIndex;
			}

			if (mask == _cachedScalerTypesMask && _cachedScalerOptions != null)
			{
				return _cachedScalerOptions;
			}

			_cachedScalerTypesMask = mask;

			var values = (FXScaler.Type[])Enum.GetValues(typeof(FXScaler.Type));
			var options = new List<string>(values.Length) { "< Add Scaler >" };
			foreach (var type in values)
			{
				if (type == FXScaler.Type.None) continue;
				if ((mask & (1 << (int)type)) != 0) continue;
				options.Add(type.ToString());
			}

			_cachedScalerOptions = options;
			return options;
		}

		private void OnEditorUpdate()
		{
			// Cheapest check first: while paused, there's nothing to animate, so don't force the
			// editor to keep repainting. The FX autoplays on selection (RestartPreview), so this
			// mainly matters once the user presses Stop or the effect naturally finishes.
			if (!_isPreviewReady || _particleSystem == null || !_isPlaying)
			{
				return;
			}

			// Gate simulation/repaint on the preview panel having actually been drawn last frame;
			// avoids advancing particles and repainting continuously while the FXDef is selected
			// but its preview foldout is collapsed or the Inspector isn't visible. Note: don't
			// touch _lastTicks here — it must only advance when we actually simulate a frame
			// below, otherwise the next visible frame would see a near-zero delta.
			if (!_previewVisibleThisFrame)
			{
				return;
			}

			double now = EditorApplication.timeSinceStartup;
			double elapsed = now - _lastTicks;

			// Throttle to ~30 fps. EditorApplication.update can fire far more often than any
			// display refreshes (potentially thousands of times/sec) — calling Repaint() (which
			// re-renders the preview camera) on every single tick pegs a CPU core and made the
			// whole editor feel sluggish while the preview panel was visible. There's no need to
			// redraw a particle preview faster than this.
			if (elapsed < TargetFrameInterval)
			{
				return;
			}

			// Consumed for this frame; OnInteractivePreviewGUI re-arms it if still being drawn.
			_previewVisibleThisFrame = false;

			float deltaTime = Mathf.Min((float)elapsed, MaxDeltaTime);
			_lastTicks = now;

			if (_isPlaying)
			{
				_time += deltaTime;
				var main = _particleSystem.main;
				float clampDuration = main.duration > 0f ? main.duration : Mathf.Max(0.01f, TargetFXDef.Duration);
				if (_time > clampDuration && !main.loop)
				{
					_time = clampDuration;
					_isPlaying = false;
				}

				// Apply scalers in the preview
				float ratio = TargetFXDef._previewSettings.scalerRatio;
				foreach (var scaler in TargetFXDef.Scalers)
				{
					scaler.Apply(_particleSystem, ratio);
				}
			}

			_particleSystem.Simulate(_time, withChildren: true, restart: _restart);
			_restart = false; // restart only on the first simulate after a reset

			Repaint();
		}

		public override bool HasPreviewGUI() => _isPreviewReady && _particleSystem != null;

		public override void OnInteractivePreviewGUI(Rect rect, GUIStyle background)
		{
			if (!_isPreviewReady || _particleSystem == null) return;

			_previewVisibleThisFrame = true;

			_previewUtility.BeginPreview(rect, background);

			_particleSystem.transform.position = Vector3.up * 2f;

			// Camera placement
			var vect = -_particleSystem.transform.forward * 5f + Vector3.up * 2f;
			vect *= TargetFXDef._previewSettings.zoom;

			_previewUtility.camera.backgroundColor = TargetFXDef._previewSettings.backgroundColor;
			_previewUtility.camera.transform.position = _particleSystem.transform.position + vect;
			_previewUtility.camera.transform.LookAt(_particleSystem.transform.position);
			_previewUtility.camera.clearFlags = TargetFXDef._previewSettings.showSkybox ? CameraClearFlags.Skybox : CameraClearFlags.Color;
			_previewUtility.camera.Render();

			_previewUtility.EndAndDrawPreview(rect);

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
				float scrubTime = GUI.HorizontalSlider(new Rect(rect.xMin + 100, rect.yMin + 5, rect.width - 160, 20), _time, 0f, sliderMax);
				if (!Mathf.Approximately(scrubTime, _time))
				{
					// While paused/stopped, OnEditorUpdate's continuous loop isn't running (it only
					// simulates while _isPlaying), so scrubbing has to simulate immediately here to
					// stay responsive instead of waiting for a tick that will never come.
					_time = scrubTime;
					_particleSystem.Simulate(_time, withChildren: true, restart: true);
				}
			}

			GUI.Label(new Rect(rect.xMax - 55, rect.yMin + 5, 50, 20), $"{_time:0.00}s");

			// Footer bar
			GUI.Label(new Rect(rect.xMin, rect.yMax - 30, rect.width, 30), GUIContent.none, EditorStyles.textArea);
			GUI.Label(new Rect(rect.xMin + 10, rect.yMax - 25, 50, 20), "Zoom");
			TargetFXDef._previewSettings.zoom = GUI.HorizontalSlider(new Rect(rect.xMin + 60, rect.yMax - 25, 60, 20), TargetFXDef._previewSettings.zoom, 0.5f, 3f);

			int index = 0;
			for (; index < _backgroundColors.Length; index++)
			{
				Color color = _backgroundColors[index];
				ShowFooterButton(rect, index, color, EditorIcon.Valid, color == TargetFXDef._previewSettings.backgroundColor, () =>
				{
					TargetFXDef._previewSettings.backgroundColor = color;
				});
			}

			ShowFooterButton(rect, index, Color.white, EditorIcon.Skybox, TargetFXDef._previewSettings.showSkybox, () =>
			{
				TargetFXDef._previewSettings.showSkybox = !TargetFXDef._previewSettings.showSkybox;
			});

			// Show the scaler ratio slider at bottom right
			GUI.Label(new Rect(rect.xMax - 100, rect.yMax - 25, 50, 20), "Scaler");
			TargetFXDef._previewSettings.scalerRatio = GUI.HorizontalSlider(new Rect(rect.xMax - 50, rect.yMax - 25, 40, 20), TargetFXDef._previewSettings.scalerRatio, 0f, 1f);
		}

		private void ShowFooterButton(Rect rect, int index, Color bgColor, Texture2D icon, bool isSelected, Action onClick)
		{
			if (isSelected)
			{
				GUI.Label(new Rect(rect.xMin + 128 + index * 35, rect.yMax - 27, 34, 24), GUIContent.none, EditorStyles.selectionRect);
			}

			GUI.backgroundColor = bgColor;
			if (GUI.Button(new Rect(rect.xMin + 130 + index * 35, rect.yMax - 25, 30, 20), icon))
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

		private void CleanupPreview()
		{
			_isPreviewReady = false;
			_particleSystem = null;
			_previewObject = null;

			if (_previewUtility != null)
			{
				_previewUtility.Cleanup();
				_previewUtility = null;
			}
		}
	}
}

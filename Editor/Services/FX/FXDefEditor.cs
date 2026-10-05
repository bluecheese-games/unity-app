using BlueCheese.Core.Editor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueCheese.App.Editor
{
	[CustomEditor(typeof(FXDef))]
	public class FXDefEditor : AssetBaseEditor
	{
		public FXDef TargetFXDef => (FXDef)target;

		private SerializedProperty _overrideDurationProperty;
		private SerializedProperty _scalersProperty;
		private SerializedProperty _prewarmProperty;

		private int _cachedScalerTypesMask = -1;
		private List<string> _cachedScalerOptions;

		protected override void OnEnable()
		{
			base.OnEnable();

			_overrideDurationProperty = serializedObject.FindProperty(nameof(TargetFXDef.OverrideDuration));
			_scalersProperty = serializedObject.FindProperty(nameof(TargetFXDef.Scalers));
			_prewarmProperty = serializedObject.FindProperty(nameof(TargetFXDef.Prewarm));
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

			var prewarmField = new PropertyField(_prewarmProperty);
			root.Add(prewarmField);

			var prewarmPoolSizeField = new PropertyField(serializedObject.FindProperty(nameof(TargetFXDef.PrewarmPoolSize)));
			root.Add(prewarmPoolSizeField);
			prewarmPoolSizeField.SetEnabled(_prewarmProperty.boolValue);
			prewarmField.RegisterValueChangeCallback(evt => prewarmPoolSizeField.SetEnabled(evt.changedProperty.boolValue));

			// Multi-object editing would have to pick one arbitrary target to preview; skip it entirely.
			var preview = serializedObject.isEditingMultipleObjects ? null : new FXPreviewElement(TargetFXDef);
			if (preview != null)
			{
				root.Add(preview);
			}

			void RefreshFields()
			{
				bool hasPrefab = TargetFXDef.Prefab != null;
				bool hasParticleSystem = hasPrefab && HasAnyParticleSystem(TargetFXDef.Prefab);

				missingParticleSystemBox.style.display = (hasPrefab && !hasParticleSystem) ? DisplayStyle.Flex : DisplayStyle.None;
				overrideDurationField.style.display = hasParticleSystem ? DisplayStyle.Flex : DisplayStyle.None;
				durationField.style.display = hasParticleSystem ? DisplayStyle.Flex : DisplayStyle.None;
				durationField.SetEnabled(_overrideDurationProperty.boolValue);
				scalersContainer.style.display = hasParticleSystem ? DisplayStyle.Flex : DisplayStyle.None;

				if (preview != null)
				{
					preview.style.display = hasParticleSystem ? DisplayStyle.Flex : DisplayStyle.None;
				}
			}

			prefabField.RegisterValueChangeCallback(_ =>
			{
				serializedObject.ApplyModifiedProperties();
				preview?.Rebuild();
				RefreshFields();
				RebuildScalersUI(scalersContainer);
			});

			overrideDurationField.RegisterValueChangeCallback(evt => durationField.SetEnabled(evt.changedProperty.boolValue));

			RefreshFields();
			RebuildScalersUI(scalersContainer);

			return root;
		}

		// The ParticleSystem no longer has to sit on the prefab root: the preview drives every system
		// that has no ParticleSystem above it, so a prefab with the emitter on a child works too.
		private static bool HasAnyParticleSystem(GameObject prefab)
			=> prefab.GetComponentInChildren<ParticleSystem>(includeInactive: true) != null;

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
	}
}

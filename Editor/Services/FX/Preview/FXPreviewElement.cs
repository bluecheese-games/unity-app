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
	/// <summary>
	/// Inspector widget hosting an <see cref="FXPreviewController"/>: a transport bar, the rendered
	/// viewport, and the view settings. The animation loop is tied to the element's lifetime and only
	/// runs while the element is genuinely being drawn.
	/// </summary>
	public class FXPreviewElement : VisualElement
	{
		private const long _tickIntervalMs = 33;
		private const int _maxRenderSize = 2048;
		private const float _handleHeight = 5f;
		private const float _labelWidth = 44f;
		private const float _iconSize = 16f;

		private readonly FXDef _def;
		private readonly FXPreviewController _controller;

		private readonly Image _image;
		private readonly Image _playPauseIcon;
		private readonly Slider _timeSlider;
		private readonly Label _timeLabel;
		private readonly Slider _zoomSlider;
		private readonly Slider _scalerSlider;
		private readonly Slider _moveSpeedSlider;
		private readonly ColorField _backgroundField;
		private readonly ToolbarToggle _skyboxToggle;
		private readonly HelpBox _localSpaceWarning;

		// Ancestors whose flexGrow we overrode to pin the section to the bottom, with their previous value
		// so detaching puts the inspector back exactly as it was.
		private readonly List<(VisualElement Element, StyleFloat FlexGrow)> _stretchedAncestors = new();

		private IVisualElementScheduledItem _ticker;
		private Vector2Int _renderSize;
		private double _lastTickTime;

		// Set from the Image's paint callback, consumed by Tick. UI Toolkit schedulers keep running even
		// when the window is hidden behind another tab, so this is the only reliable "am I visible"
		// signal — without it the preview would keep simulating and rendering off-screen forever.
		private bool _wasDrawn;

		// Forces a single render while playback is paused (a view setting changed, a scrub happened...).
		private bool _needsRender;

		// Last text pushed into _localSpaceWarning, so the per-tick sync doesn't allocate a string it is
		// about to throw away.
		private string _warningText;

		public FXPreviewElement(FXDef def)
		{
			_def = def;
			_controller = new FXPreviewController(FXPreviewPrefs.Loop);
			_controller.SetDef(def);

			style.marginTop = 8f;

			// Claim whatever vertical space the fields above leave over, then push the whole section to the
			// bottom of it, so the preview sits at a stable position instead of drifting up and down as the
			// scaler list grows. Collapses to nothing once the inspector content outgrows the viewport.
			style.flexGrow = 1f;
			Add(new VisualElement { style = { flexGrow = 1f, minHeight = 0f } });

			Add(new Label("Preview")
			{
				style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4f },
			});

			// --- Transport bar ---

			var transport = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

			// Fixed size plus ScaleToFit: letting the icon flex to fill the button stretched the small
			// builtin texture out of shape.
			_playPauseIcon = new Image
			{
				scaleMode = ScaleMode.ScaleToFit,
				style = { width = _iconSize, height = _iconSize, flexGrow = 0f, flexShrink = 0f },
			};
			var playPauseButton = MakeIconButton("Play / Pause", TogglePlayback);
			playPauseButton.Add(_playPauseIcon);
			transport.Add(playPauseButton);

			transport.Add(MakeToggle("Auto Play", FXPreviewPrefs.AutoPlay,
				"Start playing as soon as an FX is selected.",
				value => FXPreviewPrefs.AutoPlay = value));

			transport.Add(MakeToggle("Loop", FXPreviewPrefs.Loop,
				"Replay the effect indefinitely instead of pausing at the end of a pass.",
				value =>
				{
					FXPreviewPrefs.Loop = value;
					_controller.Loop = value;
					if (value && !_controller.IsPlaying)
					{
						_controller.Play();
					}

					RequestRender();
				}));

			_timeSlider = new Slider(0f, 1f) { style = { flexGrow = 1, marginLeft = 6f, marginRight = 6f } };
			_timeSlider.RegisterValueChangedCallback(evt => { _controller.Seek(evt.newValue); RequestRender(); });
			transport.Add(_timeSlider);

			_timeLabel = new Label("0.00s") { style = { width = 46f, unityTextAlign = TextAnchor.MiddleRight } };
			transport.Add(_timeLabel);

			Add(transport);

			// --- Viewport ---

			Add(MakeResizeHandle());

			_image = new Image
			{
				scaleMode = ScaleMode.StretchToFill,
				style =
				{
					height = FXPreviewPrefs.Height,
					backgroundColor = Color.black,
				},
			};
			_image.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
			_image.generateVisualContent += _ => _wasDrawn = true;
			Add(_image);

			// --- View settings ---

			var viewRow = MakeRow();

			_zoomSlider = MakeSettingSlider("Zoom", 0.5f, 3f,
				() => _def._previewSettings.zoom,
				value => _def._previewSettings.zoom = value);
			viewRow.Add(_zoomSlider);

			_backgroundField = new ColorField("Background")
			{
				value = _def._previewSettings.backgroundColor,
				showAlpha = false,
				style = { width = 170f, marginRight = 8f },
			};
			_backgroundField.labelElement.style.minWidth = 70f;
			_backgroundField.RegisterCallback<PointerDownEvent>(_ => RecordUndo(), TrickleDown.TrickleDown);
			_backgroundField.RegisterValueChangedCallback(evt =>
			{
				_def._previewSettings.backgroundColor = evt.newValue;
				CommitSettings();
			});
			viewRow.Add(_backgroundField);

			_skyboxToggle = MakeToggle("Skybox", _def._previewSettings.showSkybox,
				"Render the skybox instead of a flat background colour.",
				value =>
				{
					RecordUndo();
					_def._previewSettings.showSkybox = value;
					CommitSettings();
				});
			viewRow.Add(_skyboxToggle);

			Add(viewRow);

			var tuningRow = MakeRow();

			_scalerSlider = MakeSettingSlider("Scaler", 0f, 1f,
				() => _def._previewSettings.scalerRatio,
				value => _def._previewSettings.scalerRatio = value);
			tuningRow.Add(_scalerSlider);

			_moveSpeedSlider = MakeSettingSlider("Move speed", 0f, 20f,
				() => _def._previewSettings.moveSpeed,
				value => _def._previewSettings.moveSpeed = value,
				replayOnChange: true);
			_moveSpeedSlider.labelElement.style.minWidth = 72f;
			_moveSpeedSlider.tooltip = "Drags the emitter sideways so a world-space trail can be judged as if " +
				"the effect were attached to a moving object. The camera follows, so the emitter stays centered.";
			tuningRow.Add(_moveSpeedSlider);

			Add(tuningRow);

			_localSpaceWarning = new HelpBox(string.Empty, HelpBoxMessageType.Warning)
			{
				style = { display = DisplayStyle.None, marginTop = 2f },
			};
			Add(_localSpaceWarning);

			var actionRow = MakeRow();

			actionRow.Add(new Button(() => { _controller.Reseed(); RequestRender(); })
			{
				text = "Reseed",
				tooltip = "Re-roll the frozen random seed. The seed is pinned so the time slider scrubs instead of re-rolling the effect.",
			});

			actionRow.Add(new Button(Rebuild)
			{
				text = "Rebuild",
				tooltip = "Re-instantiate the prefab. Use after editing materials or textures the preview can't detect.",
			});

			Add(actionRow);

			RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
			RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

			SyncTransport();
			SyncLocalSpaceWarning();
		}

		/// <summary>Drops the preview instance so it is rebuilt from the prefab on the next tick.</summary>
		public void Rebuild()
		{
			_controller.Rebuild();
			if (FXPreviewPrefs.AutoPlay)
			{
				_controller.Play();
			}

			RequestRender();
		}

		/// <summary>
		/// The inspector's boxes hug their content by default, so there is never any leftover space for the
		/// bottom spacer to claim. Stretch the chain up to the inspector's scroll view to create some.
		/// Walks up to the first ScrollView rather than matching Unity's internal class names.
		/// </summary>
		private void StretchAncestors()
		{
			for (var ancestor = parent; ancestor != null; ancestor = ancestor.parent)
			{
				if (ancestor is ScrollView scrollView)
				{
					Stretch(scrollView.contentContainer);
					break;
				}

				Stretch(ancestor);
			}

			void Stretch(VisualElement element)
			{
				_stretchedAncestors.Add((element, element.style.flexGrow));
				element.style.flexGrow = 1f;
			}
		}

		/// <summary>
		/// Some of those boxes are Unity's own, shared by every inspector; leaving them stretched would
		/// follow the user to the next asset they select.
		/// </summary>
		private void RestoreAncestors()
		{
			for (int i = _stretchedAncestors.Count - 1; i >= 0; i--)
			{
				var (element, flexGrow) = _stretchedAncestors[i];
				element.style.flexGrow = flexGrow;
			}

			_stretchedAncestors.Clear();
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			StretchAncestors();
			_lastTickTime = EditorApplication.timeSinceStartup;
			_ticker = schedule.Execute(Tick).Every(_tickIntervalMs);

			PrefabStage.prefabSaved += OnPrefabSaved;
			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
			Undo.undoRedoPerformed += OnUndoRedo;

			if (FXPreviewPrefs.AutoPlay)
			{
				_controller.Play();
			}

			RequestRender();
		}

		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			RestoreAncestors();

			_ticker?.Pause();
			_ticker = null;

			PrefabStage.prefabSaved -= OnPrefabSaved;
			AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
			Undo.undoRedoPerformed -= OnUndoRedo;

			// Re-instantiating into a fresh preview scene is cheap; leaking one is not, and
			// PreviewRenderUtility complains loudly about it on finalization.
			_image.image = null;
			_controller.Dispose();
		}

		private void OnBeforeAssemblyReload()
		{
			_image.image = null;
			_controller.Dispose();
		}

		private void OnUndoRedo()
		{
			SyncSettingControls();
			RequestRender();
		}

		private void OnPrefabSaved(GameObject savedPrefabRoot)
		{
			// Keeps the preview in sync when the artist tweaks the FX prefab directly from Prefab Mode
			// and saves, instead of requiring them to reselect the asset to force a rebuild.
			if (_def.Prefab == null || savedPrefabRoot == null) return;

			string savedPath = AssetDatabase.GetAssetPath(savedPrefabRoot);
			if (string.IsNullOrEmpty(savedPath) || savedPath != AssetDatabase.GetAssetPath(_def.Prefab)) return;

			Rebuild();
		}

		private void OnViewportGeometryChanged(GeometryChangedEvent evt)
		{
			var size = new Vector2Int(
				Mathf.Clamp(Mathf.FloorToInt(evt.newRect.width), 0, _maxRenderSize),
				Mathf.Clamp(Mathf.FloorToInt(evt.newRect.height), 0, _maxRenderSize));

			if (size == _renderSize) return;

			// The utility destroys its old RenderTexture when the size changes, so let go of it first.
			_image.image = null;
			_renderSize = size;
			RequestRender();
		}

		private void Tick()
		{
			if (panel == null || _renderSize.x < 1 || !_wasDrawn) return;

			_wasDrawn = false;

			double now = EditorApplication.timeSinceStartup;
			float deltaTime = (float)(now - _lastTickTime);
			_lastTickTime = now;

			_controller.RefreshTiming();

			if (_controller.IsPlaying)
			{
				_controller.Advance(deltaTime);
			}
			else if (!_needsRender)
			{
				// Nothing is moving and nothing changed: skip the render and, crucially, skip the repaint
				// request too. Without a repaint _wasDrawn stays false and the whole loop idles until
				// RequestRender wakes it up again.
				SyncTransport();
				SyncLocalSpaceWarning();
				return;
			}

			_needsRender = false;
			_image.image = _controller.Render(_renderSize);
			SyncTransport();
			SyncLocalSpaceWarning();
			RearmDrawFlag();
		}

		/// <summary>
		/// Schedules one render on the next tick. Re-arming the draw flag is what lets a paused preview
		/// wake up for a single frame.
		/// </summary>
		private void RequestRender()
		{
			_needsRender = true;
			RearmDrawFlag();
		}

		/// <summary>
		/// Must dirty the Image itself, not this container: UI Toolkit regenerates visual content per
		/// element, so marking the parent never re-runs the Image's paint callback and the loop would
		/// render a single frame and then stall forever.
		/// </summary>
		private void RearmDrawFlag() => _image.MarkDirtyRepaint();

		private void TogglePlayback()
		{
			if (_controller.IsPlaying)
			{
				_controller.Pause();
			}
			else
			{
				_controller.Play();
			}

			RequestRender();
		}

		private void SyncTransport()
		{
			_playPauseIcon.image = _controller.IsPlaying ? EditorIcon.Pause : EditorIcon.Play;

			float max = Mathf.Max(0.01f, _controller.SliderMax);
			if (!Mathf.Approximately(_timeSlider.highValue, max))
			{
				_timeSlider.highValue = max;
			}

			// SetValueWithoutNotify: feeding the clock back into the slider must not be mistaken for the
			// user scrubbing, which would seek on every frame of playback.
			_timeSlider.SetValueWithoutNotify(_controller.Time);
			_timeLabel.text = $"{_controller.Time:0.00}s";
		}

		/// <summary>
		/// Warns when Move speed is asking for a trail the prefab physically cannot produce. Only meaningful
		/// once the preview is built, since the simulation space is read off the instantiated systems.
		/// </summary>
		private void SyncLocalSpaceWarning()
		{
			int local = _controller.LocalSpaceSystemCount;
			bool show = _def._previewSettings.moveSpeed > 0f && local > 0;

			_localSpaceWarning.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
			if (!show) return;

			int total = _controller.SystemCount;
			string text = local == total
				? "Move speed has no visible effect: this prefab simulates in local space, so its particles follow "
					+ "the emitter instead of staying behind it. Switch the ParticleSystem's Simulation Space to "
					+ "World to preview a trail."
				: $"{local} of {total} ParticleSystems simulate in local space and will not trail. Only the "
					+ "world-space ones react to Move speed.";

			if (text == _warningText) return;

			_warningText = text;
			_localSpaceWarning.text = text;
		}

		private void SyncSettingControls()
		{
			var settings = _def._previewSettings;
			_zoomSlider.SetValueWithoutNotify(settings.zoom);
			_scalerSlider.SetValueWithoutNotify(settings.scalerRatio);
			_moveSpeedSlider.SetValueWithoutNotify(settings.moveSpeed);
			_backgroundField.SetValueWithoutNotify(settings.backgroundColor);
			SetToggleWithoutNotify(_skyboxToggle, settings.showSkybox);
		}

		private static VisualElement MakeRow() => new()
		{
			style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexWrap = Wrap.Wrap, marginTop = 2f },
		};

		private Slider MakeSettingSlider(string label, float min, float max, Func<float> get, Action<float> set, bool replayOnChange = false)
		{
			var slider = new Slider(label, min, max)
			{
				value = get(),
				showInputField = true,
				style = { width = 190f, marginRight = 8f },
			};
			slider.labelElement.style.minWidth = _labelWidth;

			// Record once at the start of a drag rather than on every value change, so a single drag
			// collapses into a single undo step.
			slider.RegisterCallback<PointerDownEvent>(_ => RecordUndo(), TrickleDown.TrickleDown);
			slider.RegisterValueChangedCallback(evt =>
			{
				set(evt.newValue);
				CommitSettings(replayOnChange);
			});

			return slider;
		}

		// ToolbarToggle's checked styling barely registers outside an actual Toolbar container, which left
		// Loop / Auto Play / Skybox looking identical on and off. Paint the on-state explicitly instead of
		// relying on the editor theme to do it.
		private static ToolbarToggle MakeToggle(string text, bool initial, string tooltip, Action<bool> onChanged)
		{
			var toggle = new ToolbarToggle
			{
				text = text,
				value = initial,
				tooltip = tooltip,
				style =
				{
					height = 20f,
					marginRight = 4f,
					paddingLeft = 8f,
					paddingRight = 8f,
					borderTopLeftRadius = 3f,
					borderTopRightRadius = 3f,
					borderBottomLeftRadius = 3f,
					borderBottomRightRadius = 3f,
					unityTextAlign = TextAnchor.MiddleCenter,
				},
			};

			toggle.RegisterValueChangedCallback(evt =>
			{
				PaintToggle(toggle, evt.newValue);
				onChanged(evt.newValue);
			});

			PaintToggle(toggle, initial);
			return toggle;
		}

		private static void PaintToggle(ToolbarToggle toggle, bool isOn)
		{
			var background = isOn ? ToggleOnColor : ToggleOffColor;

			toggle.style.backgroundColor = background;
			toggle.style.color = isOn ? Color.white : new StyleColor(StyleKeyword.Null);
			toggle.style.unityFontStyleAndWeight = isOn ? FontStyle.Bold : FontStyle.Normal;

			// The toolbar stylesheet also paints the inner input element, which would otherwise sit on top
			// of the colour set on the root and hide it.
			var input = toggle.Q(className: Toggle.inputUssClassName);
			if (input != null)
			{
				input.style.backgroundColor = background;
			}
		}

		/// <summary>
		/// SetValueWithoutNotify skips the change callback, and therefore the explicit on-state painting
		/// too, so a toggle driven from code (undo/redo) has to be repainted by hand.
		/// </summary>
		private static void SetToggleWithoutNotify(ToolbarToggle toggle, bool value)
		{
			toggle.SetValueWithoutNotify(value);
			PaintToggle(toggle, value);
		}

		private static Color ToggleOnColor => EditorGUIUtility.isProSkin
			? new Color(0.23f, 0.45f, 0.69f)
			: new Color(0.35f, 0.55f, 0.80f);

		private static Color ToggleOffColor => EditorGUIUtility.isProSkin
			? new Color(0.25f, 0.25f, 0.25f)
			: new Color(0.76f, 0.76f, 0.76f);

		private Button MakeIconButton(string tooltip, Action onClick)
			=> new(onClick)
			{
				tooltip = tooltip,
				style =
				{
					width = 30f,
					height = 20f,
					marginRight = 4f,
					paddingLeft = 0f,
					paddingRight = 0f,
					paddingTop = 0f,
					paddingBottom = 0f,
					alignItems = Align.Center,
					justifyContent = Justify.Center,
				},
			};

		private VisualElement MakeResizeHandle()
		{
			var handle = new VisualElement
			{
				tooltip = "Drag to resize the preview",
				style =
				{
					height = _handleHeight,
					marginTop = 3f,
					marginBottom = 3f,
					backgroundColor = new Color(1f, 1f, 1f, 0.08f),
				},
			};

			bool isDragging = false;
			float startHeight = 0f;
			float startY = 0f;

			handle.RegisterCallback<PointerDownEvent>(evt =>
			{
				isDragging = true;
				startHeight = _image.resolvedStyle.height;
				startY = evt.position.y;
				handle.CapturePointer(evt.pointerId);
			});

			handle.RegisterCallback<PointerMoveEvent>(evt =>
			{
				if (!isDragging) return;

				// The handle sits on the viewport's top edge and the section is pinned to the bottom of the
				// inspector, so the drag is inverted: pulling up grows the viewport upwards.
				float height = Mathf.Clamp(startHeight - (evt.position.y - startY), FXPreviewPrefs.MinHeight, FXPreviewPrefs.MaxHeight);
				_image.style.height = height;
			});

			handle.RegisterCallback<PointerUpEvent>(evt =>
			{
				if (!isDragging) return;

				isDragging = false;
				handle.ReleasePointer(evt.pointerId);
				FXPreviewPrefs.Height = _image.resolvedStyle.height;
			});

			return handle;
		}

		private void RecordUndo() => Undo.RecordObject(_def, "Change FX Preview Settings");

		private void CommitSettings(bool replay = false)
		{
			// Written straight to the object rather than through the inspector's SerializedObject: the
			// shared one is applied by AssetBaseEditor.OnHeaderGUI, which queues a full AssetBank
			// regeneration whenever it finds a pending change.
			EditorUtility.SetDirty(_def);
			_controller.SetScalerRatio(_def._previewSettings.scalerRatio);

			// Move speed changes the emitter's path, which only shows up once the interval is replayed.
			if (replay)
			{
				_controller.Seek(_controller.Time);
			}

			RequestRender();
		}
	}
}

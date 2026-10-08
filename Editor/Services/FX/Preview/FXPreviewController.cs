using System;
using UnityEditor;
using UnityEngine;

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// Simulation and rendering core of the FXDef inspector preview. Owns an isolated preview scene, a
	/// single instance of the effect prefab, and the playback clock. Holds no UI: the hosting element
	/// drives it with <see cref="Advance"/> / <see cref="Seek"/> and draws whatever <see cref="Render"/>
	/// returns.
	/// </summary>
	public class FXPreviewController : IDisposable
	{
		// Guards against huge jumps in simulated time when the editor stalls (recompiling, importing
		// assets, dragging a window, ...).
		private const float _maxDeltaTime = 0.1f;

		// Granularity used when a seek has to be replayed by hand (see Seek).
		private const float _seekStep = 1f / 60f;

		// Matches the length of the fixed 5-back / 2-up offset this replaced, so an existing FXDef keeps its
		// framing when the default pitch is applied.
		private const float _cameraDistance = 5.4f;

		// Half-width of the reference grid, in whole units.
		private const int _gridHalfExtent = 12;

		// Lateral in view space: the camera looks down the instance's forward axis, so sliding along world
		// X moves the emitter across the screen, which is where a trail reads best.
		private static readonly Vector3 _moveDirection = Vector3.right;

		private PreviewRenderUtility _previewUtility;
		private Mesh _gridMesh;
		private Material _gridMaterial;
		private GameObject _instance;
		private ParticleSystem[] _systems = Array.Empty<ParticleSystem>();
		private ParticleSystem[] _roots = Array.Empty<ParticleSystem>();
		private ParticleSystem[] _scaleTargets = Array.Empty<ParticleSystem>();
		private Texture _texture;

		private FXDef _def;
		private FXParticleGraph _graph;
		private float _scrubLength = 1f;
		private int _localSpaceSystemCount;
		private Vector3 _emitterPosition;
		private bool _loop;
		private int _seed = 1;
		private float _time;
		private bool _isPlaying;
		private bool _isBuilt;
		private float _appliedScalerRatio = float.NaN;
		private bool _appliedScaleNested;

		public FXPreviewController(bool loop) => _loop = loop;

		public bool IsReady => _instance != null && _roots.Length > 0;

		public bool IsPlaying => _isPlaying;

		public float Time => _time;

		public bool HasLoopingSystem => _graph != null && _graph.AnyLooping;

		/// <summary>Number of ParticleSystems in the instantiated prefab.</summary>
		public int SystemCount => _systems.Length;

		/// <summary>
		/// How many of those simulate in local space. Their particles follow the emitter instead of staying
		/// where they were emitted, so moving the emitter leaves no trail for them.
		/// </summary>
		public int LocalSpaceSystemCount => _localSpaceSystemCount;

		/// <summary>Upper bound of the time scrubber: one pass of the effect.</summary>
		public float SliderMax => _scrubLength;

		/// <summary>Replay indefinitely instead of pausing at the end of a pass.</summary>
		public bool Loop
		{
			get => _loop;
			set => _loop = value;
		}

		private float MoveSpeed => _def != null ? _def._previewSettings.moveSpeed : 0f;

		public void SetDef(FXDef def)
		{
			_def = def;
			Rebuild();
		}

		/// <summary>
		/// Recomputes the playback bounds from the def. Cheap enough to call every tick, which keeps the
		/// scrubber honest when Duration or OverrideDuration is edited while the preview is open.
		/// </summary>
		public void RefreshTiming()
		{
			RefreshLengths();

			if (!_isBuilt || _def == null || _def.ScaleNestedSystems == _appliedScaleNested)
			{
				return;
			}

			// A scaler mutates the system it is applied to, so merely narrowing the target list would leave
			// the systems dropped from it stuck at their last ratio. Re-instantiating is the only way back.
			Rebuild();
		}

		/// <summary>
		/// Drops the current instance so the next tick re-instantiates from the prefab. Cheap enough to
		/// call whenever the prefab may have changed underneath us.
		/// </summary>
		public void Rebuild()
		{
			DestroyInstance();
			_isBuilt = false;
			_time = 0f;
			_appliedScalerRatio = float.NaN;
		}

		public void Play()
		{
			_isPlaying = true;

			// Pressing play on a finished effect should replay it rather than sit on the last frame.
			if (_isBuilt && IsReady && _time >= _scrubLength)
			{
				Seek(0f);
			}
		}

		public void Pause() => _isPlaying = false;

		public void Restart()
		{
			Seek(0f);
			_isPlaying = true;
		}

		/// <summary>Re-rolls the frozen random seed, so variation can still be checked by hand.</summary>
		public void Reseed()
		{
			if (!EnsureBuilt()) return;

			_seed++;
			FreezeSeeds();
			Seek(_time);
		}

		public void Advance(float deltaTime)
		{
			if (!_isPlaying || !EnsureBuilt()) return;

			deltaTime = Mathf.Min(deltaTime, _maxDeltaTime);
			if (deltaTime <= 0f) return;

			if (!_loop && _time + deltaTime >= _scrubLength)
			{
				// Land exactly on the end of the pass instead of overshooting, so a stopped preview always
				// freezes on the same frame.
				Step(_scrubLength - _time);
				_time = _scrubLength;
				_isPlaying = false;
				return;
			}

			Step(deltaTime);
			_time += deltaTime;

			if (_time >= _scrubLength)
			{
				_time -= _scrubLength;

				// A looping system wraps by itself and only needs the clock reset. A one-shot has already
				// burned out by now, so it has to be replayed from scratch for Loop to mean anything.
				if (!HasLoopingSystem)
				{
					Seek(_time);
				}
			}
		}

		public void Seek(float absoluteTime)
		{
			if (!EnsureBuilt()) return;

			_time = Mathf.Clamp(absoluteTime, 0f, _scrubLength);
			ResetEmitterPosition();

			if (MoveSpeed <= 0f)
			{
				foreach (var root in _roots)
				{
					// fixedTimeStep: true here, unlike Step() — seeking has to replay the whole interval in
					// small increments to land on an accurate state, and it only runs on scrub or restart.
					root.Simulate(_time, withChildren: true, restart: true, fixedTimeStep: true);
				}

				return;
			}

			// With a moving emitter the trail depends on where the emitter was at each instant, which a
			// single Simulate call cannot reproduce — it would spawn the whole interval's particles from
			// the final position. Replay the interval by hand instead, moving the emitter as we go.
			foreach (var root in _roots)
			{
				root.Simulate(0f, withChildren: true, restart: true, fixedTimeStep: false);
			}

			for (float replayed = 0f; replayed < _time;)
			{
				float step = Mathf.Min(_seekStep, _time - replayed);
				Step(step);
				replayed += step;
			}
		}

		public void SetScalerRatio(float ratio)
		{
			if (!EnsureBuilt()) return;
			if (!float.IsNaN(_appliedScalerRatio) && Mathf.Approximately(ratio, _appliedScalerRatio)) return;

			ApplyScalers(ratio);

			// Scalers only affect what the systems do from here on, so replay the current interval to show
			// the effect as it would look had it started at this ratio.
			Seek(_time);
		}

		public Texture Render(Vector2Int size)
		{
			if (size.x < 1 || size.y < 1) return null;
			if (!EnsureBuilt()) return null;

			var settings = _def._previewSettings;
			var camera = _previewUtility.camera;
			camera.backgroundColor = settings.backgroundColor;
			camera.clearFlags = settings.showSkybox ? CameraClearFlags.Skybox : CameraClearFlags.Color;

			// InitPreview never reads the rect's x/y; keeping them at zero makes its Layout and Repaint
			// branches equivalent, which matters because this runs outside of any IMGUI pass.
			var rect = new Rect(0f, 0f, size.x, size.y);

			_previewUtility.BeginPreview(rect, GUIStyle.none);
			try
			{
				PlaceCamera(settings);
				DrawGrid(settings);

				// Render() rather than camera.Render(): it re-enables the preview lights that the utility
				// switches off at the end of every frame, and compensates the FOV for the viewport aspect.
				_previewUtility.Render();
			}
			finally
			{
				// BeginPreview sets a sticky "already opened" latch. Missing the matching EndPreview leaves
				// the utility logging errors and rendering nothing until the next domain reload.
				_texture = _previewUtility.EndPreview();
			}

			return _texture;
		}

		public void Dispose()
		{
			DestroyInstance();
			_isBuilt = false;
			_texture = null;

			// Built by hand rather than loaded, so the preview scene teardown does not cover them.
			if (_gridMesh != null)
			{
				UnityEngine.Object.DestroyImmediate(_gridMesh);
				_gridMesh = null;
			}

			if (_gridMaterial != null)
			{
				UnityEngine.Object.DestroyImmediate(_gridMaterial);
				_gridMaterial = null;
			}

			if (_previewUtility != null)
			{
				// Closes the preview scene, which destroys everything parented into it.
				_previewUtility.Cleanup();
				_previewUtility = null;
			}
		}

		private bool EnsureBuilt()
		{
			if (_isBuilt) return IsReady;

			// One attempt per Rebuild: a prefab with no ParticleSystem must not retry on every tick.
			_isBuilt = true;

			if (_def == null || _def.Prefab == null) return false;

			EnsureUtility();

			_instance = InstantiateIntoPreviewScene(_def.Prefab);
			if (_instance == null) return false;

			_instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

			// Same classification the runtime uses, so the preview cannot show an effect FXInstance would be
			// unable to play -- or stay silent on one it could.
			_graph = FXParticleGraph.Build(_instance);
			if (_graph.IsEmpty)
			{
				DestroyInstance();
				return false;
			}

			_systems = _graph.All;
			_roots = _graph.Roots;
			_localSpaceSystemCount = CountLocalSpaceSystems();

			// Same targets FXInstance.Setup resolves, so the preview shows what the runtime actually does
			// rather than a more thorough version of it.
			_appliedScaleNested = _def.ScaleNestedSystems;
			_scaleTargets = _appliedScaleNested ? _graph.All : _graph.Roots;

			RefreshLengths();
			FreezeSeeds();
			ApplyScalers(_def._previewSettings.scalerRatio);
			Seek(0f);

			return IsReady;
		}

		private void EnsureUtility()
		{
			if (_previewUtility != null) return;

			_previewUtility = new PreviewRenderUtility();

			// PreviewScene defaults to a 2..10 range, which clips most effects.
			_previewUtility.camera.nearClipPlane = 0.05f;
			_previewUtility.camera.farClipPlane = 1000f;
			_previewUtility.camera.fieldOfView = 30f;
		}

		private GameObject InstantiateIntoPreviewScene(GameObject prefab)
		{
			// Instantiating into the preview scene keeps the object out of the scene the user is editing,
			// so no hierarchy flicker and no [ExecuteAlways] script running against the real scene.
			if (PrefabUtility.IsPartOfPrefabAsset(prefab))
			{
				return _previewUtility.InstantiatePrefabInScene(prefab);
			}

			var instance = UnityEngine.Object.Instantiate(prefab);
			_previewUtility.AddSingleGO(instance);
			return instance;
		}

		private void FreezeSeeds()
		{
			for (int i = 0; i < _systems.Length; i++)
			{
				var system = _systems[i];

				// Simulate(restart: true) re-rolls the random seed on every call while useAutoRandomSeed is
				// on, which would make the time scrubber boil the effect instead of scrubbing it. The seed
				// is read-only while the system is playing, hence the Stop first.
				system.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
				system.useAutoRandomSeed = false;
				system.randomSeed = (uint)(_seed * 7919 + i);
				system.Clear(withChildren: true);
			}
		}

		private void RefreshLengths()
		{
			if (_def == null) return;

			float reference = _graph != null ? _graph.ReferenceLength : 0f;
			_scrubLength = FXPreviewTiming.ResolveScrubLength(reference, _def.OverrideDuration, _def.Duration);
		}

		private void ApplyScalers(float ratio)
		{
			_appliedScalerRatio = ratio;

			if (_def.Scalers == null) return;

			foreach (var scaler in _def.Scalers)
			{
				foreach (var target in _scaleTargets)
				{
					scaler.Apply(target, ratio);
				}
			}
		}

		private void Step(float deltaTime)
		{
			if (deltaTime <= 0f) return;

			MoveEmitter(deltaTime);

			foreach (var root in _roots)
			{
				// restart: false means "advance by deltaTime from the current state", and fixedTimeStep:
				// false means a single step of that size. Both are what makes this O(1) per frame.
				root.Simulate(deltaTime, withChildren: true, restart: false, fixedTimeStep: false);
			}
		}

		private void MoveEmitter(float deltaTime)
		{
			float speed = MoveSpeed;
			if (speed <= 0f) return;

			// Particles simulating in world space stay where they were emitted, so dragging the emitter
			// leaves a real trail behind it. PlaceCamera tracks the instance, so the emitter itself still
			// reads as centered. Systems set to local space simply follow along and show nothing.
			_emitterPosition += _moveDirection * (speed * deltaTime);
			_instance.transform.position = _emitterPosition;
		}

		// Custom space is deliberately not counted: it tracks an arbitrary transform rather than the
		// emitter, so it may well still trail. Only Local is unambiguously immune to moving the emitter.
		private int CountLocalSpaceSystems()
		{
			int count = 0;
			foreach (var system in _systems)
			{
				if (system.main.simulationSpace == ParticleSystemSimulationSpace.Local)
				{
					count++;
				}
			}

			return count;
		}

		private void ResetEmitterPosition()
		{
			_emitterPosition = Vector3.zero;

			if (_instance != null)
			{
				_instance.transform.position = _emitterPosition;
			}
		}

		private void PlaceCamera(FXDef.PreviewSettings settings)
		{
			var pivot = _instance.transform.position;

			// Orbit around the effect rather than around the world origin, so the emitter stays framed even
			// once Move speed has dragged it away.
			float pitch = Mathf.Clamp(settings.cameraPitch, FXDef.PreviewSettings.MinCameraPitch, FXDef.PreviewSettings.MaxCameraPitch);
			var orbit = Quaternion.Euler(pitch, settings.cameraYaw, 0f);
			var offset = orbit * (Vector3.back * (_cameraDistance * settings.zoom));

			var camera = _previewUtility.camera;
			camera.transform.position = pivot + offset;
			camera.transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
		}

		private void DrawGrid(FXDef.PreviewSettings settings)
		{
			if (!settings.showGrid) return;

			EnsureGrid();

			// Snapped to whole units so the grid reads as unbounded no matter where the emitter has drifted,
			// while still visibly sliding past it -- which is the point when Move speed is up.
			var center = new Vector3(Mathf.Round(_emitterPosition.x), 0f, Mathf.Round(_emitterPosition.z));
			_previewUtility.DrawMesh(_gridMesh, Matrix4x4.Translate(center), _gridMaterial, 0);
		}

		private void EnsureGrid()
		{
			if (_gridMesh == null)
			{
				_gridMesh = BuildGridMesh();
			}

			if (_gridMaterial != null) return;

			// The shader the editor itself uses for handles and gizmos: unlit, vertex-coloured, and present
			// in a player build's resources only when something references it -- which is fine, this is
			// editor-only code.
			_gridMaterial = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
			_gridMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			_gridMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			_gridMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			_gridMaterial.SetInt("_ZWrite", 0);
		}

		private static Mesh BuildGridMesh()
		{
			var vertices = new System.Collections.Generic.List<Vector3>();
			var colors = new System.Collections.Generic.List<Color>();
			var indices = new System.Collections.Generic.List<int>();

			var line = new Color(1f, 1f, 1f, 0.12f);
			var axis = new Color(1f, 1f, 1f, 0.35f);

			for (int i = -_gridHalfExtent; i <= _gridHalfExtent; i++)
			{
				var tint = i == 0 ? axis : line;
				AddLine(new Vector3(i, 0f, -_gridHalfExtent), new Vector3(i, 0f, _gridHalfExtent), tint);
				AddLine(new Vector3(-_gridHalfExtent, 0f, i), new Vector3(_gridHalfExtent, 0f, i), tint);
			}

			var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave, name = "FXPreviewGrid" };
			mesh.SetVertices(vertices);
			mesh.SetColors(colors);
			mesh.SetIndices(indices, MeshTopology.Lines, 0);

			// Set by hand: RecalculateBounds on a line mesh gives a zero-height box on the ground plane, which
			// the camera happily culls as soon as it looks along it.
			mesh.bounds = new Bounds(Vector3.zero, new Vector3(_gridHalfExtent * 2f, 1f, _gridHalfExtent * 2f));
			return mesh;

			void AddLine(Vector3 from, Vector3 to, Color tint)
			{
				indices.Add(vertices.Count);
				vertices.Add(from);
				colors.Add(tint);

				indices.Add(vertices.Count);
				vertices.Add(to);
				colors.Add(tint);
			}
		}

		private void DestroyInstance()
		{
			if (_instance != null)
			{
				UnityEngine.Object.DestroyImmediate(_instance);
			}

			_instance = null;
			_graph = null;
			_systems = Array.Empty<ParticleSystem>();
			_roots = Array.Empty<ParticleSystem>();
			_scaleTargets = Array.Empty<ParticleSystem>();
			_localSpaceSystemCount = 0;
			_emitterPosition = Vector3.zero;
		}
	}
}

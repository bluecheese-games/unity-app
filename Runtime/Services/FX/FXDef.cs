using BlueCheese.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.App
{
	[CreateAssetMenu(menuName = "BlueCheese/FX/FX Def", fileName = "New FX")]
	public class FXDef : AssetBase
	{
		[Header("Prefab & Timing")]
		public GameObject Prefab;
		public bool OverrideDuration = false;
		[Tooltip("Used by the editor preview and any systems that need a reference length. If not overridden, it's auto-derived from the prefab's ParticleSystems.")]
		public float Duration = 0f;

		[Header("Scaling")]
		public FXScaler[] Scalers;

		[Tooltip("By default scalers only reach the ParticleSystems that have to be started explicitly. " +
			"Enable this to apply them to every system in the prefab instead, including transform children " +
			"and sub-emitters. Off by default because turning it on changes how existing effects look.")]
		public bool ScaleNestedSystems = false;

		[Header("Prewarm")]
		[Tooltip("If enabled, the FX service preallocates a pool of instances for this effect during initialization, avoiding a first-use hitch.")]
		public bool Prewarm = false;
		[Tooltip("Number of pooled instances to preallocate when Prewarm is enabled.")]
		[Min(1)]
		public int PrewarmPoolSize = 5;

		[HideInInspector]
		public PreviewSettings _previewSettings = new(); // Kept serialized intentionally for convenience; see notes.

		public bool IsValid => Prefab != null;

		// Caches runtime-created FXDefs by prefab so implicit GameObject -> FX/FXDef conversions
		// (e.g. FX.cs's implicit operator) don't allocate a new ScriptableObject + re-walk the
		// prefab's ParticleSystems on every call/frame. Cleared automatically on domain reload.
		private static readonly Dictionary<GameObject, FXDef> _runtimeDefCache = new();

		public static FXDef Create(GameObject prefab)
		{
			if (prefab == null)
			{
				return null;
			}

			if (_runtimeDefCache.TryGetValue(prefab, out var cached) && cached != null)
			{
				return cached;
			}

			var def = CreateInstance<FXDef>();
			def.Prefab = prefab;
			def.AutoDeriveDuration();
			_runtimeDefCache[prefab] = def;
			return def;
		}

		// Implicit conversions are convenient but risky; keep as warnings to encourage explicit usage.
		[Obsolete("Avoid implicit conversion from FXDef to GameObject; use .Prefab instead.", false)]
		public static implicit operator GameObject(FXDef def) => def?.Prefab;

		[Obsolete("Avoid implicit conversion from GameObject to FXDef; call FXDef.Create(prefab) explicitly.", false)]
		public static implicit operator FXDef(GameObject prefab) => Create(prefab);

		private void Reset()
		{
			// Called when the asset is created or Reset via inspector context menu
			if (Scalers == null) Scalers = Array.Empty<FXScaler>();
			AutoDeriveDuration();
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();

			if (!OverrideDuration)
			{
				AutoDeriveDuration();
			}

			// Ensure scalers array exists and is sane
			if (Scalers == null) Scalers = Array.Empty<FXScaler>();

			for (int i = 0; i < Scalers.Length; i++)
			{
				// If FXScaler is a UnityEngine.Object-derived type, name is valid; otherwise this is a no-op in your struct/class implementation.
				Scalers[i].name = Scalers[i].type.ToString();
				if (Scalers[i].curve == null || Scalers[i].curve.keys.Length == 0)
				{
					Scalers[i].curve = AnimationCurve.Constant(0, 1, 1);
				}
			}
		}
#endif

		[ContextMenu("Recompute Duration from Prefab")]
		private void AutoDeriveDuration()
		{
			if (Prefab == null) return;

			var graph = FXParticleGraph.Build(Prefab);
			if (graph.IsEmpty) return;

			// Exactly what the inspector preview scrubs with, so the two can no longer disagree: one loop for
			// a looping prefab, otherwise the full effect including start delays, particle lifetimes and
			// sub-emitter chains. The previous max(main.duration) ignored all of those, and counted systems
			// on deactivated GameObjects that never play.
			// Never set to zero; a tiny epsilon avoids divide-by-zero or slider issues downstream.
			Duration = Mathf.Max(0.01f, graph.ReferenceLength);
		}

		[Serializable]
		public class PreviewSettings
		{
			// Short of +/-90, where looking straight down or up makes the orbit's up vector ambiguous and
			// the camera snaps round.
			public const float MinCameraPitch = -89f;
			public const float MaxCameraPitch = 89f;

			public Color backgroundColor = Color.black;
			public bool showSkybox = false;

			[Tooltip("Editor preview only: draws a 1-unit reference grid on the ground plane, to judge the " +
				"scale of an effect and how far its particles travel.")]
			public bool showGrid = false;

			[Range(0.1f, 5f)] public float zoom = 1f;
			[Range(0f, 1f)] public float scalerRatio = 1f;

			[Tooltip("Editor preview only: horizontal angle of the preview camera around the effect, in degrees.")]
			public float cameraYaw = 0f;

			[Tooltip("Editor preview only: elevation of the preview camera above the effect, in degrees. " +
				"Clamped short of the poles, where the orbit would flip.")]
			[Range(MinCameraPitch, MaxCameraPitch)] public float cameraPitch = 22f;

			[Tooltip("Editor preview only: drags the emitter sideways at this speed so a world-space trail " +
				"can be judged as if the effect were attached to a moving object. The camera follows, so the " +
				"emitter stays centered. Has no effect on systems simulating in local space.")]
			[Range(0f, 20f)] public float moveSpeed = 0f;
		}
	}
}

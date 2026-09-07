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

			// Robustly derive a usable reference duration from ALL ParticleSystems under the prefab root.
			// This keeps the preview slider meaningful even for looping systems.
			var systems = Prefab.GetComponentsInChildren<ParticleSystem>(true);
			if (systems == null || systems.Length == 0) return;

			float maxDuration = 0f;
			for (int i = 0; i < systems.Length; i++)
			{
				var main = systems[i].main;
				// Use main.duration as a reference even if the system loops; this yields a stable scrub range in the editor.
				if (main.duration > maxDuration) maxDuration = main.duration;
			}

			// Never set to zero; a tiny epsilon avoids divide-by-zero or slider issues downstream.
			Duration = Mathf.Max(0.01f, maxDuration);
		}

		[Serializable]
		public class PreviewSettings
		{
			public Color backgroundColor = Color.black;
			public bool showSkybox = false;
			[Range(0.1f, 5f)] public float zoom = 1f;
			[Range(0f, 1f)] public float scalerRatio = 1f;
		}
	}
}

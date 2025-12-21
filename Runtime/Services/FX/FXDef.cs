//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using System.Linq;
using UnityEngine;

namespace BlueCheese.App
{
	[CreateAssetMenu(menuName = "FX/FXDef", fileName = "New FX")]
	public class FXDef : ScriptableObject
	{
		[Header("Prefab & Timing")]
		public GameObject Prefab;
		public bool OverrideDuration = false;
		[Tooltip("Used by the editor preview and any systems that need a reference length. If not overridden, it's auto-derived from the prefab's ParticleSystems.")]
		public float Duration = 0f;

		[Header("Scaling")]
		public FXScaler[] Scalers;

		[HideInInspector]
		public PreviewSettings _previewSettings = new(); // Kept serialized intentionally for convenience; see notes.

		public bool IsValid => Prefab != null;

		public static FXDef Create(GameObject prefab)
		{
			var def = CreateInstance<FXDef>();
			def.Prefab = prefab;
			def.AutoDeriveDuration();
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

		private void OnValidate()
		{
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
		}
	}
}

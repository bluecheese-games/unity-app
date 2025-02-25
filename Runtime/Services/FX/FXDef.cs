//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using NaughtyAttributes;
using System;
using UnityEngine;

namespace BlueCheese.App
{

	[CreateAssetMenu(menuName = "FX/FXDef", fileName = "New FX")]
	public class FXDef : ScriptableObject
	{
		public GameObject Prefab;
		public bool OverrideDuration = false;
		public float Duration = 0f;
		public FXScaler[] Scalers;
		[HideInInspector]
		public PreviewSettings _previewSettings = new();

		public bool IsValid => Prefab != null;

		public static FXDef Create(GameObject prefab)
		{
			var def = CreateInstance<FXDef>();
			def.Prefab = prefab;
			return def;
		}

		public static implicit operator GameObject(FXDef def) => def.Prefab;
		public static implicit operator FXDef(GameObject prefab) => Create(prefab);

		private void OnValidate()
		{
			if (!OverrideDuration && Prefab != null && Prefab.TryGetComponent<ParticleSystem>(out var particleSystem))
			{
				Duration = particleSystem.main.loop ? 0 : particleSystem.main.duration;
			}

			if (Scalers != null)
			{
				for (int i = 0; i < Scalers.Length; i++)
				{
					Scalers[i].name = Scalers[i].type.ToString();
					if (Scalers[i].curve == null || Scalers[i].curve.keys.Length == 0)
					{
						Scalers[i].curve = AnimationCurve.Constant(0, 1, 1);
					}
				}
			}
		}

		[Serializable]
		public class PreviewSettings
		{
			public Color backgroundColor = Color.black;
			public bool showSkybox = false;
			public float zoom = 1f;
		}
	}
}

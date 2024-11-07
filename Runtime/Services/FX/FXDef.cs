//
// Copyright (c) 2024 BlueCheese Games All rights reserved
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
		[EnableIf(nameof(OverrideDuration)), AllowNesting]
		public float Duration = 0f;
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
		}

		[Serializable]
		public class PreviewSettings
		{
			public Color backgroundColor = Color.black;
			public float zoom = 1f;
		}
	}
}

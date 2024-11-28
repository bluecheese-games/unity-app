//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using System;
using UnityEngine;

namespace BlueCheese.App
{
	[Serializable]
	public struct FX
	{
		public FXDef _fxDef;

		public FX(FXDef fxDef)
		{
			_fxDef = fxDef;
		}

		private readonly FXInstance CreateInstance() => Services.Get<IFXService>().CreateFX(_fxDef);

		public readonly void Play(Transform target, Vector3 offset = default, float scale = 1f)
		{
			if (!IsValid)
			{
				return;
			}

			var instance = CreateInstance();
			instance.Scale(scale);
			instance.PlayOnTarget(target, offset);
		}

		public readonly void Play(Vector3 position, float scale = 1f)
		{
			if (!IsValid)
			{
				return;
			}

			var instance = CreateInstance();
			instance.Scale(scale);
			instance.PlayAtPosition(position);
		}

		public readonly bool IsValid => _fxDef != null && _fxDef.IsValid;

		public static implicit operator FX(FXDef fxDef) => new(fxDef);
		public static implicit operator FXDef(FX fx) => fx._fxDef;

		public static implicit operator GameObject(FX fx) => fx._fxDef.Prefab;
		public static implicit operator FX(GameObject prefab) => FXDef.Create(prefab);
	}
}

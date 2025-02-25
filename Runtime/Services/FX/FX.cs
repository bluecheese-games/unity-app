//
// Copyright (c) 2025 BlueCheese Games All rights reserved
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

		public readonly FXInstance Play(Transform target, Vector3 offset = default, float scale = 1f)
		{
			if (!IsValid)
			{
				return null;
			}

			var instance = CreateInstance();
			instance.Scale(scale);
			instance.PlayOnTarget(target, offset);

			return instance;
		}

		public readonly FXInstance Play(Vector3 position, Quaternion rotation = default, float scale = 1f)
		{
			if (!IsValid)
			{
				return null;
			}

			var instance = CreateInstance();
			instance.Scale(scale);
			instance.PlayAt(position, rotation);

			return instance;
		}

		public readonly bool IsValid => _fxDef != null && _fxDef.IsValid;

		public static implicit operator FX(FXDef fxDef) => new(fxDef);
		public static implicit operator FXDef(FX fx) => fx._fxDef;

		public static implicit operator GameObject(FX fx) => fx._fxDef.Prefab;
		public static implicit operator FX(GameObject prefab) => FXDef.Create(prefab);
	}
}

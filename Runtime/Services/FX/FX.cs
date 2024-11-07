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

		private readonly FXInstance GetInstance() => Services.Get<IFXService>().CreateFX(_fxDef);

		public readonly void Play(Transform target, Vector3 offset = default)
		{
			if (!IsValid)
			{
				return;
			}
			GetInstance().Play(target, offset);
		}

		public readonly void Play(Vector3 position)
		{
			if (!IsValid)
			{
				return;
			}
			GetInstance().Play(position);
		}

		public readonly bool IsValid => _fxDef != null && _fxDef.IsValid;

		public static implicit operator FX(FXDef fxDef) => new(fxDef);
		public static implicit operator FXDef(FX fx) => fx._fxDef;

		public static implicit operator GameObject(FX fx) => fx._fxDef.Prefab;
		public static implicit operator FX(GameObject prefab) => FXDef.Create(prefab);
	}
}

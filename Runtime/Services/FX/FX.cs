//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;
using BlueCheese.Core.Utils;
using System;
using UnityEngine;

namespace BlueCheese.App
{
	[Serializable]
	public struct FX
	{
		// Serialized reference to the FXDef asset, resolved lazily through the AssetBank so the
		// FXDef (and its prefab) is not embedded into whatever serializes this FX.
		[SerializeField] private AssetRef<FXDef> _fxDef;

		// Transient definition for runtime-created FX (e.g. built from a prefab); never serialized.
		[NonSerialized] private FXDef _runtimeDef;

		public FX(FXDef fxDef)
		{
			_fxDef = default;
			_runtimeDef = fxDef;
		}

		public readonly FXDef Def => _runtimeDef != null ? _runtimeDef : _fxDef.Asset;

		private readonly FXInstance CreateInstance() => ServiceLocator.Resolve<IFXService>().CreateFX(Def);

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

		public readonly bool IsValid
		{
			get
			{
				var def = Def;
				return def != null && def.IsValid;
			}
		}

		public static implicit operator FX(FXDef fxDef) => new(fxDef);
		public static implicit operator FXDef(FX fx) => fx.Def;

		public static implicit operator GameObject(FX fx) => fx.Def != null ? fx.Def.Prefab : null;
		public static implicit operator FX(GameObject prefab) => FXDef.Create(prefab);
	}
}

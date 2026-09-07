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

		public FXDef Def
		{
			get
			{
				if (_runtimeDef == null)
				{
					if (!_fxDef.Load(out _runtimeDef))
					{
						Debug.LogWarning($"FXDef not found for FX: {_fxDef.Guid}");
					}
				}
				return _runtimeDef;
			}
		}

		private FXInstance CreateInstance() => ServiceLocator.Resolve<IFXService>().CreateFX(Def);

		public FXInstance Play(Transform target, Vector3 offset = default, float scale = 1f)
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

		public FXInstance Play(Vector3 position, Quaternion rotation = default, float scale = 1f)
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

		public bool IsValid => Def != null && Def.IsValid;

		public readonly void Release() => _fxDef.Release();

		public static implicit operator FX(FXDef fxDef) => new(fxDef);
		public static implicit operator FXDef(FX fx) => fx.Def;

		public static implicit operator GameObject(FX fx) => fx.Def != null ? fx.Def.Prefab : null;
		public static implicit operator FX(GameObject prefab) => FXDef.Create(prefab);
	}
}

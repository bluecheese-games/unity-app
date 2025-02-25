//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core;
using System;
using UnityEngine;

namespace BlueCheese.App
{
	[Serializable]
	public struct FXScaler
	{
		[Serializable]
		public enum Type
		{
			None,
			ParticleCount,
			ParticleSize,
		}

		[HideInInspector]
		public string name;
		public Type type;
		public AnimationCurve curve;

		public readonly void Apply(ParticleSystem ps, float ratio = 1f)
		{
			if (curve == null || type == Type.None)
			{
				return;
			}

			float value = curve.Evaluate(ratio);
			FXScalerBase scaler = CreateScalerComponent(ps);
			if (scaler != null)
			{
				scaler.Apply(value);
			}
		}

		private readonly FXScalerBase CreateScalerComponent(ParticleSystem ps) => type switch
		{
			Type.ParticleCount => ps.AddOrGetComponent<FXScalerParticleCount>(),
			Type.ParticleSize => ps.AddOrGetComponent<FXScalerParticleSize>(),
			_ => null,
		};
	}
}

//
// Copyright (c) 2026 BlueCheese Games All rights reserved
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
			StartSpeed,
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
				scaler.Apply(ps, value);
			}
		}

		private readonly FXScalerBase CreateScalerComponent(ParticleSystem ps) => type switch
		{
			Type.ParticleCount => ps.AddOrGetComponent<FXScalerParticleCount>(scaler => scaler.Initialize(ps)),
			Type.ParticleSize => ps.AddOrGetComponent<FXScalerParticleSize>(scaler => scaler.Initialize(ps)),
			Type.StartSpeed => ps.AddOrGetComponent<FXScalerParticleStartSpeed>(scaler => scaler.Initialize(ps)),
			_ => null,
		};
	}
}

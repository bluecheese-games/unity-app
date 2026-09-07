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
			Lifetime,
			Gravity,
			RotationSpeed,
			EmissionShapeRadius,
			NoiseStrength,
			LightIntensity,
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
			Type.Lifetime => ps.AddOrGetComponent<FXScalerParticleLifetime>(scaler => scaler.Initialize(ps)),
			Type.Gravity => ps.AddOrGetComponent<FXScalerGravity>(scaler => scaler.Initialize(ps)),
			Type.RotationSpeed => ps.AddOrGetComponent<FXScalerRotationSpeed>(scaler => scaler.Initialize(ps)),
			Type.EmissionShapeRadius => ps.AddOrGetComponent<FXScalerEmissionShapeRadius>(scaler => scaler.Initialize(ps)),
			Type.NoiseStrength => ps.AddOrGetComponent<FXScalerNoiseStrength>(scaler => scaler.Initialize(ps)),
			Type.LightIntensity => ps.AddOrGetComponent<FXScalerLightIntensity>(scaler => scaler.Initialize(ps)),
			_ => null,
		};
	}
}

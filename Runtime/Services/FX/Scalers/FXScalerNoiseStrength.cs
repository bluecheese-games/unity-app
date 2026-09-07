using UnityEngine;

namespace BlueCheese.App
{
	public class FXScalerNoiseStrength : FXScalerBase
	{
		private float _strength;

		public override void Initialize(ParticleSystem ps)
		{
			_strength = ps.noise.strengthMultiplier;
		}

		public override void Apply(ParticleSystem ps, float ratio)
		{
			var noise = ps.noise;
			noise.strengthMultiplier = _strength * ratio;
		}
	}
}

//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public class FXScalerLightIntensity : FXScalerBase
	{
		private float _intensity;
		private float _range;

		public override void Initialize(ParticleSystem ps)
		{
			var lights = ps.lights;
			_intensity = lights.intensityMultiplier;
			_range = lights.rangeMultiplier;
		}

		public override void Apply(ParticleSystem ps, float ratio)
		{
			var lights = ps.lights;
			lights.intensityMultiplier = _intensity * ratio;
			lights.rangeMultiplier = _range * ratio;
		}
	}
}

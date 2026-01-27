//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public class FXScalerParticleCount : FXScalerBase
	{
		private float[] _burstCounts;
		private float _rateOverTime;
		private float _rateOverDistance;

		public override void Initialize(ParticleSystem ps)
		{
			var emission = ps.emission;
			_burstCounts = new float[ps.emission.burstCount];
			for (int i = 0; i < ps.emission.burstCount; i++)
			{
				_burstCounts[i] = emission.GetBurst(i).count.constant;
			}
			_rateOverTime = emission.rateOverTimeMultiplier;
			_rateOverDistance = emission.rateOverDistanceMultiplier;
		}

		public override void Apply(ParticleSystem ps, float ratio)
		{
			var emission = ps.emission;
			for (int i = 0; i < ps.emission.burstCount; i++)
			{
				var burst = emission.GetBurst(i);
				burst.count = Mathf.RoundToInt(_burstCounts[i] * ratio);
				emission.SetBurst(i, burst);
			}
			emission.rateOverTimeMultiplier = _rateOverTime * ratio;
			emission.rateOverDistanceMultiplier = _rateOverDistance * ratio;
		}
	}
}

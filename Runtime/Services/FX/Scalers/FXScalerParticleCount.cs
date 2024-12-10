//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public class FXScalerParticleCount : FXScalerBase
	{
		private float[] _burstCounts;
		private float _rateOverTime;
		private float _rateOverDistance;

		protected override void Initialize()
		{
			var emission = _ps.emission;
			_burstCounts = new float[_ps.emission.burstCount];
			for (int i = 0; i < _ps.emission.burstCount; i++)
			{
				_burstCounts[i] = emission.GetBurst(i).count.constant;
			}
			_rateOverTime = emission.rateOverTimeMultiplier;
			_rateOverDistance = emission.rateOverDistanceMultiplier;
		}

		public override void Apply(float ratio)
		{
			var emission = _ps.emission;
			for (int i = 0; i < _ps.emission.burstCount; i++)
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

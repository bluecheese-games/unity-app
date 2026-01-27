//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public class FXScalerParticleSize : FXScalerBase
	{
		private float _startSize;

		public override void Initialize(ParticleSystem ps)
		{
			_startSize = ps.main.startSizeMultiplier;
		}

		public override void Apply(ParticleSystem ps, float ratio)
		{
			var main = ps.main;
			main.startSizeMultiplier = _startSize * ratio;
		}
	}
}

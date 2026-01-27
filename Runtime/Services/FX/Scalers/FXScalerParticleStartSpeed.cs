//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public class FXScalerParticleStartSpeed : FXScalerBase
	{
		private float _startSpeed;
		public override void Initialize(ParticleSystem ps)
		{
			_startSpeed = ps.main.startSpeedMultiplier;
		}
		public override void Apply(ParticleSystem ps, float ratio)
		{
			var main = ps.main;
			main.startSpeedMultiplier = _startSpeed * ratio;
		}
	}
}

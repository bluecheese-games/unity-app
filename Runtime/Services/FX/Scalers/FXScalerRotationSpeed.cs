//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public class FXScalerRotationSpeed : FXScalerBase
	{
		private float _startRotation;

		public override void Initialize(ParticleSystem ps)
		{
			_startRotation = ps.main.startRotationMultiplier;
		}

		public override void Apply(ParticleSystem ps, float ratio)
		{
			var main = ps.main;
			main.startRotationMultiplier = _startRotation * ratio;
		}
	}
}

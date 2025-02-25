//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

namespace BlueCheese.App
{
	public class FXScalerParticleSize : FXScalerBase
	{
		private float _startSize;

		protected override void Initialize()
		{
			_startSize = _ps.main.startSizeMultiplier;
		}

		public override void Apply(float ratio)
		{
			var main = _ps.main;
			main.startSizeMultiplier = _startSize * ratio;
		}
	}
}

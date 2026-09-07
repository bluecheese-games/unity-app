using UnityEngine;

namespace BlueCheese.App
{
	public class FXScalerParticleLifetime : FXScalerBase
	{
		private float _startLifetime;

		public override void Initialize(ParticleSystem ps)
		{
			_startLifetime = ps.main.startLifetimeMultiplier;
		}

		public override void Apply(ParticleSystem ps, float ratio)
		{
			var main = ps.main;
			main.startLifetimeMultiplier = _startLifetime * ratio;
		}
	}
}

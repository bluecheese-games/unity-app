using UnityEngine;

namespace BlueCheese.App
{
	public class FXScalerGravity : FXScalerBase
	{
		private float _gravityModifier;

		public override void Initialize(ParticleSystem ps)
		{
			_gravityModifier = ps.main.gravityModifierMultiplier;
		}

		public override void Apply(ParticleSystem ps, float ratio)
		{
			var main = ps.main;
			main.gravityModifierMultiplier = _gravityModifier * ratio;
		}
	}
}

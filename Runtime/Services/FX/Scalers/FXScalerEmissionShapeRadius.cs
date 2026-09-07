using UnityEngine;

namespace BlueCheese.App
{
	public class FXScalerEmissionShapeRadius : FXScalerBase
	{
		private float _radius;
		private Vector3 _scale;

		public override void Initialize(ParticleSystem ps)
		{
			var shape = ps.shape;
			_radius = shape.radius;
			_scale = shape.scale;
		}

		public override void Apply(ParticleSystem ps, float ratio)
		{
			var shape = ps.shape;
			// Not every shape type uses radius (e.g. Box relies on scale instead), so both are
			// scaled here; setting radius on a shape that ignores it is a harmless no-op.
			shape.radius = _radius * ratio;
			shape.scale = _scale * ratio;
		}
	}
}

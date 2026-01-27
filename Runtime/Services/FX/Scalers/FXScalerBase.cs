//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public abstract class FXScalerBase : MonoBehaviour
	{
		private void Awake()
		{
			Initialize(GetComponent<ParticleSystem>());
		}

		public abstract void Initialize(ParticleSystem ps);

		public abstract void Apply(ParticleSystem ps, float ratio);
	}
}

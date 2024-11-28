//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public abstract class FXScaler : MonoBehaviour
	{
		protected ParticleSystem _ps;

		private void Awake()
		{
			_ps = GetComponent<ParticleSystem>();
			Initialize();
		}

		protected abstract void Initialize();

		public abstract void Apply(float ratio);
	}
}

//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.App
{
	public class FXTarget : MonoBehaviour, IDespawnable
	{
		private event Action OnDestroyed;

		public void Subscribe(Action onDestroyed)
		{
			OnDestroyed += onDestroyed;
		}

		public void Unsubscribe(Action onDestroyed)
		{
			OnDestroyed -= onDestroyed;

			if (OnDestroyed == null || OnDestroyed.GetInvocationList().Length == 0)
			{
				Destroy(this);
			}
		}

		private void OnDestroy()
		{
			OnDestroyed?.Invoke();
			OnDestroyed = null;
		}

		void IDespawnable.OnDespawn()
		{
			OnDestroyed?.Invoke();
		}
	}
}

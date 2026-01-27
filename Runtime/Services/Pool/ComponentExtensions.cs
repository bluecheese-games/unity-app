//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.App
{
	public static class ComponentExtensions
	{
		public static bool HasComponent<T>(this Component component) where T : Component
			=> component.GetComponent<T>() != null;

		public static T AddOrGetComponent<T>(this Component component, Action<T> onAdded = null) where T : Component
		{
			if (component.TryGetComponent<T>(out var existingComponent))
			{
				return existingComponent;
			}
			var added = component.gameObject.AddComponent<T>();
			onAdded?.Invoke(added);
			return added;
		}
	}
}

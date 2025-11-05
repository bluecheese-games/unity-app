//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public static class ComponentExtensions
	{
		public static bool HasComponent<T>(this Component component) where T : Component
			=> component.GetComponent<T>() != null;

		public static T AddOrGetComponent<T>(this Component component) where T : Component
		{
			if (component.TryGetComponent<T>(out var existingComponent))
			{
				return existingComponent;
			}
			return component.gameObject.AddComponent<T>();
		}
	}
}

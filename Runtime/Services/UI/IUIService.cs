//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;

namespace BlueCheese.App
{
    public interface IUIService : IInitializable
    {
		/// <summary>
		/// Creates a UIView and returns it.
		/// </summary>
		/// <param name="viewName">The name of the view to create.</param>
		UIView SpawnView(string viewName);

		/// <summary>
		/// Despawns a UIView, returning it to the pool.
		/// </summary>
		void DespawnView(UIView view);
	}
}

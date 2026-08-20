//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;

namespace BlueCheese.App
{
    public interface IUIService : IInitializable
    {
		/// <summary>
		/// Spawns (from a pool) the <see cref="UIView"/> registered under the given name via a
		/// <see cref="UIViewDef"/> in the AssetBank.
		/// </summary>
		/// <param name="viewName">The name of the UIViewDef to spawn.</param>
		UIView SpawnView(string viewName);

		/// <summary>
		/// Spawns (from a pool) the <see cref="UIView"/> described by the given <see cref="UIViewDef"/>.
		/// Prefer this overload (e.g. via <see cref="UIViewRef.Spawn"/>) over the name-based one when you
		/// already hold a direct asset reference, since it skips the name lookup entirely.
		///
		/// If the spawned view has a <see cref="UnityEngine.UI.CanvasScaler"/>, it is reconfigured to match
		/// the single app-wide <see cref="UISettings.Canvas"/> section, regardless of what's saved on the
		/// prefab.
		/// </summary>
		UIView SpawnView(UIViewDef viewDef);

		/// <summary>
		/// Despawns a UIView, returning it to the pool.
		/// </summary>
		void DespawnView(UIView view);
	}
}

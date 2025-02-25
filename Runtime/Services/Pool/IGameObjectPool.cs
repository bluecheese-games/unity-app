//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
	public interface IGameObjectPool
	{
		/// <summary>
		/// Spawns an instance from the pool using the prefab.
		/// </summary>
		GameObject Spawn();

		/// <summary>
		/// Spawns an instance from the pool using the component type.
		/// </summary>
		T Spawn<T>() where T : Component;

		/// <summary>
		/// Despawns an instance back to the pool.
		/// </summary>
		void Despawn(GameObject instance, float delay = 0f);

		/// <summary>
		/// Despawns an instance back to the pool.
		/// </summary>
		void Despawn<T>(T instance, float delay = 0f) where T : Component;

		/// <summary>
		/// Despawns all instances back to the pool.
		/// </summary>
		void DespawnAll(float delay);

		/// <summary>
		/// Removes an instance from the pool.
		/// </summary>
		void Delete(GameObject instance);

		/// <summary>
		/// Removes all available instances from the pool.
		/// </summary>
		void DeleteAll();

		/// <summary>
		/// Clears the pool by destroying all used and available instances.
		/// </summary>
		void Destroy();

		/// <summary>
		/// Fills the pool with the amount of instances.
		/// </summary>
		void Fill(int amount);

		/// <summary>
		/// Setup the pool with the options.
		/// </summary>
		void Setup(PoolOptions options);

		/// <summary>
		/// Returns the count of currently used instances.
		/// </summary>
		int CountInUse { get; }

		/// <summary>
		/// Returns the count of currently available instances.
		/// </summary>
		int CountAvailable { get; }
	}
}
//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;

namespace BlueCheese.App
{
    public interface ISceneService
    {
        /// <summary>
        /// Loads a scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        /// <param name="payload">Any payload to carry on to the next scene.</param>
        void Load(string sceneName, object payload = null);

        /// <summary>
        /// Asyncronously loads a scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        /// <param name="payload">Any payload to carry on to the next scene.</param>
        UniTask LoadAsync(string sceneName, object payload = null);

		/// <summary>
		/// Loads a scene additively.
		/// </summary>
		/// <param name="sceneName">The name of the scene to load.</param>
		UniTask LoadAdditiveAsync(string sceneName);

		/// <summary>
		/// Unloads a scene.
		/// </summary>
		/// <param name="sceneName">The name of the scene to unload.</param>
		UniTask UnloadAsync(string sceneName);
    }
}
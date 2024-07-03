//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;

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
        Task LoadAsync(string sceneName, object payload = null);
    }
}
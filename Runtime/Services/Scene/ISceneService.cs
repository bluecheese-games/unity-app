//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using System.Threading.Tasks;

namespace BlueCheese.Unity.App.Services
{
    public interface ISceneService
    {
        /// <summary>
        /// Loads a scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        void Load(string sceneName);

        /// <summary>
        /// Asyncronously loads a scene.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load.</param>
        Task LoadAsync(string sceneName);
    }
}
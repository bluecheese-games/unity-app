//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;

namespace BlueCheese.App.Services
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
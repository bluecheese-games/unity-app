//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Core.Utils;

namespace BlueCheese.Unity.App.Services
{
    public class UnitySceneService : ISceneService
    {
        public void Load(string sceneName) => SceneManager.LoadScene(sceneName);

        public async Task LoadAsync(string sceneName) => await SceneManager.LoadSceneAsync(sceneName);
    }
}
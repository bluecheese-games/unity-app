//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Core.Utils;
using Core.Signals;

namespace BlueCheese.App.Services
{
    public class UnitySceneService : ISceneService
    {
        public void Load(string sceneName, object payload = null)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            SignalAPI.Publish(new ExitSceneSignal(currentSceneName));
            SceneManager.LoadScene(sceneName);
            SignalAPI.Publish(new EnterSceneSignal(sceneName, payload));
        }

        public async Task LoadAsync(string sceneName, object payload = null)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            await SignalAPI.PublishAsync(new ExitSceneSignal(currentSceneName));
            await SceneManager.LoadSceneAsync(sceneName);
            await SignalAPI.PublishAsync(new EnterSceneSignal(sceneName, payload));
        }
    }
}
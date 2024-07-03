//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Core.Utils;
using Core.Signals;

namespace BlueCheese.App
{
    public class UnitySceneService : ISceneService
    {
        public void Load(string sceneName, object payload = null)
        {
            string exitingSceneName = SceneManager.GetActiveScene().name;
            SignalAPI.Publish(new ExitSceneSignal(exitingSceneName, sceneName, payload));
            SceneManager.LoadScene(sceneName);
            SignalAPI.Publish(new EnterSceneSignal(sceneName, exitingSceneName, payload));
        }

        public async Task LoadAsync(string sceneName, object payload = null)
        {
            string exitingSceneName = SceneManager.GetActiveScene().name;
            await SignalAPI.PublishAsync(new ExitSceneSignal(exitingSceneName, sceneName, payload));
            await SceneManager.LoadSceneAsync(sceneName);
            await SignalAPI.PublishAsync(new EnterSceneSignal(sceneName, exitingSceneName, payload));
        }
    }
}
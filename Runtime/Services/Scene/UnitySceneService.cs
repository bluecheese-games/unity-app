//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using BlueCheese.Core.Signals;

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

		public async UniTask LoadAsync(string sceneName, object payload = null)
        {
            string exitingSceneName = SceneManager.GetActiveScene().name;
            await SignalAPI.PublishAsync(new ExitSceneSignal(exitingSceneName, sceneName, payload));
            await SceneManager.LoadSceneAsync(sceneName);
            await SignalAPI.PublishAsync(new EnterSceneSignal(sceneName, exitingSceneName, payload));
		}

		public async UniTask LoadAdditiveAsync(string sceneName)
		{
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		}

		public async UniTask UnloadAsync(string sceneName)
		{
            await SceneManager.UnloadSceneAsync(sceneName);
		}
	}
}
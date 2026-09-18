using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using BlueCheese.Core.Signals;
using System.Collections.Generic;

namespace BlueCheese.App
{
	public class UnitySceneService : ISceneService
	{
		private readonly ILogger<UnitySceneService> _logger;

		public UnitySceneService(ILogger<UnitySceneService> logger)
		{
			_logger = logger;
		}

		public void Load(SceneRef scene, object payload = null)
		{
			if (!scene.IsValid)
			{
				_logger.LogError($"Invalid scene reference: {scene}");
				return;
			}

			SceneRef currentScene = SceneManager.GetActiveScene().name;
			if (currentScene == scene)
			{
				_logger.LogWarning($"Attempted to load the same scene: {scene}");
				return;
			}

			SignalAPI.Publish(new ExitSceneSignal(currentScene, scene, payload));
			SceneManager.LoadScene(scene);
			SignalAPI.Publish(new EnterSceneSignal(scene, currentScene, payload));
		}

		public async UniTask LoadAsync(SceneRef scene, object payload = null)
		{
			if (!scene.IsValid)
			{
				_logger.LogError($"Invalid scene reference: {scene}");
				return;
			}

			SceneRef currentScene = SceneManager.GetActiveScene().name;
			if (currentScene == scene)
			{
				_logger.LogWarning($"Attempted to load the same scene: {scene}");
				return;
			}

			await SignalAPI.PublishAsync(new ExitSceneSignal(currentScene, scene, payload));
			await SceneManager.LoadSceneAsync(scene).ToUniTask();
			await SignalAPI.PublishAsync(new EnterSceneSignal(scene, currentScene, payload));
		}

		public async UniTask LoadAdditiveAsync(SceneRef scene)
		{
			if (!scene.IsValid)
			{
				_logger.LogError($"Invalid scene reference: {scene}");
				return;
			}

			await SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive).ToUniTask();
		}

		public async UniTask UnloadAsync(SceneRef scene)
		{
			if (!scene.IsValid)
			{
				_logger.LogError($"Invalid scene reference: {scene}");
				return;
			}

			await SceneManager.UnloadSceneAsync(scene).ToUniTask();
		}

		public SceneRef CurrentScene => SceneManager.GetActiveScene().name;

		public IEnumerable<SceneRef> GetLoadedScenes()
		{
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				yield return SceneManager.GetSceneAt(i).name;
			}
		}
	}
}

using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using BlueCheese.Core.Signals;
using System.Collections.Generic;

namespace BlueCheese.App
{
	public class UnitySceneService : ISceneService
	{
		private readonly ILogger<UnitySceneService> _logger;

		// Tracks in-flight additive load/unload operations per scene. A state handler's OnExit
		// kicks off UnloadAsync fire-and-forget (IStateHandler is synchronous by design), so a
		// caller that quickly returns to that same screen can reach LoadAdditiveAsync before the
		// unload has actually finished -- SceneManager still reports the scene as loaded, which
		// without this tracking would spuriously hit the "already loaded" guard below and skip the
		// reload entirely instead of just being reported as a harmless warning. Awaiting the
		// pending operation first makes both methods correct regardless of call site timing.
		private readonly Dictionary<string, UniTask> _pendingOperations = new();

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

			await WaitForPendingOperation(scene);

			// Mirrors Load()/LoadAsync()'s own "already there" guard, which single-mode loading has always
			// had -- additive load/unload never did, so a caller couldn't safely call these unconditionally
			// (e.g. a screen that might already be the one the app was launched directly into).
			if (SceneManager.GetSceneByName(scene).isLoaded)
			{
				_logger.LogWarning($"Attempted to additively load an already-loaded scene: {scene}");
				return;
			}

			var operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive).ToUniTask().Preserve();
			_pendingOperations[scene] = operation;
			await operation;
			_pendingOperations.Remove(scene);
		}

		public async UniTask UnloadAsync(SceneRef scene)
		{
			if (!scene.IsValid)
			{
				_logger.LogError($"Invalid scene reference: {scene}");
				return;
			}

			await WaitForPendingOperation(scene);

			if (!SceneManager.GetSceneByName(scene).isLoaded)
			{
				_logger.LogWarning($"Attempted to unload a scene that isn't loaded: {scene}");
				return;
			}

			var operation = SceneManager.UnloadSceneAsync(scene).ToUniTask().Preserve();
			_pendingOperations[scene] = operation;
			await operation;
			_pendingOperations.Remove(scene);
		}

		private async UniTask WaitForPendingOperation(string scene)
		{
			if (_pendingOperations.TryGetValue(scene, out var pending))
			{
				await pending;
			}
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

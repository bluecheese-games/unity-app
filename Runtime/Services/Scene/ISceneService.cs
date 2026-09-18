using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace BlueCheese.App
{
	public interface ISceneService
	{
		/// <summary>
		/// Loads a scene.
		/// </summary>
		/// <param name="scene">The scene to load.</param>
		/// <param name="payload">Any payload to carry on to the next scene.</param>
		void Load(SceneRef scene, object payload = null);

		/// <summary>
		/// Asyncronously loads a scene.
		/// </summary>
		/// <param name="scene">The scene to load.</param>
		/// <param name="payload">Any payload to carry on to the next scene.</param>
		UniTask LoadAsync(SceneRef scene, object payload = null);

		/// <summary>
		/// Loads a scene additively.
		/// </summary>
		/// <param name="scene">The scene to load.</param>
		UniTask LoadAdditiveAsync(SceneRef scene);

		/// <summary>
		/// Unloads a scene.
		/// </summary>
		/// <param name="scene">The scene to unload.</param>
		UniTask UnloadAsync(SceneRef scene);

		/// <summary>
		/// Gets the currently active scene.
		/// </summary>
		SceneRef CurrentScene { get; }

		/// <summary>
		/// Gets a list of all currently loaded scenes.
		/// </summary>
		IEnumerable<SceneRef> GetLoadedScenes();
	}
}

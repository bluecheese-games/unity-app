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
		/// Marks an already-loaded scene as Unity's active scene -- the scene new GameObjects land in
		/// by default when instantiated without an explicit parent/scene (e.g. pooled objects spawned
		/// via <see cref="IGameObjectPoolService"/>), and the scene used for lighting settings. Loading
		/// a scene additively (<see cref="LoadAdditiveAsync"/>) never changes which scene is active on
		/// its own -- callers that consider a freshly-loaded additive scene "the current screen" need to
		/// call this explicitly afterward.
		/// </summary>
		/// <param name="scene">The (already loaded) scene to make active.</param>
		void SetActiveScene(SceneRef scene);

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

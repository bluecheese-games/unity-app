using BlueCheese.Core.DI;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace BlueCheese.App
{
	[Serializable]
	public struct SceneRef
	{
		public const string None = "None";

		public string Name;

		public SceneRef(string name)
		{
			Name = name;
		}

		public static SceneRef CurrentScene => ServiceLocator.Resolve<ISceneService>().CurrentScene;

		public static IEnumerable<SceneRef> GetLoadedScenes() => ServiceLocator.Resolve<ISceneService>().GetLoadedScenes();

		public readonly void Load() => ServiceLocator.Resolve<ISceneService>().Load(Name);

		public readonly UniTask LoadAsync(object payload = null) => ServiceLocator.Resolve<ISceneService>().LoadAsync(Name, payload);

		public readonly UniTask LoadAdditiveAsync() => ServiceLocator.Resolve<ISceneService>().LoadAdditiveAsync(Name);

		public readonly UniTask UnloadAsync() => ServiceLocator.Resolve<ISceneService>().UnloadAsync(Name);

		public readonly bool IsValid => !string.IsNullOrEmpty(Name) && Name != None;

		public static implicit operator string(SceneRef sceneRef) => sceneRef.Name;
		public static implicit operator SceneRef(string sceneName) => new(sceneName);

		public static bool operator ==(SceneRef a, SceneRef b) => a.Name == b.Name;
		public static bool operator !=(SceneRef a, SceneRef b) => a.Name != b.Name;

		public override readonly bool Equals(object obj)
		{
			if (obj is SceneRef other)
			{
				return this == other;
			}
			return false;
		}

		public override readonly int GetHashCode() => Name.GetHashCode();

		public override readonly string ToString() => Name;
	}
}

//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using UnityEngine;
using BlueCheese.Core.Utils;

namespace BlueCheese.App
{
	public class GameObjectService : IGameObjectService
    {
        public GameObject CreateEmptyObject(string name = null) => new(name);

        public T CreateObject<T>() where T : Component => new GameObject(typeof(T).Name).AddComponent<T>();

        public GameObject Instantiate(GameObject prefab) => GameObject.Instantiate(prefab);

        public T Instantiate<T>(T prefab) where T : Component => GameObject.Instantiate<T>(prefab);

        public void Destroy(GameObject obj, float delay = 0f) => GameObject.Destroy(obj, delay);

        public T Instantiate<T>(GameObject prefab) where T : Component => GameObject.Instantiate(prefab).GetComponent<T>();

        public void DontDestroyOnLoad(GameObject obj) => GameObject.DontDestroyOnLoad(obj);

        public T Find<T>(bool includeInactive = false) where T : Component => GameObject.FindFirstObjectByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);

        public T[] FindAll<T>(bool includeInactive = false) where T : Component => GameObject.FindObjectsOfType<T>(includeInactive);

		public async UniTask<GameObject> InstantiateAsync(GameObject prefab) => await GameObject.InstantiateAsync(prefab);

		public async UniTask<T> InstantiateAsync<T>(T prefab) where T : Component => await GameObject.InstantiateAsync<T>(prefab);

		public async UniTask<T> InstantiateAsync<T>(GameObject prefab) where T : Component => (await GameObject.InstantiateAsync(prefab)).GetComponent<T>();
	}
}

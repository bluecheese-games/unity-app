//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using UnityEngine;

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

        public T Find<T>(bool includeInactive = false) where T : Component =>
            GameObject.FindFirstObjectByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);

        public T[] FindAll<T>(bool includeInactive = false) where T : Component =>
            GameObject.FindObjectsByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);

		public async UniTask<GameObject> InstantiateAsync(GameObject prefab)
		{
			var op = GameObject.InstantiateAsync(prefab);
            await op;
            return op.Result[0];
		}

		public async UniTask<T> InstantiateAsync<T>(T prefab) where T : Component
        {
            var obj = await InstantiateAsync(prefab.gameObject);
            return obj.GetComponent<T>();
        }

		public async UniTask<T> InstantiateAsync<T>(GameObject prefab) where T : Component
		{
            var obj =  await InstantiateAsync(prefab);
            return obj.GetComponent<T>();
		}
	}
}

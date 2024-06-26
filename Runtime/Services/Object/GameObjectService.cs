//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App.Services
{
    public class GameObjectService : IGameObjectService
    {
        public GameObject CreateEmptyObject() => new GameObject();

        public T CreateObject<T>() where T : Component => new GameObject().AddComponent<T>();

        public GameObject Instantiate(GameObject prefab) => GameObject.Instantiate(prefab);

        public T Instantiate<T>(T prefab) where T : Component => GameObject.Instantiate<T>(prefab);

        public void Destroy(GameObject obj, float delay = 0f) => GameObject.Destroy(obj, delay);

        public T Instantiate<T>(GameObject prefab) where T : Component => GameObject.Instantiate(prefab).GetComponent<T>();

        public void DontDestroyOnLoad(GameObject obj) => GameObject.DontDestroyOnLoad(obj);

        public T Find<T>(bool includeInactive = false) where T : Component => GameObject.FindFirstObjectByType<T>();

        public T[] FindAll<T>(bool includeInactive = false) where T : Component => GameObject.FindObjectsOfType<T>();
    }
}

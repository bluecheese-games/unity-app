//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.Unity.App.Services
{
    public class GameObjectService : IGameObjectService
    {
        public GameObject CreateEmptyObject() => new GameObject();

        public T CreateObject<T>() where T : Component => new GameObject().AddComponent<T>();

        public GameObject Instantiate(GameObject prefab) => GameObject.Instantiate(prefab);

        public T Instantiate<T>(T prefab) where T : Component => GameObject.Instantiate<T>(prefab);

        public void Destroy(GameObject obj, float delay = 0f) => GameObject.Destroy(obj, delay);

        public T Instantiate<T>(GameObject prefab) where T : Component => GameObject.Instantiate(prefab).GetComponent<T>();
    }
}

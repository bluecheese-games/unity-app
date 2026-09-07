using System;
using UnityEngine;

namespace BlueCheese.App
{
	public class ClockUpdater : MonoBehaviour
    {
        public Action<float> UpdateCallback;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
			UpdateCallback?.Invoke(Time.deltaTime);
		}
    }
}

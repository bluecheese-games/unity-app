//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

// 
//  Copyright (c) 2024 Pierre Martin All rights reserved
// 

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.App.Services
{
    public class UnityClockService : IClockService
    {
        public event TickEventHandler OnTick;
        public event Action OnTickSecond;

        private readonly IGameObjectService _gameObjectService;

		private bool _isInitialized;
        private float _time = 0f;

		public UnityClockService(IGameObjectService gameObjectService)
		{
			_gameObjectService = gameObjectService;
		}

		public DateTime Now => DateTime.Now;

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            ClockUpdater updater = _gameObjectService.CreateObject<ClockUpdater>();
            updater.UpdateCallback = HandleUpdate;

            _isInitialized = true;
        }

        private void HandleUpdate(float deltaTime)
        {
            OnTick?.Invoke(deltaTime);
            float previousTime = _time;
            _time += deltaTime;

            if (Mathf.RoundToInt(previousTime) != Mathf.RoundToInt(_time))
            {
                OnTickSecond?.Invoke();
            }
        }

        public async Task InvokeAsync(Action action, float delay)
        {
            await WaitAsync(delay);
            action?.Invoke();
        }

        public async Task WaitAsync(float delay) => await Task.Delay(Mathf.RoundToInt(delay * 1000));
    }
}
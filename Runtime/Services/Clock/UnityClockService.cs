//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

// 
//  Copyright (c) 2024 Pierre Martin All rights reserved
// 

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.App
{
    public class UnityClockService : IClockService
    {
        public event TickEventHandler OnTick;
        public event AsyncTickEventHandler OnTickAsync;
        public event TickSecondEventHandler OnTickSecond;
        public event AsyncTickSecondEventHandler OnTickSecondAsync;

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
            if(OnTickAsync != null)
			{
				_ = OnTickAsync.Invoke(deltaTime);
			}

			float previousTime = _time;
            _time += deltaTime;

            if (Mathf.RoundToInt(previousTime) != Mathf.RoundToInt(_time))
            {
                OnTickSecond?.Invoke();
                if(OnTickSecondAsync != null)
				{
					_ = OnTickSecondAsync?.Invoke();
				}
			}
        }

        public async UniTask InvokeAsync(Action action, float delay)
        {
            await WaitAsync(delay);
            action?.Invoke();
        }

        public async UniTask WaitAsync(float delay) => await UniTask.Delay(Mathf.RoundToInt(delay * 1000));
    }
}
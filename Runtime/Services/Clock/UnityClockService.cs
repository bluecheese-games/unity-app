//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

// 
//  Copyright (c) 2024 Pierre Martin All rights reserved
// 

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace BlueCheese.Unity.App.Services
{
    public class UnityClockService : IClockService
    {
        public event TickEventHandler OnTick;
        public event Action OnTickSecond;

        private readonly IAssetService _assetService;

        private bool _isInitialized;
        private float _time = 0f;

        public UnityClockService(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public DateTime Now => DateTime.Now;

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            ClockUpdater updater = _assetService.Instantiate<ClockUpdater>(null, nameof(ClockUpdater));
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
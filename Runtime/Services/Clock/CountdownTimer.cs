//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;

namespace BlueCheese.Unity.App.Services
{
    public class CountdownTimer
    {
        public float Duration { get; private set; }
        public float Timeleft { get; private set; }
        public float TotalTimeleft
        {
            get
            {
                if (_isRepeating && _repeatCountLeft > 0)
                {
                    return Timeleft + _repeatCountLeft * Duration;
                }
                return Timeleft;
            }
        }

        public event Action OnStarted;
        public event Action OnCompleted;
        public event Action OnRepeated;

        private bool _isStarted = false;
        private bool _isRepeating = false;
        private int _repeatCountLeft = -1;

        public static CountdownTimer Create(float duration) => new(duration);

        private CountdownTimer(float duration)
        {
            Duration = duration;
            Timeleft = duration;
        }

        public CountdownTimer SetRepeating(bool repeating, int count = -1)
        {
            _isRepeating = repeating;
            _repeatCountLeft = count;
            return this;
        }

        public void Start()
        {
            if (_isStarted)
            {
                return;
            }

            _isStarted = true;
            OnStarted?.Invoke();
        }

        public void Stop()
        {
            if (!_isStarted)
            {
                return;
            }

            _isStarted = false;
        }

        public void Update(float deltaTime)
        {
            if (!_isStarted)
            {
                return;
            }

            Timeleft -= deltaTime;
            if (Timeleft <= 0)
            {
                if (_isRepeating && (_repeatCountLeft == -1 || _repeatCountLeft > 0))
                {
                    if (_repeatCountLeft > 0)
                    {
                        _repeatCountLeft--;
                    }
                    Timeleft += Duration;
                    OnRepeated?.Invoke();
                }
                else
                {
                    OnCompleted?.Invoke();
                }
            }
        }
    }
}

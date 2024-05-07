//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Unity.Core.Services;
using System;
using System.Threading.Tasks;

namespace BlueCheese.Unity.App.Services
{
    public delegate void TickEventHandler(float deltaTime);

    public interface IClockService : IInitializable
    {
        /// <summary>
        /// This event is called every frame.
        /// The parameter is the deltaTime == the amount of seconds since last frame
        /// </summary>
        event TickEventHandler OnTick;

        /// <summary>
        /// This event is called every seconds.
        /// </summary>
        event Action OnTickSecond;

        /// <summary>
        /// The current DateTime
        /// </summary>
        DateTime Now { get; }

        Task InvokeAsync(Action action, float delay);

        Task WaitAsync(float delay);
    }
}
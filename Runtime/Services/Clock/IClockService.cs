//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using System;
using Cysharp.Threading.Tasks;

namespace BlueCheese.App
{
    public delegate void TickEventHandler(float deltaTime);
    public delegate UniTask AsyncTickEventHandler(float deltaTime);
    public delegate void TickSecondEventHandler();
    public delegate UniTask AsyncTickSecondEventHandler();

    public interface IClockService : IInitializable
    {
        /// <summary>
        /// This event is called every frame.
        /// The parameter is the deltaTime == the amount of seconds since last frame
        /// </summary>
        event TickEventHandler OnTick;

        /// <summary>
        /// This event is called every frame.
        /// The parameter is the deltaTime == the amount of seconds since last frame
        /// </summary>
        event AsyncTickEventHandler OnTickAsync;

        /// <summary>
        /// This event is called every seconds.
        /// </summary>
        event TickSecondEventHandler OnTickSecond;

		/// <summary>
		/// This event is called every seconds.
		/// </summary>
		event AsyncTickSecondEventHandler OnTickSecondAsync;

		/// <summary>
		/// The current DateTime
		/// </summary>
		DateTime Now { get; }

		UniTask InvokeAsync(Action action, float delay);

		UniTask WaitAsync(float delay);
    }
}
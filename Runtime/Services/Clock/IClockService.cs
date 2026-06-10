//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using BlueCheese.Core.DI;
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
        /// This event is called every seconds.
        /// </summary>
        event TickSecondEventHandler OnTickSecond;

		/// <summary>
		/// The current DateTime
		/// </summary>
		DateTimeOffset UtcNow { get; }
    }
}
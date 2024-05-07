//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.Unity.App.Services
{
    public interface IInputService
    {
        /// <summary>
        /// Gets the button just pressed state of an Action.
        /// </summary>
        /// <param name="actionName">The Action name.</param>
        /// <returns>The state.</returns>
        bool GetButtonDown(string actionName);
    }
}

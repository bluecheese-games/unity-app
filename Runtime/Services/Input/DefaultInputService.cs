//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App.Services
{
    public class DefaultInputService : IInputService
    {
        public bool GetButtonDown(string actionName)
        {
            return Input.GetButtonDown(actionName);
        }
    }
}
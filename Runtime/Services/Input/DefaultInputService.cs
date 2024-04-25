//
// Copyright (c) 2024 Pierre Martin All rights reserved
//

using UnityEngine;

namespace BlueCheese.Unity.App.Services
{
    public class DefaultInputService : IInputService
    {
        public bool GetButtonDown(string actionName)
        {
            return Input.GetButtonDown(actionName);
        }
    }
}
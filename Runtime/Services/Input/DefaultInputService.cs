//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
    public class DefaultInputService : IInputService
    {
        public bool GetButton(string actionName) => Input.GetButton(actionName);

        public bool GetButtonDown(string actionName) => Input.GetButtonDown(actionName);

        public bool GetButtonUp(string actionName) => Input.GetButtonUp(actionName);

        public bool GetKey(KeyCode keyCode) => Input.GetKey(keyCode);

        public bool GetKeyDown(KeyCode keyCode) => Input.GetKeyDown(keyCode);

        public bool GetKeyUp(KeyCode keyCode) => Input.GetKeyUp(keyCode);

        public bool GetMouseButton(int button) => Input.GetMouseButton(button);

        public bool GetMouseButtonDown(int button) => Input.GetMouseButtonDown(button);

        public bool GetMouseButtonUp(int button) => Input.GetMouseButtonUp(button);

    }
}
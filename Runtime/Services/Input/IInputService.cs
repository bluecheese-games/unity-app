//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using UnityEngine;

namespace BlueCheese.App
{
    public interface IInputService
    {
        // Buttons
        bool GetButton(string actionName);
        bool GetButtonDown(string actionName);
        bool GetButtonUp(string actionName);

        // Keys
        bool GetKey(KeyCode keyCode);
        bool GetKeyDown(KeyCode keyCode);
        bool GetKeyUp(KeyCode keyCode);

        // Mouse
        bool GetMouseButton(int button);
        bool GetMouseButtonDown(int button);
        bool GetMouseButtonUp(int button);
    }
}

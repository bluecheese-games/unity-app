using UnityEngine;

namespace BlueCheese.App
{
    /// <summary>
    /// <see cref="IInputService"/> backed by the legacy <see cref="UnityEngine.Input"/> class (the "Input
    /// Manager (Old)" backend). Selected automatically by <see cref="DefaultServicesInstaller"/> unless the
    /// project's Active Input Handling includes the new Input System package -- see
    /// <see cref="InputSystemInputService"/> for that case. Throws at runtime if Active Input Handling is
    /// set to "Input System Package (New)" only, since UnityEngine.Input is disabled entirely in that mode.
    /// </summary>
    public class LegacyInputService : IInputService
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

        public Vector2 GetPointerPosition() => Input.mousePosition;

    }
}

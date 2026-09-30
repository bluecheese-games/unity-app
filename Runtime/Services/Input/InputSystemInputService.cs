#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BlueCheese.App
{
    /// <summary>
    /// <see cref="IInputService"/> backed by the new Input System (com.unity.inputsystem). Selected
    /// automatically by <see cref="DefaultServicesInstaller"/> when the project's Active Input Handling
    /// includes the new Input System (Unity's own ENABLE_INPUT_SYSTEM scripting define) -- see
    /// <see cref="LegacyInputService"/> for the "Input Manager (Old)" case.
    ///
    /// <see cref="IInputService.GetButton(string)"/>'s string-named-virtual-button shape is inherently an
    /// old Input Manager idiom (one global axis/button table configured in Project Settings); the new Input
    /// System instead expects a project-specific Input Actions asset with its own bindings, which this
    /// framework-level service can't assume exists or match. Rather than guess at a binding a given project
    /// might have remapped, only the two names the framework itself actually depends on --
    /// <see cref="BackHandler"/>'s "Cancel", plus the natural UI-navigation counterpart "Submit" -- are
    /// wired up (Escape/Enter on keyboard, the gamepad East/South face buttons). Any other name returns
    /// false. Projects needing more should either bind their own action asset directly, or register their
    /// own <see cref="IInputService"/> (see <c>ServiceContainer</c>'s "last registration wins" rule).
    /// </summary>
    public class InputSystemInputService : IInputService
    {
        public bool GetButton(string actionName) => actionName switch
        {
            "Cancel" => IsPressed(Keyboard.current?.escapeKey, Gamepad.current?.buttonEast),
            "Submit" => IsPressed(Keyboard.current?.enterKey, Gamepad.current?.buttonSouth),
            _ => false,
        };

        public bool GetButtonDown(string actionName) => actionName switch
        {
            "Cancel" => WasPressedThisFrame(Keyboard.current?.escapeKey, Gamepad.current?.buttonEast),
            "Submit" => WasPressedThisFrame(Keyboard.current?.enterKey, Gamepad.current?.buttonSouth),
            _ => false,
        };

        public bool GetButtonUp(string actionName) => actionName switch
        {
            "Cancel" => WasReleasedThisFrame(Keyboard.current?.escapeKey, Gamepad.current?.buttonEast),
            "Submit" => WasReleasedThisFrame(Keyboard.current?.enterKey, Gamepad.current?.buttonSouth),
            _ => false,
        };

        public bool GetKey(KeyCode keyCode) => ToInputSystemKey(keyCode) is { } key && Keyboard.current != null && Keyboard.current[key].isPressed;

        public bool GetKeyDown(KeyCode keyCode) => ToInputSystemKey(keyCode) is { } key && Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;

        public bool GetKeyUp(KeyCode keyCode) => ToInputSystemKey(keyCode) is { } key && Keyboard.current != null && Keyboard.current[key].wasReleasedThisFrame;

        public bool GetMouseButton(int button) => ToMouseButton(button)?.isPressed ?? false;

        public bool GetMouseButtonDown(int button) => ToMouseButton(button)?.wasPressedThisFrame ?? false;

        public bool GetMouseButtonUp(int button) => ToMouseButton(button)?.wasReleasedThisFrame ?? false;

        public Vector2 GetPointerPosition() => Pointer.current?.position.ReadValue() ?? Vector2.zero;

        private static bool IsPressed(ButtonControl a, ButtonControl b) => (a?.isPressed ?? false) || (b?.isPressed ?? false);
        private static bool WasPressedThisFrame(ButtonControl a, ButtonControl b) => (a?.wasPressedThisFrame ?? false) || (b?.wasPressedThisFrame ?? false);
        private static bool WasReleasedThisFrame(ButtonControl a, ButtonControl b) => (a?.wasReleasedThisFrame ?? false) || (b?.wasReleasedThisFrame ?? false);

        private static ButtonControl ToMouseButton(int button)
        {
            if (Mouse.current == null) return null;
            return button switch
            {
                0 => Mouse.current.leftButton,
                1 => Mouse.current.rightButton,
                2 => Mouse.current.middleButton,
                _ => null,
            };
        }

        // Deliberately not exhaustive: common keys only, kept as a switch expression rather than a
        // 200-entry KeyCode<->Key table. Extend as needed if more KeyCode-based checks show up.
        private static Key? ToInputSystemKey(KeyCode keyCode) => keyCode switch
        {
            KeyCode.Escape => Key.Escape,
            KeyCode.Return => Key.Enter,
            KeyCode.KeypadEnter => Key.NumpadEnter,
            KeyCode.Space => Key.Space,
            KeyCode.Tab => Key.Tab,
            KeyCode.Backspace => Key.Backspace,
            KeyCode.Delete => Key.Delete,
            KeyCode.UpArrow => Key.UpArrow,
            KeyCode.DownArrow => Key.DownArrow,
            KeyCode.LeftArrow => Key.LeftArrow,
            KeyCode.RightArrow => Key.RightArrow,
            _ => null,
        };
    }
}
#endif

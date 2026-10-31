#if TOOLKIT_INPUTSYSTEM
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace StuMillman.Toolkit.Input
{
    /// <summary>
    /// Tracks which kind of device the player used last, so prompts can switch between keyboard and gamepad.
    /// </summary>
    public static class InputDeviceTracker
    {
        static InputDeviceFamily _current = InputDeviceFamily.KeyboardMouse;
        static IDisposable _listener;

        /// <summary>The family of the most recently used device.</summary>
        public static InputDeviceFamily Current
        {
            get => _current;
            set
            {
                if (_current == value) return;
                _current = value;
                Changed?.Invoke(value);
            }
        }

        /// <summary>Raised when the player switches to a different kind of device.</summary>
        public static event Action<InputDeviceFamily> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            _listener?.Dispose();
            Changed = null;
            _current = Gamepad.current != null && Keyboard.current == null
                ? FamilyOf(Gamepad.current)
                : InputDeviceFamily.KeyboardMouse;
            _listener = InputSystem.onAnyButtonPress.Call(control => Current = FamilyOf(control.device));
        }

        /// <summary>Works out the family for a device.</summary>
        public static InputDeviceFamily FamilyOf(InputDevice device)
        {
            if (device is Gamepad)
            {
                var layout = device.layout;
                if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "DualShockGamepad")) return InputDeviceFamily.PlayStation;
                if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "XInputController")) return InputDeviceFamily.Xbox;
                if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "SwitchProControllerHID")) return InputDeviceFamily.Switch;
                return InputDeviceFamily.Gamepad;
            }

            return InputDeviceFamily.KeyboardMouse;
        }
    }
}
#endif

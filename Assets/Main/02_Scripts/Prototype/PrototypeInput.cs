using System;
using UnityEngine.InputSystem;

namespace BombStack.Prototype
{
    internal sealed class PrototypeInput : IDisposable
    {
        private readonly InputAction LEFT = Button("Left", "<Keyboard>/leftArrow", "<Keyboard>/a", "<Gamepad>/dpad/left");
        private readonly InputAction RIGHT = Button("Right", "<Keyboard>/rightArrow", "<Keyboard>/d", "<Gamepad>/dpad/right");
        private readonly InputAction CLOCKWISE = Button("Clockwise", "<Keyboard>/upArrow", "<Keyboard>/x", "<Gamepad>/buttonSouth");
        private readonly InputAction COUNTERCLOCKWISE = Button("Counterclockwise", "<Keyboard>/z", "<Gamepad>/buttonWest");
        private readonly InputAction SOFT = Button("Soft", "<Keyboard>/downArrow", "<Keyboard>/s", "<Gamepad>/dpad/down");
        private readonly InputAction DROP = Button("Drop", "<Keyboard>/space", "<Gamepad>/rightShoulder");
        private readonly InputAction PAUSE = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
        private readonly InputAction CONFIRM = Button("Confirm", "<Keyboard>/enter", "<Keyboard>/numpadEnter", "<Gamepad>/buttonSouth");
        private readonly InputAction BACK = Button("Back", "<Keyboard>/escape", "<Gamepad>/buttonEast");
        private readonly InputAction UP = Button("Up", "<Keyboard>/upArrow", "<Keyboard>/w", "<Gamepad>/dpad/up");
        private readonly InputAction[] ACTIONS;
        private double _leftTime;
        private double _rightTime;
        private double _clockwiseTime;
        private double _counterclockwiseTime;

        public PrototypeInput()
        {
            ACTIONS = new[] { LEFT, RIGHT, CLOCKWISE, COUNTERCLOCKWISE, SOFT, DROP, PAUSE, CONFIRM, BACK, UP };
            LEFT.performed += context => _leftTime = context.time;
            RIGHT.performed += context => _rightTime = context.time;
            CLOCKWISE.performed += context => _clockwiseTime = context.time;
            COUNTERCLOCKWISE.performed += context => _counterclockwiseTime = context.time;
            foreach (InputAction action in ACTIONS) action.Enable();
        }

        public int HorizontalDirection => LEFT.IsPressed() && RIGHT.IsPressed() ? (_leftTime > _rightTime ? -1 : 1) : LEFT.IsPressed() ? -1 : RIGHT.IsPressed() ? 1 : 0;
        public bool SoftDropHeld => SOFT.IsPressed();
        public bool HardDropPressed => DROP.WasPressedThisFrame();
        public bool PausePressed => PAUSE.WasPressedThisFrame();
        public bool ConfirmPressed => CONFIRM.WasPressedThisFrame();
        public bool BackPressed => BACK.WasPressedThisFrame();
        public bool PreviousPressed => LEFT.WasPressedThisFrame() || UP.WasPressedThisFrame();
        public bool NextPressed => RIGHT.WasPressedThisFrame() || SOFT.WasPressedThisFrame();
        public bool RotationPressed => CLOCKWISE.WasPressedThisFrame() || COUNTERCLOCKWISE.WasPressedThisFrame();
        public bool RotationClockwise => !COUNTERCLOCKWISE.WasPressedThisFrame() || (CLOCKWISE.WasPressedThisFrame() && _clockwiseTime >= _counterclockwiseTime);
        public bool GamepadPressed => WasDevicePressed<Gamepad>();
        public bool KeyboardPressed => WasDevicePressed<Keyboard>();

        private bool WasDevicePressed<T>() where T : InputDevice
        {
            foreach (InputAction action in ACTIONS)
                if (action.WasPressedThisFrame() && action.activeControl?.device is T) return true;
            return false;
        }

        public bool AnyHeld
        {
            get
            {
                foreach (InputAction action in ACTIONS) if (action.IsPressed()) return true;
                return false;
            }
        }

        private static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (string binding in bindings) action.AddBinding(binding);
            return action;
        }

        public void Dispose()
        {
            foreach (InputAction action in ACTIONS) action.Dispose();
        }
    }
}

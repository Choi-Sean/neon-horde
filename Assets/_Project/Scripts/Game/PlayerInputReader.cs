using UnityEngine;
using UnityEngine.InputSystem;

namespace NeonHorde
{
    /// <summary>
    /// Produces a normalized move vector from either keyboard (editor) or a
    /// drag-anywhere virtual stick (touch/mouse). No on-screen UI required for M0;
    /// a proper floating joystick widget lands in M1.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] float dragRadiusPixels = 140f;

        public Vector2 Move { get; private set; }

        Vector2 _dragOrigin;
        bool _dragging;
        VirtualJoystick _joystick;

        void Start()
        {
            _joystick = FindFirstObjectByType<VirtualJoystick>();
        }

        void Update()
        {
            Vector2 keyboard = ReadKeyboard();

            Vector2 v;
            if (keyboard.sqrMagnitude > 0.01f)
                v = keyboard;
            else if (_joystick != null && _joystick.Value.sqrMagnitude > 0.001f)
                v = _joystick.Value;
            else
                v = ReadPointer();

            if (v.sqrMagnitude > 1f) v.Normalize();
            Move = v;
        }

        static Vector2 ReadKeyboard()
        {
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;

            Vector2 v = Vector2.zero;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1f;
            return v;
        }

        Vector2 ReadPointer()
        {
            var p = Pointer.current;
            if (p == null) return Vector2.zero;

            bool pressed = p.press.isPressed;
            Vector2 pos = p.position.ReadValue();

            if (pressed && !_dragging)
            {
                _dragging = true;
                _dragOrigin = pos;
                return Vector2.zero;
            }
            if (pressed)
            {
                Vector2 delta = (pos - _dragOrigin) / Mathf.Max(1f, dragRadiusPixels);
                if (delta.sqrMagnitude > 1f) delta.Normalize();
                return delta;
            }
            _dragging = false;
            return Vector2.zero;
        }
    }
}

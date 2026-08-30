using UnityEngine;
using UnityEngine.EventSystems;

namespace NeonHorde
{
    /// <summary>
    /// Floating on-screen joystick: appears where the finger touches, follows within a
    /// radius. Sits on a full-screen transparent raycast target. Keyboard / drag-anywhere
    /// still work via PlayerInputReader when this reads zero.
    /// </summary>
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform ring;
        public RectTransform knob;
        public float radiusPixels = 90f;

        public Vector2 Value { get; private set; }

        Vector2 _center;
        bool _active;

        void Start()
        {
            if (ring != null) ring.gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData e)
        {
            _active = true;
            _center = e.position;
            if (ring != null)
            {
                ring.gameObject.SetActive(true);
                ring.position = e.position;
            }
            if (knob != null) knob.position = e.position;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_active) return;
            Vector2 delta = e.position - _center;
            Vector2 clamped = Vector2.ClampMagnitude(delta, radiusPixels);
            if (knob != null) knob.position = _center + clamped;
            Value = clamped / radiusPixels;
        }

        public void OnPointerUp(PointerEventData e)
        {
            _active = false;
            Value = Vector2.zero;
            if (ring != null) ring.gameObject.SetActive(false);
        }
    }
}

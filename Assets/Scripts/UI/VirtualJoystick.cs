using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Labyrinth.UI
{
    /// <summary>
    /// Virtual joystick for mobile touch input.
    /// Implements Unity EventSystem interfaces for drag handling.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform knob;
        
        [Header("Settings")]
        [SerializeField] private float deadZone = 0.1f;
        
        [Header("Visual Feedback")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float idleAlpha = 0.5f;
        [SerializeField] private float activeAlpha = 1f;

        private Vector2 _inputVector;
        private Canvas _canvas;

        /// <summary>
        /// Returns the normalized input vector. Returns zero if within dead zone.
        /// </summary>
        public Vector2 InputVector => _inputVector.magnitude > deadZone ? _inputVector : Vector2.zero;

        private void Start()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = idleAlpha;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = activeAlpha;
            }
            OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _inputVector = Vector2.zero;
            knob.anchoredPosition = Vector2.zero;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = idleAlpha;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                eventData.pressEventCamera,
                out position
            );

            float radius = background.sizeDelta.x / 2;
            position = Vector2.ClampMagnitude(position, radius);

            knob.anchoredPosition = position;
            _inputVector = position / radius;
        }
    }
}

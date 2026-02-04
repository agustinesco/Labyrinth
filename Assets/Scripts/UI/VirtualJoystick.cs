using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Labyrinth.UI
{
    /// <summary>
    /// Virtual joystick for mobile touch input with dynamic origin support.
    /// The joystick appears where you touch within the touch zone (bottom-left quarter of screen).
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform knob;
        [SerializeField] private RectTransform touchZone;

        [Header("Settings")]
        [SerializeField] private float deadZone = 0.1f;
        [SerializeField] private bool dynamicOrigin = true;

        [Header("Touch Zone Settings")]
        [SerializeField, Tooltip("Width of touch zone as percentage of screen width (0-1)")]
        private float touchZoneWidthPercent = 0.5f;
        [SerializeField, Tooltip("Height of touch zone as percentage of screen height (0-1)")]
        private float touchZoneHeightPercent = 0.5f;

        [Header("Dynamic Origin Settings")]
        [SerializeField, Tooltip("Padding from touch zone edges")]
        private float edgePadding = 80f;

        [Header("Visual Feedback")]
        [SerializeField] private CanvasGroup joystickCanvasGroup;
        [SerializeField] private float idleAlpha = 0.5f;
        [SerializeField] private float activeAlpha = 1f;

        private Vector2 _inputVector;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Vector2 _defaultPosition;
        private bool _isActive;
        private CanvasGroup _backgroundCanvasGroup;

        /// <summary>
        /// Returns the normalized input vector. Returns zero if within dead zone.
        /// </summary>
        public Vector2 InputVector => _inputVector.magnitude > deadZone ? _inputVector : Vector2.zero;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                _canvasRect = _canvas.GetComponent<RectTransform>();
            }

            // Store default position
            if (background != null)
            {
                _defaultPosition = background.anchoredPosition;
                _backgroundCanvasGroup = background.GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            SetupTouchZone();
            UpdateJoystickVisibility(false);
        }

        private void SetupTouchZone()
        {
            if (touchZone == null && dynamicOrigin)
            {
                CreateTouchZone();
            }

            if (touchZone != null)
            {
                // Make touch zone receive input but be invisible
                var touchZoneImage = touchZone.GetComponent<Image>();
                if (touchZoneImage == null)
                {
                    touchZoneImage = touchZone.gameObject.AddComponent<Image>();
                }
                touchZoneImage.raycastTarget = true;
                touchZoneImage.color = new Color(0, 0, 0, 0);

                // Configure touch zone size and position (bottom-left of screen)
                touchZone.anchorMin = new Vector2(0, 0);
                touchZone.anchorMax = new Vector2(touchZoneWidthPercent, touchZoneHeightPercent);
                touchZone.offsetMin = Vector2.zero;
                touchZone.offsetMax = Vector2.zero;
                touchZone.pivot = new Vector2(0, 0);
            }
        }

        private void CreateTouchZone()
        {
            // Create touch zone as parent of the joystick
            GameObject touchZoneObj = new GameObject("JoystickTouchZone");
            touchZone = touchZoneObj.AddComponent<RectTransform>();

            // Parent to canvas
            touchZone.SetParent(_canvas.transform, false);
            touchZone.SetAsFirstSibling(); // Put behind other UI

            // Reparent joystick background to touch zone
            if (background != null)
            {
                background.SetParent(touchZone, true);
            }

            // Add the event trigger component to touch zone
            // The VirtualJoystick component stays on the original object but we need events from touch zone
            var eventTrigger = touchZoneObj.AddComponent<EventTrigger>();

            // Forward pointer events to this joystick
            var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) => OnPointerDown((PointerEventData)data));
            eventTrigger.triggers.Add(pointerDown);

            var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) => OnPointerUp((PointerEventData)data));
            eventTrigger.triggers.Add(pointerUp);

            var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            drag.callback.AddListener((data) => OnDrag((PointerEventData)data));
            eventTrigger.triggers.Add(drag);
        }

        private void UpdateJoystickVisibility(bool active)
        {
            float alpha = active ? activeAlpha : idleAlpha;

            // Always set alpha on images directly to ensure visibility
            if (background != null)
            {
                var bgImage = background.GetComponent<Image>();
                if (bgImage != null)
                {
                    var color = bgImage.color;
                    color.a = alpha;
                    bgImage.color = color;
                }
            }

            if (knob != null)
            {
                var knobImage = knob.GetComponent<Image>();
                if (knobImage != null)
                {
                    var color = knobImage.color;
                    color.a = alpha;
                    knobImage.color = color;
                }
            }

            // Also update canvas groups if they exist
            if (joystickCanvasGroup != null)
            {
                joystickCanvasGroup.alpha = 1f; // Keep at 1, we control via image alpha
            }
            if (_backgroundCanvasGroup != null)
            {
                _backgroundCanvasGroup.alpha = 1f;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isActive = true;

            if (dynamicOrigin && background != null && touchZone != null)
            {
                // Move joystick to touch position within touch zone
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    touchZone,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint
                );

                // Clamp position to keep joystick fully visible with padding
                float halfSize = background.sizeDelta.x / 2f;
                Vector2 zoneSize = touchZone.rect.size;

                float minX = halfSize + edgePadding;
                float maxX = zoneSize.x - halfSize - edgePadding;
                float minY = halfSize + edgePadding;
                float maxY = zoneSize.y - halfSize - edgePadding;

                // Convert from center-based to corner-based coordinates (touch zone pivot is 0,0)
                localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
                localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

                background.anchoredPosition = localPoint;
            }

            // Show joystick
            UpdateJoystickVisibility(true);

            // Reset knob and process touch
            if (knob != null)
            {
                knob.anchoredPosition = Vector2.zero;
            }
            OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isActive = false;
            _inputVector = Vector2.zero;

            if (knob != null)
            {
                knob.anchoredPosition = Vector2.zero;
            }

            UpdateJoystickVisibility(false);

            // Return to default position
            if (dynamicOrigin && background != null)
            {
                background.anchoredPosition = _defaultPosition;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null || knob == null) return;

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

        /// <summary>
        /// Sets whether the joystick uses dynamic origin.
        /// </summary>
        public void SetDynamicOrigin(bool enabled)
        {
            dynamicOrigin = enabled;
            if (enabled && touchZone == null)
            {
                SetupTouchZone();
            }
        }

        /// <summary>
        /// Returns whether the joystick is currently being used.
        /// </summary>
        public bool IsActive => _isActive;

#if UNITY_EDITOR
        private void OnValidate()
        {
            touchZoneWidthPercent = Mathf.Clamp01(touchZoneWidthPercent);
            touchZoneHeightPercent = Mathf.Clamp01(touchZoneHeightPercent);
        }
#endif
    }
}

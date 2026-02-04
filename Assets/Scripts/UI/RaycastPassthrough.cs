using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Labyrinth.UI
{
    /// <summary>
    /// Allows raycasts to pass through to UI elements behind this one.
    /// Only captures events if no other UI element is being hit.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class RaycastPassthrough : MonoBehaviour, ICanvasRaycastFilter
    {
        private GraphicRaycaster _raycaster;
        private EventSystem _eventSystem;
        private bool _isChecking;

        private void Awake()
        {
            _raycaster = GetComponentInParent<GraphicRaycaster>();
            _eventSystem = EventSystem.current;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            // Prevent recursive calls
            if (_isChecking)
                return true;

            // Check if there are other UI elements at this position
            if (_raycaster == null || _eventSystem == null)
            {
                _raycaster = GetComponentInParent<GraphicRaycaster>();
                _eventSystem = EventSystem.current;
            }

            if (_raycaster == null || _eventSystem == null)
                return true;

            _isChecking = true;

            try
            {
                // Perform raycast to find all UI elements at this position
                var pointerEventData = new PointerEventData(_eventSystem)
                {
                    position = screenPoint
                };

                var results = new List<RaycastResult>();
                _raycaster.Raycast(pointerEventData, results);

                // Check if any other UI element (not this one) was hit
                foreach (var result in results)
                {
                    if (result.gameObject != gameObject && result.gameObject.GetComponent<RaycastPassthrough>() == null)
                    {
                        // Another UI element is in front, let the click pass through
                        return false;
                    }
                }

                // No other UI elements, capture the event
                return true;
            }
            finally
            {
                _isChecking = false;
            }
        }
    }
}

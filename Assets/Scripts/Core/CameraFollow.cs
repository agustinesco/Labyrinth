using UnityEngine;

namespace Labyrinth.Core
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

        [Header("Bounds (optional)")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private float minX = 0f;
        [SerializeField] private float maxX = 25f;
        [SerializeField] private float minY = 0f;
        [SerializeField] private float maxY = 25f;

        private Camera _camera;
        private float _halfHeight;
        private float _halfWidth;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera != null)
            {
                _halfHeight = _camera.orthographicSize;
                _halfWidth = _halfHeight * _camera.aspect;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Sets the camera bounds based on maze size.
        /// </summary>
        public void SetBounds(float mazeWidth, float mazeHeight)
        {
            minX = 0f;
            maxX = mazeWidth - 1;
            minY = 0f;
            maxY = mazeHeight - 1;
            useBounds = true;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // Clamp to bounds if enabled
            if (useBounds && _camera != null)
            {
                // Update half dimensions in case of window resize
                _halfHeight = _camera.orthographicSize;
                _halfWidth = _halfHeight * _camera.aspect;

                smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX + _halfWidth, maxX - _halfWidth);
                smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY + _halfHeight, maxY - _halfHeight);
            }

            transform.position = smoothedPosition;
        }
    }
}

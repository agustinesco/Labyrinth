using UnityEngine;

namespace Labyrinth.Traps
{
    public class TripwireTrap : MonoBehaviour
    {
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private Color wireColor = new Color(0.4f, 0.3f, 0.2f, 0.6f);
        [SerializeField] private float wireWidth = 0.1f;

        private Vector2 _arrowDirection;
        private Vector3 _arrowSpawnPosition;
        private float _corridorWidth;
        private LineRenderer _lineRenderer;
        private BoxCollider2D _collider;
        private bool _triggered;

        public void Initialize(Vector2 arrowDirection, float corridorWidth)
        {
            _arrowDirection = arrowDirection.normalized;
            _corridorWidth = corridorWidth;

            // Arrow spawns from the wall behind its travel direction
            // Offset by half corridor width + small buffer so it starts from the edge
            _arrowSpawnPosition = transform.position - (Vector3)(_arrowDirection * (corridorWidth * 0.5f + 0.3f));

            SetupCollider();
            SetupWireVisual();
        }

        public void SetArrowPrefab(GameObject prefab)
        {
            arrowPrefab = prefab;
        }

        private void SetupCollider()
        {
            _collider = GetComponent<BoxCollider2D>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider2D>();
            }
            _collider.isTrigger = true;

            // Wire runs perpendicular to arrow direction
            // If arrow shoots up/down, wire is horizontal
            // If arrow shoots left/right, wire is vertical
            if (Mathf.Abs(_arrowDirection.x) > Mathf.Abs(_arrowDirection.y))
            {
                // Arrow shoots horizontally, wire is vertical
                _collider.size = new Vector2(0.5f, _corridorWidth);
            }
            else
            {
                // Arrow shoots vertically, wire is horizontal
                _collider.size = new Vector2(_corridorWidth, 0.5f);
            }
        }

        private void SetupWireVisual()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = wireColor;
            _lineRenderer.endColor = wireColor;
            _lineRenderer.startWidth = wireWidth;
            _lineRenderer.endWidth = wireWidth;
            _lineRenderer.positionCount = 2;
            _lineRenderer.sortingOrder = 5;
            _lineRenderer.useWorldSpace = true;

            // Wire runs perpendicular to arrow direction
            Vector2 wireDirection = new Vector2(-_arrowDirection.y, _arrowDirection.x);
            float halfWidth = _corridorWidth * 0.5f;

            Vector3 wireStart = transform.position - (Vector3)(wireDirection * halfWidth);
            Vector3 wireEnd = transform.position + (Vector3)(wireDirection * halfWidth);

            _lineRenderer.SetPosition(0, wireStart);
            _lineRenderer.SetPosition(1, wireEnd);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered) return;

            if (other.CompareTag("Player"))
            {
                TriggerTrap();
            }
        }

        private void TriggerTrap()
        {
            _triggered = true;

            // Spawn arrow
            if (arrowPrefab != null)
            {
                var arrowObj = Instantiate(arrowPrefab, _arrowSpawnPosition, Quaternion.identity);
                var arrow = arrowObj.GetComponent<Arrow>();
                if (arrow != null)
                {
                    arrow.Initialize(_arrowDirection);
                }
            }

            // Destroy the tripwire
            Destroy(gameObject);
        }
    }
}

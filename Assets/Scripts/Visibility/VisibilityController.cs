using UnityEngine;

namespace Labyrinth.Visibility
{
    /// <summary>
    /// Controls the player's visibility area using raycasts to detect walls.
    /// Creates a dynamic mesh that represents the visible area around the player.
    /// </summary>
    public class VisibilityController : MonoBehaviour
    {
        [SerializeField] private float baseVisibilityRadius = 4f;
        [SerializeField] private int rayCount = 90;
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

        private Mesh _mesh;
        private Vector3[] _vertices;
        private int[] _triangles;
        private float _visibilityBonus;
        private float _visibilityBoostTimer;

        public float CurrentRadius => baseVisibilityRadius + _visibilityBonus;

        private void Awake()
        {
            InitializeMesh();
        }

        private void InitializeMesh()
        {
            // Get components if not set
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            // Create the mesh
            _mesh = new Mesh();
            _mesh.name = "VisibilityMesh";
            _mesh.MarkDynamic();

            // Pre-allocate arrays
            _vertices = new Vector3[rayCount + 2];
            _triangles = new int[rayCount * 3];

            // Pre-calculate triangles (they don't change)
            for (int i = 1; i <= rayCount; i++)
            {
                int triIndex = (i - 1) * 3;
                _triangles[triIndex] = 0;
                _triangles[triIndex + 1] = i + 1;
                _triangles[triIndex + 2] = i;
            }

            // Assign mesh to filter
            if (meshFilter != null)
            {
                meshFilter.mesh = _mesh;
            }
        }

        private void LateUpdate()
        {
            UpdateVisibilityBoostTimer();
            UpdateVisibilityMesh();
        }

        private void UpdateVisibilityBoostTimer()
        {
            if (_visibilityBoostTimer > 0)
            {
                _visibilityBoostTimer -= Time.deltaTime;
                if (_visibilityBoostTimer <= 0)
                {
                    _visibilityBonus = 0;
                }
            }
        }

        private void UpdateVisibilityMesh()
        {
            if (_mesh == null || _vertices == null) return;

            float angleStep = 360f / rayCount;

            // Get ray origin from parent (Player) position
            Vector2 rayOrigin = transform.parent != null
                ? (Vector2)transform.parent.position
                : (Vector2)transform.position;

            // Center vertex
            _vertices[0] = Vector3.zero;

            // Calculate vertices around the circle
            for (int i = 0; i <= rayCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                float distance = CurrentRadius;

                // Raycast to find walls
                if (wallLayer.value != 0)
                {
                    RaycastHit2D hit = Physics2D.Raycast(rayOrigin, direction, CurrentRadius, wallLayer);
                    if (hit.collider != null)
                    {
                        distance = hit.distance;
                    }
                }

                _vertices[i + 1] = new Vector3(direction.x * distance, direction.y * distance, 0f);
            }

            // Update mesh
            _mesh.vertices = _vertices;
            _mesh.triangles = _triangles;
            _mesh.RecalculateBounds();
        }

        /// <summary>
        /// Applies a temporary visibility boost to the player.
        /// </summary>
        /// <param name="bonus">Additional radius to add</param>
        /// <param name="duration">Duration of the boost in seconds</param>
        public void ApplyVisibilityBoost(float bonus, float duration)
        {
            _visibilityBonus = bonus;
            _visibilityBoostTimer = duration;
        }

        /// <summary>
        /// Gets the remaining time on the visibility boost.
        /// </summary>
        /// <returns>Time remaining in seconds, or 0 if no boost is active</returns>
        public float GetVisibilityBoostTimeRemaining()
        {
            return _visibilityBoostTimer;
        }

        /// <summary>
        /// Gets whether a visibility boost is currently active.
        /// </summary>
        public bool HasVisibilityBoost => _visibilityBoostTimer > 0;
    }
}

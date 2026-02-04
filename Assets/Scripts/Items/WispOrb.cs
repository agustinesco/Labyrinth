using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Maze;
using Labyrinth.Visibility;

namespace Labyrinth.Items
{
    public class WispOrb : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speedMultiplier = 3f;
        [SerializeField] private float basePlayerSpeed = 5f;

        [Header("Visual")]
        [SerializeField] private Color orbColor = new Color(0.5f, 0.8f, 1f, 1f);
        [SerializeField] private float orbSize = 0.3f;

        [Header("Trail")]
        [SerializeField] private float trailFadeTime = 3f;
        [SerializeField] private float trailSegmentSpacing = 0.3f;
        [SerializeField] private Color trailColor = new Color(0.5f, 0.8f, 1f, 0.6f);

        private MazeGrid _grid;
        private Pathfinding _pathfinding;
        private List<Vector2Int> _path;
        private int _pathIndex;
        private Vector2 _targetPosition;
        private bool _hasReachedTarget;
        private float _lastTrailSpawnDistance;
        private Vector2 _lastTrailPosition;

        private SpriteRenderer _spriteRenderer;

        public void Initialize(MazeGrid grid, Vector2 targetPosition)
        {
            _grid = grid;
            _targetPosition = targetPosition;
            _pathfinding = new Pathfinding(grid);

            // Calculate path to target
            Vector2Int startPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)
            );
            Vector2Int goalPos = new Vector2Int(
                Mathf.RoundToInt(targetPosition.x),
                Mathf.RoundToInt(targetPosition.y)
            );

            _path = _pathfinding.FindPath(startPos, goalPos);
            _pathIndex = 0;
            _lastTrailPosition = transform.position;

            // Setup visuals
            SetupVisuals();

            if (_path == null || _path.Count == 0)
            {
                Debug.LogWarning("WispOrb: Could not find path to target!");
                Destroy(gameObject);
            }
        }

        private void SetupVisuals()
        {
            // Create sprite renderer for the orb
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite = CreateOrbSprite();
            _spriteRenderer.color = orbColor;
            _spriteRenderer.sortingOrder = 50;
            transform.localScale = Vector3.one * orbSize;

            // Add visibility awareness
            gameObject.AddComponent<VisibilityAwareEntity>();
        }

        private Sprite CreateOrbSprite()
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist < radius)
                    {
                        // Soft glow effect
                        float alpha = 1f - (dist / radius);
                        alpha = alpha * alpha; // Quadratic falloff for softer glow
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void Update()
        {
            if (_hasReachedTarget || _path == null || _pathIndex >= _path.Count)
                return;

            float speed = basePlayerSpeed * speedMultiplier;

            // Get current waypoint
            Vector3 targetWaypoint = new Vector3(_path[_pathIndex].x + 0.5f, _path[_pathIndex].y + 0.5f, 0);

            // Move towards waypoint
            transform.position = Vector3.MoveTowards(transform.position, targetWaypoint, speed * Time.deltaTime);

            // Spawn trail segments
            float distanceTraveled = Vector2.Distance(transform.position, _lastTrailPosition);
            if (distanceTraveled >= trailSegmentSpacing)
            {
                SpawnTrailSegment(_lastTrailPosition);
                _lastTrailPosition = transform.position;
            }

            // Check if reached waypoint
            if (Vector3.Distance(transform.position, targetWaypoint) < 0.1f)
            {
                _pathIndex++;

                // Check if reached final destination
                if (_pathIndex >= _path.Count)
                {
                    OnReachedTarget();
                }
            }
        }

        private void SpawnTrailSegment(Vector2 position)
        {
            GameObject trailObj = new GameObject("WispTrail");
            trailObj.transform.position = new Vector3(position.x, position.y, 0);

            SpriteRenderer sr = trailObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateOrbSprite();
            sr.color = trailColor;
            sr.sortingOrder = 49;
            trailObj.transform.localScale = Vector3.one * (orbSize * 0.6f);

            // Add visibility awareness
            trailObj.AddComponent<VisibilityAwareEntity>();

            // Add fading component
            WispTrailSegment segment = trailObj.AddComponent<WispTrailSegment>();
            segment.Initialize(trailFadeTime, trailColor);
        }

        private void OnReachedTarget()
        {
            _hasReachedTarget = true;

            // Spawn a final trail segment at the target
            SpawnTrailSegment(transform.position);

            // Destroy the orb
            Destroy(gameObject);
        }

        /// <summary>
        /// Creates and initializes a wisp orb at the specified position.
        /// </summary>
        public static WispOrb SpawnAt(Vector2 position, MazeGrid grid, Vector2 targetPosition)
        {
            GameObject orbObj = new GameObject("WispOrb");
            orbObj.transform.position = new Vector3(position.x, position.y, 0);

            WispOrb orb = orbObj.AddComponent<WispOrb>();
            orb.Initialize(grid, targetPosition);

            return orb;
        }
    }

    /// <summary>
    /// Handles the fading of wisp trail segments.
    /// </summary>
    public class WispTrailSegment : MonoBehaviour
    {
        private float _fadeTime;
        private float _timer;
        private Color _startColor;
        private SpriteRenderer _spriteRenderer;

        public void Initialize(float fadeTime, Color startColor)
        {
            _fadeTime = fadeTime;
            _startColor = startColor;
            _timer = 0f;
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float progress = _timer / _fadeTime;

            if (_spriteRenderer != null)
            {
                Color color = _startColor;
                color.a = Mathf.Lerp(_startColor.a, 0f, progress);
                _spriteRenderer.color = color;
            }

            if (_timer >= _fadeTime)
            {
                Destroy(gameObject);
            }
        }
    }
}

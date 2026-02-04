using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Enemy;

namespace Labyrinth.Items
{
    /// <summary>
    /// Creates a sonar pulse effect that reveals enemy positions through walls.
    /// Sound enemies will hear the ping and investigate the player's location.
    /// </summary>
    public class EchoPulse : MonoBehaviour
    {
        private static readonly Color PulseColor = new Color(0.3f, 0.7f, 1f, 0.5f); // Light blue
        private static readonly Color EnemyMarkerColor = new Color(1f, 0.3f, 0.3f, 0.8f); // Red

        private float _revealRadius;
        private float _revealDuration;
        private float _pulseExpandDuration = 0.5f;
        private float _timer;
        private float _pulseTimer;
        private bool _pulseComplete;

        private List<EnemyMarker> _enemyMarkers = new List<EnemyMarker>();
        private SpriteRenderer _pulseVisual;

        public static void CreateAt(Vector2 position, float revealRadius, float revealDuration)
        {
            GameObject pulseObj = new GameObject("EchoPulse");
            pulseObj.transform.position = new Vector3(position.x, position.y, 0);

            var pulse = pulseObj.AddComponent<EchoPulse>();
            pulse._revealRadius = revealRadius;
            pulse._revealDuration = revealDuration;
            pulse.Initialize();

            Debug.Log($"EchoPulse: Created at {position} with radius {revealRadius}");
        }

        private void Initialize()
        {
            // Create expanding pulse visual
            GameObject visualObj = new GameObject("PulseVisual");
            visualObj.transform.SetParent(transform);
            visualObj.transform.localPosition = Vector3.zero;

            _pulseVisual = visualObj.AddComponent<SpriteRenderer>();
            _pulseVisual.sprite = CreateCircleSprite(64);
            _pulseVisual.color = PulseColor;
            _pulseVisual.sortingOrder = 100; // Above everything
            visualObj.transform.localScale = Vector3.zero;

            // Find and mark all enemies within radius
            FindAndMarkEnemies();
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            _pulseTimer += Time.deltaTime;

            // Animate pulse expansion
            if (!_pulseComplete)
            {
                float pulseProgress = _pulseTimer / _pulseExpandDuration;
                if (pulseProgress >= 1f)
                {
                    pulseProgress = 1f;
                    _pulseComplete = true;
                }

                float currentScale = _revealRadius * 2f * pulseProgress;
                _pulseVisual.transform.localScale = Vector3.one * currentScale;

                // Fade out as it expands
                Color c = PulseColor;
                c.a = PulseColor.a * (1f - pulseProgress * 0.7f);
                _pulseVisual.color = c;
            }
            else
            {
                // Fade out completely after pulse is done
                Color c = _pulseVisual.color;
                c.a = Mathf.Max(0, c.a - Time.deltaTime * 2f);
                _pulseVisual.color = c;
            }

            // Update enemy markers (fade out over time)
            float markerAlpha = 1f - (_timer / _revealDuration);
            foreach (var marker in _enemyMarkers)
            {
                if (marker.spriteRenderer != null)
                {
                    Color c = EnemyMarkerColor;
                    c.a = EnemyMarkerColor.a * markerAlpha;
                    marker.spriteRenderer.color = c;

                    // Update position to track moving enemy
                    if (marker.trackedEnemy != null)
                    {
                        marker.markerObject.transform.position = marker.trackedEnemy.position;
                    }
                }
            }

            // Clean up when duration expires
            if (_timer >= _revealDuration)
            {
                CleanUp();
            }
        }

        private void FindAndMarkEnemies()
        {
            Vector2 pulseOrigin = transform.position;

            // Find all enemy controllers
            var basicEnemies = FindObjectsOfType<EnemyController>();
            var guards = FindObjectsOfType<PatrollingGuardController>();
            var moles = FindObjectsOfType<BlindMoleController>();
            var stalkers = FindObjectsOfType<ShadowStalkerController>();

            // Mark basic enemies
            foreach (var enemy in basicEnemies)
            {
                float distance = Vector2.Distance(pulseOrigin, enemy.transform.position);
                if (distance <= _revealRadius)
                {
                    CreateEnemyMarker(enemy.transform);
                }
            }

            // Mark guards
            foreach (var guard in guards)
            {
                float distance = Vector2.Distance(pulseOrigin, guard.transform.position);
                if (distance <= _revealRadius)
                {
                    CreateEnemyMarker(guard.transform);
                }
            }

            // Mark moles
            foreach (var mole in moles)
            {
                float distance = Vector2.Distance(pulseOrigin, mole.transform.position);
                if (distance <= _revealRadius)
                {
                    CreateEnemyMarker(mole.transform);
                }
            }

            // Mark stalkers
            foreach (var stalker in stalkers)
            {
                float distance = Vector2.Distance(pulseOrigin, stalker.transform.position);
                if (distance <= _revealRadius)
                {
                    CreateEnemyMarker(stalker.transform);
                }
            }

            Debug.Log($"EchoPulse: Revealed {_enemyMarkers.Count} enemies");
        }

        private void CreateEnemyMarker(Transform enemy)
        {
            GameObject markerObj = new GameObject("EnemyMarker");
            markerObj.transform.position = enemy.position;

            SpriteRenderer sr = markerObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateMarkerSprite();
            sr.color = EnemyMarkerColor;
            sr.sortingOrder = 101; // Above pulse

            _enemyMarkers.Add(new EnemyMarker
            {
                markerObject = markerObj,
                spriteRenderer = sr,
                trackedEnemy = enemy
            });
        }

        private void CleanUp()
        {
            foreach (var marker in _enemyMarkers)
            {
                if (marker.markerObject != null)
                {
                    Destroy(marker.markerObject);
                }
            }
            _enemyMarkers.Clear();
            Destroy(gameObject);
        }

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;
            float innerRadius = radius * 0.9f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist < radius && dist > innerRadius)
                    {
                        // Ring shape
                        float alpha = 1f - Mathf.Abs(dist - (radius + innerRadius) / 2f) / ((radius - innerRadius) / 2f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else if (dist <= innerRadius)
                    {
                        // Inner fill with gradient
                        float alpha = 0.3f * (1f - dist / innerRadius);
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

        private static Sprite CreateMarkerSprite()
        {
            int size = 24;
            Texture2D texture = new Texture2D(size, size);
            Vector2 center = new Vector2(size / 2f, size / 2f);

            // Clear background
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }

            // Draw diamond/rhombus shape for enemy marker
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = Mathf.Abs(x - center.x);
                    float dy = Mathf.Abs(y - center.y);
                    float dist = dx + dy;

                    if (dist < size / 2f * 0.8f)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private struct EnemyMarker
        {
            public GameObject markerObject;
            public SpriteRenderer spriteRenderer;
            public Transform trackedEnemy;
        }
    }
}

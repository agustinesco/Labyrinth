using UnityEngine;
using Labyrinth.Maze;
using Labyrinth.Player;

namespace Labyrinth.Items
{
    public class Explosion : MonoBehaviour
    {
        [SerializeField] private float fuseTime = 1f;
        [SerializeField] private float explosionDuration = 0.3f;
        [SerializeField] private Color explosionColor = new Color(1f, 0.5f, 0f, 0.8f);

        private int _range;
        private int _damage;
        private float _timer;
        private bool _exploded;
        private SpriteRenderer _spriteRenderer;

        public void Initialize(int range, int damage)
        {
            _range = range;
            _damage = damage;
            _timer = fuseTime;

            // Create visual for the bomb
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite = CreateCircleSprite();
            _spriteRenderer.color = Color.black;
            _spriteRenderer.sortingOrder = 10;
            transform.localScale = Vector3.one * 0.5f;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;

            if (!_exploded)
            {
                // Pulsing effect before explosion
                float pulse = Mathf.Abs(Mathf.Sin(_timer * 8f));
                _spriteRenderer.color = Color.Lerp(Color.black, Color.red, pulse);

                if (_timer <= 0)
                {
                    Explode();
                }
            }
            else
            {
                // After explosion, fade out
                if (_timer <= 0)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void Explode()
        {
            _exploded = true;
            _timer = explosionDuration;

            // Expand visual to show explosion range
            transform.localScale = Vector3.one * (_range * 2);
            _spriteRenderer.color = explosionColor;

            // Get maze renderer to destroy walls
            var mazeRenderer = FindFirstObjectByType<MazeRenderer>();

            // Calculate explosion center in grid coordinates
            int centerX = Mathf.RoundToInt(transform.position.x);
            int centerY = Mathf.RoundToInt(transform.position.y);

            // Destroy walls in range (2x2 means 1 cell in each direction from center)
            int halfRange = _range / 2;
            for (int x = centerX - halfRange; x <= centerX + halfRange; x++)
            {
                for (int y = centerY - halfRange; y <= centerY + halfRange; y++)
                {
                    if (mazeRenderer != null)
                    {
                        mazeRenderer.DestroyWallAt(x, y);
                    }
                }
            }

            // Check if player is in explosion range
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector2.Distance(transform.position, player.transform.position);
                if (distance <= _range)
                {
                    var playerHealth = player.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(_damage);
                    }
                }
            }
        }

        private Sprite CreateCircleSprite()
        {
            int size = 32;
            var texture = new Texture2D(size, size);
            var center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        texture.SetPixel(x, y, Color.white);
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
    }
}

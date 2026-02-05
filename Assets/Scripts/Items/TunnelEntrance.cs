using UnityEngine;
using System.Collections;

namespace Labyrinth.Items
{
    /// <summary>
    /// A tunnel entrance that teleports the player to its paired entrance.
    /// </summary>
    public class TunnelEntrance : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float teleportDelay = 0.2f;
        [SerializeField] private float cooldownDuration = 0.5f;

        private TunnelEntrance _pairedEntrance;
        private bool _isOnCooldown;
        private static bool _isTeleporting;

        public TunnelEntrance PairedEntrance => _pairedEntrance;

        private void Start()
        {
            // Ensure collider is set as trigger
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        /// <summary>
        /// Links this entrance to its paired entrance.
        /// </summary>
        public void SetPairedEntrance(TunnelEntrance other)
        {
            _pairedEntrance = other;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (_pairedEntrance == null) return;
            if (_isOnCooldown || _isTeleporting) return;

            StartCoroutine(TeleportPlayer(other.transform));
        }

        private IEnumerator TeleportPlayer(Transform player)
        {
            _isTeleporting = true;

            // Brief delay before teleport
            yield return new WaitForSeconds(teleportDelay);

            // Teleport player to paired entrance with small offset to avoid retriggering
            Vector3 targetPos = _pairedEntrance.transform.position;
            player.position = targetPos;

            // Start cooldown on the destination entrance
            _pairedEntrance.StartCooldown();

            _isTeleporting = false;

            Debug.Log($"[Tunnel] Teleported player to {targetPos}");
        }

        public void StartCooldown()
        {
            StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            _isOnCooldown = true;
            yield return new WaitForSeconds(cooldownDuration);
            _isOnCooldown = false;
        }

        /// <summary>
        /// Creates a tunnel between two positions.
        /// Returns true if successful.
        /// </summary>
        public static bool CreateTunnel(Vector2 entranceA, Vector2 entranceB, Sprite tunnelSprite = null)
        {
            // Create shared sprite
            Sprite sprite = tunnelSprite != null ? tunnelSprite : CreateDefaultSprite();

            // Create entrance A
            GameObject objA = new GameObject("TunnelEntrance_A");
            objA.transform.position = new Vector3(entranceA.x, entranceA.y, 0);
            objA.transform.localScale = Vector3.one * 0.8f;

            // Add collider first
            CircleCollider2D colA = objA.AddComponent<CircleCollider2D>();
            colA.isTrigger = true;
            colA.radius = 0.4f;

            // Add visuals
            SpriteRenderer srA = objA.AddComponent<SpriteRenderer>();
            srA.sprite = sprite;
            srA.color = new Color(0.4f, 0.2f, 0.1f); // Brown color
            srA.sortingOrder = 5;

            // Add visibility awareness
            objA.AddComponent<Labyrinth.Visibility.VisibilityAwareEntity>();

            // Add TunnelEntrance component last
            TunnelEntrance compA = objA.AddComponent<TunnelEntrance>();

            // Create entrance B
            GameObject objB = new GameObject("TunnelEntrance_B");
            objB.transform.position = new Vector3(entranceB.x, entranceB.y, 0);
            objB.transform.localScale = Vector3.one * 0.8f;

            // Add collider first
            CircleCollider2D colB = objB.AddComponent<CircleCollider2D>();
            colB.isTrigger = true;
            colB.radius = 0.4f;

            // Add visuals
            SpriteRenderer srB = objB.AddComponent<SpriteRenderer>();
            srB.sprite = sprite;
            srB.color = new Color(0.4f, 0.2f, 0.1f); // Brown color
            srB.sortingOrder = 5;

            // Add visibility awareness
            objB.AddComponent<Labyrinth.Visibility.VisibilityAwareEntity>();

            // Add TunnelEntrance component last
            TunnelEntrance compB = objB.AddComponent<TunnelEntrance>();

            // Link them together
            compA.SetPairedEntrance(compB);
            compB.SetPairedEntrance(compA);

            Debug.Log($"[Tunnel] Created tunnel between {entranceA} and {entranceB}");
            return true;
        }

        private static Sprite CreateDefaultSprite()
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
                    if (dist < radius * 0.8f)
                    {
                        // Inner dark circle (tunnel hole)
                        texture.SetPixel(x, y, new Color(0.1f, 0.05f, 0f));
                    }
                    else if (dist < radius)
                    {
                        // Outer ring
                        texture.SetPixel(x, y, new Color(0.5f, 0.3f, 0.15f));
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

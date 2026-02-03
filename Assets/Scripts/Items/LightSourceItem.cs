using UnityEngine;
using Labyrinth.Visibility;

namespace Labyrinth.Items
{
    public class LightSourceItem : BaseItem
    {
        [SerializeField] private float lightRadius = 4f;
        [SerializeField] private float searchRadius = 10f;
        [SerializeField] private Color lightColor = new Color(1f, 0.9f, 0.6f); // Warm yellow

        public override ItemType ItemType => ItemType.Light;

        // Light sources are stored and used from inventory
        public override bool IsStorable => true;

        protected override void Start()
        {
            base.Start();
            Debug.Log($"LightSourceItem: Start() called, IsStorable={IsStorable}");
        }

        public override InventoryItem CreateInventoryItem()
        {
            return new InventoryItem(
                ItemType.Light,
                itemIcon,
                lightRadius,  // EffectValue stores the light radius
                0f            // Duration not used
            );
        }

        protected override void OnCollected(GameObject player)
        {
            Debug.Log("LightSourceItem: OnCollected called");
            Vector2 playerPos = player.transform.position;
            var wallData = FindClosestWall(playerPos);

            if (wallData.HasValue)
            {
                // Offset the light position slightly away from the wall (toward player)
                Vector2 wallPos = wallData.Value.point;
                Vector2 offsetDir = (playerPos - wallPos).normalized;
                Vector2 lightPos = wallPos + offsetDir * 0.6f; // Offset 0.6 units from wall

                Debug.Log($"LightSourceItem: Found wall at {wallPos}, spawning light at {lightPos}");
                SpawnLightSource(lightPos);
            }
            else
            {
                Debug.LogWarning("LightSourceItem: No wall found nearby!");
            }
        }

        private (Vector2 point, Vector2 normal)? FindClosestWall(Vector2 fromPosition)
        {
            if (FogOfWarManager.Instance == null)
            {
                Debug.LogError("LightSourceItem: FogOfWarManager.Instance is null!");
                return null;
            }

            LayerMask wallLayer = FogOfWarManager.Instance.WallLayer;
            (Vector2 point, Vector2 normal)? closestData = null;
            float closestDistance = float.MaxValue;

            // Cast rays in all directions to find the closest wall
            int rayCount = 36; // Every 10 degrees
            for (int i = 0; i < rayCount; i++)
            {
                float angle = (i * 360f / rayCount) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                RaycastHit2D hit = Physics2D.Raycast(fromPosition, direction, searchRadius, wallLayer);
                if (hit.collider != null && hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestData = (hit.point, hit.normal);
                }
            }

            return closestData;
        }

        private void SpawnLightSource(Vector2 position)
        {
            // Create the light source GameObject
            GameObject lightObj = new GameObject("PlacedLight");
            lightObj.transform.position = new Vector3(position.x, position.y, 0);

            // Add the PlacedLightSource component
            PlacedLightSource lightSource = lightObj.AddComponent<PlacedLightSource>();
            lightSource.Initialize(lightRadius);

            // Add a visual indicator (small glowing sprite)
            SpriteRenderer sr = lightObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateLightSprite();
            sr.color = lightColor;
            sr.sortingOrder = 10;
            lightObj.transform.localScale = Vector3.one * 0.5f;

            // Add visibility awareness so it shows/hides with fog
            lightObj.AddComponent<VisibilityAwareEntity>();
        }

        private Sprite CreateLightSprite()
        {
            // Create a simple circular sprite for the light indicator
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
                        float alpha = 1f - (dist / radius);
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
    }
}

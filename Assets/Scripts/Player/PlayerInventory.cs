using System;
using System.Collections.Generic;
using UnityEngine;
using Labyrinth.Items;
using Labyrinth.Visibility;
using Labyrinth.Leveling;

namespace Labyrinth.Player
{
    /// <summary>
    /// Manages the player's item inventory.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public const int BaseMaxSlots = 3;

        private List<InventoryItem> _items = new List<InventoryItem>();

        public event Action OnInventoryChanged;

        public int MaxSlots => BaseMaxSlots + (PlayerLevelSystem.Instance?.ExtraInventorySlots ?? 0);
        public IReadOnlyList<InventoryItem> Items => _items;
        public int ItemCount => _items.Count;
        public bool IsFull => _items.Count >= MaxSlots;

        /// <summary>
        /// Attempts to add an item to the inventory.
        /// Returns true if successful, false if inventory is full.
        /// </summary>
        public bool TryAddItem(InventoryItem item)
        {
            if (IsFull)
                return false;

            _items.Add(item);
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Uses the first item in the inventory.
        /// </summary>
        public void UseFirstItem()
        {
            if (_items.Count == 0)
                return;

            var item = _items[0];
            ApplyItemEffect(item);

            // Check if item should be removed (single use or no uses remaining)
            if (item.ConsumeUse())
            {
                _items.RemoveAt(0);
            }

            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Uses the item at the specified index.
        /// </summary>
        public void UseItemAt(int index)
        {
            if (index < 0 || index >= _items.Count)
                return;

            var item = _items[index];
            ApplyItemEffect(item);

            // Check if item should be removed (single use or no uses remaining)
            if (item.ConsumeUse())
            {
                _items.RemoveAt(index);
            }

            OnInventoryChanged?.Invoke();
        }

        private void ApplyItemEffect(InventoryItem item)
        {
            switch (item.Type)
            {
                case ItemType.Speed:
                    var controller = GetComponent<PlayerController>();
                    if (controller != null)
                    {
                        controller.ApplySpeedBoost(item.EffectValue, item.Duration);
                    }
                    break;

                case ItemType.Light:
                    SpawnPlacedLight(item);
                    break;

                case ItemType.Heal:
                    var health = GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        health.Heal((int)item.EffectValue);
                    }
                    break;

                case ItemType.Explosive:
                    // Spawn bomb at player's position
                    var bombObj = new GameObject("Bomb");
                    bombObj.transform.position = transform.position;
                    var explosion = bombObj.AddComponent<Items.Explosion>();
                    explosion.Initialize((int)item.EffectValue, (int)item.Duration);
                    break;

                case ItemType.Pebbles:
                    // Drop a pebble at player's position
                    Labyrinth.Items.PlacedPebble.SpawnAt(transform.position);
                    break;

                case ItemType.Invisibility:
                    if (InvisibilityManager.Instance != null)
                    {
                        InvisibilityManager.Instance.ActivateInvisibility(item.Duration);
                    }
                    break;

                case ItemType.Wisp:
                    SpawnWispOrb();
                    break;

                case ItemType.Caltrops:
                    // Scatter caltrops at player's position
                    // EffectValue = speed multiplier, Duration = slow duration
                    // Default player damage is 1
                    Labyrinth.Items.PlacedCaltrops.SpawnAt(transform.position, item.EffectValue, item.Duration, 1);
                    break;

                case ItemType.EchoStone:
                    // Create echo pulse to reveal enemies
                    // EffectValue = reveal radius, Duration = reveal duration
                    Labyrinth.Items.EchoPulse.CreateAt(transform.position, item.EffectValue, item.Duration);
                    break;
            }
        }

        private void SpawnWispOrb()
        {
            // Find the MazeRenderer to get the grid and key position
            var mazeRenderer = FindObjectOfType<Labyrinth.Maze.MazeRenderer>();
            if (mazeRenderer == null)
            {
                Debug.LogWarning("PlayerInventory: Could not find MazeRenderer for wisp!");
                return;
            }

            var grid = mazeRenderer.GetGrid();
            if (grid == null)
            {
                Debug.LogWarning("PlayerInventory: MazeGrid is null!");
                return;
            }

            // The key is at the exit position
            Vector2 keyPosition = mazeRenderer.ExitPosition;

            // Spawn the wisp orb
            Labyrinth.Items.WispOrb.SpawnAt(transform.position, grid, keyPosition);
        }

        private void SpawnPlacedLight(InventoryItem item)
        {
            Vector2 playerPos = transform.position;
            var wallData = FindClosestWall(playerPos);

            if (wallData.HasValue)
            {
                // Offset the light position slightly away from the wall (toward player)
                Vector2 wallPos = wallData.Value.point;
                Vector2 offsetDir = (playerPos - wallPos).normalized;
                Vector2 lightPos = wallPos + offsetDir * 0.6f;

                // Create the light source GameObject
                GameObject lightObj = new GameObject("PlacedLight");
                lightObj.transform.position = new Vector3(lightPos.x, lightPos.y, 0);

                // Add the PlacedLightSource component
                PlacedLightSource lightSource = lightObj.AddComponent<PlacedLightSource>();
                lightSource.Initialize(item.EffectValue > 0 ? item.EffectValue : 4f);

                // Add a visual indicator
                SpriteRenderer sr = lightObj.AddComponent<SpriteRenderer>();
                if (item.ExtraSprite != null)
                {
                    sr.sprite = item.ExtraSprite;
                    sr.color = Color.white;
                }
                else
                {
                    sr.sprite = CreateLightSprite();
                    sr.color = new Color(1f, 0.9f, 0.6f); // Warm yellow
                }
                sr.sortingOrder = 10;
                lightObj.transform.localScale = Vector3.one * 0.5f;

                // Add visibility awareness so it shows/hides with fog
                lightObj.AddComponent<VisibilityAwareEntity>();

                Debug.Log($"PlayerInventory: Spawned PlacedLight at {lightPos}");
            }
            else
            {
                Debug.LogWarning("PlayerInventory: No wall found nearby to place light!");
            }
        }

        private (Vector2 point, Vector2 normal)? FindClosestWall(Vector2 fromPosition)
        {
            if (FogOfWarManager.Instance == null)
            {
                Debug.LogError("PlayerInventory: FogOfWarManager.Instance is null!");
                return null;
            }

            LayerMask wallLayer = FogOfWarManager.Instance.WallLayer;
            (Vector2 point, Vector2 normal)? closestData = null;
            float closestDistance = float.MaxValue;
            float searchRadius = 10f;

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

        private Sprite CreateLightSprite()
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

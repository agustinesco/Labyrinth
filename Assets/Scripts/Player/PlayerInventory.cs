using System;
using System.Collections.Generic;
using UnityEngine;
using Labyrinth.Items;
using Labyrinth.Visibility;
using Labyrinth.Leveling;
using Labyrinth.UI;

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
            bool success = ApplyItemEffect(item);

            if (!success)
                return;

            // Check if item should be removed (single use or no uses remaining)
            if (item.ConsumeUse())
            {
                _items.RemoveAt(0);
            }

            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Swaps the item at the given index with the first item.
        /// </summary>
        public void SwapWithFirst(int index)
        {
            if (index <= 0 || index >= _items.Count)
                return;

            (_items[0], _items[index]) = (_items[index], _items[0]);
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
            bool success = ApplyItemEffect(item);

            if (!success)
                return;

            // Check if item should be removed (single use or no uses remaining)
            if (item.ConsumeUse())
            {
                _items.RemoveAt(index);
            }

            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Applies the item effect. Returns false if the item failed to activate
        /// and should NOT be consumed.
        /// </summary>
        private bool ApplyItemEffect(InventoryItem item)
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
                    // Increases max health by 1 and restores 1 heart
                    var health = GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        health.IncreaseMaxHealth((int)item.EffectValue);
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

                case ItemType.Glider:
                    // Allow player to pass through walls briefly
                    ActivateGlider(item.Duration);
                    break;

                case ItemType.Tunnel:
                    // Create a tunnel through a 1-tile thick wall
                    return ActivateTunnel();

                case ItemType.SilkWorm:
                    // Create a silk string trap between walls
                    return ActivateSilkWorm(item.EffectValue, item.Duration);

                case ItemType.EagleEye:
                    // Temporarily increase vision range
                    if (FogOfWarManager.Instance != null)
                    {
                        FogOfWarManager.Instance.ApplyVisibilityBoost(item.EffectValue, item.Duration);
                    }
                    break;
            }

            return true;
        }

        private void ActivateGlider(float duration)
        {
            // Ensure GliderEffect component exists
            var gliderEffect = GetComponent<Items.GliderEffect>();
            if (gliderEffect == null)
            {
                gliderEffect = gameObject.AddComponent<Items.GliderEffect>();
            }

            // Get maze grid reference
            var mazeRenderer = FindObjectOfType<Labyrinth.Maze.MazeRenderer>();
            Labyrinth.Maze.MazeGrid grid = mazeRenderer != null ? mazeRenderer.GetGrid() : null;

            if (grid != null)
            {
                gliderEffect.Activate(duration, grid);
            }
            else
            {
                Debug.LogWarning("PlayerInventory: Could not find MazeGrid for glider effect!");
            }
        }

        private bool ActivateTunnel()
        {
            var mazeRenderer = FindObjectOfType<Labyrinth.Maze.MazeRenderer>();
            if (mazeRenderer == null)
            {
                ShowItemMessage("No valid target found");
                return false;
            }

            var grid = mazeRenderer.GetGrid();
            if (grid == null)
            {
                ShowItemMessage("No valid target found");
                return false;
            }

            Vector2 playerPos = transform.position;
            float maxRange = 1.5f;

            // Check all 4 cardinal directions for a valid tunnel location
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            Vector2[] dirVectors = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

            float closestDistance = float.MaxValue;
            Vector2Int bestWallCell = Vector2Int.zero;
            Vector2Int bestDirection = Vector2Int.zero;
            bool foundValidWall = false;

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2 dir = dirVectors[i];
                Vector2Int dirInt = directions[i];

                // Raycast to find wall
                LayerMask wallLayer = FogOfWarManager.Instance != null ? FogOfWarManager.Instance.WallLayer : (LayerMask)(1 << 8);
                RaycastHit2D hit = Physics2D.Raycast(playerPos, dir, maxRange, wallLayer);

                if (hit.collider != null)
                {
                    float distance = hit.distance;

                    // Get the wall cell coordinates
                    Vector2 wallWorldPos = hit.point + dir * 0.1f; // Slight offset into the wall
                    int wallCellX = Mathf.FloorToInt(wallWorldPos.x);
                    int wallCellY = Mathf.FloorToInt(wallWorldPos.y);

                    // Check bounds
                    if (wallCellX < 0 || wallCellX >= grid.Width || wallCellY < 0 || wallCellY >= grid.Height)
                        continue;

                    // Verify it's actually a wall
                    if (!grid.GetCell(wallCellX, wallCellY).IsWall)
                        continue;

                    // Check if the cell on the other side is a floor (1-tile thick wall)
                    int otherSideX = wallCellX + dirInt.x;
                    int otherSideY = wallCellY + dirInt.y;

                    if (otherSideX < 0 || otherSideX >= grid.Width || otherSideY < 0 || otherSideY >= grid.Height)
                        continue;

                    if (!grid.GetCell(otherSideX, otherSideY).IsWall)
                    {
                        // Found a valid 1-tile thick wall!
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            bestWallCell = new Vector2Int(wallCellX, wallCellY);
                            bestDirection = dirInt;
                            foundValidWall = true;
                        }
                    }
                }
            }

            if (!foundValidWall)
            {
                // Check if there's any wall nearby at all
                bool anyWallNearby = false;
                for (int i = 0; i < dirVectors.Length; i++)
                {
                    LayerMask wallLayer = FogOfWarManager.Instance != null ? FogOfWarManager.Instance.WallLayer : (LayerMask)(1 << 8);
                    RaycastHit2D hit = Physics2D.Raycast(playerPos, dirVectors[i], maxRange, wallLayer);
                    if (hit.collider != null)
                    {
                        anyWallNearby = true;
                        break;
                    }
                }

                if (anyWallNearby)
                {
                    ShowItemMessage("Wall is too thick to tunnel through!");
                }
                else
                {
                    ShowItemMessage("No wall nearby!");
                }
                return false;
            }

            // Calculate entrance positions (center of floor cells on each side)
            // Entrance A: player's side (cell before the wall)
            int entranceAX = bestWallCell.x - bestDirection.x;
            int entranceAY = bestWallCell.y - bestDirection.y;
            Vector2 entranceAPos = new Vector2(entranceAX + 0.5f, entranceAY + 0.5f);

            // Entrance B: other side of the wall
            int entranceBX = bestWallCell.x + bestDirection.x;
            int entranceBY = bestWallCell.y + bestDirection.y;
            Vector2 entranceBPos = new Vector2(entranceBX + 0.5f, entranceBY + 0.5f);

            // Create the tunnel
            Labyrinth.Items.TunnelEntrance.CreateTunnel(entranceAPos, entranceBPos);
            return true;
        }

        private void ShowItemMessage(string message)
        {
            Debug.LogWarning($"[Item] {message}");
            if (ItemMessageUI.Instance != null)
            {
                ItemMessageUI.Instance.ShowMessage(message);
            }
        }

        private bool ActivateSilkWorm(float snareDuration, float lifetime)
        {
            Vector2 playerPos = transform.position;
            LayerMask wallLayer = FogOfWarManager.Instance != null ? FogOfWarManager.Instance.WallLayer : (LayerMask)(1 << 8);
            float maxDistance = 5f; // Max reach for the string

            // Find closest wall in cardinal directions
            Vector2[] cardinalDirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            float closestDistance = float.MaxValue;
            Vector2 closestWallPoint = Vector2.zero;
            Vector2 closestDirection = Vector2.zero;
            Vector2 closestWallNormal = Vector2.zero;

            foreach (var dir in cardinalDirs)
            {
                RaycastHit2D hit = Physics2D.Raycast(playerPos, dir, 50f, wallLayer);
                if (hit.collider != null && hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestWallPoint = hit.point;
                    closestDirection = dir;
                    closestWallNormal = hit.normal;
                }
            }

            if (closestDistance == float.MaxValue)
            {
                ShowItemMessage("No wall found nearby!");
                return false;
            }

            // Find a second wall that faces the OPPOSITE direction (toward the first wall)
            // Walls facing each other have opposite normals (dot product close to -1)
            Vector2 secondWallPoint = Vector2.zero;
            Vector2 secondDirection = Vector2.zero;
            bool foundValidSecondWall = false;

            // First try the opposite cardinal direction (most common case: corridor walls)
            Vector2 oppositeDir = -closestDirection;
            RaycastHit2D oppositeHit = Physics2D.Raycast(playerPos, oppositeDir, maxDistance, wallLayer);

            if (oppositeHit.collider != null)
            {
                // Check if walls face each other (normals are opposite)
                float dot = Vector2.Dot(closestWallNormal, oppositeHit.normal);
                if (dot < -0.7f) // Normals pointing toward each other
                {
                    secondWallPoint = oppositeHit.point;
                    secondDirection = oppositeDir;
                    foundValidSecondWall = true;
                }
            }

            // If opposite wall doesn't face toward first wall, try other cardinal directions
            if (!foundValidSecondWall)
            {
                foreach (var dir in cardinalDirs)
                {
                    if (dir == closestDirection || dir == oppositeDir) continue;

                    RaycastHit2D hit = Physics2D.Raycast(playerPos, dir, maxDistance, wallLayer);
                    if (hit.collider != null)
                    {
                        float dot = Vector2.Dot(closestWallNormal, hit.normal);
                        if (dot < -0.7f) // Normals pointing toward each other
                        {
                            secondWallPoint = hit.point;
                            secondDirection = dir;
                            foundValidSecondWall = true;
                            break;
                        }
                    }
                }
            }

            // If still no valid wall, try diagonal directions
            if (!foundValidSecondWall)
            {
                Vector2[] diagonalDirs = {
                    new Vector2(1, 1).normalized,
                    new Vector2(-1, 1).normalized,
                    new Vector2(1, -1).normalized,
                    new Vector2(-1, -1).normalized
                };

                float nearestDiagDistance = float.MaxValue;
                foreach (var dir in diagonalDirs)
                {
                    RaycastHit2D hit = Physics2D.Raycast(playerPos, dir, maxDistance, wallLayer);
                    if (hit.collider != null && hit.distance < nearestDiagDistance)
                    {
                        float dot = Vector2.Dot(closestWallNormal, hit.normal);
                        if (dot < -0.7f) // Normals pointing toward each other
                        {
                            nearestDiagDistance = hit.distance;
                            secondWallPoint = hit.point;
                            secondDirection = dir;
                            foundValidSecondWall = true;
                        }
                    }
                }

                if (foundValidSecondWall)
                {
                    Debug.Log("[SilkWorm] Using diagonal wall facing opposite direction");
                }
            }

            if (!foundValidSecondWall)
            {
                ShowItemMessage("Can't find two facing walls!");
                return false;
            }

            // Offset points slightly away from walls so the string is visible
            Vector2 pointA = closestWallPoint - closestDirection * 0.1f;
            Vector2 pointB = secondWallPoint - secondDirection * 0.1f;

            // Create the silk string
            Labyrinth.Items.SilkString.CreateBetween(pointA, pointB, snareDuration, 3, lifetime);
            return true;
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

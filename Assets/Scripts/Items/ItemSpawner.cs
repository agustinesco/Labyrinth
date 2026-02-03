using UnityEngine;
using System.Collections.Generic;
using Labyrinth.Maze;
using Labyrinth.Visibility;

namespace Labyrinth.Items
{
    public class ItemSpawner : MonoBehaviour
    {
        [Header("Item Prefabs")]
        [SerializeField] private GameObject keyItemPrefab;
        [SerializeField] private GameObject speedItemPrefab;
        [SerializeField] private GameObject lightSourcePrefab;
        [SerializeField] private GameObject healItemPrefab;
        [SerializeField] private GameObject explosiveItemPrefab;
        [SerializeField] private GameObject xpItemPrefab;
        [SerializeField] private GameObject pebblesItemPrefab;

        [Header("Item Sprites (for dynamic creation)")]
        [SerializeField] private Sprite speedItemSprite;
        [SerializeField] private Sprite lightItemSprite;
        [SerializeField] private Sprite healItemSprite;
        [SerializeField] private Sprite explosiveItemSprite;
        [SerializeField] private Sprite keyItemSprite;
        [SerializeField] private Sprite xpItemSprite;
        [SerializeField] private Sprite pebblesItemSprite;

        [Header("Spawn Counts")]
        [SerializeField] private int speedItemCount = 3;
        [SerializeField] private int lightSourceCount = 3;
        [SerializeField] private int healItemCount = 2;
        [SerializeField] private int explosiveItemCount = 2;
        [SerializeField] private int xpItemCount = 15;
        [SerializeField] private int pebblesItemCount = 2;

        public void SpawnItems(MazeGrid grid, Vector2 startPos, Vector2 exitPos)
        {
            // Spawn key at exit
            Instantiate(keyItemPrefab, new Vector3(exitPos.x, exitPos.y, 0), Quaternion.identity);

            // Find valid spawn positions (floor tiles, not start/exit)
            var validPositions = new List<Vector2>();
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var cell = grid.GetCell(x, y);
                    if (!cell.IsWall && !cell.IsStart && !cell.IsExit)
                    {
                        validPositions.Add(new Vector2(x, y));
                    }
                }
            }

            // Shuffle positions
            for (int i = validPositions.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = validPositions[i];
                validPositions[i] = validPositions[j];
                validPositions[j] = temp;
            }

            // Spawn speed items
            int spawned = 0;
            for (int i = 0; i < validPositions.Count && spawned < speedItemCount; i++)
            {
                var pos = validPositions[i];
                Instantiate(speedItemPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                spawned++;
            }

            // Spawn light sources
            int lightSpawned = 0;
            for (int i = spawned; i < validPositions.Count && lightSpawned < lightSourceCount; i++)
            {
                var pos = validPositions[i];
                if (lightSourcePrefab != null)
                {
                    Debug.Log($"ItemSpawner: Spawning light from prefab at {pos}");
                    Instantiate(lightSourcePrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                }
                else
                {
                    // Create dynamically if no prefab
                    Debug.Log($"ItemSpawner: Creating light dynamically at {pos}");
                    var lightObj = new GameObject("LightSourceItem");
                    lightObj.transform.position = new Vector3(pos.x, pos.y, 0);
                    var sr = lightObj.AddComponent<SpriteRenderer>();
                    sr.sprite = lightItemSprite != null ? lightItemSprite : CreateSquareSprite();
                    sr.color = Color.white;
                    sr.sortingOrder = 5;
                    lightObj.transform.localScale = Vector3.one * 0.6f;
                    var collider = lightObj.AddComponent<BoxCollider2D>();
                    collider.isTrigger = true;
                    collider.size = Vector2.one;
                    lightObj.AddComponent<LightSourceItem>();
                    lightObj.AddComponent<Visibility.VisibilityAwareEntity>();
                }
                lightSpawned++;
            }
            spawned += lightSpawned;

            // Spawn heal items
            int healSpawned = 0;
            for (int i = spawned; i < validPositions.Count && healSpawned < healItemCount; i++)
            {
                var pos = validPositions[i];
                if (healItemPrefab != null)
                {
                    Instantiate(healItemPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                }
                else
                {
                    // Create dynamically if no prefab
                    var healObj = new GameObject("HealItem");
                    healObj.transform.position = new Vector3(pos.x, pos.y, 0);
                    var sr = healObj.AddComponent<SpriteRenderer>();
                    sr.sprite = healItemSprite != null ? healItemSprite : CreateSquareSprite();
                    sr.color = Color.white;
                    sr.sortingOrder = 5;
                    healObj.transform.localScale = Vector3.one * 0.6f;
                    var healCollider = healObj.AddComponent<BoxCollider2D>();
                    healCollider.isTrigger = true;
                    healCollider.size = Vector2.one;
                    healObj.AddComponent<HealItem>();
                    healObj.AddComponent<Visibility.VisibilityAwareEntity>();
                }
                healSpawned++;
            }
            spawned += healSpawned;

            // Spawn explosive items
            int explosiveSpawned = 0;
            for (int i = spawned; i < validPositions.Count && explosiveSpawned < explosiveItemCount; i++)
            {
                var pos = validPositions[i];
                if (explosiveItemPrefab != null)
                {
                    Instantiate(explosiveItemPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                }
                else
                {
                    // Create dynamically if no prefab
                    var explosiveObj = new GameObject("ExplosiveItem");
                    explosiveObj.transform.position = new Vector3(pos.x, pos.y, 0);
                    var sr = explosiveObj.AddComponent<SpriteRenderer>();
                    sr.sprite = explosiveItemSprite != null ? explosiveItemSprite : CreateSquareSprite();
                    sr.color = Color.white;
                    sr.sortingOrder = 5;
                    explosiveObj.transform.localScale = Vector3.one * 0.6f;
                    var explosiveCollider = explosiveObj.AddComponent<BoxCollider2D>();
                    explosiveCollider.isTrigger = true;
                    explosiveCollider.size = Vector2.one;
                    explosiveObj.AddComponent<ExplosiveItem>();
                    explosiveObj.AddComponent<Visibility.VisibilityAwareEntity>();
                }
                explosiveSpawned++;
            }
            spawned += explosiveSpawned;

            // Spawn XP items
            int xpSpawned = 0;
            for (int i = spawned; i < validPositions.Count && xpSpawned < xpItemCount; i++)
            {
                var pos = validPositions[i];
                if (xpItemPrefab != null)
                {
                    Instantiate(xpItemPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                }
                else
                {
                    // Create dynamically if no prefab
                    var xpObj = new GameObject("XPItem");
                    xpObj.transform.position = new Vector3(pos.x, pos.y, 0);
                    var sr = xpObj.AddComponent<SpriteRenderer>();
                    sr.sprite = xpItemSprite != null ? xpItemSprite : CreateSquareSprite();
                    sr.color = Color.white;
                    sr.sortingOrder = 5;
                    xpObj.transform.localScale = Vector3.one * 0.4f;
                    var xpCollider = xpObj.AddComponent<BoxCollider2D>();
                    xpCollider.isTrigger = true;
                    xpCollider.size = Vector2.one;
                    xpObj.AddComponent<XPItem>();
                    xpObj.AddComponent<Visibility.VisibilityAwareEntity>();
                }
                xpSpawned++;
            }
            spawned += xpSpawned;

            // Spawn pebbles items
            int pebblesSpawned = 0;
            for (int i = spawned; i < validPositions.Count && pebblesSpawned < pebblesItemCount; i++)
            {
                var pos = validPositions[i];
                if (pebblesItemPrefab != null)
                {
                    Instantiate(pebblesItemPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                }
                else
                {
                    // Create dynamically if no prefab
                    var pebblesObj = new GameObject("PebblesItem");
                    pebblesObj.transform.position = new Vector3(pos.x, pos.y, 0);
                    var sr = pebblesObj.AddComponent<SpriteRenderer>();
                    sr.sprite = pebblesItemSprite != null ? pebblesItemSprite : CreateSquareSprite();
                    sr.color = Color.white;
                    sr.sortingOrder = 5;
                    pebblesObj.transform.localScale = Vector3.one * 0.6f;
                    var pebblesCollider = pebblesObj.AddComponent<BoxCollider2D>();
                    pebblesCollider.isTrigger = true;
                    pebblesCollider.size = Vector2.one;
                    pebblesObj.AddComponent<PebblesItem>();
                    pebblesObj.AddComponent<Visibility.VisibilityAwareEntity>();
                }
                pebblesSpawned++;
            }
        }

        private Sprite CreateSquareSprite()
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }
    }
}
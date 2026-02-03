using UnityEngine;
using Labyrinth.Visibility;

namespace Labyrinth.Items
{
    /// <summary>
    /// A pebble dropped on the ground by the player.
    /// Serves as a visual marker without emitting light.
    /// </summary>
    public class PlacedPebble : MonoBehaviour
    {
        private static readonly Color PebbleColor = new Color(0.6f, 0.6f, 0.6f); // Grey

        public static void SpawnAt(Vector2 position)
        {
            GameObject pebbleObj = new GameObject("PlacedPebble");
            pebbleObj.transform.position = new Vector3(position.x, position.y, 0);

            // Add the PlacedPebble component
            pebbleObj.AddComponent<PlacedPebble>();

            // Add visual (small grey circle)
            SpriteRenderer sr = pebbleObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePebbleSprite();
            sr.color = PebbleColor;
            sr.sortingOrder = 3; // Above floor, below items
            pebbleObj.transform.localScale = Vector3.one * 0.3f;

            // Add visibility awareness so it shows/hides with fog
            pebbleObj.AddComponent<VisibilityAwareEntity>();

            Debug.Log($"PlacedPebble: Spawned at {position}");
        }

        private static Sprite CreatePebbleSprite()
        {
            int size = 16;
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
                        // Solid inner circle
                        texture.SetPixel(x, y, Color.white);
                    }
                    else if (dist < radius)
                    {
                        // Slight fade at edge
                        float alpha = 1f - ((dist - radius * 0.8f) / (radius * 0.2f));
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

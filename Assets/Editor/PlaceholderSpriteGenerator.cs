using UnityEngine;
using UnityEditor;
using System.IO;

namespace Labyrinth.Editor
{
    public static class PlaceholderSpriteGenerator
    {
        [MenuItem("Labyrinth/Generate Placeholder Sprites")]
        public static void GeneratePlaceholderSprites()
        {
            GenerateCaltropsSprite();
            GenerateEchoStoneSprite();
            AssetDatabase.Refresh();
            Debug.Log("Placeholder sprites generated successfully!");
        }

        private static void GenerateCaltropsSprite()
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);

            // Clear to transparent
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;
            texture.SetPixels(pixels);

            // Dark metallic grey color
            Color spikeColor = new Color(0.35f, 0.35f, 0.4f, 1f);
            Color highlightColor = new Color(0.5f, 0.5f, 0.55f, 1f);

            // Draw 4 triangular spikes
            DrawSpike(texture, center, 0, size, spikeColor, highlightColor);    // Up
            DrawSpike(texture, center, 90, size, spikeColor, highlightColor);   // Right
            DrawSpike(texture, center, 180, size, spikeColor, highlightColor);  // Down
            DrawSpike(texture, center, 270, size, spikeColor, highlightColor);  // Left

            // Draw central hub
            float hubRadius = size * 0.18f;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist < hubRadius)
                    {
                        texture.SetPixel(x, y, highlightColor);
                    }
                }
            }

            texture.Apply();
            SaveTextureAsPNG(texture, "Assets/Sprites/Items/CaltropsItem.png");
            Object.DestroyImmediate(texture);
        }

        private static void DrawSpike(Texture2D texture, Vector2 center, float angleDegrees, int size, Color mainColor, Color highlightColor)
        {
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));

            float spikeLength = size * 0.42f;
            float spikeWidth = size * 0.18f;

            for (int i = 0; i < (int)spikeLength; i++)
            {
                float progress = i / spikeLength;
                float currentWidth = spikeWidth * (1f - progress);

                Vector2 spikePoint = center + direction * i;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);

                for (int w = (int)(-currentWidth); w <= (int)currentWidth; w++)
                {
                    Vector2 pixelPos = spikePoint + perpendicular * w;
                    int px = Mathf.RoundToInt(pixelPos.x);
                    int py = Mathf.RoundToInt(pixelPos.y);

                    if (px >= 0 && px < size && py >= 0 && py < size)
                    {
                        // Add slight highlight on one side
                        Color c = (w < 0) ? highlightColor : mainColor;
                        texture.SetPixel(px, py, c);
                    }
                }
            }
        }

        private static void GenerateEchoStoneSprite()
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);

            // Clear to transparent
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;
            texture.SetPixels(pixels);

            // Blue/cyan colors for the echo stone
            Color stoneColor = new Color(0.3f, 0.5f, 0.7f, 1f);
            Color glowColor = new Color(0.5f, 0.8f, 1f, 1f);
            Color coreColor = new Color(0.7f, 0.9f, 1f, 1f);

            float radius = size * 0.4f;

            // Draw stone body (oval shape)
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = (x - center.x) / radius;
                    float dy = (y - center.y) / (radius * 0.8f); // Slightly oval
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < 1f)
                    {
                        // Gradient from edge to center
                        float t = 1f - dist;
                        Color c;
                        if (t > 0.7f)
                            c = Color.Lerp(glowColor, coreColor, (t - 0.7f) / 0.3f);
                        else if (t > 0.3f)
                            c = Color.Lerp(stoneColor, glowColor, (t - 0.3f) / 0.4f);
                        else
                            c = stoneColor;

                        texture.SetPixel(x, y, c);
                    }
                }
            }

            // Add wave rings emanating from stone
            for (int ring = 1; ring <= 2; ring++)
            {
                float ringRadius = radius + ring * 3f;
                float ringThickness = 1.5f;

                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (Mathf.Abs(dist - ringRadius) < ringThickness)
                        {
                            float alpha = 0.4f - ring * 0.15f;
                            Color existing = texture.GetPixel(x, y);
                            if (existing.a < 0.1f) // Only draw on transparent pixels
                            {
                                texture.SetPixel(x, y, new Color(0.5f, 0.8f, 1f, alpha));
                            }
                        }
                    }
                }
            }

            texture.Apply();
            SaveTextureAsPNG(texture, "Assets/Sprites/Items/EchoStoneItem.png");
            Object.DestroyImmediate(texture);
        }

        private static void SaveTextureAsPNG(Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            string fullPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), path);

            // Ensure directory exists
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(fullPath, bytes);
            Debug.Log($"Saved sprite to: {path}");
        }
    }
}

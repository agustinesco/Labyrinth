using UnityEngine;
using UnityEditor;
using System.IO;

namespace Labyrinth.Editor
{
    public class ItemSpriteGenerator : EditorWindow
    {
        [MenuItem("Labyrinth/Generate Item Sprites")]
        public static void GenerateSprites()
        {
            string folderPath = "Assets/Sprites/Items";

            // Create folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
                {
                    AssetDatabase.CreateFolder("Assets", "Sprites");
                }
                AssetDatabase.CreateFolder("Assets/Sprites", "Items");
            }

            // Generate each item sprite
            GenerateSpeedSprite(folderPath);
            GenerateLightSprite(folderPath);
            GenerateHealSprite(folderPath);
            GenerateExplosiveSprite(folderPath);
            GenerateKeySprite(folderPath);
            GenerateXPSprite(folderPath);
            GeneratePebblesSprite(folderPath);

            AssetDatabase.Refresh();
            Debug.Log("Item sprites generated successfully in " + folderPath);
        }

        private static void SaveSprite(Texture2D texture, string folderPath, string name)
        {
            byte[] bytes = texture.EncodeToPNG();
            string path = Path.Combine(folderPath, name + ".png");
            File.WriteAllBytes(path, bytes);

            // Import and configure as sprite
            AssetDatabase.Refresh();
            string assetPath = folderPath + "/" + name + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void GenerateSpeedSprite(string folderPath)
        {
            // Lightning bolt - 16x16
            Texture2D tex = new Texture2D(16, 16);
            Color transparent = new Color(0, 0, 0, 0);
            Color yellow = new Color(1f, 0.9f, 0.2f);
            Color orange = new Color(1f, 0.6f, 0.1f);

            // Clear to transparent
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                    tex.SetPixel(x, y, transparent);

            // Draw lightning bolt
            int[] boltX = { 9, 10, 8, 9, 7, 8, 6, 7, 5, 6, 7, 8, 9, 10, 5, 6, 4, 5, 3, 4, 5, 6 };
            int[] boltY = { 14, 14, 13, 13, 12, 12, 11, 11, 10, 10, 9, 9, 8, 8, 7, 7, 6, 6, 5, 5, 4, 4 };

            // Main bolt shape
            tex.SetPixel(9, 15, yellow);
            tex.SetPixel(10, 15, yellow);
            tex.SetPixel(8, 14, yellow);
            tex.SetPixel(9, 14, yellow);
            tex.SetPixel(10, 14, orange);
            tex.SetPixel(7, 13, yellow);
            tex.SetPixel(8, 13, yellow);
            tex.SetPixel(9, 13, orange);
            tex.SetPixel(6, 12, yellow);
            tex.SetPixel(7, 12, yellow);
            tex.SetPixel(8, 12, orange);
            tex.SetPixel(5, 11, yellow);
            tex.SetPixel(6, 11, yellow);
            tex.SetPixel(7, 11, orange);
            // Middle wide part
            tex.SetPixel(5, 10, yellow);
            tex.SetPixel(6, 10, yellow);
            tex.SetPixel(7, 10, yellow);
            tex.SetPixel(8, 10, yellow);
            tex.SetPixel(9, 10, yellow);
            tex.SetPixel(10, 10, orange);
            tex.SetPixel(6, 9, yellow);
            tex.SetPixel(7, 9, yellow);
            tex.SetPixel(8, 9, yellow);
            tex.SetPixel(9, 9, orange);
            // Lower part
            tex.SetPixel(7, 8, yellow);
            tex.SetPixel(8, 8, yellow);
            tex.SetPixel(9, 8, orange);
            tex.SetPixel(6, 7, yellow);
            tex.SetPixel(7, 7, yellow);
            tex.SetPixel(8, 7, orange);
            tex.SetPixel(5, 6, yellow);
            tex.SetPixel(6, 6, yellow);
            tex.SetPixel(7, 6, orange);
            tex.SetPixel(4, 5, yellow);
            tex.SetPixel(5, 5, yellow);
            tex.SetPixel(6, 5, orange);
            tex.SetPixel(5, 4, yellow);
            tex.SetPixel(6, 4, orange);
            tex.SetPixel(5, 3, yellow);
            tex.SetPixel(6, 3, orange);
            tex.SetPixel(5, 2, yellow);
            tex.SetPixel(6, 2, orange);
            tex.SetPixel(5, 1, yellow);

            tex.Apply();
            SaveSprite(tex, folderPath, "SpeedItem");
        }

        private static void GenerateLightSprite(string folderPath)
        {
            // Torch - 16x16
            Texture2D tex = new Texture2D(16, 16);
            Color transparent = new Color(0, 0, 0, 0);
            Color flame1 = new Color(1f, 0.9f, 0.3f);
            Color flame2 = new Color(1f, 0.6f, 0.1f);
            Color flame3 = new Color(1f, 0.3f, 0.1f);
            Color wood = new Color(0.55f, 0.35f, 0.15f);
            Color woodDark = new Color(0.4f, 0.25f, 0.1f);

            // Clear to transparent
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                    tex.SetPixel(x, y, transparent);

            // Draw flame
            tex.SetPixel(7, 15, flame1);
            tex.SetPixel(8, 15, flame1);
            tex.SetPixel(6, 14, flame1);
            tex.SetPixel(7, 14, flame1);
            tex.SetPixel(8, 14, flame1);
            tex.SetPixel(9, 14, flame1);
            tex.SetPixel(6, 13, flame2);
            tex.SetPixel(7, 13, flame1);
            tex.SetPixel(8, 13, flame1);
            tex.SetPixel(9, 13, flame2);
            tex.SetPixel(5, 12, flame2);
            tex.SetPixel(6, 12, flame1);
            tex.SetPixel(7, 12, flame1);
            tex.SetPixel(8, 12, flame1);
            tex.SetPixel(9, 12, flame1);
            tex.SetPixel(10, 12, flame2);
            tex.SetPixel(5, 11, flame3);
            tex.SetPixel(6, 11, flame2);
            tex.SetPixel(7, 11, flame1);
            tex.SetPixel(8, 11, flame1);
            tex.SetPixel(9, 11, flame2);
            tex.SetPixel(10, 11, flame3);
            tex.SetPixel(6, 10, flame3);
            tex.SetPixel(7, 10, flame2);
            tex.SetPixel(8, 10, flame2);
            tex.SetPixel(9, 10, flame3);

            // Draw torch handle
            tex.SetPixel(7, 9, wood);
            tex.SetPixel(8, 9, woodDark);
            tex.SetPixel(7, 8, wood);
            tex.SetPixel(8, 8, woodDark);
            tex.SetPixel(7, 7, wood);
            tex.SetPixel(8, 7, woodDark);
            tex.SetPixel(7, 6, wood);
            tex.SetPixel(8, 6, woodDark);
            tex.SetPixel(7, 5, wood);
            tex.SetPixel(8, 5, woodDark);
            tex.SetPixel(7, 4, wood);
            tex.SetPixel(8, 4, woodDark);
            tex.SetPixel(7, 3, wood);
            tex.SetPixel(8, 3, woodDark);
            tex.SetPixel(7, 2, wood);
            tex.SetPixel(8, 2, woodDark);

            tex.Apply();
            SaveSprite(tex, folderPath, "LightItem");
        }

        private static void GenerateHealSprite(string folderPath)
        {
            // Heart - 16x16
            Texture2D tex = new Texture2D(16, 16);
            Color transparent = new Color(0, 0, 0, 0);
            Color red = new Color(0.9f, 0.2f, 0.2f);
            Color pink = new Color(1f, 0.4f, 0.4f);
            Color darkRed = new Color(0.7f, 0.1f, 0.1f);

            // Clear to transparent
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                    tex.SetPixel(x, y, transparent);

            // Draw heart shape
            // Top bumps
            tex.SetPixel(3, 13, red);
            tex.SetPixel(4, 13, red);
            tex.SetPixel(11, 13, red);
            tex.SetPixel(12, 13, red);

            tex.SetPixel(2, 12, red);
            tex.SetPixel(3, 12, pink);
            tex.SetPixel(4, 12, pink);
            tex.SetPixel(5, 12, red);
            tex.SetPixel(10, 12, red);
            tex.SetPixel(11, 12, pink);
            tex.SetPixel(12, 12, pink);
            tex.SetPixel(13, 12, red);

            tex.SetPixel(1, 11, red);
            tex.SetPixel(2, 11, pink);
            tex.SetPixel(3, 11, pink);
            tex.SetPixel(4, 11, red);
            tex.SetPixel(5, 11, red);
            tex.SetPixel(6, 11, red);
            tex.SetPixel(9, 11, red);
            tex.SetPixel(10, 11, red);
            tex.SetPixel(11, 11, red);
            tex.SetPixel(12, 11, pink);
            tex.SetPixel(13, 11, pink);
            tex.SetPixel(14, 11, red);

            tex.SetPixel(1, 10, red);
            tex.SetPixel(2, 10, pink);
            tex.SetPixel(3, 10, red);
            tex.SetPixel(4, 10, red);
            tex.SetPixel(5, 10, red);
            tex.SetPixel(6, 10, red);
            tex.SetPixel(7, 10, red);
            tex.SetPixel(8, 10, red);
            tex.SetPixel(9, 10, red);
            tex.SetPixel(10, 10, red);
            tex.SetPixel(11, 10, red);
            tex.SetPixel(12, 10, red);
            tex.SetPixel(13, 10, pink);
            tex.SetPixel(14, 10, red);

            // Middle section
            for (int row = 9; row >= 6; row--)
            {
                int indent = 9 - row;
                for (int x = 1 + indent; x <= 14 - indent; x++)
                {
                    tex.SetPixel(x, row, red);
                }
                if (row >= 7)
                {
                    tex.SetPixel(2 + indent, row, pink);
                }
            }

            // Bottom point
            tex.SetPixel(6, 5, red);
            tex.SetPixel(7, 5, red);
            tex.SetPixel(8, 5, red);
            tex.SetPixel(9, 5, red);
            tex.SetPixel(7, 4, red);
            tex.SetPixel(8, 4, red);
            tex.SetPixel(7, 3, darkRed);
            tex.SetPixel(8, 3, darkRed);

            tex.Apply();
            SaveSprite(tex, folderPath, "HealItem");
        }

        private static void GenerateExplosiveSprite(string folderPath)
        {
            // Bomb - 16x16
            Texture2D tex = new Texture2D(16, 16);
            Color transparent = new Color(0, 0, 0, 0);
            Color black = new Color(0.15f, 0.15f, 0.15f);
            Color gray = new Color(0.3f, 0.3f, 0.3f);
            Color fuse = new Color(0.6f, 0.4f, 0.2f);
            Color spark = new Color(1f, 0.8f, 0.2f);
            Color sparkOrange = new Color(1f, 0.5f, 0.1f);

            // Clear to transparent
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                    tex.SetPixel(x, y, transparent);

            // Draw spark
            tex.SetPixel(11, 15, spark);
            tex.SetPixel(12, 15, sparkOrange);
            tex.SetPixel(10, 14, spark);
            tex.SetPixel(11, 14, spark);
            tex.SetPixel(12, 14, sparkOrange);

            // Draw fuse
            tex.SetPixel(10, 13, fuse);
            tex.SetPixel(9, 12, fuse);
            tex.SetPixel(9, 11, fuse);

            // Draw bomb body (circle)
            // Row 10
            tex.SetPixel(5, 10, black);
            tex.SetPixel(6, 10, black);
            tex.SetPixel(7, 10, black);
            tex.SetPixel(8, 10, black);
            tex.SetPixel(9, 10, black);
            tex.SetPixel(10, 10, black);

            // Row 9
            tex.SetPixel(4, 9, black);
            tex.SetPixel(5, 9, gray);
            tex.SetPixel(6, 9, gray);
            tex.SetPixel(7, 9, black);
            tex.SetPixel(8, 9, black);
            tex.SetPixel(9, 9, black);
            tex.SetPixel(10, 9, black);
            tex.SetPixel(11, 9, black);

            // Row 8
            tex.SetPixel(3, 8, black);
            tex.SetPixel(4, 8, gray);
            tex.SetPixel(5, 8, gray);
            tex.SetPixel(6, 8, black);
            tex.SetPixel(7, 8, black);
            tex.SetPixel(8, 8, black);
            tex.SetPixel(9, 8, black);
            tex.SetPixel(10, 8, black);
            tex.SetPixel(11, 8, black);
            tex.SetPixel(12, 8, black);

            // Row 7
            tex.SetPixel(3, 7, black);
            tex.SetPixel(4, 7, black);
            tex.SetPixel(5, 7, black);
            tex.SetPixel(6, 7, black);
            tex.SetPixel(7, 7, black);
            tex.SetPixel(8, 7, black);
            tex.SetPixel(9, 7, black);
            tex.SetPixel(10, 7, black);
            tex.SetPixel(11, 7, black);
            tex.SetPixel(12, 7, black);

            // Row 6
            tex.SetPixel(3, 6, black);
            tex.SetPixel(4, 6, black);
            tex.SetPixel(5, 6, black);
            tex.SetPixel(6, 6, black);
            tex.SetPixel(7, 6, black);
            tex.SetPixel(8, 6, black);
            tex.SetPixel(9, 6, black);
            tex.SetPixel(10, 6, black);
            tex.SetPixel(11, 6, black);
            tex.SetPixel(12, 6, black);

            // Row 5
            tex.SetPixel(3, 5, black);
            tex.SetPixel(4, 5, black);
            tex.SetPixel(5, 5, black);
            tex.SetPixel(6, 5, black);
            tex.SetPixel(7, 5, black);
            tex.SetPixel(8, 5, black);
            tex.SetPixel(9, 5, black);
            tex.SetPixel(10, 5, black);
            tex.SetPixel(11, 5, black);
            tex.SetPixel(12, 5, black);

            // Row 4
            tex.SetPixel(4, 4, black);
            tex.SetPixel(5, 4, black);
            tex.SetPixel(6, 4, black);
            tex.SetPixel(7, 4, black);
            tex.SetPixel(8, 4, black);
            tex.SetPixel(9, 4, black);
            tex.SetPixel(10, 4, black);
            tex.SetPixel(11, 4, black);

            // Row 3
            tex.SetPixel(5, 3, black);
            tex.SetPixel(6, 3, black);
            tex.SetPixel(7, 3, black);
            tex.SetPixel(8, 3, black);
            tex.SetPixel(9, 3, black);
            tex.SetPixel(10, 3, black);

            tex.Apply();
            SaveSprite(tex, folderPath, "ExplosiveItem");
        }

        private static void GenerateKeySprite(string folderPath)
        {
            // Key - 16x16
            Texture2D tex = new Texture2D(16, 16);
            Color transparent = new Color(0, 0, 0, 0);
            Color gold = new Color(1f, 0.85f, 0.2f);
            Color goldDark = new Color(0.85f, 0.65f, 0.1f);
            Color goldLight = new Color(1f, 0.95f, 0.5f);

            // Clear to transparent
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                    tex.SetPixel(x, y, transparent);

            // Draw key head (ring)
            tex.SetPixel(4, 14, gold);
            tex.SetPixel(5, 14, gold);
            tex.SetPixel(6, 14, gold);
            tex.SetPixel(3, 13, gold);
            tex.SetPixel(4, 13, goldLight);
            tex.SetPixel(5, 13, transparent);
            tex.SetPixel(6, 13, goldDark);
            tex.SetPixel(7, 13, gold);
            tex.SetPixel(2, 12, gold);
            tex.SetPixel(3, 12, goldLight);
            tex.SetPixel(4, 12, transparent);
            tex.SetPixel(5, 12, transparent);
            tex.SetPixel(6, 12, transparent);
            tex.SetPixel(7, 12, goldDark);
            tex.SetPixel(8, 12, gold);
            tex.SetPixel(2, 11, gold);
            tex.SetPixel(3, 11, goldLight);
            tex.SetPixel(4, 11, transparent);
            tex.SetPixel(5, 11, transparent);
            tex.SetPixel(6, 11, transparent);
            tex.SetPixel(7, 11, goldDark);
            tex.SetPixel(8, 11, gold);
            tex.SetPixel(3, 10, gold);
            tex.SetPixel(4, 10, goldLight);
            tex.SetPixel(5, 10, transparent);
            tex.SetPixel(6, 10, goldDark);
            tex.SetPixel(7, 10, gold);
            tex.SetPixel(4, 9, gold);
            tex.SetPixel(5, 9, gold);
            tex.SetPixel(6, 9, gold);

            // Draw key shaft
            tex.SetPixel(5, 8, gold);
            tex.SetPixel(6, 8, goldDark);
            tex.SetPixel(5, 7, gold);
            tex.SetPixel(6, 7, goldDark);
            tex.SetPixel(5, 6, gold);
            tex.SetPixel(6, 6, goldDark);
            tex.SetPixel(5, 5, gold);
            tex.SetPixel(6, 5, goldDark);
            tex.SetPixel(5, 4, gold);
            tex.SetPixel(6, 4, goldDark);

            // Draw key teeth
            tex.SetPixel(7, 4, gold);
            tex.SetPixel(8, 4, goldDark);
            tex.SetPixel(5, 3, gold);
            tex.SetPixel(6, 3, goldDark);
            tex.SetPixel(7, 3, gold);
            tex.SetPixel(5, 2, gold);
            tex.SetPixel(6, 2, goldDark);
            tex.SetPixel(7, 2, gold);
            tex.SetPixel(8, 2, goldDark);

            tex.Apply();
            SaveSprite(tex, folderPath, "KeyItem");
        }

        private static void GenerateXPSprite(string folderPath)
        {
            // Star/gem - 16x16
            Texture2D tex = new Texture2D(16, 16);
            Color transparent = new Color(0, 0, 0, 0);
            Color purple = new Color(0.6f, 0.2f, 0.8f);
            Color purpleLight = new Color(0.8f, 0.5f, 1f);
            Color purpleDark = new Color(0.4f, 0.1f, 0.5f);

            // Clear to transparent
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                    tex.SetPixel(x, y, transparent);

            // Draw star shape
            // Top point
            tex.SetPixel(7, 15, purple);
            tex.SetPixel(8, 15, purple);
            tex.SetPixel(7, 14, purpleLight);
            tex.SetPixel(8, 14, purple);
            tex.SetPixel(6, 13, purple);
            tex.SetPixel(7, 13, purpleLight);
            tex.SetPixel(8, 13, purple);
            tex.SetPixel(9, 13, purpleDark);

            // Upper arms and body
            tex.SetPixel(1, 12, purple);
            tex.SetPixel(2, 12, purple);
            tex.SetPixel(5, 12, purple);
            tex.SetPixel(6, 12, purpleLight);
            tex.SetPixel(7, 12, purpleLight);
            tex.SetPixel(8, 12, purple);
            tex.SetPixel(9, 12, purple);
            tex.SetPixel(10, 12, purpleDark);
            tex.SetPixel(13, 12, purple);
            tex.SetPixel(14, 12, purpleDark);

            tex.SetPixel(2, 11, purple);
            tex.SetPixel(3, 11, purple);
            tex.SetPixel(4, 11, purple);
            tex.SetPixel(5, 11, purpleLight);
            tex.SetPixel(6, 11, purpleLight);
            tex.SetPixel(7, 11, purple);
            tex.SetPixel(8, 11, purple);
            tex.SetPixel(9, 11, purple);
            tex.SetPixel(10, 11, purple);
            tex.SetPixel(11, 11, purple);
            tex.SetPixel(12, 11, purple);
            tex.SetPixel(13, 11, purpleDark);

            // Middle row
            tex.SetPixel(3, 10, purple);
            tex.SetPixel(4, 10, purpleLight);
            tex.SetPixel(5, 10, purpleLight);
            tex.SetPixel(6, 10, purple);
            tex.SetPixel(7, 10, purple);
            tex.SetPixel(8, 10, purple);
            tex.SetPixel(9, 10, purple);
            tex.SetPixel(10, 10, purple);
            tex.SetPixel(11, 10, purple);
            tex.SetPixel(12, 10, purpleDark);

            tex.SetPixel(4, 9, purple);
            tex.SetPixel(5, 9, purple);
            tex.SetPixel(6, 9, purple);
            tex.SetPixel(7, 9, purple);
            tex.SetPixel(8, 9, purple);
            tex.SetPixel(9, 9, purple);
            tex.SetPixel(10, 9, purple);
            tex.SetPixel(11, 9, purpleDark);

            // Lower body
            tex.SetPixel(5, 8, purple);
            tex.SetPixel(6, 8, purple);
            tex.SetPixel(7, 8, purple);
            tex.SetPixel(8, 8, purple);
            tex.SetPixel(9, 8, purple);
            tex.SetPixel(10, 8, purpleDark);

            tex.SetPixel(4, 7, purple);
            tex.SetPixel(5, 7, purple);
            tex.SetPixel(6, 7, purple);
            tex.SetPixel(7, 7, purple);
            tex.SetPixel(8, 7, purple);
            tex.SetPixel(9, 7, purple);
            tex.SetPixel(10, 7, purple);
            tex.SetPixel(11, 7, purpleDark);

            // Lower points
            tex.SetPixel(3, 6, purple);
            tex.SetPixel(4, 6, purple);
            tex.SetPixel(5, 6, purple);
            tex.SetPixel(6, 6, purple);
            tex.SetPixel(9, 6, purple);
            tex.SetPixel(10, 6, purple);
            tex.SetPixel(11, 6, purple);
            tex.SetPixel(12, 6, purpleDark);

            tex.SetPixel(2, 5, purple);
            tex.SetPixel(3, 5, purple);
            tex.SetPixel(4, 5, purpleDark);
            tex.SetPixel(11, 5, purple);
            tex.SetPixel(12, 5, purple);
            tex.SetPixel(13, 5, purpleDark);

            tex.SetPixel(1, 4, purple);
            tex.SetPixel(2, 4, purpleDark);
            tex.SetPixel(13, 4, purple);
            tex.SetPixel(14, 4, purpleDark);

            tex.Apply();
            SaveSprite(tex, folderPath, "XPItem");
        }

        private static void GeneratePebblesSprite(string folderPath)
        {
            // Pebbles - 16x16 (3 small grey stones)
            Texture2D tex = new Texture2D(16, 16);
            Color transparent = new Color(0, 0, 0, 0);
            Color gray = new Color(0.5f, 0.5f, 0.5f);
            Color grayLight = new Color(0.7f, 0.7f, 0.7f);
            Color grayDark = new Color(0.35f, 0.35f, 0.35f);

            // Clear to transparent
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                    tex.SetPixel(x, y, transparent);

            // Draw three pebbles

            // Large pebble (bottom center)
            tex.SetPixel(6, 5, gray);
            tex.SetPixel(7, 5, gray);
            tex.SetPixel(8, 5, gray);
            tex.SetPixel(9, 5, grayDark);
            tex.SetPixel(5, 4, gray);
            tex.SetPixel(6, 4, grayLight);
            tex.SetPixel(7, 4, grayLight);
            tex.SetPixel(8, 4, gray);
            tex.SetPixel(9, 4, gray);
            tex.SetPixel(10, 4, grayDark);
            tex.SetPixel(5, 3, gray);
            tex.SetPixel(6, 3, grayLight);
            tex.SetPixel(7, 3, gray);
            tex.SetPixel(8, 3, gray);
            tex.SetPixel(9, 3, grayDark);
            tex.SetPixel(10, 3, grayDark);
            tex.SetPixel(6, 2, gray);
            tex.SetPixel(7, 2, gray);
            tex.SetPixel(8, 2, grayDark);
            tex.SetPixel(9, 2, grayDark);

            // Medium pebble (top left)
            tex.SetPixel(3, 10, gray);
            tex.SetPixel(4, 10, gray);
            tex.SetPixel(5, 10, grayDark);
            tex.SetPixel(2, 9, gray);
            tex.SetPixel(3, 9, grayLight);
            tex.SetPixel(4, 9, gray);
            tex.SetPixel(5, 9, grayDark);
            tex.SetPixel(2, 8, gray);
            tex.SetPixel(3, 8, gray);
            tex.SetPixel(4, 8, grayDark);
            tex.SetPixel(5, 8, grayDark);
            tex.SetPixel(3, 7, grayDark);
            tex.SetPixel(4, 7, grayDark);

            // Small pebble (top right)
            tex.SetPixel(11, 11, gray);
            tex.SetPixel(12, 11, grayDark);
            tex.SetPixel(10, 10, gray);
            tex.SetPixel(11, 10, grayLight);
            tex.SetPixel(12, 10, gray);
            tex.SetPixel(13, 10, grayDark);
            tex.SetPixel(10, 9, gray);
            tex.SetPixel(11, 9, gray);
            tex.SetPixel(12, 9, grayDark);
            tex.SetPixel(11, 8, grayDark);

            tex.Apply();
            SaveSprite(tex, folderPath, "PebblesItem");
        }
    }
}

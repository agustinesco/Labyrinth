using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

public class TileGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Tiles from Sprites")]
    public static void GenerateTiles()
    {
        string spritesPath = "Assets/Sprites/Tiles";
        string tilesPath = "Assets/Tiles";

        // Ensure tiles folder exists
        if (!AssetDatabase.IsValidFolder(tilesPath))
        {
            AssetDatabase.CreateFolder("Assets", "Tiles");
        }

        // Find all sprites in the tiles folder
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { spritesPath });

        int created = 0;
        foreach (string guid in guids)
        {
            string spritePath = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            if (sprite != null)
            {
                string spriteName = Path.GetFileNameWithoutExtension(spritePath);
                string tilePath = $"{tilesPath}/{spriteName}.asset";

                // Check if tile already exists
                if (AssetDatabase.LoadAssetAtPath<Tile>(tilePath) != null)
                {
                    Debug.Log($"Tile already exists: {tilePath}");
                    continue;
                }

                // Create new tile
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = spriteName.Contains("Wall") ? Tile.ColliderType.Grid : Tile.ColliderType.None;

                AssetDatabase.CreateAsset(tile, tilePath);
                created++;
                Debug.Log($"Created tile: {tilePath}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Tile generation complete. Created {created} new tiles.");
        EditorUtility.DisplayDialog("Tile Generator", $"Created {created} new tile assets in {tilesPath}", "OK");
    }
}

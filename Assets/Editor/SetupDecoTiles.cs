using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using Labyrinth.Maze;
using System.Collections.Generic;

public static class SetupDecoTiles
{
    static readonly string AtlasPath = "Assets/Sprites/TX_MirrasMaze_AssetsAtlas.png";

    // Decoration tiles (sprites 21-26)
    static readonly string[] DecoSpriteNames = {
        "TX_MirrasMaze_AssetsAtlas_21",
        "TX_MirrasMaze_AssetsAtlas_22",
        "TX_MirrasMaze_AssetsAtlas_23",
        "TX_MirrasMaze_AssetsAtlas_24",
        "TX_MirrasMaze_AssetsAtlas_25",
        "TX_MirrasMaze_AssetsAtlas_26"
    };
    static readonly Rect[] DecoSpriteRects = {
        new Rect(64, 96, 32, 32),
        new Rect(96, 96, 32, 32),
        new Rect(128, 96, 32, 32),
        new Rect(160, 96, 32, 32),
        new Rect(0, 64, 32, 32),
        new Rect(32, 64, 32, 32)
    };
    static readonly int[] DecoIndices = { 21, 22, 23, 24, 25, 26 };

    // Floor rule tiles — diagonal wall tiles use sprite 29
    static readonly string[] FloorSpriteNames = {
        "TX_MirrasMaze_AssetsAtlas_29",
        "TX_MirrasMaze_AssetsAtlas_27",
        "TX_MirrasMaze_AssetsAtlas_29",
        "TX_MirrasMaze_AssetsAtlas_30",
        "TX_MirrasMaze_AssetsAtlas_32",
        "TX_MirrasMaze_AssetsAtlas_33"
    };
    static readonly Rect[] FloorSpriteRects = {
        new Rect(128, 32, 32, 32),
        new Rect(64, 64, 32, 32),
        new Rect(128, 32, 32, 32),
        new Rect(64, 32, 32, 32),
        new Rect(64, 0, 32, 32),
        new Rect(96, 0, 32, 32)
    };
    // Maps to: floorTileDiagonalWallRight(29), floorTileWallAbove(27), floorTileDiagonalWall(29), floorTileCenter(30), floorTileOneWall(32), floorTileCorner(33)
    static readonly string[] FloorTileAssetNames = {
        "FloorDiagonalWallRight",
        "FloorWallAbove",
        "FloorDiagonalWall",
        "FloorOpen",
        "FloorOneWall",
        "FloorCorner"
    };
    static readonly string[] FloorFieldNames = {
        "floorTileDiagonalWallRight",
        "floorTileWallAbove",
        "floorTileDiagonalWall",
        "floorTileCenter",
        "floorTileOneWall",
        "floorTileCorner"
    };

    [MenuItem("Tools/Setup Step1 - Ensure Sprites")]
    public static void Step1_EnsureSprites()
    {
        var importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("SETUP: No TextureImporter at " + AtlasPath);
            return;
        }

        var sheet = new List<SpriteMetaData>(importer.spritesheet);
        bool changed = false;

        // Combine all sprites we need
        var allNames = new List<string>(DecoSpriteNames);
        allNames.AddRange(FloorSpriteNames);
        var allRects = new List<Rect>(DecoSpriteRects);
        allRects.AddRange(FloorSpriteRects);

        for (int i = 0; i < allNames.Count; i++)
        {
            bool exists = false;
            foreach (var m in sheet)
                if (m.name == allNames[i]) { exists = true; break; }

            if (!exists)
            {
                sheet.Add(new SpriteMetaData {
                    name = allNames[i],
                    rect = allRects[i],
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = 0
                });
                changed = true;
                Debug.LogWarning("SETUP: Added sprite: " + allNames[i]);
            }
        }

        if (changed)
        {
            importer.spritesheet = sheet.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.LogWarning("SETUP: Step1 done - sprites added and reimported. Now run Step2.");
        }
        else
        {
            Debug.LogWarning("SETUP: Step1 - all sprites already exist. Run Step2.");
        }
    }

    [MenuItem("Tools/Setup Step2 - Create Tiles and Assign")]
    public static void Step2_CreateAndAssign()
    {
        Object[] allObjects = AssetDatabase.LoadAllAssetsAtPath(AtlasPath);

        // --- Decoration tiles ---
        Sprite[] decoSprites = new Sprite[DecoSpriteNames.Length];
        for (int i = 0; i < DecoSpriteNames.Length; i++)
        {
            foreach (Object obj in allObjects)
                if (obj is Sprite s && s.name == DecoSpriteNames[i])
                { decoSprites[i] = s; break; }

            if (decoSprites[i] == null)
            {
                Debug.LogError("SETUP: Sprite not found: " + DecoSpriteNames[i] + ". Run Step1 first.");
                return;
            }
        }

        Tile[] decoTiles = new Tile[DecoIndices.Length];
        for (int i = 0; i < DecoIndices.Length; i++)
        {
            string path = "Assets/Tiles/DecoFloor" + DecoIndices[i] + ".asset";
            decoTiles[i] = CreateOrUpdateTile(path, decoSprites[i]);
        }

        // --- Floor rule tiles ---
        Sprite[] floorSprites = new Sprite[FloorSpriteNames.Length];
        for (int i = 0; i < FloorSpriteNames.Length; i++)
        {
            foreach (Object obj in allObjects)
                if (obj is Sprite s && s.name == FloorSpriteNames[i])
                { floorSprites[i] = s; break; }

            if (floorSprites[i] == null)
            {
                Debug.LogError("SETUP: Sprite not found: " + FloorSpriteNames[i] + ". Run Step1 first.");
                return;
            }
        }

        Tile[] floorTiles = new Tile[FloorTileAssetNames.Length];
        for (int i = 0; i < FloorTileAssetNames.Length; i++)
        {
            string path = "Assets/Tiles/" + FloorTileAssetNames[i] + ".asset";
            floorTiles[i] = CreateOrUpdateTile(path, floorSprites[i]);
        }

        AssetDatabase.SaveAssets();

        // --- Assign to MazeRenderer ---
        var renderer = Object.FindObjectOfType<MazeRenderer>();
        if (renderer == null)
        {
            Debug.LogError("SETUP: No MazeRenderer in scene! Load MazeTest scene first.");
            return;
        }

        var so = new SerializedObject(renderer);

        // Decoration tiles
        var decoProp = so.FindProperty("decorationTiles");
        decoProp.arraySize = decoTiles.Length;
        for (int i = 0; i < decoTiles.Length; i++)
        {
            var elem = decoProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("tile").objectReferenceValue = decoTiles[i];
            elem.FindPropertyRelative("weight").floatValue = 1f;
        }
        so.FindProperty("decorationCoverage").floatValue = 15f;

        // Floor rule tiles
        for (int i = 0; i < FloorFieldNames.Length; i++)
        {
            var prop = so.FindProperty(FloorFieldNames[i]);
            if (prop != null)
                prop.objectReferenceValue = floorTiles[i];
            else
                Debug.LogError("SETUP: Field not found: " + FloorFieldNames[i]);
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(renderer);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.LogWarning("SETUP: Step2 done! All tiles created and assigned. Save the scene!");
    }

    static Tile CreateOrUpdateTile(string path, Sprite sprite)
    {
        Tile existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (existing != null)
        {
            existing.sprite = sprite;
            existing.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.None;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }
}

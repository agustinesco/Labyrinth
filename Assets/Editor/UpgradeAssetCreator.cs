using UnityEngine;
using UnityEditor;
using Labyrinth.Leveling;

public class UpgradeAssetCreator : Editor
{
    [MenuItem("Labyrinth/Create New Upgrade Assets")]
    public static void CreateNewUpgradeAssets()
    {
        // Wall Hugger
        CreateUpgradeAsset(
            "WallHuggerUpgrade",
            "Wall Hugger",
            "Move 15% faster when near walls. Master the art of hugging shadows.",
            UpgradeType.WallHugger,
            0.15f,
            new Color(0.4f, 0.6f, 0.4f)
        );

        // Shadow Blend
        CreateUpgradeAsset(
            "ShadowBlendUpgrade",
            "Shadow Blend",
            "Stand still for 2 seconds to blend into shadows. Enemies have reduced detection range.",
            UpgradeType.ShadowBlend,
            1f,
            new Color(0.3f, 0.3f, 0.5f)
        );

        // Deep Pockets
        CreateUpgradeAsset(
            "DeepPocketsUpgrade",
            "Deep Pockets",
            "Gain +1 inventory slot. More room for tools of the trade.",
            UpgradeType.DeepPockets,
            1f,
            new Color(0.6f, 0.5f, 0.3f)
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created new upgrade assets in Assets/Config/Upgrades/");
    }

    private static void CreateUpgradeAsset(string fileName, string displayName, string description,
        UpgradeType upgradeType, float effectValue, Color cardTint)
    {
        string path = $"Assets/Config/Upgrades/{fileName}.asset";

        // Check if asset already exists
        var existing = AssetDatabase.LoadAssetAtPath<LevelUpUpgrade>(path);
        if (existing != null)
        {
            Debug.Log($"Upgrade asset already exists: {path}");
            return;
        }

        var upgrade = ScriptableObject.CreateInstance<LevelUpUpgrade>();

        // Use SerializedObject to set private fields
        var so = new SerializedObject(upgrade);
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("description").stringValue = description;
        so.FindProperty("upgradeType").enumValueIndex = (int)upgradeType;
        so.FindProperty("effectValue").floatValue = effectValue;
        so.FindProperty("cardTint").colorValue = cardTint;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(upgrade, path);
        Debug.Log($"Created upgrade asset: {path}");
    }
}

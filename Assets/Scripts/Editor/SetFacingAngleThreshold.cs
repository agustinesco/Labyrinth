using UnityEngine;
using UnityEditor;
using Labyrinth.Enemy.Awareness;

public static class SetFacingAngleThreshold
{
    [MenuItem("Tools/Set Facing Angle Threshold (One-Time)")]
    public static void Execute()
    {
        var config = AssetDatabase.LoadAssetAtPath<EnemyAwarenessConfig>("Assets/Config/Enemies/ShadowStalkerAwareness.asset");
        if (config == null)
        {
            Debug.LogError("Could not find ShadowStalkerAwareness.asset");
            return;
        }

        var so = new SerializedObject(config);
        var prop = so.FindProperty("facingAngleThreshold");
        if (prop == null)
        {
            Debug.LogError("Could not find facingAngleThreshold property");
            return;
        }

        prop.floatValue = 75f;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log("Set facingAngleThreshold to 75 on ShadowStalkerAwareness.asset");
    }
}

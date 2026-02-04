using UnityEngine;

namespace Labyrinth.UI.Bestiary
{
    /// <summary>
    /// Data for a single enemy entry in the bestiary.
    /// </summary>
    [System.Serializable]
    public class BestiaryEntryData
    {
        public string enemyId;
        public string displayName;
        public Sprite icon;
        [TextArea(3, 6)]
        public string description;
        [TextArea(2, 4)]
        public string behaviorTip;
    }

    /// <summary>
    /// ScriptableObject containing all bestiary entries.
    /// </summary>
    [CreateAssetMenu(fileName = "BestiaryData", menuName = "Labyrinth/Bestiary Data", order = 10)]
    public class BestiaryData : ScriptableObject
    {
        [SerializeField] private BestiaryEntryData[] entries;

        public BestiaryEntryData[] Entries => entries;

        public BestiaryEntryData GetEntry(string enemyId)
        {
            if (entries == null) return null;

            foreach (var entry in entries)
            {
                if (entry.enemyId == enemyId)
                {
                    return entry;
                }
            }
            return null;
        }
    }
}

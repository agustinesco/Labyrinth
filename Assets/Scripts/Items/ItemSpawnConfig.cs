using UnityEngine;
using System;
using System.Collections.Generic;

namespace Labyrinth.Items
{
    /// <summary>
    /// ScriptableObject containing item spawn configuration.
    /// Create via Assets > Create > Labyrinth > Item Spawn Config
    /// </summary>
    [CreateAssetMenu(fileName = "ItemSpawnConfig", menuName = "Labyrinth/Item Spawn Config", order = 2)]
    public class ItemSpawnConfig : ScriptableObject
    {
        [Header("Key Item (always spawns at exit)")]
        [SerializeField] private GameObject keyItemPrefab;

        [Header("XP Items")]
        [SerializeField] private GameObject xpItemPrefab;
        [SerializeField, Min(0)] private int xpItemCount = 45;

        [Header("General Items Pool")]
        [SerializeField, Tooltip("Total number of general items to spawn (randomly picked from the pool)")]
        [Min(0)] private int generalItemCount = 20;

        [SerializeField, Tooltip("Item prefabs to randomly pick from")]
        private List<GameObject> generalItemPool = new List<GameObject>();

        // Public accessors
        public GameObject KeyItemPrefab => keyItemPrefab;
        public GameObject XpItemPrefab => xpItemPrefab;
        public int XpItemCount => xpItemCount;
        public int GeneralItemCount => generalItemCount;
        public IReadOnlyList<GameObject> GeneralItemPool => generalItemPool;

        /// <summary>
        /// Gets a random item prefab from the general pool.
        /// </summary>
        public GameObject GetRandomGeneralItem()
        {
            if (generalItemPool == null || generalItemPool.Count == 0)
                return null;

            // Filter out null entries
            var validItems = new List<GameObject>();
            foreach (var item in generalItemPool)
            {
                if (item != null)
                    validItems.Add(item);
            }

            if (validItems.Count == 0)
                return null;

            return validItems[UnityEngine.Random.Range(0, validItems.Count)];
        }

        /// <summary>
        /// Gets the total number of items that will be spawned (excluding key).
        /// </summary>
        public int GetTotalItemCount()
        {
            return xpItemCount + generalItemCount;
        }
    }
}

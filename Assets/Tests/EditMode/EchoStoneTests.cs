using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Labyrinth.Tests
{
    public class EchoStoneTests
    {
        [Test]
        public void EchoStoneEffect_HasCorrectRevealRadius()
        {
            var effect = new EchoStoneEffectData(10f, 3f);
            Assert.AreEqual(10f, effect.RevealRadius);
        }

        [Test]
        public void EchoStoneEffect_HasCorrectRevealDuration()
        {
            var effect = new EchoStoneEffectData(10f, 3f);
            Assert.AreEqual(3f, effect.RevealDuration);
        }

        [Test]
        public void EchoStoneEffect_RadiusMustBePositive()
        {
            var effect = new EchoStoneEffectData(-5f, 3f);
            Assert.IsTrue(effect.RevealRadius > 0);
        }

        [Test]
        public void EchoStoneEffect_DurationMustBePositive()
        {
            var effect = new EchoStoneEffectData(10f, -2f);
            Assert.IsTrue(effect.RevealDuration > 0);
        }

        [Test]
        public void EchoStoneEffect_DetectsEnemiesWithinRadius()
        {
            var effect = new EchoStoneEffectData(10f, 3f);
            var playerPos = Vector2.zero;

            var enemyPositions = new List<Vector2>
            {
                new Vector2(5f, 0f),   // Within radius
                new Vector2(15f, 0f),  // Outside radius
                new Vector2(0f, 8f),   // Within radius
                new Vector2(10f, 10f)  // Outside radius (distance ~14.14)
            };

            var detected = effect.GetDetectedEnemies(playerPos, enemyPositions);

            Assert.AreEqual(2, detected.Count);
            Assert.Contains(new Vector2(5f, 0f), detected);
            Assert.Contains(new Vector2(0f, 8f), detected);
        }

        [Test]
        public void EchoStoneEffect_DetectsEnemiesAtExactRadius()
        {
            var effect = new EchoStoneEffectData(10f, 3f);
            var playerPos = Vector2.zero;

            var enemyPositions = new List<Vector2>
            {
                new Vector2(10f, 0f)  // Exactly at radius
            };

            var detected = effect.GetDetectedEnemies(playerPos, enemyPositions);

            Assert.AreEqual(1, detected.Count);
        }

        [Test]
        public void EchoStoneEffect_ReturnsEmptyListWhenNoEnemies()
        {
            var effect = new EchoStoneEffectData(10f, 3f);
            var playerPos = Vector2.zero;

            var enemyPositions = new List<Vector2>();

            var detected = effect.GetDetectedEnemies(playerPos, enemyPositions);

            Assert.IsEmpty(detected);
        }
    }

    /// <summary>
    /// Data class for testing echo stone effect logic without MonoBehaviour.
    /// </summary>
    public class EchoStoneEffectData
    {
        public float RevealRadius { get; }
        public float RevealDuration { get; }

        private const float MinRadius = 1f;
        private const float MinDuration = 0.5f;

        public EchoStoneEffectData(float revealRadius, float revealDuration)
        {
            RevealRadius = Mathf.Max(MinRadius, revealRadius);
            RevealDuration = Mathf.Max(MinDuration, revealDuration);
        }

        /// <summary>
        /// Returns positions of enemies within the reveal radius.
        /// </summary>
        public List<Vector2> GetDetectedEnemies(Vector2 playerPosition, List<Vector2> enemyPositions)
        {
            var detected = new List<Vector2>();
            foreach (var enemyPos in enemyPositions)
            {
                float distance = Vector2.Distance(playerPosition, enemyPos);
                if (distance <= RevealRadius)
                {
                    detected.Add(enemyPos);
                }
            }
            return detected;
        }
    }
}

using NUnit.Framework;
using Labyrinth.Player;

namespace Labyrinth.Tests
{
    public class PlayerHealthTests
    {
        [Test]
        public void Health_StartsAtMaxHealth()
        {
            var health = new PlayerHealthData(3);
            Assert.AreEqual(3, health.CurrentHealth);
        }

        [Test]
        public void TakeDamage_ReducesHealth()
        {
            var health = new PlayerHealthData(3);
            health.TakeDamage(1);
            Assert.AreEqual(2, health.CurrentHealth);
        }

        [Test]
        public void TakeDamage_CannotGoBelowZero()
        {
            var health = new PlayerHealthData(3);
            health.TakeDamage(10);
            Assert.AreEqual(0, health.CurrentHealth);
        }

        [Test]
        public void IsDead_ReturnsTrueWhenHealthIsZero()
        {
            var health = new PlayerHealthData(1);
            health.TakeDamage(1);
            Assert.IsTrue(health.IsDead);
        }
    }

    // Simple data class for testing health logic without MonoBehaviour
    public class PlayerHealthData
    {
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        public PlayerHealthData(int maxHealth)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            CurrentHealth = System.Math.Max(0, CurrentHealth - amount);
        }
    }
}

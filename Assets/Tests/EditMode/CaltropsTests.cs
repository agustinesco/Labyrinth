using NUnit.Framework;

namespace Labyrinth.Tests
{
    public class CaltropsTests
    {
        [Test]
        public void CaltropsEffect_AppliesSlowToEnemy()
        {
            var effect = new CaltropsEffectData(0.5f, 3f, 1);

            float originalSpeed = 4f;
            float slowedSpeed = effect.ApplySlowEffect(originalSpeed);

            Assert.AreEqual(2f, slowedSpeed, 0.01f);
        }

        [Test]
        public void CaltropsEffect_SlowMultiplierClampedBetweenZeroAndOne()
        {
            // Multiplier above 1 should be clamped
            var effectHigh = new CaltropsEffectData(1.5f, 3f, 1);
            Assert.AreEqual(1f, effectHigh.SpeedMultiplier);

            // Multiplier below 0 should be clamped
            var effectLow = new CaltropsEffectData(-0.5f, 3f, 1);
            Assert.AreEqual(0f, effectLow.SpeedMultiplier);
        }

        [Test]
        public void CaltropsEffect_HasCorrectDuration()
        {
            var effect = new CaltropsEffectData(0.5f, 5f, 1);
            Assert.AreEqual(5f, effect.SlowDuration);
        }

        [Test]
        public void CaltropsEffect_DamagesPlayerWhenSteppedOn()
        {
            var effect = new CaltropsEffectData(0.5f, 3f, 1);
            Assert.AreEqual(1, effect.PlayerDamage);
        }

        [Test]
        public void CaltropsEffect_ZeroDamageIsValid()
        {
            var effect = new CaltropsEffectData(0.5f, 3f, 0);
            Assert.AreEqual(0, effect.PlayerDamage);
        }
    }

    /// <summary>
    /// Data class for testing caltrops effect logic without MonoBehaviour.
    /// </summary>
    public class CaltropsEffectData
    {
        public float SpeedMultiplier { get; }
        public float SlowDuration { get; }
        public int PlayerDamage { get; }

        public CaltropsEffectData(float speedMultiplier, float slowDuration, int playerDamage)
        {
            SpeedMultiplier = System.Math.Clamp(speedMultiplier, 0f, 1f);
            SlowDuration = slowDuration;
            PlayerDamage = System.Math.Max(0, playerDamage);
        }

        public float ApplySlowEffect(float originalSpeed)
        {
            return originalSpeed * SpeedMultiplier;
        }
    }
}

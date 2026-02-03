using UnityEngine;

namespace Labyrinth.Visibility
{
    /// <summary>
    /// A permanent light source that provides circular visibility.
    /// Placed on walls when the player uses a light source item.
    /// </summary>
    public class PlacedLightSource : MonoBehaviour
    {
        [SerializeField] private float lightRadius = 4f;
        [SerializeField] private int rayCount = 60;

        private bool _registered = false;

        public float LightRadius => lightRadius;
        public int RayCount => rayCount;
        public Vector2 Position => transform.position;

        public void Initialize(float radius)
        {
            lightRadius = radius;
            Debug.Log($"PlacedLightSource: Initialized with radius {radius}");
        }

        private void OnEnable()
        {
            TryRegister();
        }

        private void Start()
        {
            // Retry registration in Start in case FogOfWarManager wasn't ready in OnEnable
            TryRegister();
        }

        private void TryRegister()
        {
            if (_registered) return;

            if (FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.RegisterLightSource(this);
                _registered = true;
                Debug.Log($"PlacedLightSource: Registered with FogOfWarManager at position {Position}");
            }
            else
            {
                Debug.LogWarning("PlacedLightSource: FogOfWarManager.Instance is null, cannot register!");
            }
        }

        private void OnDisable()
        {
            if (_registered && FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.UnregisterLightSource(this);
                _registered = false;
                Debug.Log("PlacedLightSource: Unregistered from FogOfWarManager");
            }
        }
    }
}

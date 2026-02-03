using UnityEngine;
using Labyrinth.UI;

namespace Labyrinth.Player
{
    /// <summary>
    /// Handles player input from virtual joystick (mobile) or keyboard (editor).
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private VirtualJoystick joystick;

        private void Awake()
        {
            // Try to get PlayerController from same GameObject if not assigned
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }
        }

        /// <summary>
        /// Sets the joystick reference at runtime (for dynamically spawned players).
        /// </summary>
        public void SetJoystick(VirtualJoystick joystick)
        {
            this.joystick = joystick;
        }

        private void Update()
        {
            if (playerController == null)
            {
                return;
            }

            // Use joystick input if available
            if (joystick != null && joystick.InputVector != Vector2.zero)
            {
                playerController.SetMoveInput(joystick.InputVector);
                return;
            }

            // Keyboard fallback for testing in editor
            #if UNITY_EDITOR
            Vector2 keyboardInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );
            playerController.SetMoveInput(keyboardInput);
            #else
            // On mobile, if no joystick input, set to zero
            if (joystick == null || joystick.InputVector == Vector2.zero)
            {
                playerController.SetMoveInput(Vector2.zero);
            }
            #endif
        }
    }
}

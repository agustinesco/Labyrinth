using UnityEngine;
using UnityEngine.UI;
using Labyrinth.Maze;

namespace Labyrinth.UI
{
    public class DebugResetButton : MonoBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnResetClicked);
            }
        }

        private void OnResetClicked()
        {
            var initializer = FindObjectOfType<MazeInitializer>();
            if (initializer != null)
            {
                initializer.ResetGame();
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnResetClicked);
            }
        }
    }
}

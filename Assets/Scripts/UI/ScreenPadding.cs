using UnityEngine;
using UnityEngine.UI;

namespace Labyrinth.UI
{
    /// <summary>
    /// Creates black padding bars on left and right sides of the screen
    /// to avoid phone camera notches. The game renders in the center area
    /// and black bars fill the padding zones.
    /// </summary>
    public class ScreenPadding : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.3f)]
        private float paddingPercent = 0.05f;

        private void Start()
        {
            // Create background camera that renders black behind everything
            CreateBackgroundCamera();

            // Adjust the main game camera to render in the center
            AdjustMainCamera();

            // Adjust UI elements to have margins from edges
            AdjustUIElements();
        }

        private void CreateBackgroundCamera()
        {
            // This camera renders first and fills the entire screen with black
            var bgCameraObj = new GameObject("BackgroundCamera");
            bgCameraObj.transform.SetParent(transform);

            var bgCamera = bgCameraObj.AddComponent<Camera>();
            bgCamera.depth = -100; // Render before everything else
            bgCamera.clearFlags = CameraClearFlags.SolidColor;
            bgCamera.backgroundColor = Color.black;
            bgCamera.cullingMask = 0; // Don't render any objects
            bgCamera.orthographic = true;
            // Full viewport - this fills the entire screen with black
            bgCamera.rect = new Rect(0, 0, 1, 1);
        }

        private void AdjustMainCamera()
        {
            // The main camera renders the game in the center portion
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Clear only depth so the black background shows through the padding areas
                mainCamera.clearFlags = CameraClearFlags.Depth;
                // Render in the center, leaving padding on left and right
                mainCamera.rect = new Rect(paddingPercent, 0f, 1f - 2f * paddingPercent, 1f);
            }
        }

        private void AdjustUIElements()
        {
            float paddingPixels = Screen.width * paddingPercent;

            var allCanvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in allCanvases)
            {
                if (!canvas.isRootCanvas) continue;
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;

                var scaler = canvas.GetComponent<CanvasScaler>();
                float scaleFactor = 1f;
                if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    float widthRatio = Screen.width / scaler.referenceResolution.x;
                    float heightRatio = Screen.height / scaler.referenceResolution.y;
                    scaleFactor = Mathf.Lerp(widthRatio, heightRatio, scaler.matchWidthOrHeight);
                }

                float paddingInCanvasUnits = paddingPixels / scaleFactor;

                var canvasRect = canvas.GetComponent<RectTransform>();
                foreach (Transform child in canvasRect)
                {
                    var rectChild = child as RectTransform;
                    if (rectChild != null)
                    {
                        AdjustRectTransform(rectChild, paddingInCanvasUnits);
                    }
                }
            }
        }

        private void AdjustRectTransform(RectTransform rect, float padding)
        {
            // Left-anchored elements
            if (rect.anchorMin.x < 0.2f && rect.anchorMax.x < 0.5f)
            {
                rect.anchoredPosition = new Vector2(
                    rect.anchoredPosition.x + padding,
                    rect.anchoredPosition.y
                );
            }
            // Right-anchored elements
            else if (rect.anchorMax.x > 0.8f && rect.anchorMin.x > 0.5f)
            {
                rect.anchoredPosition = new Vector2(
                    rect.anchoredPosition.x - padding,
                    rect.anchoredPosition.y
                );
            }
        }
    }
}

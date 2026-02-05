using UnityEngine;
using UnityEngine.UI;

namespace Labyrinth.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILineRenderer : Graphic
    {
        [SerializeField] private float _lineWidth = 4f;
        [SerializeField] private Color _lineColor = Color.white;

        private Vector2 _startPoint;
        private Vector2 _endPoint;

        public void SetPositions(Vector3 worldStart, Vector3 worldEnd)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldStart),
                canvas.worldCamera,
                out _startPoint
            );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldEnd),
                canvas.worldCamera,
                out _endPoint
            );

            SetVerticesDirty();
        }

        public void SetLocalPositions(Vector2 start, Vector2 end)
        {
            _startPoint = start;
            _endPoint = end;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (_startPoint == _endPoint) return;

            Vector2 direction = (_endPoint - _startPoint).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * _lineWidth * 0.5f;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = _lineColor;

            // Create quad for line
            vertex.position = _startPoint - perpendicular;
            vh.AddVert(vertex);

            vertex.position = _startPoint + perpendicular;
            vh.AddVert(vertex);

            vertex.position = _endPoint + perpendicular;
            vh.AddVert(vertex);

            vertex.position = _endPoint - perpendicular;
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }
    }
}

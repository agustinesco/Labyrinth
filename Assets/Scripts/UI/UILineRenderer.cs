using UnityEngine;
using UnityEngine.UI;

namespace Labyrinth.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILineRenderer : MaskableGraphic
    {
        [SerializeField] private float _lineWidth = 4f;
        [SerializeField] private Color _lineColor = Color.white;

        [Header("Connected Nodes")]
        [SerializeField] private RectTransform _fromNode;
        [SerializeField] private RectTransform _toNode;

        private Vector2 _startPoint;
        private Vector2 _endPoint;

        protected override void OnEnable()
        {
            base.OnEnable();
            transform.SetAsFirstSibling();
            UpdatePositionsFromNodes();
        }

        private void LateUpdate()
        {
            UpdatePositionsFromNodes();
        }

        private void UpdatePositionsFromNodes()
        {
            if (_fromNode == null || _toNode == null) return;

            var parentRect = rectTransform.parent as RectTransform;
            if (parentRect == null) return;

            Vector2 from = _fromNode.anchoredPosition;
            Vector2 to = _toNode.anchoredPosition;

            // Offset to node center (pivot is at left-center, node is 150x100)
            from.x += _fromNode.sizeDelta.x * 0.5f;
            to.x += _toNode.sizeDelta.x * 0.5f;

            if (from != _startPoint || to != _endPoint)
            {
                _startPoint = from;
                _endPoint = to;
                SetVerticesDirty();
            }
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

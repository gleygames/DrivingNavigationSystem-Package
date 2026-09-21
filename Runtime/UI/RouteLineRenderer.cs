using System.Collections.Generic;
using UnityEngine;

namespace Gley.NavigationSystem
{
    [RequireComponent(typeof(RectTransform))]
    public class RouteLineRenderer : MonoBehaviour
    {
        private const int TrimModeRemove = 0;

        private readonly List<RouteLineGraphic> _graphics = new List<RouteLineGraphic>();
        private readonly List<RouteLineChunk> _chunks = new List<RouteLineChunk>();
        private readonly List<Vector2> _chunkPoints = new List<Vector2>();
        private readonly List<float> _chunkDistances = new List<float>();
        private readonly List<float> _chunkEndDistances = new List<float>();
        private readonly List<bool> _chunkDashed = new List<bool>();
        private readonly RouteLineChunker _chunker = new RouteLineChunker();

        private Shader _shader;
        private Canvas _ownCanvas;
        private Color _lineColor;
        private Color _outlineColor;
        private Color _fadedColor;
        private float _halfWidth;
        private float _outlineWidth;
        private float _dashLength;
        private float _gapLength;
        private float _canvasUnitsPerMeter = 1f;
        private float _trimDistance;
        private int _trimMode;
        private int _activeCount;
        [SerializeField] private bool useOwnCanvas = true;
        private bool _hasStyle;

        private void OnEnable()
        {
            EnsureOwnCanvas();
        }

        public void SetShader(Shader shader)
        {
            _shader = shader;
            for (int i = 0; i < _graphics.Count; i++)
            {
                _graphics[i].SetShader(_shader);
            }
        }

        public void SetLine(List<Vector2> points, List<float> distances, List<bool> dashed)
        {
            _chunker.Split(points, distances, dashed, _chunks);

            for (int i = 0; i < _chunks.Count; i++)
            {
                RouteLineChunk chunk = _chunks[i];
                RouteLineGraphic graphic = GetOrCreateGraphic(i);
                BuildChunkSlice(points, distances, dashed, chunk);
                graphic.SetLine(_chunkPoints, _chunkDistances, _chunkDashed);
                _chunkEndDistances[i] = chunk.EndDistance;
                graphic.gameObject.SetActive(true);
            }

            _activeCount = _chunks.Count;

            for (int i = _activeCount; i < _graphics.Count; i++)
            {
                _graphics[i].gameObject.SetActive(false);
            }
        }

        public void SetStyle(Color line, Color outline, Color faded, float halfWidth, float outlineWidth, float dashLength, float gapLength, int trimMode)
        {
            _lineColor = line;
            _outlineColor = outline;
            _fadedColor = faded;
            _halfWidth = halfWidth;
            _outlineWidth = outlineWidth;
            _dashLength = dashLength;
            _gapLength = gapLength;
            _trimMode = trimMode;
            _hasStyle = true;

            for (int i = 0; i < _activeCount; i++)
            {
                ApplyStyleToGraphic(_graphics[i]);
            }
        }

        public void SetTrimDistance(float meters)
        {
            _trimDistance = meters;

            for (int i = 0; i < _activeCount; i++)
            {
                _graphics[i].SetTrimDistance(meters);
            }

            if (_trimMode == TrimModeRemove)
            {
                UpdateChunkVisibilityForTrim();
            }
        }

        public void SetCanvasUnitsPerMeter(float value)
        {
            _canvasUnitsPerMeter = value;
            for (int i = 0; i < _activeCount; i++)
            {
                _graphics[i].SetCanvasUnitsPerMeter(value);
            }
        }

        private void EnsureOwnCanvas()
        {
            if (!useOwnCanvas)
            {
                return;
            }

            if (_ownCanvas != null)
            {
                return;
            }

            _ownCanvas = GetComponent<Canvas>();
            if (_ownCanvas == null)
            {
                _ownCanvas = gameObject.AddComponent<Canvas>();
            }

            _ownCanvas.overrideSorting = false;
        }

        private RouteLineGraphic GetOrCreateGraphic(int index)
        {
            if (index < _graphics.Count)
            {
                return _graphics[index];
            }

            RouteLineGraphic graphic = CreateGraphic(index);
            _graphics.Add(graphic);
            _chunkEndDistances.Add(0f);
            return graphic;
        }

        private RouteLineGraphic CreateGraphic(int index)
        {
            GameObject graphicObject = new GameObject("Chunk" + index, typeof(RectTransform));
            graphicObject.transform.SetParent(transform, false);

            RectTransform rectTransform = graphicObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            RouteLineGraphic graphic = graphicObject.AddComponent<RouteLineGraphic>();
            ApplyShaderToGraphic(graphic);
            ApplyStyleToGraphic(graphic);
            graphic.SetTrimDistance(_trimDistance);
            graphic.SetCanvasUnitsPerMeter(_canvasUnitsPerMeter);
            return graphic;
        }

        private void ApplyShaderToGraphic(RouteLineGraphic graphic)
        {
            if (_shader != null)
            {
                graphic.SetShader(_shader);
            }
        }

        private void ApplyStyleToGraphic(RouteLineGraphic graphic)
        {
            if (!_hasStyle)
            {
                return;
            }

            graphic.SetStyle(_lineColor, _outlineColor, _fadedColor, _halfWidth, _outlineWidth, _dashLength, _gapLength, _trimMode);
        }

        private void BuildChunkSlice(List<Vector2> points, List<float> distances, List<bool> dashed, RouteLineChunk chunk)
        {
            _chunkPoints.Clear();
            _chunkDistances.Clear();
            _chunkDashed.Clear();

            int endIndex = chunk.StartIndex + chunk.Count - 1;
            for (int i = chunk.StartIndex; i <= endIndex; i++)
            {
                _chunkPoints.Add(points[i]);
                _chunkDistances.Add(distances[i]);
            }

            for (int i = chunk.StartIndex; i < endIndex; i++)
            {
                _chunkDashed.Add(dashed[i]);
            }
        }

        private void UpdateChunkVisibilityForTrim()
        {
            for (int i = 0; i < _activeCount; i++)
            {
                bool shouldBeActive = _chunkEndDistances[i] > _trimDistance;
                if (_graphics[i].gameObject.activeSelf != shouldBeActive)
                {
                    _graphics[i].gameObject.SetActive(shouldBeActive);
                }
            }
        }
    }
}

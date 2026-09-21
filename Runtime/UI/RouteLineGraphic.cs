using System.Collections.Generic;
using Gley.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class RouteLineGraphic : MaskableGraphic
    {
        private const string ShaderName = "Gley/NavigationSystem/RouteLine";
        private const string ColorProperty = "_Color";
        private const string OutlineColorProperty = "_OutlineColor";
        private const string FadedColorProperty = "_FadedColor";
        private const string HalfWidthProperty = "_HalfWidth";
        private const string OutlineWidthProperty = "_OutlineWidth";
        private const string CanvasUnitsPerMeterProperty = "_CanvasUnitsPerMeter";
        private const string TrimDistanceProperty = "_TrimDistance";
        private const string TrimModeProperty = "_TrimMode";
        private const string DashLengthProperty = "_DashLength";
        private const string GapLengthProperty = "_GapLength";
        private const string CanvasOffsetMatrixProperty = "_CanvasOffsetMatrix";
        private const float DefaultHalfWidth = 3f;
        private const float DefaultOutlineWidth = 1f;
        private const float DefaultDashLength = 8f;
        private const float DefaultGapLength = 6f;
        private const float MinScale = 0.000001f;
        private const AdditionalCanvasShaderChannels RequiredShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;

        private readonly List<Vector2> _points = new List<Vector2>();
        private readonly List<UIVertex> _vertices = new List<UIVertex>();
        private readonly List<float> _distances = new List<float>();
        private readonly List<int> _indices = new List<int>();
        private readonly List<bool> _dashed = new List<bool>();
        private readonly RouteLineMeshBuilder _meshBuilder = new RouteLineMeshBuilder();

        [SerializeField] private Shader lineShader;
        private Material _materialInstance;
        private Color _lineColor = new Color(0.16f, 0.47f, 1f, 1f);
        private Color _outlineColor = new Color(0.05f, 0.2f, 0.55f, 1f);
        private Color _fadedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        private Vector4 _canvasOffsetMatrix = new Vector4(1f, 0f, 0f, 1f);
        private float _halfWidth = DefaultHalfWidth;
        private float _outlineWidth = DefaultOutlineWidth;
        private float _dashLength = DefaultDashLength;
        private float _gapLength = DefaultGapLength;
        private float _canvasUnitsPerMeter = 1f;
        private float _trimDistance;
        private int _trimMode;
        [SerializeField][HideInInspector] private bool defaultsApplied;
        private bool _hasCanvasOffsetMatrix;

        internal Vector4 CanvasOffsetMatrix
        {
            get
            {
                return _canvasOffsetMatrix;
            }
        }

        internal int MeshBuildCount { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            ApplyDefaultsOnce();
        }

        protected override void OnEnable()
        {
            EnsureMaterial();
            EnsureShaderChannels();
            _hasCanvasOffsetMatrix = false;
            Canvas.willRenderCanvases += HandleWillRenderCanvases;
            base.OnEnable();
        }

        public void UpdateRouteLineVisuals(float deltaTime)
        {
            if (_materialInstance == null)
            {
                return;
            }

            Canvas batchCanvas = canvas;
            if (batchCanvas == null)
            {
                return;
            }

            Vector4 offsetMatrix;
            if (!TryComputeCanvasOffsetMatrix(batchCanvas, out offsetMatrix))
            {
                return;
            }

            if (_hasCanvasOffsetMatrix && offsetMatrix == _canvasOffsetMatrix)
            {
                return;
            }

            _canvasOffsetMatrix = offsetMatrix;
            _hasCanvasOffsetMatrix = true;
            ApplyCanvasOffsetMatrixToMaterials();
        }

        public void SetShader(Shader shader)
        {
            lineShader = shader;
            DestroyMaterialInstance();
            if (isActiveAndEnabled)
            {
                EnsureMaterial();
            }
        }

        public void SetLine(List<Vector2> points, List<float> distances, List<bool> dashed)
        {
            _points.Clear();
            _distances.Clear();
            _dashed.Clear();

            for (int i = 0; i < points.Count; i++)
            {
                _points.Add(points[i]);
            }

            for (int i = 0; i < distances.Count; i++)
            {
                _distances.Add(distances[i]);
            }

            for (int i = 0; i < dashed.Count; i++)
            {
                _dashed.Add(dashed[i]);
            }

            SetVerticesDirty();
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
            ApplyToMaterials();
        }

        public void SetTrimDistance(float meters)
        {
            _trimDistance = meters;
            ApplyToMaterials();
        }

        public void SetCanvasUnitsPerMeter(float value)
        {
            _canvasUnitsPerMeter = value;
            ApplyToMaterials();
        }

        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            Material modified = base.GetModifiedMaterial(baseMaterial);
            if (_materialInstance != null && modified != null)
            {
                ApplyMaterialProperties(modified);
            }

            return modified;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            MeshBuildCount++;

            if (_materialInstance == null)
            {
                return;
            }

            _meshBuilder.Build(_points, _distances, _dashed, _vertices, _indices);
            if (_vertices.Count == 0)
            {
                return;
            }

            vh.AddUIVertexStream(_vertices, _indices);
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            if (isActiveAndEnabled)
            {
                EnsureShaderChannels();
                _hasCanvasOffsetMatrix = false;
            }
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (isActiveAndEnabled)
            {
                EnsureShaderChannels();
                _hasCanvasOffsetMatrix = false;
            }
        }

        private void ApplyDefaultsOnce()
        {
            if (defaultsApplied)
            {
                return;
            }

            raycastTarget = false;
            defaultsApplied = true;
        }

        private void EnsureMaterial()
        {
            if (_materialInstance != null)
            {
                return;
            }

            Shader shaderToUse = lineShader;
            if (shaderToUse == null)
            {
                shaderToUse = Shader.Find(ShaderName);
            }

            if (shaderToUse == null)
            {
                CustomLogger.LogError("RouteLineGraphic: shader " + ShaderName + " was not found. The route line will not be drawn.", this);
                return;
            }

            _materialInstance = new Material(shaderToUse);
            _materialInstance.name = "RouteLine (Instance)";
            _materialInstance.hideFlags = HideFlags.HideAndDontSave;
            ApplyMaterialProperties(_materialInstance);
            material = _materialInstance;
        }

        private void ApplyMaterialProperties(Material target)
        {
            target.SetColor(ColorProperty, _lineColor);
            target.SetColor(OutlineColorProperty, _outlineColor);
            target.SetColor(FadedColorProperty, _fadedColor);
            target.SetFloat(HalfWidthProperty, _halfWidth);
            target.SetFloat(OutlineWidthProperty, _outlineWidth);
            target.SetFloat(CanvasUnitsPerMeterProperty, _canvasUnitsPerMeter);
            target.SetFloat(TrimDistanceProperty, _trimDistance);
            target.SetFloat(TrimModeProperty, _trimMode);
            target.SetFloat(DashLengthProperty, _dashLength);
            target.SetFloat(GapLengthProperty, _gapLength);
            target.SetVector(CanvasOffsetMatrixProperty, _canvasOffsetMatrix);
        }

        private void EnsureShaderChannels()
        {
            Canvas batchCanvas = canvas;
            if (batchCanvas == null)
            {
                return;
            }

            AddRequiredShaderChannels(batchCanvas);

            Canvas root = batchCanvas.rootCanvas;
            if (root != null && root != batchCanvas)
            {
                AddRequiredShaderChannels(root);
            }
        }

        private void AddRequiredShaderChannels(Canvas target)
        {
            AdditionalCanvasShaderChannels current = target.additionalShaderChannels;
            if ((current & RequiredShaderChannels) == RequiredShaderChannels)
            {
                return;
            }

            target.additionalShaderChannels = current | RequiredShaderChannels;
        }

        private void HandleWillRenderCanvases()
        {
            UpdateRouteLineVisuals(Time.unscaledDeltaTime);
        }

        private bool TryComputeCanvasOffsetMatrix(Canvas batchCanvas, out Vector4 offsetMatrix)
        {
            offsetMatrix = new Vector4(1f, 0f, 0f, 1f);

            Matrix4x4 graphicToCanvas = batchCanvas.transform.worldToLocalMatrix * transform.localToWorldMatrix;
            Vector2 column0 = new Vector2(graphicToCanvas.m00, graphicToCanvas.m10);
            Vector2 column1 = new Vector2(graphicToCanvas.m01, graphicToCanvas.m11);
            float length0 = column0.magnitude;
            float length1 = column1.magnitude;
            if (length0 < MinScale || length1 < MinScale)
            {
                return false;
            }

            float rootToBatchFactor = 1f;
            Canvas root = batchCanvas.rootCanvas;
            if (root != null && root != batchCanvas)
            {
                float batchScale = Mathf.Abs(batchCanvas.transform.lossyScale.x);
                float rootScale = Mathf.Abs(root.transform.lossyScale.x);
                if (batchScale < MinScale)
                {
                    return false;
                }

                rootToBatchFactor = rootScale / batchScale;
            }

            column0 = column0 / length0 * rootToBatchFactor;
            column1 = column1 / length1 * rootToBatchFactor;
            offsetMatrix = new Vector4(column0.x, column1.x, column0.y, column1.y);
            return true;
        }

        private void ApplyCanvasOffsetMatrixToMaterials()
        {
            _materialInstance.SetVector(CanvasOffsetMatrixProperty, _canvasOffsetMatrix);

            Material renderMaterial = materialForRendering;
            if (renderMaterial != null && renderMaterial != _materialInstance)
            {
                renderMaterial.SetVector(CanvasOffsetMatrixProperty, _canvasOffsetMatrix);
            }
        }

        private void ApplyToMaterials()
        {
            if (_materialInstance == null)
            {
                return;
            }

            ApplyMaterialProperties(_materialInstance);

            if (!isActiveAndEnabled)
            {
                return;
            }

            Material renderMaterial = materialForRendering;
            if (renderMaterial != null && renderMaterial != _materialInstance)
            {
                ApplyMaterialProperties(renderMaterial);
            }
        }

        private void DestroyMaterialInstance()
        {
            if (_materialInstance == null)
            {
                return;
            }

            if (material == _materialInstance)
            {
                material = null;
            }

            if (Application.isPlaying)
            {
                Destroy(_materialInstance);
            }
            else
            {
                DestroyImmediate(_materialInstance);
            }

            _materialInstance = null;
        }

        protected override void OnDisable()
        {
            Canvas.willRenderCanvases -= HandleWillRenderCanvases;
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            DestroyMaterialInstance();
            base.OnDestroy();
        }
    }
}

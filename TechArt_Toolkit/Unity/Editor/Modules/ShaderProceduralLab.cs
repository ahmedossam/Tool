using UnityEngine;
using UnityEditor;
using System;

namespace TechArtToolkit.Editor.Modules
{
    /// <summary>
    /// MODULE 1: Shader & Procedural Lab
    ///
    /// WHAT:  Real-time editor for procedural shader parameters.
    ///        Controls noise type/scale, UV manipulation, SDF shapes,
    ///        and trigonometric animation — all updating a live preview mesh.
    ///
    /// WHY:   Demonstrates procedural shading fundamentals — the ability to
    ///        generate surface detail mathematically without texture artists.
    ///        Core Technical Artist skill: bridging art and GPU code.
    ///
    /// HOW:   Uses PreviewRenderUtility for an isolated preview scene.
    ///        MaterialPropertyBlock pushes parameter changes to the shader
    ///        without creating new material instances.
    ///        Shader: ProceduralNoiseLab.shader (URP Unlit + custom HLSL)
    /// </summary>
    public partial class ShaderProceduralLab : ModuleBase
    {
        // ─────────────────────────────────────────────────────────────────────
        // Identity
        // ─────────────────────────────────────────────────────────────────────

        public override string ModuleName        => "Shader & Procedural Lab";
        public override string ModuleDescription => "Real-time control of noise, UVs, SDF shapes, and trig functions. Demonstrates procedural shading fundamentals.";
        public override string ModuleIcon        => "d_ShaderGraph Icon";

        // ─────────────────────────────────────────────────────────────────────
        // Enums
        // ─────────────────────────────────────────────────────────────────────

        private enum NoiseType    { FBM = 0, Voronoi = 1, Perlin = 2, Simplex = 3, Value = 4 }
        private enum SDFShape     { Circle = 0, Box = 1, Ring = 2, Cross = 3, None = 4 }
        private enum PreviewMesh  { Sphere = 0, Plane = 1, Cube = 2, Cylinder = 3 }
        private enum ColorMode    { TwoColor = 0, Gradient = 1, HSV = 2 }

        // ─────────────────────────────────────────────────────────────────────
        // Shader Parameter State
        // ─────────────────────────────────────────────────────────────────────

        // Noise
        private NoiseType _noiseType      = NoiseType.FBM;
        private float     _noiseScale     = 3.0f;
        private int       _noiseOctaves   = 4;
        private float     _noisePersist   = 0.5f;
        private float     _noiseLacunarity= 2.0f;
        private float     _noiseContrast  = 1.0f;

        // UV
        private Vector2   _uvTiling       = Vector2.one;
        private Vector2   _uvOffset       = Vector2.zero;
        private float     _uvRotation     = 0f;
        private bool      _animateUV      = false;
        private float     _uvAnimSpeed    = 0.5f;

        // SDF
        private SDFShape  _sdfShape       = SDFShape.Circle;
        private float     _sdfRadius      = 0.35f;
        private float     _sdfSoftness    = 0.05f;
        private float     _sdfBlend       = 1.0f;
        private Vector2   _sdfCenter      = new Vector2(0.5f, 0.5f);

        // Trig / Animation
        private float     _trigFrequency  = 2.0f;
        private float     _trigAmplitude  = 0.1f;
        private float     _trigPhase      = 0f;
        private bool      _animateTrig    = false;

        // Color
        private ColorMode _colorMode      = ColorMode.TwoColor;
        private Color     _colorA         = new Color(0.05f, 0.05f, 0.15f);
        private Color     _colorB         = new Color(0.2f, 0.6f, 1.0f);
        private float     _colorContrast  = 1.0f;
        private float     _colorBrightness= 0.0f;

        // Preview
        private PreviewMesh _previewMesh  = PreviewMesh.Sphere;
        private bool        _showWireframe= false;

        // ─────────────────────────────────────────────────────────────────────
        // Preview Rendering
        // ─────────────────────────────────────────────────────────────────────

        private PreviewRenderUtility _previewRenderer;
        private Material             _previewMaterial;
        private MaterialPropertyBlock _propertyBlock;
        private Mesh[]               _previewMeshes;
        private Texture2D            _previewTexture;
        private Rect                 _previewRect;

        // Shader property IDs (cached for performance)
        private static readonly int ID_NoiseType      = Shader.PropertyToID("_NoiseType");
        private static readonly int ID_NoiseScale     = Shader.PropertyToID("_NoiseScale");
        private static readonly int ID_NoiseOctaves   = Shader.PropertyToID("_NoiseOctaves");
        private static readonly int ID_NoisePersist   = Shader.PropertyToID("_NoisePersistence");
        private static readonly int ID_NoiseLacunarity= Shader.PropertyToID("_NoiseLacunarity");
        private static readonly int ID_NoiseContrast  = Shader.PropertyToID("_NoiseContrast");
        private static readonly int ID_UVTiling       = Shader.PropertyToID("_UVTiling");
        private static readonly int ID_UVOffset       = Shader.PropertyToID("_UVOffset");
        private static readonly int ID_UVRotation     = Shader.PropertyToID("_UVRotation");
        private static readonly int ID_SDFShape       = Shader.PropertyToID("_SDFShape");
        private static readonly int ID_SDFRadius      = Shader.PropertyToID("_SDFRadius");
        private static readonly int ID_SDFSoftness    = Shader.PropertyToID("_SDFSoftness");
        private static readonly int ID_SDFBlend       = Shader.PropertyToID("_SDFBlend");
        private static readonly int ID_SDFCenter      = Shader.PropertyToID("_SDFCenter");
        private static readonly int ID_TrigFrequency  = Shader.PropertyToID("_TrigFrequency");
        private static readonly int ID_TrigAmplitude  = Shader.PropertyToID("_TrigAmplitude");
        private static readonly int ID_TrigPhase      = Shader.PropertyToID("_TrigPhase");
        private static readonly int ID_ColorA         = Shader.PropertyToID("_ColorA");
        private static readonly int ID_ColorB         = Shader.PropertyToID("_ColorB");
        private static readonly int ID_ColorContrast  = Shader.PropertyToID("_ColorContrast");
        private static readonly int ID_ColorBrightness= Shader.PropertyToID("_ColorBrightness");
        private static readonly int ID_ColorMode      = Shader.PropertyToID("_ColorMode");

        // ─────────────────────────────────────────────────────────────────────
        // Foldout State
        // ─────────────────────────────────────────────────────────────────────

        private bool _foldNoise   = true;
        private bool _foldUV      = true;
        private bool _foldSDF     = true;
        private bool _foldTrig    = false;
        private bool _foldColor   = true;
        private bool _foldPreview = true;

        // ─────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        public override void OnEnable(EditorWindow parentWindow)
        {
            base.OnEnable(parentWindow);
            InitializePreview();
            EditorApplication.update += OnUpdate;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            EditorApplication.update -= OnUpdate;
        }

        public override void OnDestroy()
        {
            CleanupPreview();
        }

        private void OnUpdate()
        {
            bool needsRepaint = false;

            // Animate UV offset
            if (_animateUV)
            {
                _uvOffset.x += _uvAnimSpeed * 0.01f * (float)EditorApplication.timeSinceStartup * 0.016f;
                _uvOffset.y += _uvAnimSpeed * 0.005f * (float)EditorApplication.timeSinceStartup * 0.016f;
                needsRepaint = true;
            }

            // Animate trig phase
            if (_animateTrig)
            {
                _trigPhase = (float)(EditorApplication.timeSinceStartup * _trigFrequency) % (Mathf.PI * 2f);
                needsRepaint = true;
            }

            if (needsRepaint)
            {
                UpdateMaterialProperties();
                RequestRepaint();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Preview Setup
        // ─────────────────────────────────────────────────────────────────────

        private void InitializePreview()
        {
            _previewRenderer = new PreviewRenderUtility();
            _previewRenderer.camera.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            _previewRenderer.camera.clearFlags = CameraClearFlags.SolidColor;
            _previewRenderer.camera.transform.position = new Vector3(0, 0, -3f);
            _previewRenderer.camera.transform.LookAt(Vector3.zero);
            _previewRenderer.camera.nearClipPlane = 0.1f;
            _previewRenderer.camera.farClipPlane = 100f;

            // Add a preview light
            _previewRenderer.lights[0].intensity = 1.2f;
            _previewRenderer.lights[0].transform.rotation = Quaternion.Euler(30f, 30f, 0f);

            // Load or create preview material
            var shader = Shader.Find("TechArtToolkit/ProceduralNoiseLab");
            if (shader == null)
            {
                // Fallback to standard unlit if custom shader not found
                shader = Shader.Find("Unlit/Color");
                DrawWarningBox("ProceduralNoiseLab shader not found. Using fallback.");
            }
            _previewMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _propertyBlock = new MaterialPropertyBlock();

            // Cache preview meshes
            _previewMeshes = new Mesh[]
            {
                Resources.GetBuiltinResource<Mesh>("Sphere.fbx"),
                Resources.GetBuiltinResource<Mesh>("Plane.fbx"),
                Resources.GetBuiltinResource<Mesh>("Cube.fbx"),
                Resources.GetBuiltinResource<Mesh>("Cylinder.fbx")
            };

            UpdateMaterialProperties();
        }

        private void CleanupPreview()
        {
            _previewRenderer?.Cleanup();
            _previewRenderer = null;

            if (_previewMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(_previewMaterial);
                _previewMaterial = null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Material Property Update
        // ─────────────────────────────────────────────────────────────────────

        private void UpdateMaterialProperties()
        {
            if (_previewMaterial == null || _propertyBlock == null) return;

            _propertyBlock.SetFloat(ID_NoiseType,       (float)_noiseType);
            _propertyBlock.SetFloat(ID_NoiseScale,      _noiseScale);
            _propertyBlock.SetFloat(ID_NoiseOctaves,    _noiseOctaves);
            _propertyBlock.SetFloat(ID_NoisePersist,    _noisePersist);
            _propertyBlock.SetFloat(ID_NoiseLacunarity, _noiseLacunarity);
            _propertyBlock.SetFloat(ID_NoiseContrast,   _noiseContrast);
            _propertyBlock.SetVector(ID_UVTiling,       new Vector4(_uvTiling.x, _uvTiling.y, 0, 0));
            _propertyBlock.SetVector(ID_UVOffset,       new Vector4(_uvOffset.x, _uvOffset.y, 0, 0));
            _propertyBlock.SetFloat(ID_UVRotation,      _uvRotation * Mathf.Deg2Rad);
            _propertyBlock.SetFloat(ID_SDFShape,        (float)_sdfShape);
            _propertyBlock.SetFloat(ID_SDFRadius,       _sdfRadius);
            _propertyBlock.SetFloat(ID_SDFSoftness,     _sdfSoftness);
            _propertyBlock.SetFloat(ID_SDFBlend,        _sdfBlend);
            _propertyBlock.SetVector(ID_SDFCenter,      new Vector4(_sdfCenter.x, _sdfCenter.y, 0, 0));
            _propertyBlock.SetFloat(ID_TrigFrequency,   _trigFrequency);
            _propertyBlock.SetFloat(ID_TrigAmplitude,   _trigAmplitude);
            _propertyBlock.SetFloat(ID_TrigPhase,       _trigPhase);
            _propertyBlock.SetColor(ID_ColorA,          _colorA);
            _propertyBlock.SetColor(ID_ColorB,          _colorB);
            _propertyBlock.SetFloat(ID_ColorContrast,   _colorContrast);
            _propertyBlock.SetFloat(ID_ColorBrightness, _colorBrightness);
            _propertyBlock.SetFloat(ID_ColorMode,       (float)_colorMode);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Main GUI
        // ─────────────────────────────────────────────────────────────────────

        public override void DrawGUI()
        {
            DrawHeader();

            DrawInfoBox(
                "Adjust parameters below to control the procedural shader in real-time. " +
                "The preview updates live. Demonstrates: noise math, UV manipulation, " +
                "SDF shapes, and trigonometric animation.");

            using (new EditorGUILayout.HorizontalScope())
            {
                // ── Left Column: Controls ────────────────────────────────────
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(300)))
                {
                    DrawNoiseSection();
                    DrawUVSection();
                    DrawSDFSection();
                    DrawTrigSection();
                    DrawColorSection();
                }

                EditorGUILayout.Space(8);

                // ── Right Column: Preview ────────────────────────────────────
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawPreviewSection();
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Noise
        // ─────────────────────────────────────────────────────────────────────

        private void DrawNoiseSection()
        {
            _foldNoise = EditorGUILayout.BeginFoldoutHeaderGroup(_foldNoise, "🌊  Noise Parameters");
            if (_foldNoise)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUI.BeginChangeCheck();

                    _noiseType = (NoiseType)EditorGUILayout.EnumPopup(
                        new GUIContent("Noise Type",
                            "FBM: Fractal Brownian Motion (layered)\n" +
                            "Voronoi: Cellular/Worley noise\n" +
                            "Perlin: Classic gradient noise\n" +
                            "Simplex: Faster gradient noise\n" +
                            "Value: Simple interpolated noise"),
                        _noiseType);

                    _noiseScale = EditorGUILayout.Slider(
                        new GUIContent("Scale", "Controls the frequency/zoom of the noise pattern"),
                        _noiseScale, 0.1f, 20f);

                    if (_noiseType == NoiseType.FBM)
                    {
                        _noiseOctaves = EditorGUILayout.IntSlider(
                            new GUIContent("Octaves", "Number of noise layers stacked (FBM only)"),
                            _noiseOctaves, 1, 8);

                        _noisePersist = EditorGUILayout.Slider(
                            new GUIContent("Persistence", "Amplitude falloff per octave (0.5 = halved each layer)"),
                            _noisePersist, 0.1f, 1.0f);

                        _noiseLacunarity = EditorGUILayout.Slider(
                            new GUIContent("Lacunarity", "Frequency multiplier per octave (2.0 = doubled each layer)"),
                            _noiseLacunarity, 1.0f, 4.0f);
                    }

                    _noiseContrast = EditorGUILayout.Slider(
                        new GUIContent("Contrast", "Sharpens or softens the noise output"),
                        _noiseContrast, 0.1f, 5.0f);

                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateMaterialProperties();
                        RequestRepaint();
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: UV
        // ─────────────────────────────────────────────────────────────────────

        private void DrawUVSection()
        {
            _foldUV = EditorGUILayout.BeginFoldoutHeaderGroup(_foldUV, "🔲  UV Controls");
            if (_foldUV)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUI.BeginChangeCheck();

                    _uvTiling = EditorGUILayout.Vector2Field(
                        new GUIContent("Tiling", "UV scale multiplier (X, Y)"),
                        _uvTiling);

                    _uvOffset = EditorGUILayout.Vector2Field(
                        new GUIContent("Offset", "UV translation (X, Y)"),
                        _uvOffset);

                    _uvRotation = EditorGUILayout.Slider(
                        new GUIContent("Rotation", "UV rotation in degrees"),
                        _uvRotation, -180f, 180f);

                    EditorGUILayout.Space(4);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _animateUV = EditorGUILayout.Toggle(
                            new GUIContent("Animate UV", "Scrolls UV offset over time"),
                            _animateUV);

                        if (_animateUV)
                        {
                            _uvAnimSpeed = EditorGUILayout.Slider("Speed", _uvAnimSpeed, 0.01f, 5f);
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateMaterialProperties();
                        RequestRepaint();
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: SDF Shapes
        // ─────────────────────────────────────────────────────────────────────

        private void DrawSDFSection()
        {
            _foldSDF = EditorGUILayout.BeginFoldoutHeaderGroup(_foldSDF, "⬤  SDF Shapes");
            if (_foldSDF)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUI.BeginChangeCheck();

                    _sdfShape = (SDFShape)EditorGUILayout.EnumPopup(
                        new GUIContent("Shape",
                            "Signed Distance Field shape used as a mask.\n" +
                            "Circle: sdCircle(uv, radius)\n" +
                            "Box: sdBox(uv, halfExtents)\n" +
                            "Ring: sdCircle - sdCircle (hollow)\n" +
                            "Cross: sdBox union sdBox (rotated)\n" +
                            "None: No SDF mask applied"),
                        _sdfShape);

                    if (_sdfShape != SDFShape.None)
                    {
                        _sdfRadius = EditorGUILayout.Slider(
                            new GUIContent("Radius / Size", "Controls the size of the SDF shape"),
                            _sdfRadius, 0.01f, 0.9f);

                        _sdfSoftness = EditorGUILayout.Slider(
                            new GUIContent("Edge Softness", "Smoothstep range for anti-aliased edges"),
                            _sdfSoftness, 0.001f, 0.3f);

                        _sdfBlend = EditorGUILayout.Slider(
                            new GUIContent("Blend Strength", "How strongly the SDF masks the noise"),
                            _sdfBlend, 0f, 1f);

                        _sdfCenter = EditorGUILayout.Vector2Field(
                            new GUIContent("Center (UV)", "Center position of the SDF shape in UV space"),
                            _sdfCenter);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateMaterialProperties();
                        RequestRepaint();
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Trigonometry
        // ─────────────────────────────────────────────────────────────────────

        private void DrawTrigSection()
        {
            _foldTrig = EditorGUILayout.BeginFoldoutHeaderGroup(_foldTrig, "〜  Trig Animation");
            if (_foldTrig)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUI.BeginChangeCheck();

                    _trigFrequency = EditorGUILayout.Slider(
                        new GUIContent("Frequency", "sin(uv * frequency + phase) — controls wave density"),
                        _trigFrequency, 0.1f, 20f);

                    _trigAmplitude = EditorGUILayout.Slider(
                        new GUIContent("Amplitude", "Strength of the wave distortion on UV"),
                        _trigAmplitude, 0f, 0.5f);

                    _trigPhase = EditorGUILayout.Slider(
                        new GUIContent("Phase", "Phase offset of the wave (manual control)"),
                        _trigPhase, 0f, Mathf.PI * 2f);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _animateTrig = EditorGUILayout.Toggle(
                            new GUIContent("Animate Phase", "Drives phase with editor time"),
                            _animateTrig);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateMaterialProperties();
                        RequestRepaint();
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Color
        // ─────────────────────────────────────────────────────────────────────

        private void DrawColorSection()
        {
            _foldColor = EditorGUILayout.BeginFoldoutHeaderGroup(_foldColor, "🎨  Color Mapping");
            if (_foldColor)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUI.BeginChangeCheck();

                    _colorMode = (ColorMode)EditorGUILayout.EnumPopup(
                        new GUIContent("Color Mode",
                            "TwoColor: lerp(ColorA, ColorB, noise)\n" +
                            "Gradient: Uses a gradient texture\n" +
                            "HSV: Maps noise to hue rotation"),
                        _colorMode);

                    _colorA = EditorGUILayout.ColorField(
                        new GUIContent("Color A (Dark)", "Color at noise value 0"),
                        _colorA);

                    _colorB = EditorGUILayout.ColorField(
                        new GUIContent("Color B (Bright)", "Color at noise value 1"),
                        _colorB);

                    _colorContrast = EditorGUILayout.Slider(
                        new GUIContent("Contrast", "pow(noise, contrast) — sharpens color transitions"),
                        _colorContrast, 0.1f, 5f);

                    _colorBrightness = EditorGUILayout.Slider(
                        new GUIContent("Brightness", "Additive brightness offset"),
                        _colorBrightness, -1f, 1f);

                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateMaterialProperties();
                        RequestRepaint();
                    }

                    if (DrawResetButton())
                        ResetToDefaults();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Preview
        // ─────────────────────────────────────────────────────────────────────

        private void DrawPreviewSection()
        {
            _foldPreview = EditorGUILayout.BeginFoldoutHeaderGroup(_foldPreview, "👁  Live Preview");
            if (_foldPreview)
            {
                // Mesh selector
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Preview Mesh:", GUILayout.Width(100));
                    _previewMesh = (PreviewMesh)EditorGUILayout.EnumPopup(_previewMesh, GUILayout.Width(100));
                    _showWireframe = EditorGUILayout.ToggleLeft("Wireframe", _showWireframe, GUILayout.Width(80));
                }

                EditorGUILayout.Space(4);

                // Preview render area
                _previewRect = GUILayoutUtility.GetRect(280, 280,
                    GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));

                if (Event.current.type == EventType.Repaint && _previewRenderer != null)
                {
                    RenderPreview(_previewRect);
                }

                // Shader info panel
                EditorGUILayout.Space(6);
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUILayout.LabelField("Shader Info", SubHeaderStyle);
                    DrawMetricRow("Noise Type",    _noiseType.ToString());
                    DrawMetricRow("SDF Shape",     _sdfShape.ToString());
                    DrawMetricRow("UV Tiling",     $"({_uvTiling.x:F2}, {_uvTiling.y:F2})");
                    DrawMetricRow("Trig Freq",     $"{_trigFrequency:F1} Hz");
                    DrawMetricRow("Animated",      (_animateUV || _animateTrig) ? "Yes" : "No",
                        (_animateUV || _animateTrig) ? MetricStatus.Good : MetricStatus.Neutral);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Preview Rendering
        // ─────────────────────────────────────────────────────────────────────

        private void RenderPreview(Rect rect)
        {
            if (_previewRenderer == null || _previewMaterial == null) return;
            if (_previewMeshes == null || (int)_previewMesh >= _previewMeshes.Length) return;

            _previewRenderer.BeginPreview(rect, GUIStyle.none);

            // Rotate mesh slowly for visual interest
            float angle = (float)(EditorApplication.timeSinceStartup * 20.0) % 360f;
            Matrix4x4 matrix = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.Euler(15f, angle, 0f),
                Vector3.one * 1.5f);

            _previewRenderer.DrawMesh(
                _previewMeshes[(int)_previewMesh],
                matrix,
                _previewMaterial,
                0,
                _propertyBlock);

            _previewRenderer.camera.Render();

            var previewTexture = _previewRenderer.EndPreview();
            GUI.DrawTexture(rect, previewTexture, ScaleMode.StretchToFill, false);

            // Border
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), Color.gray);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), Color.gray);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), Color.gray);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), Color.gray);
        }

        // ResetToDefaults() and DrawBudgetBar() are implemented in
        // ShaderProceduralLab.Helpers.cs (partial class).
    }
}

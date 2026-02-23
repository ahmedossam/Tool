using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

namespace TechArtToolkit.Editor.Modules
{
    /// <summary>
    /// MODULE 3: Lighting & LookDev Tool
    ///
    /// WHAT:  A lighting control panel that adjusts HDRI/sky settings, sun
    ///        intensity/color/angle, shadow quality, and post-processing
    ///        parameters. Includes preset lighting environments and a PBR
    ///        material validation panel.
    ///
    /// WHY:   Demonstrates lighting and look development skills — understanding
    ///        how physically-based lighting interacts with PBR materials, and
    ///        how to set up consistent, art-directable lighting environments.
    ///
    /// HOW:   Uses SerializedObject to modify URP Volume components.
    ///        Directly sets Light component properties.
    ///        Preset system stores/restores full lighting configurations.
    ///        PBR validation spawns a metallic/roughness matrix of spheres.
    /// </summary>
    public partial class LightingLookDevTool : ModuleBase
    {
        // ─────────────────────────────────────────────────────────────────────
        // Identity
        // ─────────────────────────────────────────────────────────────────────

        public override string ModuleName        => "Lighting & LookDev Tool";
        public override string ModuleDescription => "Controls HDRI/sky, exposure, shadow quality, and post-processing. Validates PBR materials under different lighting conditions.";
        public override string ModuleIcon        => "d_Light Icon";

        // ─────────────────────────────────────────────────────────────────────
        // Scene References
        // ─────────────────────────────────────────────────────────────────────

        private Light            _sunLight;
        private ReflectionProbe  _reflectionProbe;
        private Volume           _postProcessVolume;
        private Material         _skyboxMaterial;

        // ─────────────────────────────────────────────────────────────────────
        // Lighting Parameters
        // ─────────────────────────────────────────────────────────────────────

        // Sun / Directional Light
        private float   _sunIntensity    = 1.0f;
        private Color   _sunColor        = Color.white;
        private float   _sunAzimuth      = 45f;
        private float   _sunElevation    = 45f;
        private bool    _castShadows     = true;

        // Sky / HDRI
        private float   _skyExposure     = 0f;       // EV offset
        private float   _hdriRotation    = 0f;
        private Color   _ambientColor    = new Color(0.2f, 0.2f, 0.25f);
        private float   _ambientIntensity= 1.0f;

        // Shadows
        private float   _shadowDistance  = 150f;
        private int     _shadowCascades  = 4;
        private float   _shadowBias      = 0.05f;
        private float   _shadowNormalBias= 0.4f;
        private ShadowResolution _shadowResolution = ShadowResolution.High;

        // Post-Processing (URP Volume overrides)
        private float   _bloomIntensity  = 0.5f;
        private float   _bloomThreshold  = 0.9f;
        private float   _vignetteIntensity = 0.3f;
        private float   _vignetteRoundness = 1.0f;
        private float   _exposureBias    = 0f;
        private float   _colorTemp       = 6500f;
        private float   _colorTint       = 0f;
        private float   _saturation      = 0f;
        private float   _contrast        = 0f;

        // ─────────────────────────────────────────────────────────────────────
        // Preset System
        // ─────────────────────────────────────────────────────────────────────

        private enum LightingPreset
        {
            NeutralGrey,
            StudioThreePoint,
            OutdoorDay,
            OutdoorGoldenHour,
            OutdoorNight,
            Overcast
        }

        private LightingPreset _selectedPreset = LightingPreset.OutdoorDay;

        private struct LightingPresetData
        {
            public string  Name;
            public float   SunIntensity;
            public Color   SunColor;
            public float   SunAzimuth;
            public float   SunElevation;
            public float   SkyExposure;
            public Color   AmbientColor;
            public float   AmbientIntensity;
            public float   BloomIntensity;
            public float   ExposureBias;
            public float   ColorTemp;
            public string  Description;
        }

        private static readonly Dictionary<LightingPreset, LightingPresetData> PRESETS =
            new Dictionary<LightingPreset, LightingPresetData>
        {
            {
                LightingPreset.NeutralGrey, new LightingPresetData
                {
                    Name = "Neutral Grey",
                    SunIntensity = 0f,
                    SunColor = Color.white,
                    SunAzimuth = 45f, SunElevation = 45f,
                    SkyExposure = 0f,
                    AmbientColor = new Color(0.5f, 0.5f, 0.5f),
                    AmbientIntensity = 1.0f,
                    BloomIntensity = 0f,
                    ExposureBias = 0f,
                    ColorTemp = 6500f,
                    Description = "Flat neutral grey — ideal for PBR material validation. No directional bias."
                }
            },
            {
                LightingPreset.StudioThreePoint, new LightingPresetData
                {
                    Name = "Studio 3-Point",
                    SunIntensity = 2.5f,
                    SunColor = new Color(1.0f, 0.97f, 0.9f),
                    SunAzimuth = 30f, SunElevation = 60f,
                    SkyExposure = -1f,
                    AmbientColor = new Color(0.15f, 0.15f, 0.2f),
                    AmbientIntensity = 0.5f,
                    BloomIntensity = 0.2f,
                    ExposureBias = 0f,
                    ColorTemp = 5600f,
                    Description = "Classic 3-point studio lighting. Key, fill, and rim lights. Good for character/prop review."
                }
            },
            {
                LightingPreset.OutdoorDay, new LightingPresetData
                {
                    Name = "Outdoor Day",
                    SunIntensity = 3.14f,
                    SunColor = new Color(1.0f, 0.98f, 0.92f),
                    SunAzimuth = 135f, SunElevation = 55f,
                    SkyExposure = 0f,
                    AmbientColor = new Color(0.3f, 0.4f, 0.6f),
                    AmbientIntensity = 1.2f,
                    BloomIntensity = 0.5f,
                    ExposureBias = 0f,
                    ColorTemp = 6500f,
                    Description = "Midday outdoor sun. High intensity, blue sky ambient. Tests PBR under natural light."
                }
            },
            {
                LightingPreset.OutdoorGoldenHour, new LightingPresetData
                {
                    Name = "Golden Hour",
                    SunIntensity = 1.5f,
                    SunColor = new Color(1.0f, 0.7f, 0.3f),
                    SunAzimuth = 270f, SunElevation = 8f,
                    SkyExposure = -0.5f,
                    AmbientColor = new Color(0.4f, 0.25f, 0.15f),
                    AmbientIntensity = 0.8f,
                    BloomIntensity = 0.8f,
                    ExposureBias = -0.5f,
                    ColorTemp = 3200f,
                    Description = "Warm sunset/sunrise. Low sun angle, orange tones. Tests specular highlights on metals."
                }
            },
            {
                LightingPreset.OutdoorNight, new LightingPresetData
                {
                    Name = "Outdoor Night",
                    SunIntensity = 0.05f,
                    SunColor = new Color(0.6f, 0.7f, 1.0f),
                    SunAzimuth = 180f, SunElevation = -10f,
                    SkyExposure = -3f,
                    AmbientColor = new Color(0.05f, 0.05f, 0.1f),
                    AmbientIntensity = 0.3f,
                    BloomIntensity = 1.0f,
                    ExposureBias = -2f,
                    ColorTemp = 8000f,
                    Description = "Moonlit night. Very low exposure, cool blue tones. Tests emissive materials."
                }
            },
            {
                LightingPreset.Overcast, new LightingPresetData
                {
                    Name = "Overcast",
                    SunIntensity = 0.5f,
                    SunColor = new Color(0.9f, 0.9f, 0.95f),
                    SunAzimuth = 90f, SunElevation = 70f,
                    SkyExposure = -0.5f,
                    AmbientColor = new Color(0.4f, 0.42f, 0.45f),
                    AmbientIntensity = 1.5f,
                    BloomIntensity = 0.1f,
                    ExposureBias = 0.5f,
                    ColorTemp = 7500f,
                    Description = "Overcast sky. Soft diffuse light, no harsh shadows. Good for testing roughness values."
                }
            }
        };

        // ─────────────────────────────────────────────────────────────────────
        // PBR Validation
        // ─────────────────────────────────────────────────────────────────────

        private bool _showPBRGrid = false;
        private List<GameObject> _pbrSpheres = new List<GameObject>();
        private const int PBR_GRID_SIZE = 5; // 5x5 grid

        // ─────────────────────────────────────────────────────────────────────
        // UI State
        // ─────────────────────────────────────────────────────────────────────

        private bool _foldSceneRefs   = true;
        private bool _foldSun         = true;
        private bool _foldSky         = false;
        private bool _foldShadows     = false;
        private bool _foldPostProcess = true;
        private bool _foldPresets     = true;
        private bool _foldPBR         = false;
        private Vector2 _scrollPos;

        // ─────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        public override void OnEnable(EditorWindow parentWindow)
        {
            base.OnEnable(parentWindow);
            AutoDetectSceneReferences();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnDestroy()
        {
            ClearPBRGrid();
        }

        private void AutoDetectSceneReferences()
        {
            // Try to find scene objects automatically
            if (_sunLight == null)
            {
                var lights = Object.FindObjectsOfType<Light>();
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        _sunLight = l;
                        break;
                    }
                }
            }

            if (_reflectionProbe == null)
                _reflectionProbe = Object.FindObjectOfType<ReflectionProbe>();

            if (_postProcessVolume == null)
                _postProcessVolume = Object.FindObjectOfType<Volume>();

            if (_skyboxMaterial == null)
                _skyboxMaterial = RenderSettings.skybox;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Main GUI
        // ─────────────────────────────────────────────────────────────────────

        public override void DrawGUI()
        {
            DrawHeader();

            DrawInfoBox(
                "Assign scene references or use Auto-Detect. Adjust lighting parameters " +
                "and apply presets to see how different lighting conditions affect PBR materials. " +
                "Use the PBR Validation Grid to verify material correctness.");

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawSceneReferencesSection();
            DrawPresetsSection();
            DrawSunSection();
            DrawSkySection();
            DrawShadowSection();
            DrawPostProcessSection();
            DrawPBRValidationSection();

            EditorGUILayout.EndScrollView();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Scene References
        // ─────────────────────────────────────────────────────────────────────

        private void DrawSceneReferencesSection()
        {
            _foldSceneRefs = EditorGUILayout.BeginFoldoutHeaderGroup(_foldSceneRefs, "🔗  Scene References");
            if (_foldSceneRefs)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    _sunLight = (Light)EditorGUILayout.ObjectField(
                        new GUIContent("Directional Light", "The main sun/directional light in the scene"),
                        _sunLight, typeof(Light), true);

                    _reflectionProbe = (ReflectionProbe)EditorGUILayout.ObjectField(
                        new GUIContent("Reflection Probe", "Scene reflection probe for IBL"),
                        _reflectionProbe, typeof(ReflectionProbe), true);

                    _postProcessVolume = (Volume)EditorGUILayout.ObjectField(
                        new GUIContent("Post-Process Volume", "URP Global Volume with post-processing overrides"),
                        _postProcessVolume, typeof(Volume), true);

                    _skyboxMaterial = (Material)EditorGUILayout.ObjectField(
                        new GUIContent("Skybox Material", "Scene skybox material (HDRI or procedural)"),
                        _skyboxMaterial, typeof(Material), false);

                    EditorGUILayout.Space(4);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("🔍 Auto-Detect Scene Objects", GUILayout.Height(26)))
                        {
                            AutoDetectSceneReferences();
                            RequestRepaint();
                        }
                    }

                    // Status indicators
                    DrawReferenceStatus("Sun Light",        _sunLight != null);
                    DrawReferenceStatus("Reflection Probe", _reflectionProbe != null);
                    DrawReferenceStatus("Post-Process Vol", _postProcessVolume != null);
                    DrawReferenceStatus("Skybox Material",  _skyboxMaterial != null);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawReferenceStatus(string label, bool isAssigned)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                string icon = isAssigned ? "✓" : "○";
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = isAssigned ? new Color(0.3f, 0.8f, 0.3f) : Color.gray }
                };
                EditorGUILayout.LabelField($"{icon} {label}", style);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Presets
        // ─────────────────────────────────────────────────────────────────────

        private void DrawPresetsSection()
        {
            _foldPresets = EditorGUILayout.BeginFoldoutHeaderGroup(_foldPresets, "🎬  Lighting Presets");
            if (_foldPresets)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    // Preset buttons in a grid
                    int col = 0;
                    EditorGUILayout.BeginHorizontal();

                    foreach (LightingPreset preset in System.Enum.GetValues(typeof(LightingPreset)))
                    {
                        bool isActive = (_selectedPreset == preset);
                        Color origBg = GUI.backgroundColor;
                        GUI.backgroundColor = isActive
                            ? new Color(0.3f, 0.6f, 1.0f)
                            : new Color(0.35f, 0.35f, 0.35f);

                        if (GUILayout.Button(PRESETS[preset].Name,
                            GUILayout.Height(28), GUILayout.MinWidth(100)))
                        {
                            _selectedPreset = preset;
                            ApplyPreset(preset);
                        }

                        GUI.backgroundColor = origBg;

                        col++;
                        if (col >= 3)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            col = 0;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // Preset description
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField(PRESETS[_selectedPreset].Description,
                        new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                        { normal = { textColor = new Color(0.7f, 0.7f, 0.5f) } });
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Sun / Directional Light
        // ─────────────────────────────────────────────────────────────────────

        private void DrawSunSection()
        {
            _foldSun = EditorGUILayout.BeginFoldoutHeaderGroup(_foldSun, "☀  Sun / Directional Light");
            if (_foldSun)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    if (_sunLight == null)
                    {
                        DrawWarningBox("No Directional Light assigned. Assign one in Scene References.");
                    }
                    else
                    {
                        EditorGUI.BeginChangeCheck();

                        _sunIntensity = EditorGUILayout.Slider(
                            new GUIContent("Intensity", "Sun light intensity in lux (0 = off, 3.14 = physically correct noon)"),
                            _sunIntensity, 0f, 10f);

                        _sunColor = EditorGUILayout.ColorField(
                            new GUIContent("Color", "Sun light color. Use warm tones for sunset, cool for overcast."),
                            _sunColor);

                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Direction", SubHeaderStyle);

                        _sunAzimuth = EditorGUILayout.Slider(
                            new GUIContent("Azimuth", "Horizontal rotation of the sun (0° = North, 90° = East)"),
                            _sunAzimuth, 0f, 360f);

                        _sunElevation = EditorGUILayout.Slider(
                            new GUIContent("Elevation", "Vertical angle of the sun (0° = horizon, 90° = zenith)"),
                            _sunElevation, -10f, 90f);

                        _castShadows = EditorGUILayout.Toggle(
                            new GUIContent("Cast Shadows", "Enable/disable shadow casting from this light"),
                            _castShadows);

                        if (EditorGUI.EndChangeCheck())
                            ApplySunSettings();
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Sky / Ambient
        // ─────────────────────────────────────────────────────────────────────

        private void DrawSkySection()
        {
            _foldSky = EditorGUILayout.BeginFoldoutHeaderGroup(_foldSky, "🌤  Sky & Ambient");
            if (_foldSky)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUI.BeginChangeCheck();

                    _skyExposure = EditorGUILayout.Slider(
                        new GUIContent("Sky Exposure (EV)", "Exposure value offset for the skybox (-4 = very dark, +4 = very bright)"),
                        _skyExposure, -4f, 4f);

                    _hdriRotation = EditorGUILayout.Slider(
                        new GUIContent("HDRI Rotation", "Rotates the HDRI/skybox around the Y axis"),
                        _hdriRotation, 0f, 360f);

                    EditorGUILayout.Space(4);

                    _ambientColor = EditorGUILayout.ColorField(
                        new GUIContent("Ambient Color", "Scene ambient/sky color for indirect lighting"),
                        _ambientColor);

                    _ambientIntensity = EditorGUILayout.Slider(
                        new GUIContent("Ambient Intensity", "Multiplier for ambient light contribution"),
                        _ambientIntensity, 0f, 3f);

                    if (EditorGUI.EndChangeCheck())
                        ApplySkySettings();

                    EditorGUILayout.Space(4);
                    if (GUILayout.Button("🔄 Rebake Reflection Probe", GUILayout.Height(24)))
                    {
                        if (_reflectionProbe != null)
                        {
                            _reflectionProbe.RenderProbe();
                            ((TechArtToolkitWindow)_parentWindow)?.SetStatus("Reflection probe rebaked.");
                        }
                        else
                        {
                            DrawWarningBox("No Reflection Probe assigned.");
                        }
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Shadows
        // ─────────────────────────────────────────────────────────────────────

        private void DrawShadowSection()
        {
            _foldShadows = EditorGUILayout.BeginFoldoutHeaderGroup(_foldShadows, "🌑  Shadow Settings");
            if (_foldShadows)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUI.BeginChangeCheck();

                    _shadowDistance = EditorGUILayout.Slider(
                        new GUIContent("Shadow Distance", "Maximum distance at which shadows are rendered (meters)"),
                        _shadowDistance, 10f, 500f);

                    _shadowCascades = EditorGUILayout.IntSlider(
                        new GUIContent("Shadow Cascades", "Number of CSM (Cascaded Shadow Map) splits. More = better quality, higher cost."),
                        _shadowCascades, 1, 4);

                    _shadowResolution = (ShadowResolution)EditorGUILayout.EnumPopup(
                        new GUIContent("Shadow Resolution", "Shadow map resolution. Higher = sharper shadows, more VRAM."),
                        _shadowResolution);

                    _shadowBias = EditorGUILayout.Slider(
                        new GUIContent("Shadow Bias", "Depth bias to prevent shadow acne. Too high = Peter-panning."),
                        _shadowBias, 0f, 0.5f);

                    _shadowNormalBias = EditorGUILayout.Slider(
                        new GUIContent("Normal Bias", "Normal offset bias. Reduces self-shadowing on curved surfaces."),
                        _shadowNormalBias, 0f, 1f);

                    if (EditorGUI.EndChangeCheck())
                        ApplyShadowSettings();

                    // Shadow quality info
                    EditorGUILayout.Space(4);
                    int shadowMapSize = _shadowResolution == ShadowResolution.Low    ? 512  :
                                        _shadowResolution == ShadowResolution.Medium ? 1024 :
                                        _shadowResolution == ShadowResolution.High   ? 2048 : 4096;
                    long shadowMemory = (long)shadowMapSize * shadowMapSize * 4 * _shadowCascades;
                    DrawMetricRow("Shadow Map Memory", FormatBytes(shadowMemory),
                        EvaluateBudget(shadowMemory, 8 * 1024 * 1024, 32 * 1024 * 1024));
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Post-Processing
        // ─────────────────────────────────────────────────────────────────────

        private void DrawPostProcessSection()
        {
            _foldPostProcess = EditorGUILayout.BeginFoldoutHeaderGroup(_foldPostProcess, "✨  Post-Processing");
            if (_foldPostProcess)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    if (_postProcessVolume == null)
                    {
                        DrawWarningBox("No Post-Process Volume assigned. Assign one in Scene References.");
                    }
                    else
                    {
                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.LabelField("Exposure", SubHeaderStyle);
                        _exposureBias = EditorGUILayout.Slider(
                            new GUIContent("Exposure Bias (EV)", "Manual exposure compensation in EV stops"),
                            _exposureBias, -4f, 4f);

                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Bloom", SubHeaderStyle);
                        _bloomIntensity = EditorGUILayout.Slider(
                            new GUIContent("Bloom Intensity", "Strength of the bloom glow effect"),
                            _bloomIntensity, 0f, 2f);
                        _bloomThreshold = EditorGUILayout.Slider(
                            new GUIContent("Bloom Threshold", "Luminance threshold above which bloom is applied"),
                            _bloomThreshold, 0f, 2f);

                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Color Grading", SubHeaderStyle);
                        _colorTemp = EditorGUILayout.Slider(
                            new GUIContent("Color Temperature (K)", "White balance. 6500K = neutral, lower = warm, higher = cool."),
                            _colorTemp, 1500f, 20000f);
                        _colorTint = EditorGUILayout.Slider(
                            new GUIContent("Color Tint", "Green/Magenta tint offset"),
                            _colorTint, -100f, 100f);
                        _saturation = EditorGUILayout.Slider(
                            new GUIContent("Saturation", "Color saturation (-100 = greyscale, 0 = neutral, +100 = vivid)"),
                            _saturation, -100f, 100f);
                        _contrast = EditorGUILayout.Slider(
                            new GUIContent("Contrast", "Tonal contrast (-100 = flat, 0 = neutral, +100 = high contrast)"),
                            _contrast, -100f, 100f);

                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Vignette", SubHeaderStyle);
                        _vignetteIntensity = EditorGUILayout.Slider(
                            new GUIContent("Vignette Intensity", "Darkening at screen edges"),
                            _vignetteIntensity, 0f, 1f);
                        _vignetteRoundness = EditorGUILayout.Slider(
                            new GUIContent("Vignette Roundness", "Shape of the vignette (0 = square, 1 = round)"),
                            _vignetteRoundness, 0f, 1f);

                        if (EditorGUI.EndChangeCheck())
                            ApplyPostProcessSettings();

                        if (DrawResetButton())
                            ResetPostProcessToDefaults();
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: PBR Validation Grid
        // ─────────────────────────────────────────────────────────────────────

        private void DrawPBRValidationSection()
        {
            _foldPBR = EditorGUILayout.BeginFoldoutHeaderGroup(_foldPBR, "🔬  PBR Material Validation");
            if (_foldPBR)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUILayout.LabelField(
                        "Spawns a 5×5 grid of spheres with varying Metallic (X axis) and " +
                        "Roughness (Y axis) values. Use this to validate PBR materials look " +
                        "correct under the current lighting setup.",
                        new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                        { normal = { textColor = new Color(0.7f, 0.7f, 0.5f) } });

                    EditorGUILayout.Space(4);

                    // PBR correctness rules
                    EditorGUILayout.LabelField("PBR Correctness Rules:", SubHeaderStyle);
                    DrawPBRRule("Dielectric albedo should be 50–240 sRGB (0.2–0.94 linear)", true);
                    DrawPBRRule("Metal albedo = tinted reflectance (no pure black/white)", true);
                    DrawPBRRule("Metallic map: 0 = dielectric, 1 = metal (no in-between)", true);
                    DrawPBRRule("Roughness 0 = mirror, 1 = fully diffuse", true);

                    EditorGUILayout.Space(6);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (DrawActionButton(_showPBRGrid ? "🗑  Clear PBR Grid" : "⬤  Spawn PBR Grid", 180, 28))
                        {
                            if (_showPBRGrid) ClearPBRGrid();
                            else             SpawnPBRGrid();
                        }
                    }

                    if (_showPBRGrid)
                    {
                        EditorGUILayout.LabelField(
                            $"✓ {PBR_GRID_SIZE}×{PBR_GRID_SIZE} spheres active  " +
                            "(X axis = Metallic 0→1,  Z axis = Roughness 0→1)",
                            new GUIStyle(EditorStyles.miniLabel)
                            { normal = { textColor = new Color(0.3f, 0.8f, 0.3f) } });
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ApplyPreset(), ApplySunSettings(), ApplySkySettings(), ApplyShadowSettings(),
        // ApplyPostProcessSettings(), SpawnPBRGrid(), ClearPBRGrid(), DrawPBRRule(),
        // ResetPostProcessToDefaults(), and DrawBudgetBar() are in
        // LightingLookDevTool.Helpers.cs (partial class).
    }
}

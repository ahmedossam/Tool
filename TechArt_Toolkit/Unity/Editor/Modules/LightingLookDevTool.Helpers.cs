// LightingLookDevTool.Helpers.cs
// Partial class — contains Apply* methods, PBR grid, preset logic, and helpers.

using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

namespace TechArtToolkit.Editor.Modules
{
    public partial class LightingLookDevTool
    {
        // ─────────────────────────────────────────────────────────────────────
        // Apply: Sun Settings
        // ─────────────────────────────────────────────────────────────────────

        private void ApplySunSettings()
        {
            if (_sunLight == null) return;

            Undo.RecordObject(_sunLight, "TAT: Apply Sun Settings");

            _sunLight.intensity  = _sunIntensity;
            _sunLight.color      = _sunColor;
            _sunLight.shadows    = _castShadows ? LightShadows.Soft : LightShadows.None;

            // Convert azimuth + elevation to rotation
            _sunLight.transform.rotation = Quaternion.Euler(
                -_sunElevation,
                _sunAzimuth,
                0f);

            SceneView.RepaintAll();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Apply: Sky / Ambient Settings
        // ─────────────────────────────────────────────────────────────────────

        private void ApplySkySettings()
        {
            // Ambient color and intensity
            Undo.RecordObject(new UnityEngine.Object[] { }, "TAT: Apply Sky Settings");

            RenderSettings.ambientLight     = _ambientColor;
            RenderSettings.ambientIntensity = _ambientIntensity;

            // HDRI rotation via skybox material
            if (_skyboxMaterial != null)
            {
                if (_skyboxMaterial.HasProperty("_Rotation"))
                    _skyboxMaterial.SetFloat("_Rotation", _hdriRotation);
                if (_skyboxMaterial.HasProperty("_Exposure"))
                    _skyboxMaterial.SetFloat("_Exposure", Mathf.Pow(2f, _skyExposure));
            }

            DynamicGI.UpdateEnvironment();
            SceneView.RepaintAll();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Apply: Shadow Settings
        // ─────────────────────────────────────────────────────────────────────

        private void ApplyShadowSettings()
        {
            QualitySettings.shadowDistance = _shadowDistance;
            QualitySettings.shadowCascades = _shadowCascades;

            if (_sunLight != null)
            {
                Undo.RecordObject(_sunLight, "TAT: Apply Shadow Settings");
                _sunLight.shadowBias       = _shadowBias;
                _sunLight.shadowNormalBias = _shadowNormalBias;

                // Map ShadowResolution enum to Unity's LightShadowResolution
                _sunLight.shadowResolution = _shadowResolution switch
                {
                    ShadowResolution.Low    => UnityEngine.Rendering.LightShadowResolution.Low,
                    ShadowResolution.Medium => UnityEngine.Rendering.LightShadowResolution.Medium,
                    ShadowResolution.High   => UnityEngine.Rendering.LightShadowResolution.High,
                    _                       => UnityEngine.Rendering.LightShadowResolution.VeryHigh
                };
            }

            SceneView.RepaintAll();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Apply: Post-Process Settings
        // ─────────────────────────────────────────────────────────────────────

        private void ApplyPostProcessSettings()
        {
            if (_postProcessVolume == null) return;

            var profile = _postProcessVolume.sharedProfile;
            if (profile == null) return;

            Undo.RecordObject(profile, "TAT: Apply Post-Process Settings");

            // Bloom
            if (profile.TryGet<Bloom>(out var bloom))
            {
                bloom.intensity.Override(_bloomIntensity);
                bloom.threshold.Override(_bloomThreshold);
            }

            // Color Adjustments
            if (profile.TryGet<ColorAdjustments>(out var colorAdj))
            {
                colorAdj.postExposure.Override(_exposureBias);
                colorAdj.saturation.Override(_saturation);
                colorAdj.contrast.Override(_contrast);
            }

            // White Balance
            if (profile.TryGet<WhiteBalance>(out var wb))
            {
                wb.temperature.Override(_colorTemp - 6500f); // offset from neutral
                wb.tint.Override(_colorTint);
            }

            // Vignette
            if (profile.TryGet<Vignette>(out var vignette))
            {
                vignette.intensity.Override(_vignetteIntensity);
                vignette.roundness.Override(_vignetteRoundness);
            }

            SceneView.RepaintAll();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Apply: Lighting Preset
        // ─────────────────────────────────────────────────────────────────────

        private void ApplyPreset(LightingPreset preset)
        {
            if (!PRESETS.TryGetValue(preset, out var data)) return;

            _sunIntensity    = data.SunIntensity;
            _sunColor        = data.SunColor;
            _sunAzimuth      = data.SunAzimuth;
            _sunElevation    = data.SunElevation;
            _skyExposure     = data.SkyExposure;
            _ambientColor    = data.AmbientColor;
            _ambientIntensity= data.AmbientIntensity;
            _bloomIntensity  = data.BloomIntensity;
            _exposureBias    = data.ExposureBias;
            _colorTemp       = data.ColorTemp;

            ApplySunSettings();
            ApplySkySettings();
            ApplyPostProcessSettings();

            ((TechArtToolkitWindow)_parentWindow)?.SetStatus($"Preset applied: {data.Name}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Reset Post-Process to Defaults
        // ─────────────────────────────────────────────────────────────────────

        private void ResetPostProcessToDefaults()
        {
            _bloomIntensity   = 0.5f;
            _bloomThreshold   = 0.9f;
            _vignetteIntensity= 0.3f;
            _vignetteRoundness= 1.0f;
            _exposureBias     = 0f;
            _colorTemp        = 6500f;
            _colorTint        = 0f;
            _saturation       = 0f;
            _contrast         = 0f;
            ApplyPostProcessSettings();
        }

        // ─────────────────────────────────────────────────────────────────────
        // PBR Validation Grid
        // ─────────────────────────────────────────────────────────────────────

        private void SpawnPBRGrid()
        {
            ClearPBRGrid();

            var parent = new GameObject("[TAT] PBR Validation Grid");
            Undo.RegisterCreatedObjectUndo(parent, "Spawn PBR Grid");

            for (int m = 0; m < PBR_GRID_SIZE; m++)
            {
                for (int r = 0; r < PBR_GRID_SIZE; r++)
                {
                    float metallic  = m / (float)(PBR_GRID_SIZE - 1);
                    float roughness = r / (float)(PBR_GRID_SIZE - 1);

                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.name = $"PBR_M{metallic:F2}_R{roughness:F2}";
                    go.transform.parent   = parent.transform;
                    go.transform.position = new Vector3(m * 1.2f, 0f, r * 1.2f);
                    go.transform.localScale = Vector3.one * 0.9f;

                    // Create PBR material
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    mat.SetFloat("_Metallic",  metallic);
                    mat.SetFloat("_Smoothness", 1f - roughness);
                    mat.color = new Color(0.8f, 0.8f, 0.8f); // neutral grey albedo

                    go.GetComponent<Renderer>().sharedMaterial = mat;
                    Undo.RegisterCreatedObjectUndo(go, "Spawn PBR Sphere");
                    _pbrSpheres.Add(go);
                }
            }

            _showPBRGrid = true;
            ((TechArtToolkitWindow)_parentWindow)?.SetStatus(
                $"PBR Grid spawned: {PBR_GRID_SIZE}×{PBR_GRID_SIZE} spheres " +
                "(X = Metallic 0→1, Z = Roughness 0→1)");
        }

        private void ClearPBRGrid()
        {
            foreach (var sphere in _pbrSpheres)
            {
                if (sphere != null)
                    Undo.DestroyObjectImmediate(sphere);
            }
            _pbrSpheres.Clear();

            // Also destroy the parent container if it exists
            var parent = GameObject.Find("[TAT] PBR Validation Grid");
            if (parent != null)
                Undo.DestroyObjectImmediate(parent);

            _showPBRGrid = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PBR Rule Display Helper
        // ─────────────────────────────────────────────────────────────────────

        private void DrawPBRRule(string text, bool isValid)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                string icon = isValid ? "✓" : "✗";
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = isValid ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.9f, 0.3f, 0.3f) }
                };
                EditorGUILayout.LabelField($"{icon} {text}", style);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Budget Bar
        // ─────────────────────────────────────────────────────────────────────

        protected void DrawBudgetBar(string label, float value, float max)
        {
            float t = max > 0 ? Mathf.Clamp01(value / max) : 0f;
            Color barColor = t <= 0.5f ? new Color(0.2f, 0.8f, 0.2f) :
                             t <= 0.8f ? new Color(0.9f, 0.7f, 0.1f) :
                                         new Color(0.9f, 0.2f, 0.2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(160));
                Rect barRect = EditorGUILayout.GetControlRect(false,
                    EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f));
                EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width * t, barRect.height), barColor);
                EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width, 1), Color.gray);
                EditorGUI.DrawRect(new Rect(barRect.x, barRect.yMax - 1, barRect.width, 1), Color.gray);
                EditorGUI.LabelField(barRect, $"  {value:N0} / {max:N0}  ({t * 100f:F0}%)",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } });
            }
        }
    }
}

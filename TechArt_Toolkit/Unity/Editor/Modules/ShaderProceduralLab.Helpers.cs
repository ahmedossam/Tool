// ShaderProceduralLab.Helpers.cs
// Partial class — contains Reset logic and utility methods.
// Split from ShaderProceduralLab.cs using C# partial class pattern.

using UnityEngine;
using UnityEditor;

namespace TechArtToolkit.Editor.Modules
{
    public partial class ShaderProceduralLab
    {
        // ─────────────────────────────────────────────────────────────────────
        // Reset to Defaults
        // ─────────────────────────────────────────────────────────────────────

        private void ResetToDefaults()
        {
            _noiseType        = NoiseType.FBM;
            _noiseScale       = 3.0f;
            _noiseOctaves     = 4;
            _noisePersist     = 0.5f;
            _noiseLacunarity  = 2.0f;
            _noiseContrast    = 1.0f;

            _uvTiling         = Vector2.one;
            _uvOffset         = Vector2.zero;
            _uvRotation       = 0f;
            _animateUV        = false;
            _uvAnimSpeed      = 0.5f;

            _sdfShape         = SDFShape.Circle;
            _sdfRadius        = 0.35f;
            _sdfSoftness      = 0.05f;
            _sdfBlend         = 1.0f;
            _sdfCenter        = new Vector2(0.5f, 0.5f);

            _trigFrequency    = 2.0f;
            _trigAmplitude    = 0.1f;
            _trigPhase        = 0f;
            _animateTrig      = false;

            _colorMode        = ColorMode.TwoColor;
            _colorA           = new Color(0.05f, 0.05f, 0.15f);
            _colorB           = new Color(0.2f, 0.6f, 1.0f);
            _colorContrast    = 1.0f;
            _colorBrightness  = 0.0f;

            _previewMesh      = PreviewMesh.Sphere;
            _showWireframe    = false;

            UpdateMaterialProperties();
            RequestRepaint();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Budget Bar Helper (shared visual element)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws a horizontal progress bar showing value vs max budget.
        /// Color: green ≤ 50%, yellow ≤ 80%, red > 80%.
        /// </summary>
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

                // Background
                EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f));

                // Fill
                Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * t, barRect.height);
                EditorGUI.DrawRect(fillRect, barColor);

                // Border
                EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width, 1), Color.gray);
                EditorGUI.DrawRect(new Rect(barRect.x, barRect.yMax - 1, barRect.width, 1), Color.gray);

                // Label
                EditorGUI.LabelField(barRect,
                    $"  {value:N0} / {max:N0}  ({t * 100f:F0}%)",
                    new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = Color.white } });
            }
        }
    }
}

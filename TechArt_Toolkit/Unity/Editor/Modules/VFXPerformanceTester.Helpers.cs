// VFXPerformanceTester.Helpers.cs
// Partial class — contains cleanup, delta computation, and budget bar helpers.

using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;

namespace TechArtToolkit.Editor.Modules
{
    public partial class VFXPerformanceTester
    {
        // ─────────────────────────────────────────────────────────────────────
        // Cleanup
        // ─────────────────────────────────────────────────────────────────────

        private void CleanupSpawnedObjects()
        {
            if (_spawnedOptimized != null)
            {
                Undo.DestroyObjectImmediate(_spawnedOptimized);
                _spawnedOptimized = null;
            }

            if (_spawnedUnoptimized != null)
            {
                Undo.DestroyObjectImmediate(_spawnedUnoptimized);
                _spawnedUnoptimized = null;
            }

            _activeVFXComponent = null;
            _fpsHistory.Clear();
            _particleHistory.Clear();

            _currentMetrics = default;
            RequestRepaint();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Delta Computation
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes a formatted delta string between optimized (a) and
        /// unoptimized (b) values.
        /// higherIsBetter: true for FPS, false for particle count / frame time.
        /// </summary>
        private string ComputeDelta(float a, float b, bool higherIsBetter)
        {
            if (a <= 0 || b <= 0) return "—";

            float delta = a - b;
            float pct   = b > 0 ? (delta / b) * 100f : 0f;

            string sign = delta >= 0 ? "+" : "";
            return $"{sign}{pct:F0}%";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Budget Bar
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws a horizontal progress bar showing value vs max budget.
        /// Green ≤ 50%, Yellow ≤ 80%, Red > 80%.
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

                EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f));

                Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * t, barRect.height);
                EditorGUI.DrawRect(fillRect, barColor);

                EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width, 1), Color.gray);
                EditorGUI.DrawRect(new Rect(barRect.x, barRect.yMax - 1, barRect.width, 1), Color.gray);

                EditorGUI.LabelField(barRect,
                    $"  {value:N0} / {max:N0}  ({t * 100f:F0}%)",
                    new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = Color.white } });
            }
        }
    }
}

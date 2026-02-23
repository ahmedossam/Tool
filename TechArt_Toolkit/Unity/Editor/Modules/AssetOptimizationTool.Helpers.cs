// AssetOptimizationTool.Helpers.cs
// Partial class — contains analysis logic, drawing helpers, and comparison utilities.

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace TechArtToolkit.Editor.Modules
{
    public partial class AssetOptimizationTool
    {
        // ─────────────────────────────────────────────────────────────────────
        // Mesh Analysis
        // ─────────────────────────────────────────────────────────────────────

        private MeshAnalysis AnalyzeMesh(GameObject prefab)
        {
            var result = new MeshAnalysis { IsValid = false };
            if (prefab == null) return result;

            result.IsValid   = true;
            result.AssetName = prefab.name;

            var meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
            var renderers   = prefab.GetComponentsInChildren<Renderer>(true);

            int  totalTris     = 0;
            int  totalVerts    = 0;
            int  totalSubMeshes= 0;
            long totalMemory   = 0;
            bool hasReadWrite  = false;

            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                var mesh = mf.sharedMesh;

                totalTris      += mesh.triangles.Length / 3;
                totalVerts     += mesh.vertexCount;
                totalSubMeshes += mesh.subMeshCount;
                totalMemory    += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(mesh);

                if (mesh.isReadable) hasReadWrite = true;
            }

            result.TotalTriangles = totalTris;
            result.TotalVertices  = totalVerts;
            result.SubMeshCount   = totalSubMeshes;
            result.HasReadWrite   = hasReadWrite;
            result.MeshMemoryBytes= totalMemory;

            // Count unique materials
            var mats = new HashSet<Material>();
            foreach (var r in renderers)
                foreach (var m in r.sharedMaterials)
                    if (m != null) mats.Add(m);
            result.MaterialCount = mats.Count;

            // LOD Group
            var lodGroup = prefab.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                result.HasLODGroup = true;
                var lods = lodGroup.GetLODs();
                result.LODCount = lods.Length;
                result.LODTriCounts = new int[lods.Length];
                result.LODScreenPercents = new float[lods.Length];

                for (int i = 0; i < lods.Length; i++)
                {
                    result.LODScreenPercents[i] = lods[i].screenRelativeTransitionHeight;
                    int lodTris = 0;
                    foreach (var r in lods[i].renderers)
                    {
                        if (r is MeshRenderer mr)
                        {
                            var mf = mr.GetComponent<MeshFilter>();
                            if (mf?.sharedMesh != null)
                                lodTris += mf.sharedMesh.triangles.Length / 3;
                        }
                    }
                    result.LODTriCounts[i] = lodTris;
                }
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Texture Analysis
        // ─────────────────────────────────────────────────────────────────────

        private TextureAnalysis AnalyzeTexture(Texture2D texture)
        {
            var result = new TextureAnalysis { IsValid = false };
            if (texture == null) return result;

            result.IsValid   = true;
            result.AssetName = texture.name;
            result.Width     = texture.width;
            result.Height    = texture.height;
            result.MipCount  = texture.mipmapCount;
            result.HasMips   = texture.mipmapCount > 1;
            result.IsReadable= texture.isReadable;
            result.Format    = texture.format.ToString();

            // Memory estimates
            result.MemoryBytes  = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
            result.StorageBytes = UnityEditor.EditorUtility.GetStorageMemorySize(texture);

            // Import settings
            string assetPath = AssetDatabase.GetAssetPath(texture);
            if (!string.IsNullOrEmpty(assetPath))
            {
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    result.IsSRGB             = importer.sRGBTexture;
                    result.TextureType        = importer.textureType.ToString();
                    result.CompressionQuality = importer.textureCompression.ToString();
                }
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Drawing Helpers
        // ─────────────────────────────────────────────────────────────────────

        private void DrawAnalysisColumnHeaders()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var headerStyle = new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 10, normal = { textColor = new Color(0.8f, 0.8f, 0.8f) } };

                EditorGUILayout.LabelField("Property",  headerStyle, GUILayout.Width(160));
                EditorGUILayout.LabelField("Slot A  (Unoptimized)", new GUIStyle(headerStyle)
                { normal = { textColor = new Color(0.9f, 0.4f, 0.4f) } }, GUILayout.Width(160));
                EditorGUILayout.LabelField("Slot B  (Optimized)", new GUIStyle(headerStyle)
                { normal = { textColor = new Color(0.4f, 0.9f, 0.4f) } }, GUILayout.Width(160));
            }
        }

        private void DrawMeshAnalysisRow(string label, string valueA, string valueB,
            int numA = -1, int numB = -1, int budget = -1)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(160));

                // Value A — color code against budget
                GUIStyle styleA = EditorStyles.label;
                if (numA >= 0 && budget > 0)
                {
                    MetricStatus s = EvaluateBudget(numA, budget * 0.5f, budget);
                    styleA = s == MetricStatus.Good    ? GoodValueStyle :
                             s == MetricStatus.Warning ? WarningValueStyle : BadValueStyle;
                }
                EditorGUILayout.LabelField(valueA, styleA, GUILayout.Width(160));

                // Value B — color code against budget
                GUIStyle styleB = EditorStyles.label;
                if (numB >= 0 && budget > 0)
                {
                    MetricStatus s = EvaluateBudget(numB, budget * 0.5f, budget);
                    styleB = s == MetricStatus.Good    ? GoodValueStyle :
                             s == MetricStatus.Warning ? WarningValueStyle : BadValueStyle;
                }
                EditorGUILayout.LabelField(valueB, styleB, GUILayout.Width(160));
            }
        }

        private void DrawLODBreakdown(MeshAnalysis analysis)
        {
            if (analysis.LODTriCounts == null) return;

            using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
            {
                // Header
                using (new EditorGUILayout.HorizontalScope())
                {
                    var h = new GUIStyle(EditorStyles.boldLabel) { fontSize = 10 };
                    EditorGUILayout.LabelField("LOD",        h, GUILayout.Width(40));
                    EditorGUILayout.LabelField("Triangles",  h, GUILayout.Width(100));
                    EditorGUILayout.LabelField("Screen %",   h, GUILayout.Width(80));
                    EditorGUILayout.LabelField("Reduction",  h, GUILayout.Width(80));
                }

                int lod0Tris = analysis.LODTriCounts.Length > 0 ? analysis.LODTriCounts[0] : 1;

                for (int i = 0; i < analysis.LODTriCounts.Length; i++)
                {
                    int   tris      = analysis.LODTriCounts[i];
                    float screenPct = analysis.LODScreenPercents != null
                        ? analysis.LODScreenPercents[i] * 100f : 0f;
                    float reduction = lod0Tris > 0 ? (1f - tris / (float)lod0Tris) * 100f : 0f;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"LOD{i}", GUILayout.Width(40));
                        EditorGUILayout.LabelField(FormatTriCount(tris), GUILayout.Width(100));
                        EditorGUILayout.LabelField($"{screenPct:F1}%", GUILayout.Width(80));

                        var reductStyle = i == 0 ? EditorStyles.label : GoodValueStyle;
                        EditorGUILayout.LabelField(
                            i == 0 ? "—" : $"-{reduction:F0}%",
                            reductStyle, GUILayout.Width(80));
                    }
                }
            }
        }

        private void DrawSavingsRow(string label, string value, string pct, bool isGood)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(180));
                EditorGUILayout.LabelField(value, GUILayout.Width(120));
                EditorGUILayout.LabelField(pct,
                    isGood ? GoodValueStyle : BadValueStyle,
                    GUILayout.Width(100));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Delta Computation
        // ─────────────────────────────────────────────────────────────────────

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

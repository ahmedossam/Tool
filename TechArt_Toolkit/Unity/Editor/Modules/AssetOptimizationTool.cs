using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace TechArtToolkit.Editor.Modules
{
    /// <summary>
    /// MODULE 4: Asset Optimization Tool
    ///
    /// WHAT:  Analyzes selected meshes and textures, displaying LOD levels,
    ///        triangle counts, texture resolutions, memory usage, and draw call
    ///        estimates. Allows side-by-side comparison of optimized vs
    ///        unoptimized assets to demonstrate the impact of proper budgeting.
    ///
    /// WHY:   Demonstrates asset pipeline and optimization skills — understanding
    ///        how mesh complexity, texture resolution, and draw call count affect
    ///        runtime performance, and knowing how to set appropriate budgets.
    ///
    /// HOW:   Uses MeshUtility, LODGroup, and TextureImporter APIs.
    ///        EditorUtility.GetStorageMemorySize() for texture memory.
    ///        Mesh.triangles.Length / 3 for triangle counts.
    ///        Two-column comparison layout with color-coded deltas.
    /// </summary>
    public partial class AssetOptimizationTool : ModuleBase
    {
        // ─────────────────────────────────────────────────────────────────────
        // Identity
        // ─────────────────────────────────────────────────────────────────────

        public override string ModuleName        => "Asset Optimization Tool";
        public override string ModuleDescription => "Analyzes mesh LODs, triangle counts, texture memory, and draw calls. Compare optimized vs unoptimized assets side-by-side.";
        public override string ModuleIcon        => "d_Mesh Icon";

        // ─────────────────────────────────────────────────────────────────────
        // Asset References
        // ─────────────────────────────────────────────────────────────────────

        // Slot A (left column — "Before" / Unoptimized)
        private GameObject  _meshA;
        private Texture2D   _textureA;

        // Slot B (right column — "After" / Optimized)
        private GameObject  _meshB;
        private Texture2D   _textureB;

        // ─────────────────────────────────────────────────────────────────────
        // Analysis Results
        // ─────────────────────────────────────────────────────────────────────

        private struct MeshAnalysis
        {
            public string   AssetName;
            public int      TotalTriangles;
            public int      TotalVertices;
            public int      SubMeshCount;
            public int      LODCount;
            public int[]    LODTriCounts;
            public float[]  LODScreenPercents;
            public int      BoneCount;
            public bool     HasReadWrite;
            public bool     HasLODGroup;
            public int      MaterialCount;
            public long     MeshMemoryBytes;
            public bool     IsValid;
        }

        private struct TextureAnalysis
        {
            public string   AssetName;
            public int      Width;
            public int      Height;
            public string   Format;
            public int      MipCount;
            public long     MemoryBytes;
            public long     StorageBytes;
            public bool     HasMips;
            public bool     IsSRGB;
            public bool     IsReadable;
            public string   CompressionQuality;
            public string   TextureType;
            public bool     IsValid;
        }

        private MeshAnalysis    _analysisA;
        private MeshAnalysis    _analysisB;
        private TextureAnalysis _texAnalysisA;
        private TextureAnalysis _texAnalysisB;

        private bool _meshAnalyzed    = false;
        private bool _textureAnalyzed = false;

        // ─────────────────────────────────────────────────────────────────────
        // Platform Budgets
        // ─────────────────────────────────────────────────────────────────────

        private enum TargetPlatform { PC_High, PC_Mid, Console, Mobile }
        private TargetPlatform _targetPlatform = TargetPlatform.Console;

        private static readonly Dictionary<TargetPlatform, (int trisBudget, long texMemBudget, int drawCallBudget)>
            PLATFORM_BUDGETS = new Dictionary<TargetPlatform, (int, long, int)>
        {
            { TargetPlatform.PC_High,  (100_000, 8L * 1024 * 1024,  500) },
            { TargetPlatform.PC_Mid,   (50_000,  4L * 1024 * 1024,  300) },
            { TargetPlatform.Console,  (30_000,  2L * 1024 * 1024,  200) },
            { TargetPlatform.Mobile,   (10_000,  512L * 1024,        100) },
        };

        // ─────────────────────────────────────────────────────────────────────
        // UI State
        // ─────────────────────────────────────────────────────────────────────

        private bool _foldMeshInput    = true;
        private bool _foldMeshAnalysis = true;
        private bool _foldTexInput     = true;
        private bool _foldTexAnalysis  = true;
        private bool _foldComparison   = true;
        private bool _foldBudgets      = true;
        private bool _foldTips         = false;
        private Vector2 _scrollPos;

        // ─────────────────────────────────────────────────────────────────────
        // Optimization Tips
        // ─────────────────────────────────────────────────────────────────────

        private static readonly string[] MESH_TIPS = new string[]
        {
            "💡 LOD0 should be the hero mesh. LOD1 at 50%, LOD2 at 25%, LOD3 at 10% of LOD0 tris.",
            "💡 Merge meshes that always appear together to reduce draw calls.",
            "💡 Remove interior faces — players never see inside solid objects.",
            "💡 Use GPU instancing for repeated objects (rocks, trees, props).",
            "💡 Disable Read/Write on meshes that don't need CPU access — saves 50% memory.",
            "💡 Use 16-bit index buffers for meshes with <65535 vertices.",
            "💡 Avoid sub-mesh counts > 1 per LOD — each sub-mesh = 1 draw call.",
        };

        private static readonly string[] TEXTURE_TIPS = new string[]
        {
            "💡 Use power-of-two textures (256, 512, 1024, 2048, 4096) for GPU compatibility.",
            "💡 Always enable mipmaps for world-space textures — prevents aliasing at distance.",
            "💡 Use BC7 for high-quality color textures, BC5 for normal maps on PC.",
            "💡 Use ASTC 6x6 for mobile — good quality/size balance.",
            "💡 Pack Metallic (R), AO (G), Roughness (B) into one texture to save samples.",
            "💡 Normal maps should NOT be sRGB — uncheck 'sRGB' in import settings.",
            "💡 Mask/data textures should use Linear color space (no sRGB).",
        };

        private int _meshTipIndex = 0;
        private int _texTipIndex  = 0;

        // ─────────────────────────────────────────────────────────────────────
        // Main GUI
        // ─────────────────────────────────────────────────────────────────────

        public override void DrawGUI()
        {
            DrawHeader();

            DrawInfoBox(
                "Assign assets to Slot A (unoptimized) and Slot B (optimized). " +
                "Click Analyze to inspect LODs, triangle counts, texture memory, and draw calls. " +
                "The comparison table shows the optimization delta.");

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawPlatformBudgetSelector();
            DrawMeshInputSection();
            DrawMeshAnalysisSection();
            DrawTextureInputSection();
            DrawTextureAnalysisSection();
            DrawComparisonSection();
            DrawOptimizationTipsSection();

            EditorGUILayout.EndScrollView();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Platform Budget Selector
        // ─────────────────────────────────────────────────────────────────────

        private void DrawPlatformBudgetSelector()
        {
            using (new EditorGUILayout.HorizontalScope(SectionBoxStyle))
            {
                EditorGUILayout.LabelField("🎯 Target Platform:", GUILayout.Width(120));
                _targetPlatform = (TargetPlatform)EditorGUILayout.EnumPopup(_targetPlatform, GUILayout.Width(120));

                var budget = PLATFORM_BUDGETS[_targetPlatform];
                EditorGUILayout.LabelField(
                    $"Budget:  {budget.trisBudget:N0} tris  |  {FormatBytes(budget.texMemBudget)} tex  |  {budget.drawCallBudget} draw calls",
                    new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.6f, 0.8f, 0.6f) } });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Mesh Input
        // ─────────────────────────────────────────────────────────────────────

        private void DrawMeshInputSection()
        {
            _foldMeshInput = EditorGUILayout.BeginFoldoutHeaderGroup(_foldMeshInput, "📐  Mesh / Prefab Input");
            if (_foldMeshInput)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    // Two-column header
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var redStyle = new GUIStyle(EditorStyles.boldLabel)
                        { normal = { textColor = new Color(0.9f, 0.4f, 0.4f) } };
                        var greenStyle = new GUIStyle(EditorStyles.boldLabel)
                        { normal = { textColor = new Color(0.4f, 0.9f, 0.4f) } };

                        GUILayout.Space(4);
                        EditorGUILayout.LabelField("Slot A  (Unoptimized)", redStyle, GUILayout.Width(200));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField("Slot B  (Optimized)", greenStyle, GUILayout.Width(200));
                        GUILayout.Space(4);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _meshA = (GameObject)EditorGUILayout.ObjectField(
                            _meshA, typeof(GameObject), false, GUILayout.Width(200));
                        GUILayout.FlexibleSpace();
                        _meshB = (GameObject)EditorGUILayout.ObjectField(
                            _meshB, typeof(GameObject), false, GUILayout.Width(200));
                    }

                    EditorGUILayout.Space(6);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (DrawActionButton("🔍  Analyze Meshes", 180, 28))
                        {
                            _analysisA = AnalyzeMesh(_meshA);
                            _analysisB = AnalyzeMesh(_meshB);
                            _meshAnalyzed = true;
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Mesh Analysis Results
        // ─────────────────────────────────────────────────────────────────────

        private void DrawMeshAnalysisSection()
        {
            _foldMeshAnalysis = EditorGUILayout.BeginFoldoutHeaderGroup(_foldMeshAnalysis, "📊  Mesh Analysis Results");
            if (_foldMeshAnalysis)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    if (!_meshAnalyzed)
                    {
                        EditorGUILayout.LabelField("Assign meshes and click Analyze to see results.",
                            new GUIStyle(EditorStyles.centeredGreyMiniLabel));
                    }
                    else
                    {
                        // Column headers
                        DrawAnalysisColumnHeaders();
                        DrawHorizontalLine(new Color(0.3f, 0.3f, 0.3f), 1f, 2f);

                        // Rows
                        DrawMeshAnalysisRow("Asset Name",
                            _analysisA.IsValid ? _analysisA.AssetName : "—",
                            _analysisB.IsValid ? _analysisB.AssetName : "—");

                        DrawMeshAnalysisRow("Total Triangles",
                            _analysisA.IsValid ? FormatTriCount(_analysisA.TotalTriangles) : "—",
                            _analysisB.IsValid ? FormatTriCount(_analysisB.TotalTriangles) : "—",
                            _analysisA.TotalTriangles, _analysisB.TotalTriangles,
                            PLATFORM_BUDGETS[_targetPlatform].trisBudget);

                        DrawMeshAnalysisRow("Total Vertices",
                            _analysisA.IsValid ? _analysisA.TotalVertices.ToString("N0") : "—",
                            _analysisB.IsValid ? _analysisB.TotalVertices.ToString("N0") : "—");

                        DrawMeshAnalysisRow("Sub-Meshes (Draw Calls)",
                            _analysisA.IsValid ? _analysisA.SubMeshCount.ToString() : "—",
                            _analysisB.IsValid ? _analysisB.SubMeshCount.ToString() : "—",
                            _analysisA.SubMeshCount, _analysisB.SubMeshCount, 2);

                        DrawMeshAnalysisRow("LOD Levels",
                            _analysisA.IsValid ? (_analysisA.HasLODGroup ? _analysisA.LODCount.ToString() : "No LOD Group") : "—",
                            _analysisB.IsValid ? (_analysisB.HasLODGroup ? _analysisB.LODCount.ToString() : "No LOD Group") : "—");

                        DrawMeshAnalysisRow("Material Count",
                            _analysisA.IsValid ? _analysisA.MaterialCount.ToString() : "—",
                            _analysisB.IsValid ? _analysisB.MaterialCount.ToString() : "—");

                        DrawMeshAnalysisRow("Read/Write Enabled",
                            _analysisA.IsValid ? (_analysisA.HasReadWrite ? "⚠ Yes (2x memory)" : "✓ No") : "—",
                            _analysisB.IsValid ? (_analysisB.HasReadWrite ? "⚠ Yes (2x memory)" : "✓ No") : "—");

                        DrawMeshAnalysisRow("Mesh Memory",
                            _analysisA.IsValid ? FormatBytes(_analysisA.MeshMemoryBytes) : "—",
                            _analysisB.IsValid ? FormatBytes(_analysisB.MeshMemoryBytes) : "—");

                        // LOD breakdown for Slot A
                        if (_analysisA.IsValid && _analysisA.HasLODGroup && _analysisA.LODTriCounts != null)
                        {
                            EditorGUILayout.Space(4);
                            EditorGUILayout.LabelField("Slot A — LOD Breakdown:", SubHeaderStyle);
                            DrawLODBreakdown(_analysisA);
                        }

                        // LOD breakdown for Slot B
                        if (_analysisB.IsValid && _analysisB.HasLODGroup && _analysisB.LODTriCounts != null)
                        {
                            EditorGUILayout.Space(4);
                            EditorGUILayout.LabelField("Slot B — LOD Breakdown:", SubHeaderStyle);
                            DrawLODBreakdown(_analysisB);
                        }
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Texture Input
        // ─────────────────────────────────────────────────────────────────────

        private void DrawTextureInputSection()
        {
            _foldTexInput = EditorGUILayout.BeginFoldoutHeaderGroup(_foldTexInput, "🖼  Texture Input");
            if (_foldTexInput)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var redStyle = new GUIStyle(EditorStyles.boldLabel)
                        { normal = { textColor = new Color(0.9f, 0.4f, 0.4f) } };
                        var greenStyle = new GUIStyle(EditorStyles.boldLabel)
                        { normal = { textColor = new Color(0.4f, 0.9f, 0.4f) } };

                        GUILayout.Space(4);
                        EditorGUILayout.LabelField("Texture A  (Unoptimized)", redStyle, GUILayout.Width(200));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField("Texture B  (Optimized)", greenStyle, GUILayout.Width(200));
                        GUILayout.Space(4);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _textureA = (Texture2D)EditorGUILayout.ObjectField(
                            _textureA, typeof(Texture2D), false, GUILayout.Width(200), GUILayout.Height(64));
                        GUILayout.FlexibleSpace();
                        _textureB = (Texture2D)EditorGUILayout.ObjectField(
                            _textureB, typeof(Texture2D), false, GUILayout.Width(200), GUILayout.Height(64));
                    }

                    EditorGUILayout.Space(6);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (DrawActionButton("🔍  Analyze Textures", 180, 28))
                        {
                            _texAnalysisA = AnalyzeTexture(_textureA);
                            _texAnalysisB = AnalyzeTexture(_textureB);
                            _textureAnalyzed = true;
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Texture Analysis Results
        // ─────────────────────────────────────────────────────────────────────

        private void DrawTextureAnalysisSection()
        {
            _foldTexAnalysis = EditorGUILayout.BeginFoldoutHeaderGroup(_foldTexAnalysis, "📊  Texture Analysis Results");
            if (_foldTexAnalysis)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    if (!_textureAnalyzed)
                    {
                        EditorGUILayout.LabelField("Assign textures and click Analyze to see results.",
                            new GUIStyle(EditorStyles.centeredGreyMiniLabel));
                    }
                    else
                    {
                        DrawAnalysisColumnHeaders();
                        DrawHorizontalLine(new Color(0.3f, 0.3f, 0.3f), 1f, 2f);

                        DrawMeshAnalysisRow("Asset Name",
                            _texAnalysisA.IsValid ? _texAnalysisA.AssetName : "—",
                            _texAnalysisB.IsValid ? _texAnalysisB.AssetName : "—");

                        DrawMeshAnalysisRow("Resolution",
                            _texAnalysisA.IsValid ? $"{_texAnalysisA.Width}×{_texAnalysisA.Height}" : "—",
                            _texAnalysisB.IsValid ? $"{_texAnalysisB.Width}×{_texAnalysisB.Height}" : "—");

                        DrawMeshAnalysisRow("Format",
                            _texAnalysisA.IsValid ? _texAnalysisA.Format : "—",
                            _texAnalysisB.IsValid ? _texAnalysisB.Format : "—");

                        DrawMeshAnalysisRow("Mip Maps",
                            _texAnalysisA.IsValid ? (_texAnalysisA.HasMips ? $"✓ {_texAnalysisA.MipCount} levels" : "✗ None") : "—",
                            _texAnalysisB.IsValid ? (_texAnalysisB.HasMips ? $"✓ {_texAnalysisB.MipCount} levels" : "✗ None") : "—");

                        DrawMeshAnalysisRow("sRGB",
                            _texAnalysisA.IsValid ? (_texAnalysisA.IsSRGB ? "Yes" : "No") : "—",
                            _texAnalysisB.IsValid ? (_texAnalysisB.IsSRGB ? "Yes" : "No") : "—");

                        DrawMeshAnalysisRow("GPU Memory",
                            _texAnalysisA.IsValid ? FormatBytes(_texAnalysisA.MemoryBytes) : "—",
                            _texAnalysisB.IsValid ? FormatBytes(_texAnalysisB.MemoryBytes) : "—",
                            (int)(_texAnalysisA.MemoryBytes / 1024),
                            (int)(_texAnalysisB.MemoryBytes / 1024),
                            (int)(PLATFORM_BUDGETS[_targetPlatform].texMemBudget / 1024));

                        DrawMeshAnalysisRow("Disk Size",
                            _texAnalysisA.IsValid ? FormatBytes(_texAnalysisA.StorageBytes) : "—",
                            _texAnalysisB.IsValid ? FormatBytes(_texAnalysisB.StorageBytes) : "—");

                        DrawMeshAnalysisRow("Compression",
                            _texAnalysisA.IsValid ? _texAnalysisA.CompressionQuality : "—",
                            _texAnalysisB.IsValid ? _texAnalysisB.CompressionQuality : "—");

                        DrawMeshAnalysisRow("Read/Write",
                            _texAnalysisA.IsValid ? (_texAnalysisA.IsReadable ? "⚠ Enabled" : "✓ Disabled") : "—",
                            _texAnalysisB.IsValid ? (_texAnalysisB.IsReadable ? "⚠ Enabled" : "✓ Disabled") : "—");
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Comparison Summary
        // ─────────────────────────────────────────────────────────────────────

        private void DrawComparisonSection()
        {
            _foldComparison = EditorGUILayout.BeginFoldoutHeaderGroup(_foldComparison, "⚖  Optimization Summary");
            if (_foldComparison)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    if (!_meshAnalyzed && !_textureAnalyzed)
                    {
                        EditorGUILayout.LabelField("Analyze assets above to see the optimization summary.",
                            new GUIStyle(EditorStyles.centeredGreyMiniLabel));
                        return;
                    }

                    // Mesh savings
                    if (_meshAnalyzed && _analysisA.IsValid && _analysisB.IsValid)
                    {
                        EditorGUILayout.LabelField("Mesh Savings:", SubHeaderStyle);

                        int triSaving = _analysisA.TotalTriangles - _analysisB.TotalTriangles;
                        float triPct  = _analysisA.TotalTriangles > 0
                            ? (triSaving / (float)_analysisA.TotalTriangles) * 100f : 0f;

                        DrawSavingsRow("Triangle Reduction",
                            $"{triSaving:N0} tris saved", $"{triPct:F1}% reduction",
                            triSaving > 0);

                        long memSaving = _analysisA.MeshMemoryBytes - _analysisB.MeshMemoryBytes;
                        DrawSavingsRow("Mesh Memory Saved",
                            FormatBytes(System.Math.Abs(memSaving)),
                            memSaving > 0 ? "✓ Reduced" : "⚠ Increased",
                            memSaving > 0);

                        int dcSaving = _analysisA.SubMeshCount - _analysisB.SubMeshCount;
                        DrawSavingsRow("Draw Call Reduction",
                            $"{System.Math.Abs(dcSaving)} draw calls",
                            dcSaving > 0 ? "✓ Reduced" : dcSaving == 0 ? "= Same" : "⚠ Increased",
                            dcSaving >= 0);
                    }

                    // Texture savings
                    if (_textureAnalyzed && _texAnalysisA.IsValid && _texAnalysisB.IsValid)
                    {
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Texture Savings:", SubHeaderStyle);

                        long texMemSaving = _texAnalysisA.MemoryBytes - _texAnalysisB.MemoryBytes;
                        float texMemPct   = _texAnalysisA.MemoryBytes > 0
                            ? (texMemSaving / (float)_texAnalysisA.MemoryBytes) * 100f : 0f;

                        DrawSavingsRow("GPU Memory Saved",
                            FormatBytes(System.Math.Abs(texMemSaving)),
                            $"{texMemPct:F1}% reduction",
                            texMemSaving > 0);

                        long diskSaving = _texAnalysisA.StorageBytes - _texAnalysisB.StorageBytes;
                        DrawSavingsRow("Disk Size Saved",
                            FormatBytes(System.Math.Abs(diskSaving)),
                            diskSaving > 0 ? "✓ Smaller" : "⚠ Larger",
                            diskSaving > 0);
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Optimization Tips
        // ─────────────────────────────────────────────────────────────────────

        private void DrawOptimizationTipsSection()
        {
            _foldTips = EditorGUILayout.BeginFoldoutHeaderGroup(_foldTips, "💡  Optimization Tips");
            if (_foldTips)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUILayout.LabelField("Mesh Tips:", SubHeaderStyle);
                    EditorGUILayout.LabelField(MESH_TIPS[_meshTipIndex],
                        new GUIStyle(EditorStyles.wordWrappedLabel)
                        { normal = { textColor = new Color(0.8f, 0.8f, 0.4f) } });

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("◀", GUILayout.Width(30)))
                            _meshTipIndex = (_meshTipIndex - 1 + MESH_TIPS.Length) % MESH_TIPS.Length;
                        EditorGUILayout.LabelField($"{_meshTipIndex + 1}/{MESH_TIPS.Length}",
                            new GUIStyle(EditorStyles.centeredGreyMiniLabel));
                        if (GUILayout.Button("▶", GUILayout.Width(30)))
                            _meshTipIndex = (_meshTipIndex + 1) % MESH_TIPS.Length;
                    }

                    EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField("Texture Tips:", SubHeaderStyle);
                    EditorGUILayout.LabelField(TEXTURE_TIPS[_texTipIndex],
                        new GUIStyle(EditorStyles.wordWrappedLabel)
                        { normal = { textColor = new Color(0.8f, 0.8f, 0.4f) } });

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("◀", GUILayout.Width(30)))
                            _texTipIndex = (_texTipIndex - 1 + TEXTURE_TIPS.Length) % TEXTURE_TIPS.Length;
                        EditorGUILayout.LabelField($"{_texTipIndex + 1}/{TEXTURE_TIPS.Length}",
                            new GUIStyle(EditorStyles.centeredGreyMiniLabel));
                        if (GUILayout.Button("▶", GUILayout.Width(30)))
                            _texTipIndex = (_texTipIndex + 1) % TEXTURE_TIPS.Length;
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Analysis Logic
        // ─────────────────────────────────────────────────────────────────────

        // AnalyzeMesh(), AnalyzeTexture(), DrawAnalysisColumnHeaders(),
        // DrawMeshAnalysisRow(), DrawLODBreakdown(), DrawSavingsRow(),
        // ComputeDelta(), and DrawBudgetBar() are in
        // AssetOptimizationTool.Helpers.cs (partial class).
    }
}

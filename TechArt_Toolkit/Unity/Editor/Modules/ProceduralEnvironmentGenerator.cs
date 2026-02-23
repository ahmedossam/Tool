using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TechArtToolkit.Editor.Modules
{
    /// <summary>
    /// MODULE 5: Procedural Environment Generator
    ///
    /// WHAT:  Generates terrain heightmaps, rock scatter, and foliage
    ///        distribution using procedural parameters. Exposes controls for
    ///        noise seed, scale, density, slope masking, and biome type.
    ///        All generation is undoable and non-destructive.
    ///
    /// WHY:   Demonstrates procedural content generation skills — using noise
    ///        functions, scatter algorithms, and rule-based placement to
    ///        generate game-ready environments efficiently without manual work.
    ///
    /// HOW:   Unity Terrain API for heightmap generation via layered FBM noise.
    ///        Poisson disk sampling for rock placement (avoids clustering).
    ///        TerrainData.SetDetailLayer() for grass density maps.
    ///        TreeInstance[] for tree placement with slope/height masking.
    ///        Undo.RegisterCreatedObjectUndo() for full undo support.
    /// </summary>
    public partial class ProceduralEnvironmentGenerator : ModuleBase
    {
        // ─────────────────────────────────────────────────────────────────────
        // Identity
        // ─────────────────────────────────────────────────────────────────────

        public override string ModuleName        => "Procedural Environment Generator";
        public override string ModuleDescription => "Generates terrain, rock scatter, and foliage using procedural noise and Poisson disk sampling. Full undo support.";
        public override string ModuleIcon        => "d_Terrain Icon";

        // ─────────────────────────────────────────────────────────────────────
        // Terrain Parameters
        // ─────────────────────────────────────────────────────────────────────

        private int   _terrainSeed        = 42;
        private float _terrainScale       = 80f;
        private int   _terrainOctaves     = 5;
        private float _terrainPersistence = 0.45f;
        private float _terrainLacunarity  = 2.1f;
        private float _terrainHeightScale = 0.25f;   // fraction of terrain height
        private int   _terrainResolution  = 513;     // heightmap resolution (must be 2^n + 1)
        private float _terrainSize        = 500f;    // world units
        private float _terrainHeight      = 100f;    // max height in world units

        // ─────────────────────────────────────────────────────────────────────
        // Rock Scatter Parameters
        // ─────────────────────────────────────────────────────────────────────

        private GameObject[] _rockPrefabs     = new GameObject[3];
        private float        _rockDensity     = 0.4f;
        private float        _rockMinRadius   = 3f;    // Poisson disk min distance
        private float        _rockScaleMin    = 0.5f;
        private float        _rockScaleMax    = 2.5f;
        private float        _rockSlopeMax    = 35f;   // degrees — no rocks on steep slopes
        private float        _rockHeightMin   = 0.05f; // fraction of terrain height
        private float        _rockHeightMax   = 0.85f;
        private bool         _rockAlignNormal = true;  // align to terrain normal
        private int          _rockMaxCount    = 500;

        // ─────────────────────────────────────────────────────────────────────
        // Foliage Parameters
        // ─────────────────────────────────────────────────────────────────────

        private float _grassDensity    = 0.6f;
        private float _grassHeightMin  = 0.0f;
        private float _grassHeightMax  = 0.5f;
        private float _grassSlopeMax   = 25f;

        private GameObject[] _treePrefabs   = new GameObject[2];
        private float        _treeDensity   = 0.2f;
        private float        _treeHeightMin = 0.05f;
        private float        _treeHeightMax = 0.7f;
        private float        _treeSlopeMax  = 20f;
        private float        _treeScaleMin  = 0.8f;
        private float        _treeScaleMax  = 1.4f;
        private int          _treeMaxCount  = 200;

        // ─────────────────────────────────────────────────────────────────────
        // Biome Presets
        // ─────────────────────────────────────────────────────────────────────

        private enum BiomePreset { Custom, Alpine, Desert, Forest, Tundra, Coastal }
        private BiomePreset _biome = BiomePreset.Forest;

        // ─────────────────────────────────────────────────────────────────────
        // Generation State
        // ─────────────────────────────────────────────────────────────────────

        private Terrain              _generatedTerrain;
        private List<GameObject>     _generatedRocks   = new List<GameObject>();
        private int                  _lastRockCount    = 0;
        private int                  _lastTreeCount    = 0;
        private bool                 _terrainGenerated = false;
        private string               _statusText       = "";

        // ─────────────────────────────────────────────────────────────────────
        // UI State
        // ─────────────────────────────────────────────────────────────────────

        private bool _foldBiome    = true;
        private bool _foldTerrain  = true;
        private bool _foldRocks    = true;
        private bool _foldFoliage  = false;
        private bool _foldGenerate = true;
        private bool _foldStats    = true;
        private Vector2 _scrollPos;

        // ─────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        public override void OnEnable(EditorWindow parentWindow)
        {
            base.OnEnable(parentWindow);
            // Try to find existing generated terrain
            _generatedTerrain = Object.FindObjectOfType<Terrain>();
        }

        public override void OnDisable()
        {
            base.OnDisable();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Main GUI
        // ─────────────────────────────────────────────────────────────────────

        public override void DrawGUI()
        {
            DrawHeader();

            DrawInfoBox(
                "Generate procedural terrain, rock scatter, and foliage using noise-based " +
                "algorithms. All operations are undoable (Ctrl+Z). Use biome presets for " +
                "quick setups, or fine-tune each parameter manually.");

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawBiomeSection();
            DrawTerrainSection();
            DrawRockSection();
            DrawFoliageSection();
            DrawGenerationControls();
            DrawStatsSection();

            EditorGUILayout.EndScrollView();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Biome Presets
        // ─────────────────────────────────────────────────────────────────────

        private void DrawBiomeSection()
        {
            _foldBiome = EditorGUILayout.BeginFoldoutHeaderGroup(_foldBiome, "🌍  Biome Preset");
            if (_foldBiome)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUI.BeginChangeCheck();
                    _biome = (BiomePreset)EditorGUILayout.EnumPopup(
                        new GUIContent("Biome", "Select a preset to auto-fill terrain and scatter parameters"),
                        _biome);

                    if (EditorGUI.EndChangeCheck() && _biome != BiomePreset.Custom)
                        ApplyBiomePreset(_biome);

                    // Biome description
                    string desc = _biome switch
                    {
                        BiomePreset.Alpine  => "High altitude rocky terrain. Steep slopes, sparse vegetation, large boulders.",
                        BiomePreset.Desert  => "Flat sandy terrain with dunes. Low vegetation, scattered rocks and cacti.",
                        BiomePreset.Forest  => "Rolling hills with dense tree coverage. Moderate rocks, rich undergrowth.",
                        BiomePreset.Tundra  => "Flat frozen terrain. Very sparse vegetation, small rocks, low grass.",
                        BiomePreset.Coastal => "Gentle slopes near water. Sandy beaches, coastal rocks, sparse trees.",
                        _                   => "Custom parameters — adjust each section manually."
                    };

                    EditorGUILayout.LabelField(desc,
                        new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                        { normal = { textColor = new Color(0.7f, 0.7f, 0.5f) } });

                    // Seed control
                    EditorGUILayout.Space(4);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _terrainSeed = EditorGUILayout.IntField(
                            new GUIContent("Seed", "Random seed — same seed always produces the same result"),
                            _terrainSeed);

                        if (GUILayout.Button("🎲 Randomize", GUILayout.Width(100), GUILayout.Height(20)))
                        {
                            _terrainSeed = Random.Range(0, 99999);
                            _biome = BiomePreset.Custom;
                        }
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Terrain
        // ─────────────────────────────────────────────────────────────────────

        private void DrawTerrainSection()
        {
            _foldTerrain = EditorGUILayout.BeginFoldoutHeaderGroup(_foldTerrain, "⛰  Terrain Generation");
            if (_foldTerrain)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUILayout.LabelField("Dimensions", SubHeaderStyle);

                    _terrainSize = EditorGUILayout.Slider(
                        new GUIContent("Terrain Size (m)", "World-space size of the terrain in meters"),
                        _terrainSize, 50f, 2000f);

                    _terrainHeight = EditorGUILayout.Slider(
                        new GUIContent("Max Height (m)", "Maximum terrain height in meters"),
                        _terrainHeight, 10f, 500f);

                    int[] resOptions = { 33, 65, 129, 257, 513, 1025 };
                    string[] resLabels = { "33 (Low)", "65", "129", "257 (Med)", "513 (High)", "1025 (Ultra)" };
                    int resIndex = System.Array.IndexOf(resOptions, _terrainResolution);
                    if (resIndex < 0) resIndex = 4;
                    resIndex = EditorGUILayout.Popup(
                        new GUIContent("Heightmap Resolution", "Resolution of the terrain heightmap. Higher = more detail, more memory."),
                        resIndex, resLabels);
                    _terrainResolution = resOptions[resIndex];

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("FBM Noise", SubHeaderStyle);

                    _terrainScale = EditorGUILayout.Slider(
                        new GUIContent("Noise Scale", "Controls the zoom/frequency of the terrain features"),
                        _terrainScale, 10f, 500f);

                    _terrainOctaves = EditorGUILayout.IntSlider(
                        new GUIContent("Octaves", "Number of noise layers. More = more detail, more computation."),
                        _terrainOctaves, 1, 8);

                    _terrainPersistence = EditorGUILayout.Slider(
                        new GUIContent("Persistence", "Amplitude falloff per octave. Lower = smoother terrain."),
                        _terrainPersistence, 0.1f, 1.0f);

                    _terrainLacunarity = EditorGUILayout.Slider(
                        new GUIContent("Lacunarity", "Frequency multiplier per octave. Higher = more fine detail."),
                        _terrainLacunarity, 1.0f, 4.0f);

                    _terrainHeightScale = EditorGUILayout.Slider(
                        new GUIContent("Height Scale", "Vertical exaggeration of the noise (fraction of max height)"),
                        _terrainHeightScale, 0.05f, 1.0f);

                    // Memory estimate
                    long heightmapMem = (long)_terrainResolution * _terrainResolution * 4;
                    EditorGUILayout.Space(4);
                    DrawMetricRow("Heightmap Memory", FormatBytes(heightmapMem));
                    DrawMetricRow("Heightmap Resolution", $"{_terrainResolution}×{_terrainResolution}");
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Rock Scatter
        // ─────────────────────────────────────────────────────────────────────

        private void DrawRockSection()
        {
            _foldRocks = EditorGUILayout.BeginFoldoutHeaderGroup(_foldRocks, "🪨  Rock Scatter");
            if (_foldRocks)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUILayout.LabelField("Rock Prefabs (up to 3 variations)", SubHeaderStyle);
                    for (int i = 0; i < _rockPrefabs.Length; i++)
                    {
                        _rockPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                            $"Rock Prefab {i + 1}", _rockPrefabs[i], typeof(GameObject), false);
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Scatter Parameters", SubHeaderStyle);

                    _rockDensity = EditorGUILayout.Slider(
                        new GUIContent("Density", "Fraction of Poisson disk sample points that spawn a rock (0 = none, 1 = all)"),
                        _rockDensity, 0f, 1f);

                    _rockMinRadius = EditorGUILayout.Slider(
                        new GUIContent("Min Spacing (m)", "Minimum distance between rocks (Poisson disk radius)"),
                        _rockMinRadius, 0.5f, 20f);

                    _rockMaxCount = EditorGUILayout.IntSlider(
                        new GUIContent("Max Count", "Hard cap on total rocks spawned"),
                        _rockMaxCount, 1, 2000);

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Scale & Placement", SubHeaderStyle);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Scale Range:", GUILayout.Width(90));
                        _rockScaleMin = EditorGUILayout.FloatField(_rockScaleMin, GUILayout.Width(50));
                        EditorGUILayout.LabelField("–", GUILayout.Width(15));
                        _rockScaleMax = EditorGUILayout.FloatField(_rockScaleMax, GUILayout.Width(50));
                    }

                    _rockSlopeMax = EditorGUILayout.Slider(
                        new GUIContent("Max Slope (°)", "Rocks won't spawn on slopes steeper than this angle"),
                        _rockSlopeMax, 0f, 90f);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Height Range:", GUILayout.Width(90));
                        _rockHeightMin = EditorGUILayout.Slider(_rockHeightMin, 0f, 1f, GUILayout.Width(80));
                        EditorGUILayout.LabelField("–", GUILayout.Width(15));
                        _rockHeightMax = EditorGUILayout.Slider(_rockHeightMax, 0f, 1f, GUILayout.Width(80));
                    }

                    _rockAlignNormal = EditorGUILayout.Toggle(
                        new GUIContent("Align to Normal", "Rotates rocks to match the terrain surface normal"),
                        _rockAlignNormal);

                    // Estimate
                    int estimatedRocks = Mathf.Min(
                        Mathf.RoundToInt((_terrainSize / _rockMinRadius) * (_terrainSize / _rockMinRadius) * _rockDensity * 0.25f),
                        _rockMaxCount);
                    DrawMetricRow("Estimated Rock Count", estimatedRocks.ToString("N0"),
                        EvaluateBudget(estimatedRocks, 200, 800));
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Foliage
        // ─────────────────────────────────────────────────────────────────────

        private void DrawFoliageSection()
        {
            _foldFoliage = EditorGUILayout.BeginFoldoutHeaderGroup(_foldFoliage, "🌿  Foliage Distribution");
            if (_foldFoliage)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUILayout.LabelField("Grass", SubHeaderStyle);

                    _grassDensity = EditorGUILayout.Slider(
                        new GUIContent("Grass Density", "Density of grass detail layer (0 = none, 1 = maximum)"),
                        _grassDensity, 0f, 1f);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Height Range:", GUILayout.Width(90));
                        _grassHeightMin = EditorGUILayout.Slider(_grassHeightMin, 0f, 1f, GUILayout.Width(80));
                        EditorGUILayout.LabelField("–", GUILayout.Width(15));
                        _grassHeightMax = EditorGUILayout.Slider(_grassHeightMax, 0f, 1f, GUILayout.Width(80));
                    }

                    _grassSlopeMax = EditorGUILayout.Slider(
                        new GUIContent("Max Slope (°)", "Grass won't grow on slopes steeper than this"),
                        _grassSlopeMax, 0f, 60f);

                    EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField("Trees", SubHeaderStyle);

                    for (int i = 0; i < _treePrefabs.Length; i++)
                    {
                        _treePrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                            $"Tree Prefab {i + 1}", _treePrefabs[i], typeof(GameObject), false);
                    }

                    _treeDensity = EditorGUILayout.Slider(
                        new GUIContent("Tree Density", "Probability of tree placement at each sample point"),
                        _treeDensity, 0f, 1f);

                    _treeMaxCount = EditorGUILayout.IntSlider(
                        new GUIContent("Max Tree Count", "Hard cap on total trees"),
                        _treeMaxCount, 1, 1000);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Scale Range:", GUILayout.Width(90));
                        _treeScaleMin = EditorGUILayout.FloatField(_treeScaleMin, GUILayout.Width(50));
                        EditorGUILayout.LabelField("–", GUILayout.Width(15));
                        _treeScaleMax = EditorGUILayout.FloatField(_treeScaleMax, GUILayout.Width(50));
                    }

                    _treeSlopeMax = EditorGUILayout.Slider(
                        new GUIContent("Max Slope (°)", "Trees won't grow on slopes steeper than this"),
                        _treeSlopeMax, 0f, 45f);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Generation Controls
        // ─────────────────────────────────────────────────────────────────────

        private void DrawGenerationControls()
        {
            _foldGenerate = EditorGUILayout.BeginFoldoutHeaderGroup(_foldGenerate, "⚙  Generation Controls");
            if (_foldGenerate)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    // Terrain
                    EditorGUILayout.LabelField("Terrain", SubHeaderStyle);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (DrawActionButton("⛰  Generate Terrain", 180, 28))
                            GenerateTerrain();

                        GUILayout.Space(8);

                        if (DrawDestructiveButton("✕  Clear Terrain", 140, 28))
                            ClearTerrain();
                    }

                    EditorGUILayout.Space(6);

                    // Rocks
                    EditorGUILayout.LabelField("Rock Scatter", SubHeaderStyle);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool canScatter = _terrainGenerated || _generatedTerrain != null;
                        GUI.enabled = canScatter;

                        if (DrawActionButton("🪨  Scatter Rocks", 180, 28))
                            ScatterRocks();

                        GUILayout.Space(8);

                        if (DrawDestructiveButton("✕  Clear Rocks", 140, 28))
                            ClearRocks();

                        GUI.enabled = true;
                    }

                    if (!_terrainGenerated && _generatedTerrain == null)
                        EditorGUILayout.LabelField("  ↑ Generate terrain first to enable rock scatter.",
                            new GUIStyle(EditorStyles.miniLabel)
                            { normal = { textColor = Color.gray } });

                    EditorGUILayout.Space(6);

                    // Foliage
                    EditorGUILayout.LabelField("Foliage", SubHeaderStyle);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool canFoliage = _terrainGenerated || _generatedTerrain != null;
                        GUI.enabled = canFoliage;

                        if (DrawActionButton("🌿  Place Foliage", 180, 28))
                            PlaceFoliage();

                        GUILayout.Space(8);

                        if (DrawDestructiveButton("✕  Clear Foliage", 140, 28))
                            ClearFoliage();

                        GUI.enabled = true;
                    }

                    EditorGUILayout.Space(8);

                    // Generate All / Clear All
                    DrawHorizontalLine(new Color(0.3f, 0.3f, 0.3f), 1f, 2f);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        if (DrawActionButton("✨  Generate All", 160, 32))
                        {
                            GenerateTerrain();
                            ScatterRocks();
                            PlaceFoliage();
                        }

                        GUILayout.Space(12);

                        if (DrawDestructiveButton("🗑  Clear All", 140, 32))
                        {
                            ClearFoliage();
                            ClearRocks();
                            ClearTerrain();
                        }

                        GUILayout.FlexibleSpace();
                    }

                    // Status
                    if (!string.IsNullOrEmpty(_statusText))
                    {
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField(_statusText,
                            new GUIStyle(EditorStyles.miniLabel)
                            { normal = { textColor = new Color(0.5f, 0.9f, 0.5f) } });
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Stats
        // ─────────────────────────────────────────────────────────────────────

        private void DrawStatsSection()
        {
            _foldStats = EditorGUILayout.BeginFoldoutHeaderGroup(_foldStats, "📊  Generation Stats");
            if (_foldStats)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    DrawMetricRow("Terrain Generated",
                        _terrainGenerated ? "✓ Yes" : "○ No",
                        _terrainGenerated ? MetricStatus.Good : MetricStatus.Neutral);

                    DrawMetricRow("Terrain Size",
                        _terrainGenerated ? $"{_terrainSize}m × {_terrainSize}m" : "—");

                    DrawMetricRow("Heightmap Resolution",
                        _terrainGenerated ? $"{_terrainResolution}×{_terrainResolution}" : "—");

                    DrawMetricRow("Rocks Placed",
                        _lastRockCount > 0 ? _lastRockCount.ToString("N0") : "—",
                        EvaluateBudget(_lastRockCount, 300, 800));

                    DrawMetricRow("Trees Placed",
                        _lastTreeCount > 0 ? _lastTreeCount.ToString("N0") : "—",
                        EvaluateBudget(_lastTreeCount, 100, 300));

                    DrawMetricRow("Active Seed", _terrainSeed.ToString());
                    DrawMetricRow("Biome", _biome.ToString());
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Generation: Terrain
        // ─────────────────────────────────────────────────────────────────────

        private void GenerateTerrain()
        {
            // Create TerrainData
            var terrainData = new TerrainData();
            terrainData.heightmapResolution = _terrainResolution;
            terrainData.size = new Vector3(_terrainSize, _terrainHeight, _terrainSize);

            // Generate heightmap using FBM noise
            float[,] heights = GenerateFBMHeightmap(
                _terrainResolution, _terrainResolution,
                _terrainSeed, _terrainScale,
                _terrainOctaves, _terrainPersistence,
                _terrainLacunarity, _terrainHeightScale);

            terrainData.SetHeights(0, 0, heights);

            // Create terrain GameObject
            var terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = "[TAT] Generated Terrain";
            terrainGO.transform.position = new Vector3(
                -_terrainSize * 0.5f, 0f, -_terrainSize * 0.5f);

            // Register for undo
            Undo.RegisterCreatedObjectUndo(terrainGO, "Generate Terrain");

            // Destroy old terrain if exists
            if (_generatedTerrain != null)
                Undo.DestroyObjectImmediate(_generatedTerrain.gameObject);

            _generatedTerrain = terrainGO.GetComponent<Terrain>();
            _terrainGenerated = true;

            _statusText = $"✓ Terrain generated: {_terrainResolution}×{_terrainResolution} heightmap, seed {_terrainSeed}";
            ((TechArtToolkitWindow)_parentWindow)?.SetStatus(_statusText);

            SceneView.RepaintAll();
        }

        // ─────────────────────────────────────────────────────────────────────
        // FBM Noise Heightmap
        // ─────────────────────────────────────────────────────────────────────

        private float[,] GenerateFBMHeightmap(int width, int height,
            int seed, float scale, int octaves,
            float persistence, float lacunarity, float heightScale)
        {
            float[,] map = new float[width, height];

            // Seed-based offset
            var rng = new System.Random(seed);
            Vector2[] octaveOffsets = new Vector2[octaves];
            for (int i = 0; i < octaves; i++)
            {
                float offsetX = rng.Next(-100000, 100000);
                float offsetY = rng.Next(-100000, 100000);
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float amplitude = 1f;
                    float frequency = 1f;
                    float noiseHeight = 0f;

                    for (int o = 0; o < octaves; o++)
                    {
                        float sampleX = (x / (float)width  * scale + octaveOffsets[o].x) * frequency;
                        float sampleY = (y / (float)height * scale + octaveOffsets[o].y) * frequency;

                        // Perlin noise in [0,1] → remap to [-1,1] for FBM
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }

                    if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                    if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;

                    map[x, y] = noiseHeight;
                }
            }

            // Normalize to [0, heightScale] — via NormalizeHeightmap() in Helpers.cs
            return NormalizeHeightmap(map, width, height, minNoiseHeight, maxNoiseHeight, heightScale);
        }

        // ScatterRocks(), PlaceFoliage(), ClearTerrain(), ClearRocks(), ClearFoliage(),
        // ApplyBiomePreset(), PoissonDiskSample(), and NormalizeHeightmap() are in
        // ProceduralEnvironmentGenerator.Helpers.cs (partial class).
    }
}

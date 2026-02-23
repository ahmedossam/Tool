// ProceduralEnvironmentGenerator.Helpers.cs
// Partial class — contains ScatterRocks, PlaceFoliage, Clear*, ApplyBiomePreset,
// and the FBM heightmap normalization loop.

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TechArtToolkit.Editor.Modules
{
    public partial class ProceduralEnvironmentGenerator
    {
        // ─────────────────────────────────────────────────────────────────────
        // FBM Heightmap — Normalization (completes GenerateFBMHeightmap)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Normalizes a raw FBM heightmap to [0, heightScale].
        /// Called after the noise generation loop in GenerateFBMHeightmap.
        /// </summary>
        private float[,] NormalizeHeightmap(float[,] map, int width, int height,
            float minVal, float maxVal, float heightScale)
        {
            float range = maxVal - minVal;
            if (range <= 0f) range = 1f;

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    map[x, y] = ((map[x, y] - minVal) / range) * heightScale;

            return map;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Rock Scatter — Poisson Disk Sampling
        // ─────────────────────────────────────────────────────────────────────

        private void ScatterRocks()
        {
            if (_generatedTerrain == null)
            {
                _statusText = "⚠ Generate terrain first before scattering rocks.";
                return;
            }

            // Validate prefabs
            var validPrefabs = new List<GameObject>();
            foreach (var p in _rockPrefabs)
                if (p != null) validPrefabs.Add(p);

            if (validPrefabs.Count == 0)
            {
                _statusText = "⚠ Assign at least one Rock Prefab before scattering.";
                return;
            }

            ClearRocks();

            var rng = new System.Random(_terrainSeed + 1000);
            var points = PoissonDiskSample(_terrainSize, _terrainSize, _rockMinRadius, rng);

            int placed = 0;
            var terrainData = _generatedTerrain.terrainData;
            Vector3 terrainPos = _generatedTerrain.transform.position;

            foreach (var point in points)
            {
                if (placed >= _rockMaxCount) break;
                if ((float)rng.NextDouble() > _rockDensity) continue;

                // Normalize to [0,1] for terrain sampling
                float normX = point.x / _terrainSize;
                float normZ = point.y / _terrainSize;

                // Height check
                float height = terrainData.GetInterpolatedHeight(normX, normZ);
                float heightFraction = height / _terrainHeight;
                if (heightFraction < _rockHeightMin || heightFraction > _rockHeightMax) continue;

                // Slope check
                float slope = terrainData.GetSteepness(normX, normZ);
                if (slope > _rockSlopeMax) continue;

                // World position
                Vector3 worldPos = new Vector3(
                    terrainPos.x + point.x,
                    terrainPos.y + height,
                    terrainPos.z + point.y);

                // Pick random prefab
                var prefab = validPrefabs[rng.Next(validPrefabs.Count)];

                // Random scale
                float scale = Mathf.Lerp(_rockScaleMin, _rockScaleMax, (float)rng.NextDouble());

                // Instantiate
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.position   = worldPos;
                go.transform.localScale = Vector3.one * scale;
                go.transform.rotation   = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

                // Align to terrain normal
                if (_rockAlignNormal)
                {
                    Vector3 normal = terrainData.GetInterpolatedNormal(normX, normZ);
                    go.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal)
                        * Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                }

                Undo.RegisterCreatedObjectUndo(go, "Scatter Rock");
                _generatedRocks.Add(go);
                placed++;
            }

            _lastRockCount = placed;
            _statusText = $"✓ Scattered {placed} rocks (seed {_terrainSeed})";
            ((TechArtToolkitWindow)_parentWindow)?.SetStatus(_statusText);
            SceneView.RepaintAll();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Poisson Disk Sampling
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a set of 2D points with minimum distance r between them.
        /// Uses Bridson's algorithm (fast Poisson disk sampling).
        /// </summary>
        private List<Vector2> PoissonDiskSample(float width, float height,
            float minRadius, System.Random rng)
        {
            float cellSize = minRadius / Mathf.Sqrt(2f);
            int gridW = Mathf.CeilToInt(width  / cellSize);
            int gridH = Mathf.CeilToInt(height / cellSize);

            int[,] grid = new int[gridW, gridH];
            for (int i = 0; i < gridW; i++)
                for (int j = 0; j < gridH; j++)
                    grid[i, j] = -1;

            var points  = new List<Vector2>();
            var active  = new List<int>();
            const int K = 30; // max attempts per point

            // Seed point
            Vector2 seed = new Vector2(
                (float)rng.NextDouble() * width,
                (float)rng.NextDouble() * height);
            points.Add(seed);
            active.Add(0);
            int gx = Mathf.FloorToInt(seed.x / cellSize);
            int gy = Mathf.FloorToInt(seed.y / cellSize);
            if (gx >= 0 && gx < gridW && gy >= 0 && gy < gridH)
                grid[gx, gy] = 0;

            while (active.Count > 0)
            {
                int idx = rng.Next(active.Count);
                Vector2 origin = points[active[idx]];
                bool found = false;

                for (int k = 0; k < K; k++)
                {
                    float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float dist  = minRadius + (float)rng.NextDouble() * minRadius;
                    Vector2 candidate = origin + new Vector2(
                        Mathf.Cos(angle) * dist,
                        Mathf.Sin(angle) * dist);

                    if (candidate.x < 0 || candidate.x >= width ||
                        candidate.y < 0 || candidate.y >= height) continue;

                    int cx = Mathf.FloorToInt(candidate.x / cellSize);
                    int cy = Mathf.FloorToInt(candidate.y / cellSize);

                    bool valid = true;
                    for (int nx = Mathf.Max(0, cx - 2); nx <= Mathf.Min(gridW - 1, cx + 2) && valid; nx++)
                    {
                        for (int ny = Mathf.Max(0, cy - 2); ny <= Mathf.Min(gridH - 1, cy + 2) && valid; ny++)
                        {
                            int ni = grid[nx, ny];
                            if (ni >= 0 && Vector2.Distance(points[ni], candidate) < minRadius)
                                valid = false;
                        }
                    }

                    if (valid)
                    {
                        points.Add(candidate);
                        active.Add(points.Count - 1);
                        if (cx >= 0 && cx < gridW && cy >= 0 && cy < gridH)
                            grid[cx, cy] = points.Count - 1;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    active.RemoveAt(idx);
            }

            return points;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Foliage Placement
        // ─────────────────────────────────────────────────────────────────────

        private void PlaceFoliage()
        {
            if (_generatedTerrain == null)
            {
                _statusText = "⚠ Generate terrain first before placing foliage.";
                return;
            }

            var terrainData = _generatedTerrain.terrainData;
            var rng = new System.Random(_terrainSeed + 2000);

            // ── Grass (Detail Layer) ─────────────────────────────────────────
            int detailRes = terrainData.detailResolution;
            int[,] detailMap = new int[detailRes, detailRes];

            for (int y = 0; y < detailRes; y++)
            {
                for (int x = 0; x < detailRes; x++)
                {
                    float normX = x / (float)detailRes;
                    float normY = y / (float)detailRes;

                    float heightFraction = terrainData.GetInterpolatedHeight(normX, normY) / _terrainHeight;
                    float slope = terrainData.GetSteepness(normX, normY);

                    bool inHeightRange = heightFraction >= _grassHeightMin && heightFraction <= _grassHeightMax;
                    bool inSlopeRange  = slope <= _grassSlopeMax;

                    if (inHeightRange && inSlopeRange && (float)rng.NextDouble() < _grassDensity)
                        detailMap[y, x] = Mathf.RoundToInt(_grassDensity * 16f);
                }
            }

            if (terrainData.detailPrototypes.Length > 0)
                terrainData.SetDetailLayer(0, 0, 0, detailMap);

            // ── Trees ────────────────────────────────────────────────────────
            var validTrees = new List<GameObject>();
            foreach (var t in _treePrefabs)
                if (t != null) validTrees.Add(t);

            if (validTrees.Count > 0)
            {
                // Register tree prototypes
                var treePrototypes = new TreePrototype[validTrees.Count];
                for (int i = 0; i < validTrees.Count; i++)
                    treePrototypes[i] = new TreePrototype { prefab = validTrees[i] };
                terrainData.treePrototypes = treePrototypes;

                var treeInstances = new List<TreeInstance>();
                var points = PoissonDiskSample(_terrainSize, _terrainSize, _rockMinRadius * 3f, rng);

                foreach (var point in points)
                {
                    if (treeInstances.Count >= _treeMaxCount) break;
                    if ((float)rng.NextDouble() > _treeDensity) continue;

                    float normX = point.x / _terrainSize;
                    float normZ = point.y / _terrainSize;

                    float heightFraction = terrainData.GetInterpolatedHeight(normX, normZ) / _terrainHeight;
                    float slope = terrainData.GetSteepness(normX, normZ);

                    if (heightFraction < _treeHeightMin || heightFraction > _treeHeightMax) continue;
                    if (slope > _treeSlopeMax) continue;

                    float scale = Mathf.Lerp(_treeScaleMin, _treeScaleMax, (float)rng.NextDouble());

                    treeInstances.Add(new TreeInstance
                    {
                        position       = new Vector3(normX, 0f, normZ),
                        prototypeIndex = rng.Next(validTrees.Count),
                        widthScale     = scale,
                        heightScale    = scale,
                        color          = Color.white,
                        lightmapColor  = Color.white,
                        rotation       = (float)rng.NextDouble() * Mathf.PI * 2f
                    });
                }

                terrainData.SetTreeInstances(treeInstances.ToArray(), true);
                _lastTreeCount = treeInstances.Count;
            }

            _statusText = $"✓ Foliage placed: {_lastTreeCount} trees (seed {_terrainSeed})";
            ((TechArtToolkitWindow)_parentWindow)?.SetStatus(_statusText);
            SceneView.RepaintAll();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Clear Operations
        // ─────────────────────────────────────────────────────────────────────

        private void ClearTerrain()
        {
            if (_generatedTerrain != null)
            {
                Undo.DestroyObjectImmediate(_generatedTerrain.gameObject);
                _generatedTerrain = null;
            }
            _terrainGenerated = false;
            _lastRockCount    = 0;
            _lastTreeCount    = 0;
            _statusText       = "Terrain cleared.";
            SceneView.RepaintAll();
        }

        private void ClearRocks()
        {
            foreach (var rock in _generatedRocks)
                if (rock != null) Undo.DestroyObjectImmediate(rock);
            _generatedRocks.Clear();
            _lastRockCount = 0;
            SceneView.RepaintAll();
        }

        private void ClearFoliage()
        {
            if (_generatedTerrain == null) return;

            var terrainData = _generatedTerrain.terrainData;

            // Clear trees
            terrainData.SetTreeInstances(new TreeInstance[0], false);

            // Clear detail layers
            for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
            {
                int[,] empty = new int[terrainData.detailResolution, terrainData.detailResolution];
                terrainData.SetDetailLayer(0, 0, i, empty);
            }

            _lastTreeCount = 0;
            _statusText    = "Foliage cleared.";
            SceneView.RepaintAll();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Biome Presets
        // ─────────────────────────────────────────────────────────────────────

        private void ApplyBiomePreset(BiomePreset biome)
        {
            switch (biome)
            {
                case BiomePreset.Alpine:
                    _terrainScale       = 120f;
                    _terrainOctaves     = 6;
                    _terrainPersistence = 0.55f;
                    _terrainLacunarity  = 2.2f;
                    _terrainHeightScale = 0.6f;
                    _rockDensity        = 0.5f;
                    _rockMinRadius      = 4f;
                    _rockScaleMin       = 0.8f;
                    _rockScaleMax       = 4.0f;
                    _rockSlopeMax       = 60f;
                    _grassDensity       = 0.2f;
                    _treeDensity        = 0.05f;
                    break;

                case BiomePreset.Desert:
                    _terrainScale       = 200f;
                    _terrainOctaves     = 3;
                    _terrainPersistence = 0.3f;
                    _terrainLacunarity  = 1.8f;
                    _terrainHeightScale = 0.15f;
                    _rockDensity        = 0.15f;
                    _rockMinRadius      = 8f;
                    _rockScaleMin       = 0.3f;
                    _rockScaleMax       = 1.5f;
                    _rockSlopeMax       = 15f;
                    _grassDensity       = 0.05f;
                    _treeDensity        = 0.02f;
                    break;

                case BiomePreset.Forest:
                    _terrainScale       = 80f;
                    _terrainOctaves     = 5;
                    _terrainPersistence = 0.45f;
                    _terrainLacunarity  = 2.1f;
                    _terrainHeightScale = 0.25f;
                    _rockDensity        = 0.3f;
                    _rockMinRadius      = 3f;
                    _rockScaleMin       = 0.5f;
                    _rockScaleMax       = 2.0f;
                    _rockSlopeMax       = 30f;
                    _grassDensity       = 0.7f;
                    _treeDensity        = 0.4f;
                    break;

                case BiomePreset.Tundra:
                    _terrainScale       = 300f;
                    _terrainOctaves     = 4;
                    _terrainPersistence = 0.35f;
                    _terrainLacunarity  = 1.9f;
                    _terrainHeightScale = 0.08f;
                    _rockDensity        = 0.1f;
                    _rockMinRadius      = 5f;
                    _rockScaleMin       = 0.2f;
                    _rockScaleMax       = 0.8f;
                    _rockSlopeMax       = 10f;
                    _grassDensity       = 0.15f;
                    _treeDensity        = 0.01f;
                    break;

                case BiomePreset.Coastal:
                    _terrainScale       = 150f;
                    _terrainOctaves     = 4;
                    _terrainPersistence = 0.4f;
                    _terrainLacunarity  = 2.0f;
                    _terrainHeightScale = 0.12f;
                    _rockDensity        = 0.25f;
                    _rockMinRadius      = 5f;
                    _rockScaleMin       = 0.3f;
                    _rockScaleMax       = 1.5f;
                    _rockSlopeMax       = 20f;
                    _grassDensity       = 0.3f;
                    _treeDensity        = 0.1f;
                    break;
            }
        }
    }
}

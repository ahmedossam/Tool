using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;
using System.Collections.Generic;
using System.Linq;

namespace TechArtToolkit.Editor.Modules
{
    /// <summary>
    /// MODULE 2: VFX Performance Tester
    ///
    /// WHAT:  Spawns VFX Graph effects in the scene and displays real-time
    ///        performance metrics: particle count, FPS, frame time, GPU time,
    ///        and overdraw estimation. Allows switching between an optimized
    ///        and unoptimized version of the same effect to show the delta.
    ///
    /// WHY:   Demonstrates VFX optimization skills — understanding GPU cost
    ///        of particle systems and how to reduce overdraw, particle count,
    ///        and shader complexity without sacrificing visual quality.
    ///
    /// HOW:   Uses VisualEffect.aliveParticleCount for particle metrics.
    ///        Uses FrameTimingManager for GPU/CPU timing.
    ///        EditorApplication.update polls metrics at 10Hz.
    ///        Side-by-side comparison table shows optimized vs unoptimized delta.
    /// </summary>
    public partial class VFXPerformanceTester : ModuleBase
    {
        // ─────────────────────────────────────────────────────────────────────
        // Identity
        // ─────────────────────────────────────────────────────────────────────

        public override string ModuleName        => "VFX Performance Tester";
        public override string ModuleDescription => "Spawns VFX Graph effects and measures particle count, overdraw, FPS, and GPU time. Compare optimized vs unoptimized effects.";
        public override string ModuleIcon        => "d_ParticleSystem Icon";

        // ─────────────────────────────────────────────────────────────────────
        // VFX Asset References
        // ─────────────────────────────────────────────────────────────────────

        private VisualEffectAsset _optimizedVFX;
        private VisualEffectAsset _unoptimizedVFX;

        // ─────────────────────────────────────────────────────────────────────
        // Spawned Objects
        // ─────────────────────────────────────────────────────────────────────

        private GameObject    _spawnedOptimized;
        private GameObject    _spawnedUnoptimized;
        private VisualEffect  _activeVFXComponent;
        private bool          _isOptimizedActive = true;

        // ─────────────────────────────────────────────────────────────────────
        // Metrics State
        // ─────────────────────────────────────────────────────────────────────

        private struct EffectMetrics
        {
            public int   ParticleCount;
            public float FPS;
            public float FrameTimeMs;
            public float GPUTimeMs;
            public float CPUTimeMs;
            public int   DrawCalls;
            public float OverdrawEstimate;
            public string EffectName;
        }

        private EffectMetrics _optimizedMetrics;
        private EffectMetrics _unoptimizedMetrics;
        private EffectMetrics _currentMetrics;

        // Metric history for sparkline graph
        private const int HISTORY_SIZE = 60;
        private Queue<float> _fpsHistory    = new Queue<float>(HISTORY_SIZE);
        private Queue<float> _particleHistory = new Queue<float>(HISTORY_SIZE);

        // Polling
        private double _lastMetricPollTime = 0;
        private const double POLL_INTERVAL = 0.1; // 10Hz

        // ─────────────────────────────────────────────────────────────────────
        // Spawn Settings
        // ─────────────────────────────────────────────────────────────────────

        private Vector3 _spawnPosition = new Vector3(0, 0, 0);
        private float   _spawnScale    = 1.0f;
        private bool    _autoRotate    = false;

        // ─────────────────────────────────────────────────────────────────────
        // Optimization Tips
        // ─────────────────────────────────────────────────────────────────────

        private static readonly string[] OPTIMIZATION_TIPS = new string[]
        {
            "💡 Use GPU Sprite emitters instead of CPU for large particle counts",
            "💡 Use texture atlases (SubUV flipbooks) to reduce draw calls",
            "💡 Enable Frustum Culling to skip off-screen particles",
            "💡 Use Depth Buffer Collision instead of mesh collision",
            "💡 Reduce overdraw by using smaller, more opaque particles",
            "💡 Use LOD on VFX Graph to reduce count at distance",
            "💡 Bake static effects to flipbook textures for hero shots",
            "💡 Use Ribbon emitters sparingly — they are CPU-bound",
        };
        private int _currentTipIndex = 0;

        // ─────────────────────────────────────────────────────────────────────
        // UI State
        // ─────────────────────────────────────────────────────────────────────

        private bool _foldAssets      = true;
        private bool _foldMetrics     = true;
        private bool _foldComparison  = true;
        private bool _foldTips        = true;
        private Vector2 _scrollPos;

        // ─────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        public override void OnEnable(EditorWindow parentWindow)
        {
            base.OnEnable(parentWindow);
            EditorApplication.update += OnEditorUpdate;
            FrameTimingManager.CaptureFrameTimings();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            EditorApplication.update -= OnEditorUpdate;
        }

        public override void OnDestroy()
        {
            CleanupSpawnedObjects();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Editor Update — Metrics Polling
        // ─────────────────────────────────────────────────────────────────────

        private void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastMetricPollTime < POLL_INTERVAL) return;
            _lastMetricPollTime = now;

            PollMetrics();
            RequestRepaint();
        }

        private void PollMetrics()
        {
            // Capture frame timings from GPU
            FrameTimingManager.CaptureFrameTimings();
            FrameTiming[] timings = new FrameTiming[1];
            uint captured = FrameTimingManager.GetLatestTimings(1, timings);

            float gpuTime = captured > 0 ? (float)timings[0].gpuFrameTime : 0f;
            float cpuTime = captured > 0 ? (float)timings[0].cpuFrameTime : 0f;
            float fps     = Time.deltaTime > 0 ? 1f / Time.deltaTime : 0f;
            float frameMs = Time.deltaTime * 1000f;

            // Particle count from active VFX component
            int particleCount = 0;
            if (_activeVFXComponent != null && _activeVFXComponent.isActiveAndEnabled)
                particleCount = _activeVFXComponent.aliveParticleCount;

            // Estimate overdraw (simplified: particles * avg_size / screen_area)
            float overdrawEst = EstimateOverdraw(particleCount);

            _currentMetrics = new EffectMetrics
            {
                ParticleCount    = particleCount,
                FPS              = fps,
                FrameTimeMs      = frameMs,
                GPUTimeMs        = gpuTime,
                CPUTimeMs        = cpuTime,
                DrawCalls        = UnityStats.drawCalls,
                OverdrawEstimate = overdrawEst,
                EffectName       = _isOptimizedActive ? "Optimized" : "Unoptimized"
            };

            // Store in appropriate slot for comparison
            if (_isOptimizedActive)
                _optimizedMetrics = _currentMetrics;
            else
                _unoptimizedMetrics = _currentMetrics;

            // Update history queues
            EnqueueHistory(_fpsHistory, fps);
            EnqueueHistory(_particleHistory, particleCount);
        }

        private float EstimateOverdraw(int particleCount)
        {
            // Simplified overdraw estimate:
            // Assumes average particle covers ~2% of screen at 1080p
            // Real overdraw requires GPU profiler (RenderDoc, Nsight)
            const float avgParticleCoverage = 0.02f;
            return particleCount * avgParticleCoverage;
        }

        private void EnqueueHistory(Queue<float> queue, float value)
        {
            if (queue.Count >= HISTORY_SIZE)
                queue.Dequeue();
            queue.Enqueue(value);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Main GUI
        // ─────────────────────────────────────────────────────────────────────

        public override void DrawGUI()
        {
            DrawHeader();

            DrawInfoBox(
                "Assign VFX Graph assets below, spawn them in the scene, and observe " +
                "real-time performance metrics. Switch between optimized/unoptimized " +
                "to see the performance delta. Demonstrates VFX budget management.");

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawAssetSection();
            DrawSpawnControls();
            DrawMetricsSection();
            DrawComparisonSection();
            DrawSparklineGraph();
            DrawOptimizationTips();

            EditorGUILayout.EndScrollView();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Asset Assignment
        // ─────────────────────────────────────────────────────────────────────

        private void DrawAssetSection()
        {
            _foldAssets = EditorGUILayout.BeginFoldoutHeaderGroup(_foldAssets, "📦  VFX Graph Assets");
            if (_foldAssets)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    // Optimized VFX
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var greenStyle = new GUIStyle(EditorStyles.label)
                        { normal = { textColor = new Color(0.2f, 0.8f, 0.2f) } };
                        EditorGUILayout.LabelField("✓ Optimized Effect:", greenStyle, GUILayout.Width(140));
                        _optimizedVFX = (VisualEffectAsset)EditorGUILayout.ObjectField(
                            _optimizedVFX, typeof(VisualEffectAsset), false);
                    }

                    EditorGUILayout.LabelField(
                        "   Target: <200 particles, GPU emitter, atlas UVs, frustum culling ON",
                        new GUIStyle(EditorStyles.miniLabel)
                        { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } });

                    EditorGUILayout.Space(4);

                    // Unoptimized VFX
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var redStyle = new GUIStyle(EditorStyles.label)
                        { normal = { textColor = new Color(0.9f, 0.3f, 0.3f) } };
                        EditorGUILayout.LabelField("✗ Unoptimized Effect:", redStyle, GUILayout.Width(140));
                        _unoptimizedVFX = (VisualEffectAsset)EditorGUILayout.ObjectField(
                            _unoptimizedVFX, typeof(VisualEffectAsset), false);
                    }

                    EditorGUILayout.LabelField(
                        "   Target: >2000 particles, CPU emitter, no atlas, no culling",
                        new GUIStyle(EditorStyles.miniLabel)
                        { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } });
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Spawn Controls
        // ─────────────────────────────────────────────────────────────────────

        private void DrawSpawnControls()
        {
            EditorGUILayout.LabelField("⚙  Spawn Controls", SubHeaderStyle);
            using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
            {
                _spawnPosition = EditorGUILayout.Vector3Field("Spawn Position", _spawnPosition);
                _spawnScale    = EditorGUILayout.Slider("Scale", _spawnScale, 0.1f, 5f);
                _autoRotate    = EditorGUILayout.Toggle("Auto-Rotate", _autoRotate);

                EditorGUILayout.Space(6);

                // Active effect toggle
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Active Effect:", GUILayout.Width(100));

                    Color origColor = GUI.backgroundColor;

                    GUI.backgroundColor = _isOptimizedActive
                        ? new Color(0.3f, 0.7f, 0.3f)
                        : new Color(0.4f, 0.4f, 0.4f);
                    if (GUILayout.Button("✓ Optimized", GUILayout.Height(28)))
                        SwitchToEffect(true);

                    GUI.backgroundColor = !_isOptimizedActive
                        ? new Color(0.8f, 0.3f, 0.3f)
                        : new Color(0.4f, 0.4f, 0.4f);
                    if (GUILayout.Button("✗ Unoptimized", GUILayout.Height(28)))
                        SwitchToEffect(false);

                    GUI.backgroundColor = origColor;
                }

                EditorGUILayout.Space(4);

                // Spawn / Stop buttons
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (DrawActionButton("▶  Spawn Effect", 160, 28))
                        SpawnActiveEffect();

                    GUILayout.Space(8);

                    if (DrawDestructiveButton("■  Stop & Clear", 160, 28))
                        CleanupSpawnedObjects();
                }

                // Status
                bool hasActive = _activeVFXComponent != null;
                string statusText = hasActive
                    ? $"● Active: {_currentMetrics.EffectName} effect running"
                    : "○ No effect spawned";
                var statusStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = hasActive ? new Color(0.2f, 0.8f, 0.2f) : Color.gray }
                };
                EditorGUILayout.LabelField(statusText, statusStyle);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Live Metrics
        // ─────────────────────────────────────────────────────────────────────

        private void DrawMetricsSection()
        {
            _foldMetrics = EditorGUILayout.BeginFoldoutHeaderGroup(_foldMetrics, "📊  Live Metrics");
            if (_foldMetrics)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    if (_activeVFXComponent == null)
                    {
                        EditorGUILayout.LabelField("Spawn an effect to see live metrics.",
                            new GUIStyle(EditorStyles.centeredGreyMiniLabel));
                    }
                    else
                    {
                        // FPS
                        MetricStatus fpsStatus = EvaluateBudget(_currentMetrics.FPS, 60f, 30f);
                        // Note: for FPS, higher is better, so invert thresholds
                        fpsStatus = _currentMetrics.FPS >= 60f ? MetricStatus.Good :
                                    _currentMetrics.FPS >= 30f ? MetricStatus.Warning : MetricStatus.Bad;
                        DrawMetricRow("FPS",
                            $"{_currentMetrics.FPS:F1} fps", fpsStatus);

                        // Frame Time
                        MetricStatus ftStatus = _currentMetrics.FrameTimeMs <= 16.67f ? MetricStatus.Good :
                                                _currentMetrics.FrameTimeMs <= 33.33f ? MetricStatus.Warning :
                                                MetricStatus.Bad;
                        DrawMetricRow("Frame Time",
                            $"{_currentMetrics.FrameTimeMs:F2} ms", ftStatus);

                        // GPU Time
                        DrawMetricRow("GPU Time",
                            _currentMetrics.GPUTimeMs > 0
                                ? $"{_currentMetrics.GPUTimeMs:F2} ms"
                                : "N/A (requires GPU profiler)");

                        // Particle Count
                        MetricStatus pcStatus = EvaluateBudget(_currentMetrics.ParticleCount, 500, 2000);
                        DrawMetricRow("Particle Count",
                            _currentMetrics.ParticleCount.ToString("N0"), pcStatus);

                        // Draw Calls
                        MetricStatus dcStatus = EvaluateBudget(_currentMetrics.DrawCalls, 100, 300);
                        DrawMetricRow("Draw Calls (Scene)",
                            _currentMetrics.DrawCalls.ToString("N0"), dcStatus);

                        // Overdraw Estimate
                        MetricStatus odStatus = EvaluateBudget(_currentMetrics.OverdrawEstimate, 2f, 5f);
                        DrawMetricRow("Overdraw Estimate",
                            $"{_currentMetrics.OverdrawEstimate:F1}x", odStatus);

                        // Budget bar for particle count
                        EditorGUILayout.Space(4);
                        DrawBudgetBar("Particle Budget", _currentMetrics.ParticleCount, 2000);
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Comparison Table
        // ─────────────────────────────────────────────────────────────────────

        private void DrawComparisonSection()
        {
            _foldComparison = EditorGUILayout.BeginFoldoutHeaderGroup(_foldComparison, "⚖  Optimized vs Unoptimized Comparison");
            if (_foldComparison)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    // Header row
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 10 };
                        EditorGUILayout.LabelField("Metric",       headerStyle, GUILayout.Width(160));
                        EditorGUILayout.LabelField("Optimized",    headerStyle, GUILayout.Width(100));
                        EditorGUILayout.LabelField("Unoptimized",  headerStyle, GUILayout.Width(100));
                        EditorGUILayout.LabelField("Delta",        headerStyle, GUILayout.Width(80));
                    }

                    DrawHorizontalLine(new Color(0.3f, 0.3f, 0.3f), 1f, 2f);

                    bool hasOpt   = _optimizedMetrics.ParticleCount > 0 || _optimizedMetrics.FPS > 0;
                    bool hasUnopt = _unoptimizedMetrics.ParticleCount > 0 || _unoptimizedMetrics.FPS > 0;

                    if (!hasOpt && !hasUnopt)
                    {
                        EditorGUILayout.LabelField(
                            "Spawn both effects and record metrics to populate this table.",
                            new GUIStyle(EditorStyles.centeredGreyMiniLabel));
                    }
                    else
                    {
                        // FPS comparison (higher is better)
                        DrawComparisonRow("FPS",
                            hasOpt   ? $"{_optimizedMetrics.FPS:F1}"   : "—",
                            hasUnopt ? $"{_unoptimizedMetrics.FPS:F1}" : "—",
                            ComputeDelta(_optimizedMetrics.FPS, _unoptimizedMetrics.FPS, true),
                            lowerIsBetter: false);

                        // Frame Time (lower is better)
                        DrawComparisonRow("Frame Time (ms)",
                            hasOpt   ? $"{_optimizedMetrics.FrameTimeMs:F2}"   : "—",
                            hasUnopt ? $"{_unoptimizedMetrics.FrameTimeMs:F2}" : "—",
                            ComputeDelta(_optimizedMetrics.FrameTimeMs, _unoptimizedMetrics.FrameTimeMs, false),
                            lowerIsBetter: true);

                        // Particle Count (lower is better)
                        DrawComparisonRow("Particle Count",
                            hasOpt   ? _optimizedMetrics.ParticleCount.ToString("N0")   : "—",
                            hasUnopt ? _unoptimizedMetrics.ParticleCount.ToString("N0") : "—",
                            ComputeDelta(_optimizedMetrics.ParticleCount, _unoptimizedMetrics.ParticleCount, false),
                            lowerIsBetter: true);

                        // Draw Calls (lower is better)
                        DrawComparisonRow("Draw Calls",
                            hasOpt   ? _optimizedMetrics.DrawCalls.ToString()   : "—",
                            hasUnopt ? _unoptimizedMetrics.DrawCalls.ToString() : "—",
                            ComputeDelta(_optimizedMetrics.DrawCalls, _unoptimizedMetrics.DrawCalls, false),
                            lowerIsBetter: true);

                        // Overdraw (lower is better)
                        DrawComparisonRow("Overdraw Est.",
                            hasOpt   ? $"{_optimizedMetrics.OverdrawEstimate:F1}x"   : "—",
                            hasUnopt ? $"{_unoptimizedMetrics.OverdrawEstimate:F1}x" : "—",
                            ComputeDelta(_optimizedMetrics.OverdrawEstimate, _unoptimizedMetrics.OverdrawEstimate, false),
                            lowerIsBetter: true);
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField(
                        "Tip: Spawn optimized effect first, record metrics, then switch to unoptimized.",
                        new GUIStyle(EditorStyles.miniLabel)
                        { normal = { textColor = new Color(0.5f, 0.7f, 0.9f) } });
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Sparkline Graph
        // ─────────────────────────────────────────────────────────────────────

        private void DrawSparklineGraph()
        {
            EditorGUILayout.LabelField("📈  FPS History (last 6 seconds)", SubHeaderStyle);
            using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
            {
                Rect graphRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                    GUILayout.Height(60), GUILayout.ExpandWidth(true));

                if (Event.current.type == EventType.Repaint)
                    DrawSparkline(graphRect, _fpsHistory, 0f, 120f, new Color(0.3f, 0.7f, 1.0f));

                // 60fps reference line label
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("0 fps", GUILayout.Width(40));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("— 60fps target", new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.2f, 0.8f, 0.2f) } });
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("120 fps", GUILayout.Width(50));
                }
            }
        }

        private void DrawSparkline(Rect rect, Queue<float> data, float minVal, float maxVal, Color lineColor)
        {
            if (data.Count < 2) return;

            // Background
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));

            // 60fps reference line
            float refY = rect.yMax - ((60f - minVal) / (maxVal - minVal)) * rect.height;
            EditorGUI.DrawRect(new Rect(rect.x, refY, rect.width, 1f), new Color(0.2f, 0.6f, 0.2f, 0.5f));

            // Draw sparkline
            float[] values = data.ToArray();
            float stepX = rect.width / (values.Length - 1);

            Handles.color = lineColor;
            for (int i = 1; i < values.Length; i++)
            {
                float x0 = rect.x + (i - 1) * stepX;
                float x1 = rect.x + i * stepX;
                float y0 = rect.yMax - Mathf.Clamp01((values[i - 1] - minVal) / (maxVal - minVal)) * rect.height;
                float y1 = rect.yMax - Mathf.Clamp01((values[i]     - minVal) / (maxVal - minVal)) * rect.height;
                Handles.DrawLine(new Vector3(x0, y0), new Vector3(x1, y1));
            }

            // Border
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), Color.gray);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), Color.gray);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Section: Optimization Tips
        // ─────────────────────────────────────────────────────────────────────

        private void DrawOptimizationTips()
        {
            _foldTips = EditorGUILayout.BeginFoldoutHeaderGroup(_foldTips, "💡  VFX Optimization Tips");
            if (_foldTips)
            {
                using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
                {
                    EditorGUILayout.LabelField(OPTIMIZATION_TIPS[_currentTipIndex],
                        new GUIStyle(EditorStyles.wordWrappedLabel)
                        { normal = { textColor = new Color(0.8f, 0.8f, 0.4f) } });

                    EditorGUILayout.Space(4);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("◀ Prev", GUILayout.Width(70)))
                            _currentTipIndex = (_currentTipIndex - 1 + OPTIMIZATION_TIPS.Length) % OPTIMIZATION_TIPS.Length;
                        EditorGUILayout.LabelField(
                            $"{_currentTipIndex + 1} / {OPTIMIZATION_TIPS.Length}",
                            new GUIStyle(EditorStyles.centeredGreyMiniLabel));
                        if (GUILayout.Button("Next ▶", GUILayout.Width(70)))
                            _currentTipIndex = (_currentTipIndex + 1) % OPTIMIZATION_TIPS.Length;
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Spawn / Cleanup
        // ─────────────────────────────────────────────────────────────────────

        private void SpawnActiveEffect()
        {
            CleanupSpawnedObjects();

            VisualEffectAsset asset = _isOptimizedActive ? _optimizedVFX : _unoptimizedVFX;
            if (asset == null)
            {
                DrawWarningBox(_isOptimizedActive
                    ? "No optimized VFX asset assigned."
                    : "No unoptimized VFX asset assigned.");
                return;
            }

            string name = _isOptimizedActive ? "[TAT] Optimized VFX" : "[TAT] Unoptimized VFX";
            var go = new GameObject(name);
            go.transform.position = _spawnPosition;
            go.transform.localScale = Vector3.one * _spawnScale;

            var vfx = go.AddComponent<VisualEffect>();
            vfx.visualEffectAsset = asset;
            vfx.Play();

            if (_isOptimizedActive)
                _spawnedOptimized = go;
            else
                _spawnedUnoptimized = go;

            _activeVFXComponent = vfx;

            // Register for undo
            Undo.RegisterCreatedObjectUndo(go, $"Spawn {name}");

            // Reset history
            _fpsHistory.Clear();
            _particleHistory.Clear();
        }

        private void SwitchToEffect(bool useOptimized)
        {
            _isOptimizedActive = useOptimized;
            // If an effect is currently spawned, respawn with new asset
            if (_activeVFXComponent != null)
                SpawnActiveEffect();
        }

        // CleanupSpawnedObjects(), ComputeDelta(), and DrawBudgetBar()
        // are implemented in VFXPerformanceTester.Helpers.cs (partial class).
    }
}

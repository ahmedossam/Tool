using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TechArtToolkit.Editor.Modules;

namespace TechArtToolkit.Editor
{
    /// <summary>
    /// Main EditorWindow for the Tech Art Toolkit.
    /// Hosts all 5 modules as tabs and manages their lifecycle.
    ///
    /// Open via: Tools → Tech Art Toolkit  (Unity menu bar)
    ///
    /// Architecture:
    ///   - Maintains a list of ModuleBase instances
    ///   - Renders a tab toolbar at the top
    ///   - Delegates DrawGUI() to the active module
    ///   - Calls OnEnable/OnDisable on tab switches
    ///   - Provides a status bar at the bottom
    /// </summary>
    public class TechArtToolkitWindow : EditorWindow
    {
        // ─────────────────────────────────────────────────────────────────────
        // Constants
        // ─────────────────────────────────────────────────────────────────────

        private const string WINDOW_TITLE    = "Tech Art Toolkit";
        private const float  MIN_WINDOW_WIDTH  = 520f;
        private const float  MIN_WINDOW_HEIGHT = 600f;
        private const float  TAB_HEIGHT       = 32f;
        private const float  STATUS_BAR_HEIGHT = 22f;
        private const string PREFS_ACTIVE_TAB  = "TechArtToolkit_ActiveTab";

        // ─────────────────────────────────────────────────────────────────────
        // Module Registry
        // ─────────────────────────────────────────────────────────────────────

        private List<ModuleBase> _modules;
        private int _activeTabIndex = 0;
        private int _previousTabIndex = -1;

        // Tab labels with icons (Unity built-in icon names)
        private readonly GUIContent[] _tabLabels = new GUIContent[]
        {
            new GUIContent("  Shader Lab",    EditorGUIUtility.IconContent("d_ShaderGraph Icon").image),
            new GUIContent("  VFX Tester",    EditorGUIUtility.IconContent("d_ParticleSystem Icon").image),
            new GUIContent("  Lighting",      EditorGUIUtility.IconContent("d_Light Icon").image),
            new GUIContent("  Asset Opt.",    EditorGUIUtility.IconContent("d_Mesh Icon").image),
            new GUIContent("  Procedural",    EditorGUIUtility.IconContent("d_Terrain Icon").image),
        };

        // ─────────────────────────────────────────────────────────────────────
        // UI State
        // ─────────────────────────────────────────────────────────────────────

        private Vector2 _scrollPosition;
        private string  _statusMessage = "Ready";
        private double  _statusMessageTime;
        private bool    _showWelcomeBanner = true;

        // Styles
        private GUIStyle _tabStyle;
        private GUIStyle _activeTabStyle;
        private GUIStyle _statusBarStyle;
        private GUIStyle _bannerStyle;
        private bool     _stylesInitialized = false;

        // ─────────────────────────────────────────────────────────────────────
        // Window Entry Point
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Opens the Tech Art Toolkit window from the Unity menu bar.
        /// </summary>
        [MenuItem("Tools/Tech Art Toolkit", priority = 100)]
        public static void OpenWindow()
        {
            var window = GetWindow<TechArtToolkitWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }

        /// <summary>
        /// Opens the window and navigates directly to a specific module tab.
        /// Useful for deep-linking from other editor tools.
        /// </summary>
        public static void OpenWindowAtTab(int tabIndex)
        {
            var window = GetWindow<TechArtToolkitWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window._activeTabIndex = Mathf.Clamp(tabIndex, 0, 4);
            window.Show();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Unity EditorWindow Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            // Restore last active tab from EditorPrefs
            _activeTabIndex = EditorPrefs.GetInt(PREFS_ACTIVE_TAB, 0);

            // Initialize all modules
            _modules = new List<ModuleBase>
            {
                new ShaderProceduralLab(),
                new VFXPerformanceTester(),
                new LightingLookDevTool(),
                new AssetOptimizationTool(),
                new ProceduralEnvironmentGenerator()
            };

            // Enable the initially active module
            if (_activeTabIndex < _modules.Count)
            {
                _modules[_activeTabIndex].OnEnable(this);
                _previousTabIndex = _activeTabIndex;
            }

            // Subscribe to editor update for real-time metrics
            EditorApplication.update += OnEditorUpdate;

            SetStatus("Tech Art Toolkit loaded — " + _modules.Count + " modules ready.");
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;

            // Disable all modules cleanly
            if (_modules != null)
            {
                foreach (var module in _modules)
                    module.OnDisable();
            }

            // Save active tab
            EditorPrefs.SetInt(PREFS_ACTIVE_TAB, _activeTabIndex);
        }

        private void OnDestroy()
        {
            if (_modules != null)
            {
                foreach (var module in _modules)
                    module.OnDestroy();
            }
        }

        private void OnEditorUpdate()
        {
            // Clear status message after 5 seconds
            if (!string.IsNullOrEmpty(_statusMessage) &&
                EditorApplication.timeSinceStartup - _statusMessageTime > 5.0)
            {
                _statusMessage = "Ready";
                Repaint();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Main GUI
        // ─────────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            InitializeStyles();

            // ── Top Banner ──────────────────────────────────────────────────
            if (_showWelcomeBanner)
                DrawWelcomeBanner();

            // ── Tab Toolbar ─────────────────────────────────────────────────
            DrawTabToolbar();

            // ── Handle Tab Switch ────────────────────────────────────────────
            HandleTabSwitch();

            // ── Module Content (scrollable) ──────────────────────────────────
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                if (_modules != null && _activeTabIndex < _modules.Count)
                {
                    EditorGUI.BeginChangeCheck();
                    _modules[_activeTabIndex].DrawGUI();
                    if (EditorGUI.EndChangeCheck())
                        Repaint();
                }
            }
            EditorGUILayout.EndScrollView();

            // ── Status Bar ───────────────────────────────────────────────────
            DrawStatusBar();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Drawing Sub-Methods
        // ─────────────────────────────────────────────────────────────────────

        private void DrawWelcomeBanner()
        {
            using (new EditorGUILayout.HorizontalScope(_bannerStyle))
            {
                EditorGUILayout.LabelField(
                    "🎨  Cross-Engine Technical Art Toolkit  |  Unity URP + Unreal Engine 5",
                    new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 11,
                        normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
                    });

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("✕", GUILayout.Width(20), GUILayout.Height(18)))
                    _showWelcomeBanner = false;
            }
        }

        private void DrawTabToolbar()
        {
            EditorGUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Space(4);

                // Render each tab as a toggle button
                for (int i = 0; i < _tabLabels.Length; i++)
                {
                    bool isActive = (i == _activeTabIndex);
                    GUIStyle style = isActive ? _activeTabStyle : _tabStyle;

                    if (GUILayout.Toggle(isActive, _tabLabels[i], style,
                        GUILayout.Height(TAB_HEIGHT), GUILayout.MinWidth(90)))
                    {
                        if (!isActive)
                            _activeTabIndex = i;
                    }
                }

                EditorGUILayout.Space(4);
            }

            // Tab underline
            Rect lineRect = EditorGUILayout.GetControlRect(false, 2f);
            EditorGUI.DrawRect(lineRect, new Color(0.2f, 0.5f, 0.9f, 0.8f));
            EditorGUILayout.Space(4);
        }

        private void HandleTabSwitch()
        {
            if (_activeTabIndex == _previousTabIndex || _modules == null)
                return;

            // Disable previous module
            if (_previousTabIndex >= 0 && _previousTabIndex < _modules.Count)
                _modules[_previousTabIndex].OnDisable();

            // Enable new module
            if (_activeTabIndex < _modules.Count)
            {
                _modules[_activeTabIndex].OnEnable(this);
                SetStatus($"Switched to: {_modules[_activeTabIndex].ModuleName}");
            }

            _previousTabIndex = _activeTabIndex;
            EditorPrefs.SetInt(PREFS_ACTIVE_TAB, _activeTabIndex);
        }

        private void DrawStatusBar()
        {
            // Separator line
            Rect sepRect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(sepRect, new Color(0.2f, 0.2f, 0.2f));

            using (new EditorGUILayout.HorizontalScope(_statusBarStyle,
                GUILayout.Height(STATUS_BAR_HEIGHT)))
            {
                // Status icon
                string icon = _statusMessage.StartsWith("⚠") ? "console.warnicon.sml" :
                              _statusMessage.StartsWith("✕") ? "console.erroricon.sml" :
                              "console.infoicon.sml";

                GUILayout.Label(EditorGUIUtility.IconContent(icon),
                    GUILayout.Width(18), GUILayout.Height(18));

                // Status message
                EditorGUILayout.LabelField(_statusMessage,
                    new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                    });

                GUILayout.FlexibleSpace();

                // Module indicator
                if (_modules != null && _activeTabIndex < _modules.Count)
                {
                    EditorGUILayout.LabelField(
                        $"Module {_activeTabIndex + 1}/{_modules.Count}  |  " +
                        $"{_modules[_activeTabIndex].ModuleName}",
                        new GUIStyle(EditorStyles.miniLabel)
                        {
                            normal = { textColor = new Color(0.5f, 0.5f, 0.5f) },
                            alignment = TextAnchor.MiddleRight
                        },
                        GUILayout.Width(220));
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Style Initialization
        // ─────────────────────────────────────────────────────────────────────

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _tabStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                fixedHeight = TAB_HEIGHT,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 4, 4)
            };

            _activeTabStyle = new GUIStyle(_tabStyle)
            {
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = new Color(0.3f, 0.7f, 1.0f),
                    background = MakeSolidTexture(new Color(0.15f, 0.15f, 0.2f))
                }
            };

            _statusBarStyle = new GUIStyle()
            {
                padding = new RectOffset(6, 6, 2, 2),
                normal = { background = MakeSolidTexture(new Color(0.18f, 0.18f, 0.18f)) }
            };

            _bannerStyle = new GUIStyle()
            {
                padding = new RectOffset(8, 8, 4, 4),
                normal = { background = MakeSolidTexture(new Color(0.12f, 0.18f, 0.28f)) }
            };

            _stylesInitialized = true;
        }

        /// <summary>Creates a 1x1 solid color Texture2D for use as a GUIStyle background.</summary>
        private static Texture2D MakeSolidTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API (for modules to call back into the window)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the status bar message. Automatically clears after 5 seconds.
        /// Prefix with "⚠" for warning style, "✕" for error style.
        /// </summary>
        public void SetStatus(string message)
        {
            _statusMessage = message;
            _statusMessageTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        /// <summary>
        /// Navigates to a specific module tab by index (0–4).
        /// </summary>
        public void NavigateToTab(int index)
        {
            _activeTabIndex = Mathf.Clamp(index, 0, _modules.Count - 1);
            Repaint();
        }

        /// <summary>
        /// Returns the currently active module.
        /// </summary>
        public ModuleBase GetActiveModule()
        {
            if (_modules == null || _activeTabIndex >= _modules.Count)
                return null;
            return _modules[_activeTabIndex];
        }
    }
}

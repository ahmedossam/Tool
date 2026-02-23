using UnityEngine;
using UnityEditor;

namespace TechArtToolkit.Editor
{
    /// <summary>
    /// Abstract base class for all Tech Art Toolkit modules.
    /// Each module is a self-contained panel rendered inside TechArtToolkitWindow.
    /// 
    /// Lifecycle:
    ///   OnEnable()  → called when the module tab becomes active
    ///   OnDisable() → called when switching away from this module
    ///   DrawGUI()   → called every OnGUI() frame while this module is active
    /// </summary>
    public abstract class ModuleBase
    {
        // ─────────────────────────────────────────────────────────────────────
        // Identity
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Display name shown in the tab bar.</summary>
        public abstract string ModuleName { get; }

        /// <summary>Short description shown in the module header.</summary>
        public abstract string ModuleDescription { get; }

        /// <summary>Icon name from EditorGUIUtility.IconContent (optional).</summary>
        public virtual string ModuleIcon => "d_UnityEditor.InspectorWindow";

        // ─────────────────────────────────────────────────────────────────────
        // State
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Whether this module has been initialized.</summary>
        protected bool _isInitialized = false;

        /// <summary>Reference to the parent EditorWindow for Repaint() calls.</summary>
        protected EditorWindow _parentWindow;

        // ─────────────────────────────────────────────────────────────────────
        // Shared Styles (initialized lazily)
        // ─────────────────────────────────────────────────────────────────────

        private static GUIStyle _headerStyle;
        private static GUIStyle _subHeaderStyle;
        private static GUIStyle _descriptionStyle;
        private static GUIStyle _sectionBoxStyle;
        private static GUIStyle _metricLabelStyle;
        private static GUIStyle _goodValueStyle;
        private static GUIStyle _badValueStyle;
        private static GUIStyle _warningValueStyle;

        protected static GUIStyle HeaderStyle
        {
            get
            {
                if (_headerStyle == null)
                {
                    _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 16,
                        margin = new RectOffset(4, 4, 8, 4)
                    };
                }
                return _headerStyle;
            }
        }

        protected static GUIStyle SubHeaderStyle
        {
            get
            {
                if (_subHeaderStyle == null)
                {
                    _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                        margin = new RectOffset(4, 4, 6, 2)
                    };
                }
                return _subHeaderStyle;
            }
        }

        protected static GUIStyle DescriptionStyle
        {
            get
            {
                if (_descriptionStyle == null)
                {
                    _descriptionStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                    {
                        fontSize = 11,
                        fontStyle = FontStyle.Italic,
                        normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                    };
                }
                return _descriptionStyle;
            }
        }

        protected static GUIStyle SectionBoxStyle
        {
            get
            {
                if (_sectionBoxStyle == null)
                {
                    _sectionBoxStyle = new GUIStyle("box")
                    {
                        padding = new RectOffset(8, 8, 6, 6),
                        margin = new RectOffset(4, 4, 4, 4)
                    };
                }
                return _sectionBoxStyle;
            }
        }

        protected static GUIStyle MetricLabelStyle
        {
            get
            {
                if (_metricLabelStyle == null)
                {
                    _metricLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 11,
                        fontStyle = FontStyle.Bold
                    };
                }
                return _metricLabelStyle;
            }
        }

        protected static GUIStyle GoodValueStyle
        {
            get
            {
                if (_goodValueStyle == null)
                {
                    _goodValueStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                        fontStyle = FontStyle.Bold
                    };
                }
                return _goodValueStyle;
            }
        }

        protected static GUIStyle BadValueStyle
        {
            get
            {
                if (_badValueStyle == null)
                {
                    _badValueStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = new Color(0.9f, 0.2f, 0.2f) },
                        fontStyle = FontStyle.Bold
                    };
                }
                return _badValueStyle;
            }
        }

        protected static GUIStyle WarningValueStyle
        {
            get
            {
                if (_warningValueStyle == null)
                {
                    _warningValueStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = new Color(0.9f, 0.7f, 0.1f) },
                        fontStyle = FontStyle.Bold
                    };
                }
                return _warningValueStyle;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called once when the module is first created, and each time
        /// the user switches to this module's tab.
        /// Override to initialize scene objects, materials, etc.
        /// </summary>
        public virtual void OnEnable(EditorWindow parentWindow)
        {
            _parentWindow = parentWindow;
            _isInitialized = true;
        }

        /// <summary>
        /// Called when the user switches away from this module's tab,
        /// or when the toolkit window is closed.
        /// Override to clean up preview objects, materials, etc.
        /// </summary>
        public virtual void OnDisable()
        {
            _isInitialized = false;
        }

        /// <summary>
        /// Called every frame while this module is the active tab.
        /// Implement all IMGUI drawing here.
        /// </summary>
        public abstract void DrawGUI();

        /// <summary>
        /// Called when the EditorWindow is destroyed.
        /// Override for final cleanup (destroy preview objects, etc.)
        /// </summary>
        public virtual void OnDestroy() { }

        // ─────────────────────────────────────────────────────────────────────
        // Shared Drawing Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws the standard module header with name, description, and divider.
        /// Call this at the top of DrawGUI().
        /// </summary>
        protected void DrawHeader()
        {
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                // Module icon
                var icon = EditorGUIUtility.IconContent(ModuleIcon);
                GUILayout.Label(icon, GUILayout.Width(24), GUILayout.Height(24));

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(ModuleName, HeaderStyle);
                    EditorGUILayout.LabelField(ModuleDescription, DescriptionStyle);
                }
            }

            DrawHorizontalLine(new Color(0.3f, 0.3f, 0.3f), 1, 6);
        }

        /// <summary>
        /// Draws a horizontal divider line.
        /// </summary>
        protected void DrawHorizontalLine(Color color, float thickness = 1f, float padding = 4f)
        {
            EditorGUILayout.Space(padding);
            Rect rect = EditorGUILayout.GetControlRect(false, thickness);
            rect.height = thickness;
            EditorGUI.DrawRect(rect, color);
            EditorGUILayout.Space(padding);
        }

        /// <summary>
        /// Draws a labeled section box. Use with 'using' statement.
        /// </summary>
        protected System.IDisposable BeginSection(string label)
        {
            EditorGUILayout.LabelField(label, SubHeaderStyle);
            return new EditorGUILayout.VerticalScope(SectionBoxStyle);
        }

        /// <summary>
        /// Draws a two-column metric row: label on left, value on right.
        /// </summary>
        protected void DrawMetricRow(string label, string value,
            MetricStatus status = MetricStatus.Neutral)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, MetricLabelStyle, GUILayout.Width(180));

                GUIStyle valueStyle = status switch
                {
                    MetricStatus.Good    => GoodValueStyle,
                    MetricStatus.Bad     => BadValueStyle,
                    MetricStatus.Warning => WarningValueStyle,
                    _                    => EditorStyles.label
                };

                EditorGUILayout.LabelField(value, valueStyle);
            }
        }

        /// <summary>
        /// Draws a comparison row: label | before value | after value | delta.
        /// </summary>
        protected void DrawComparisonRow(string label, string before,
            string after, string delta, bool lowerIsBetter = true)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(160));
                EditorGUILayout.LabelField(before, GUILayout.Width(100));
                EditorGUILayout.LabelField(after, GUILayout.Width(100));

                // Parse delta for color coding
                if (float.TryParse(delta.Replace("%", "").Replace("+", ""), out float deltaVal))
                {
                    bool isImprovement = lowerIsBetter ? deltaVal < 0 : deltaVal > 0;
                    GUIStyle deltaStyle = isImprovement ? GoodValueStyle : BadValueStyle;
                    EditorGUILayout.LabelField(delta, deltaStyle, GUILayout.Width(80));
                }
                else
                {
                    EditorGUILayout.LabelField(delta, GUILayout.Width(80));
                }
            }
        }

        /// <summary>
        /// Draws a standard "Reset to Defaults" button at the bottom of a section.
        /// </summary>
        protected bool DrawResetButton()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                return GUILayout.Button("↺  Reset to Defaults",
                    GUILayout.Width(160), GUILayout.Height(22));
            }
        }

        /// <summary>
        /// Draws a prominent action button (green tint).
        /// </summary>
        protected bool DrawActionButton(string label, float width = 200f, float height = 30f)
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            bool clicked = GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(height));
            GUI.backgroundColor = originalColor;
            return clicked;
        }

        /// <summary>
        /// Draws a destructive action button (red tint).
        /// </summary>
        protected bool DrawDestructiveButton(string label, float width = 200f, float height = 30f)
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
            bool clicked = GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(height));
            GUI.backgroundColor = originalColor;
            return clicked;
        }

        /// <summary>
        /// Draws an info help box with the module's usage tip.
        /// </summary>
        protected void DrawInfoBox(string message)
        {
            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        /// <summary>
        /// Draws a warning help box.
        /// </summary>
        protected void DrawWarningBox(string message)
        {
            EditorGUILayout.HelpBox(message, MessageType.Warning);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Utility
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Requests the parent window to repaint on the next frame.
        /// Call this after changing any values that affect the display.
        /// </summary>
        protected void RequestRepaint()
        {
            _parentWindow?.Repaint();
        }

        /// <summary>
        /// Formats a byte count into a human-readable string (KB, MB, GB).
        /// </summary>
        protected static string FormatBytes(long bytes)
        {
            if (bytes < 1024)        return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0f:F1} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0f * 1024):F1} MB";
            return $"{bytes / (1024.0f * 1024 * 1024):F2} GB";
        }

        /// <summary>
        /// Formats a triangle count with thousands separator.
        /// </summary>
        protected static string FormatTriCount(int tris)
        {
            return tris.ToString("N0") + " tris";
        }

        /// <summary>
        /// Returns a MetricStatus based on a value vs a budget threshold.
        /// </summary>
        protected static MetricStatus EvaluateBudget(float value, float goodThreshold, float badThreshold)
        {
            if (value <= goodThreshold) return MetricStatus.Good;
            if (value >= badThreshold)  return MetricStatus.Bad;
            return MetricStatus.Warning;
        }
    }

    /// <summary>
    /// Status enum for color-coded metric display.
    /// </summary>
    public enum MetricStatus
    {
        Neutral,
        Good,
        Warning,
        Bad
    }
}

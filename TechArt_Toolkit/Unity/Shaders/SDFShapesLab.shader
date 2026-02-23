// ============================================================================
// SDFShapesLab.shader
// Tech Art Toolkit — Module 1: Shader & Procedural Lab (SDF Focus)
//
// PURPOSE:
//   Dedicated SDF (Signed Distance Field) shape explorer shader.
//   Demonstrates how SDF math generates crisp, resolution-independent
//   shapes entirely in the fragment shader — no textures required.
//
//   SDF shapes are fundamental to:
//   - UI/HUD rendering (health bars, icons, crosshairs)
//   - Procedural masks for material blending
//   - Particle shape modules
//   - Font rendering (MSDF fonts)
//   - Raymarching and implicit surface rendering
//
// FEATURES:
//   - 8 SDF primitives: Circle, Box, Ring, Cross, Triangle, Star, Capsule, Arrow
//   - Boolean operations: Union, Subtract, Intersect, SmoothUnion
//   - Outline mode with configurable width
//   - Fill + outline dual-layer rendering
//   - Animated SDF morphing between two shapes
//   - Grid overlay for UV debugging
//
// ENGINE: Unity URP (Universal Render Pipeline)
// ============================================================================

Shader "TechArtToolkit/SDFShapesLab"
{
    Properties
    {
        // ── Shape A ──────────────────────────────────────────────────────────
        [Header(Shape A Primary)]
        [Enum(Circle,0,Box,1,Ring,2,Cross,3,Triangle,4,Star,5,Capsule,6,Arrow,7)]
        _ShapeA             ("Shape A",              Float)      = 0
        _ShapeASize         ("Shape A Size",         Float)      = 0.35
        _ShapeACenter       ("Shape A Center (UV)",  Vector)     = (0.5,0.5,0,0)
        _ShapeARotation     ("Shape A Rotation",     Float)      = 0.0
        _ShapeAColor        ("Shape A Fill Color",   Color)      = (0.2,0.6,1.0,1)

        // ── Shape B ──────────────────────────────────────────────────────────
        [Header(Shape B Secondary)]
        [Enum(Circle,0,Box,1,Ring,2,Cross,3,Triangle,4,Star,5,Capsule,6,Arrow,7)]
        _ShapeB             ("Shape B",              Float)      = 1
        _ShapeBSize         ("Shape B Size",         Float)      = 0.2
        _ShapeBCenter       ("Shape B Center (UV)",  Vector)     = (0.6,0.6,0,0)
        _ShapeBRotation     ("Shape B Rotation",     Float)      = 0.0
        _ShapeBColor        ("Shape B Fill Color",   Color)      = (1.0,0.4,0.2,1)

        // ── Boolean Operation ────────────────────────────────────────────────
        [Header(Boolean Operation)]
        [Enum(None,0,Union,1,Subtract,2,Intersect,3,SmoothUnion,4,Xor,5)]
        _BooleanOp          ("Boolean Operation",    Float)      = 0
        _SmoothK            ("Smooth Union K",       Float)      = 0.1

        // ── Edge & Outline ───────────────────────────────────────────────────
        [Header(Edge and Outline)]
        _EdgeSoftness       ("Edge Softness",        Float)      = 0.01
        _OutlineWidth       ("Outline Width",        Float)      = 0.02
        _OutlineColor       ("Outline Color",        Color)      = (1,1,1,1)
        [Toggle] _ShowOutline ("Show Outline",       Float)      = 1

        // ── Background ───────────────────────────────────────────────────────
        [Header(Background)]
        _BackgroundColor    ("Background Color",     Color)      = (0.05,0.05,0.1,1)
        [Toggle] _ShowGrid  ("Show UV Grid",         Float)      = 0
        _GridColor          ("Grid Color",           Color)      = (0.15,0.15,0.2,1)
        _GridSize           ("Grid Cell Size",       Float)      = 0.1

        // ── Morph Animation ──────────────────────────────────────────────────
        [Header(Morph Animation)]
        [Toggle] _AnimateMorph ("Animate Morph",    Float)      = 0
        _MorphSpeed         ("Morph Speed",          Float)      = 1.0
        _MorphT             ("Morph T (manual)",     Range(0,1)) = 0.0

        // ── Debug ────────────────────────────────────────────────────────────
        [Header(Debug)]
        [Toggle] _ShowSDF   ("Show SDF Distance Field", Float)  = 0
        [Toggle] _ShowUV    ("Show UV Coordinates",      Float)  = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        Pass
        {
            Name "SDFShapesLab"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Structs ──────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            // ── Constant Buffer ───────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float  _ShapeA;
                float  _ShapeASize;
                float4 _ShapeACenter;
                float  _ShapeARotation;
                float4 _ShapeAColor;

                float  _ShapeB;
                float  _ShapeBSize;
                float4 _ShapeBCenter;
                float  _ShapeBRotation;
                float4 _ShapeBColor;

                float  _BooleanOp;
                float  _SmoothK;

                float  _EdgeSoftness;
                float  _OutlineWidth;
                float4 _OutlineColor;
                float  _ShowOutline;

                float4 _BackgroundColor;
                float  _ShowGrid;
                float4 _GridColor;
                float  _GridSize;

                float  _AnimateMorph;
                float  _MorphSpeed;
                float  _MorphT;

                float  _ShowSDF;
                float  _ShowUV;
            CBUFFER_END

            // ================================================================
            // UTILITY
            // ================================================================

            /// Rotates a 2D point around the origin by angle (radians).
            float2 Rotate2D(float2 p, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
            }

            // ================================================================
            // SDF PRIMITIVES
            // ================================================================

            float sdCircle(float2 p, float r)
            {
                return length(p) - r;
            }

            float sdBox(float2 p, float2 b)
            {
                float2 d = abs(p) - b;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            float sdRing(float2 p, float r, float t)
            {
                return abs(length(p) - r) - t;
            }

            float sdCross(float2 p, float2 b, float r)
            {
                p = abs(p);
                p = (p.y > p.x) ? p.yx : p.xy;
                float2 q = p - b;
                float  k = max(q.y, q.x);
                float2 w = (k > 0.0) ? q : float2(b.y - p.x, -k);
                return sign(k) * length(max(w, 0.0)) - r;
            }

            float sdEquilateralTriangle(float2 p, float r)
            {
                const float k = sqrt(3.0);
                p.x = abs(p.x) - r;
                p.y = p.y + r / k;
                if (p.x + k * p.y > 0.0) p = float2(p.x - k * p.y, -k * p.x - p.y) / 2.0;
                p.x -= clamp(p.x, -2.0 * r, 0.0);
                return -length(p) * sign(p.y);
            }

            float sdStar5(float2 p, float r, float rf)
            {
                const float2 k1 = float2(0.809016994375, -0.587785252192);
                const float2 k2 = float2(-k1.x, k1.y);
                p.x = abs(p.x);
                p -= 2.0 * max(dot(k1, p), 0.0) * k1;
                p -= 2.0 * max(dot(k2, p), 0.0) * k2;
                p.x = abs(p.x);
                p.y -= r;
                float2 ba = rf * float2(-k1.y, k1.x) - float2(0, 1);
                float h = clamp(dot(p, ba) / dot(ba, ba), 0.0, r);
                return length(p - ba * h) * sign(p.x * ba.y - p.y * ba.x);
            }

            float sdCapsule(float2 p, float2 a, float2 b, float r)
            {
                float2 pa = p - a;
                float2 ba = b - a;
                float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
                return length(pa - ba * h) - r;
            }

            float sdArrow(float2 p, float size)
            {
                // Arrow pointing right: shaft + head
                float shaft = sdBox(p - float2(-size * 0.1, 0), float2(size * 0.4, size * 0.08));
                float head  = sdEquilateralTriangle(
                    Rotate2D(p - float2(size * 0.25, 0), -1.5708), // rotate 90°
                    size * 0.25);
                return min(shaft, head);
            }

            // ── Shape Dispatcher ─────────────────────────────────────────────

            float EvaluateShape(float2 uv, float shapeType, float size,
                                float4 center, float rotation)
            {
                float2 p = uv - center.xy;
                p = Rotate2D(p, rotation);

                int type = (int)shapeType;

                if      (type == 0) return sdCircle(p, size);
                else if (type == 1) return sdBox(p, float2(size, size));
                else if (type == 2) return sdRing(p, size, size * 0.25);
                else if (type == 3) return sdCross(p, float2(size, size * 0.3), 0.005);
                else if (type == 4) return sdEquilateralTriangle(p, size);
                else if (type == 5) return sdStar5(p, size, 0.4);
                else if (type == 6) return sdCapsule(p,
                                        float2(-size * 0.5, 0),
                                        float2( size * 0.5, 0),
                                        size * 0.3);
                else                return sdArrow(p, size);
            }

            // ================================================================
            // BOOLEAN OPERATIONS
            // ================================================================

            float opUnion(float d1, float d2)        { return min(d1, d2); }
            float opSubtract(float d1, float d2)     { return max(d1, -d2); }
            float opIntersect(float d1, float d2)    { return max(d1, d2); }
            float opXor(float d1, float d2)          { return max(min(d1,d2), -max(d1,d2)); }

            /// Polynomial smooth minimum — blends two SDF shapes smoothly.
            float opSmoothUnion(float d1, float d2, float k)
            {
                float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0);
                return lerp(d2, d1, h) - k * h * (1.0 - h);
            }

            float ApplyBooleanOp(float dA, float dB)
            {
                int op = (int)_BooleanOp;
                if      (op == 0) return dA;                              // None (Shape A only)
                else if (op == 1) return opUnion(dA, dB);                 // Union
                else if (op == 2) return opSubtract(dA, dB);              // Subtract B from A
                else if (op == 3) return opIntersect(dA, dB);             // Intersect
                else if (op == 4) return opSmoothUnion(dA, dB, _SmoothK); // Smooth Union
                else              return opXor(dA, dB);                   // XOR
            }

            // ================================================================
            // GRID OVERLAY
            // ================================================================

            float3 DrawGrid(float2 uv, float3 baseColor)
            {
                float2 grid = frac(uv / _GridSize);
                float  line = min(
                    smoothstep(0.0, 0.02, grid.x) * smoothstep(1.0, 0.98, grid.x),
                    smoothstep(0.0, 0.02, grid.y) * smoothstep(1.0, 0.98, grid.y));
                return lerp(_GridColor.rgb, baseColor, line);
            }

            // ================================================================
            // SDF VISUALIZATION (debug mode)
            // ================================================================

            /// Maps SDF distance to a color for visualization.
            float3 VisualizeSDF(float d)
            {
                // Contour lines every 0.05 units
                float contour = abs(sin(d * 20.0 * 3.14159));

                // Color: inside = warm, outside = cool, boundary = white
                float3 inside  = float3(0.9, 0.4, 0.1);
                float3 outside = float3(0.1, 0.3, 0.7);
                float3 col = d < 0.0 ? inside : outside;

                // Darken with distance
                col *= 1.0 - 0.3 * abs(d) * 5.0;

                // Add contour lines
                col = lerp(col, float3(1,1,1), (1.0 - contour) * 0.3);

                // Zero crossing = bright white line
                col = lerp(col, float3(1,1,1), 1.0 - smoothstep(0.0, 0.01, abs(d)));

                return col;
            }

            // ================================================================
            // VERTEX SHADER
            // ================================================================

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            // ================================================================
            // FRAGMENT SHADER
            // ================================================================

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // ── Debug: Show UV coordinates ────────────────────────────────
                if (_ShowUV > 0.5)
                    return half4(uv.x, uv.y, 0.0, 1.0);

                // ── Morph T ───────────────────────────────────────────────────
                float morphT = _AnimateMorph > 0.5
                    ? (sin(_Time.y * _MorphSpeed) * 0.5 + 0.5)
                    : _MorphT;

                // ── Evaluate SDF shapes ───────────────────────────────────────
                float dA = EvaluateShape(uv, _ShapeA, _ShapeASize, _ShapeACenter, _ShapeARotation);
                float dB = EvaluateShape(uv, _ShapeB, _ShapeBSize, _ShapeBCenter, _ShapeBRotation);

                // ── Morph between shapes ──────────────────────────────────────
                float dA_morphed = lerp(dA, dB, morphT);

                // ── Apply boolean operation ───────────────────────────────────
                float dFinal = ApplyBooleanOp(dA_morphed, dB);

                // ── Debug: Show SDF distance field ────────────────────────────
                if (_ShowSDF > 0.5)
                    return half4(VisualizeSDF(dFinal), 1.0);

                // ── Background ────────────────────────────────────────────────
                float3 color = _BackgroundColor.rgb;

                // ── Grid overlay ──────────────────────────────────────────────
                if (_ShowGrid > 0.5)
                    color = DrawGrid(uv, color);

                // ── Shape B fill (rendered first, behind A) ───────────────────
                if ((int)_BooleanOp == 0) // Only when no boolean op
                {
                    float maskB = 1.0 - smoothstep(-_EdgeSoftness, _EdgeSoftness, dB);
                    color = lerp(color, _ShapeBColor.rgb, maskB);
                }

                // ── Shape A fill ──────────────────────────────────────────────
                float maskA = 1.0 - smoothstep(-_EdgeSoftness, _EdgeSoftness, dFinal);
                color = lerp(color, _ShapeAColor.rgb, maskA);

                // ── Outline ───────────────────────────────────────────────────
                if (_ShowOutline > 0.5)
                {
                    // Outline = thin band around the SDF zero crossing
                    float outlineMask = 1.0 - smoothstep(0.0, _EdgeSoftness, abs(dFinal) - _OutlineWidth);
                    color = lerp(color, _OutlineColor.rgb, outlineMask);
                }

                // ── Subtle vignette for visual polish ─────────────────────────
                float2 vigUV = uv - 0.5;
                float vignette = 1.0 - dot(vigUV, vigUV) * 1.5;
                color *= saturate(vignette);

                return half4(color, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

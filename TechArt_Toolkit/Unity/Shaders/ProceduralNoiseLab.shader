// ============================================================================
// ProceduralNoiseLab.shader
// Tech Art Toolkit — Module 1: Shader & Procedural Lab
//
// PURPOSE:
//   Demonstrates procedural shading fundamentals used in game studios:
//   - Fractal Brownian Motion (FBM) noise
//   - Voronoi / Worley cellular noise
//   - UV manipulation (tiling, offset, rotation)
//   - Trigonometric UV distortion
//   - SDF (Signed Distance Field) shape masking
//   - Two-color remapping with contrast/brightness
//
// ENGINE: Unity URP (Universal Render Pipeline)
// SHADER MODEL: 4.5 (SM4.5)
// RENDER QUEUE: Geometry (2000)
//
// USAGE:
//   Assigned to a preview mesh by ShaderProceduralLab.cs via MaterialPropertyBlock.
//   All properties are driven at runtime from the editor tool — no manual
//   material editing required.
// ============================================================================

Shader "TechArtToolkit/ProceduralNoiseLab"
{
    Properties
    {
        // ── Noise ────────────────────────────────────────────────────────────
        [Header(Noise Parameters)]
        [Enum(FBM,0,Voronoi,1,Perlin,2,Value,3)]
        _NoiseType          ("Noise Type",          Float)      = 0
        _NoiseScale         ("Noise Scale",          Float)      = 3.0
        _NoiseOctaves       ("Noise Octaves (FBM)",  Float)      = 4.0
        _NoisePersistence   ("Persistence (FBM)",    Float)      = 0.5
        _NoiseLacunarity    ("Lacunarity (FBM)",      Float)      = 2.0
        _NoiseContrast      ("Noise Contrast",        Float)      = 1.0

        // ── UV ───────────────────────────────────────────────────────────────
        [Header(UV Controls)]
        _UVTiling           ("UV Tiling",            Vector)     = (1,1,0,0)
        _UVOffset           ("UV Offset",            Vector)     = (0,0,0,0)
        _UVRotation         ("UV Rotation (radians)",Float)      = 0.0

        // ── Trig Distortion ──────────────────────────────────────────────────
        [Header(Trig Distortion)]
        _TrigFrequency      ("Trig Frequency",       Float)      = 2.0
        _TrigAmplitude      ("Trig Amplitude",       Float)      = 0.1
        _TrigPhase          ("Trig Phase",           Float)      = 0.0

        // ── SDF Shape ────────────────────────────────────────────────────────
        [Header(SDF Shape Mask)]
        [Enum(Circle,0,Box,1,Ring,2,Cross,3,None,4)]
        _SDFShape           ("SDF Shape",            Float)      = 0
        _SDFRadius          ("SDF Radius",           Float)      = 0.35
        _SDFSoftness        ("SDF Edge Softness",    Float)      = 0.05
        _SDFBlend           ("SDF Blend Strength",   Float)      = 1.0
        _SDFCenter          ("SDF Center (UV)",      Vector)     = (0.5,0.5,0,0)

        // ── Color ────────────────────────────────────────────────────────────
        [Header(Color Mapping)]
        [Enum(TwoColor,0,HSV,1)]
        _ColorMode          ("Color Mode",           Float)      = 0
        _ColorA             ("Color A (Dark)",       Color)      = (0.05,0.05,0.15,1)
        _ColorB             ("Color B (Bright)",     Color)      = (0.2,0.6,1.0,1)
        _ColorContrast      ("Color Contrast",       Float)      = 1.0
        _ColorBrightness    ("Color Brightness",     Float)      = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Opaque"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Geometry"
        }

        Pass
        {
            Name "ProceduralNoiseLab"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   4.5

            // ── Includes ─────────────────────────────────────────────────────
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Structs ──────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            // ── Constant Buffer ───────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float  _NoiseType;
                float  _NoiseScale;
                float  _NoiseOctaves;
                float  _NoisePersistence;
                float  _NoiseLacunarity;
                float  _NoiseContrast;

                float4 _UVTiling;
                float4 _UVOffset;
                float  _UVRotation;

                float  _TrigFrequency;
                float  _TrigAmplitude;
                float  _TrigPhase;

                float  _SDFShape;
                float  _SDFRadius;
                float  _SDFSoftness;
                float  _SDFBlend;
                float4 _SDFCenter;

                float  _ColorMode;
                float4 _ColorA;
                float4 _ColorB;
                float  _ColorContrast;
                float  _ColorBrightness;
            CBUFFER_END

            // ================================================================
            // UTILITY FUNCTIONS
            // ================================================================

            // ── Hash Functions ───────────────────────────────────────────────

            /// Deterministic 2D → 1D hash. Returns [0,1].
            float hash11(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            /// Deterministic 2D → 2D hash. Returns [0,1]^2.
            float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            // ── UV Transforms ────────────────────────────────────────────────

            /// Applies tiling, offset, and rotation to UV coordinates.
            float2 TransformUV(float2 uv)
            {
                // Tiling and offset
                uv = uv * _UVTiling.xy + _UVOffset.xy;

                // Rotation around UV center (0.5, 0.5)
                float sinR = sin(_UVRotation);
                float cosR = cos(_UVRotation);
                uv -= 0.5;
                uv = float2(
                    uv.x * cosR - uv.y * sinR,
                    uv.x * sinR + uv.y * cosR
                );
                uv += 0.5;

                return uv;
            }

            /// Applies trigonometric wave distortion to UV.
            float2 TrigDistortUV(float2 uv)
            {
                float distX = sin(uv.y * _TrigFrequency + _TrigPhase) * _TrigAmplitude;
                float distY = cos(uv.x * _TrigFrequency + _TrigPhase) * _TrigAmplitude;
                return uv + float2(distX, distY);
            }

            // ================================================================
            // NOISE FUNCTIONS
            // ================================================================

            // ── Value Noise ──────────────────────────────────────────────────

            /// Smooth value noise. Returns [0,1].
            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = hash11(i + float2(0,0));
                float b = hash11(i + float2(1,0));
                float c = hash11(i + float2(0,1));
                float d = hash11(i + float2(1,1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // ── Perlin Noise ─────────────────────────────────────────────────

            /// Classic gradient noise (Perlin). Returns [-1,1].
            float PerlinNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0); // quintic

                float2 ga = hash22(i + float2(0,0)) * 2.0 - 1.0;
                float2 gb = hash22(i + float2(1,0)) * 2.0 - 1.0;
                float2 gc = hash22(i + float2(0,1)) * 2.0 - 1.0;
                float2 gd = hash22(i + float2(1,1)) * 2.0 - 1.0;

                float va = dot(ga, f - float2(0,0));
                float vb = dot(gb, f - float2(1,0));
                float vc = dot(gc, f - float2(0,1));
                float vd = dot(gd, f - float2(1,1));

                return lerp(lerp(va, vb, u.x), lerp(vc, vd, u.x), u.y);
            }

            // ── FBM (Fractal Brownian Motion) ────────────────────────────────

            /// Layered Perlin noise. Returns approximately [-1,1].
            float FBM(float2 uv)
            {
                float value     = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                float maxValue  = 0.0;

                int octaves = (int)clamp(_NoiseOctaves, 1, 8);

                for (int i = 0; i < octaves; i++)
                {
                    value     += PerlinNoise(uv * frequency) * amplitude;
                    maxValue  += amplitude;
                    amplitude *= _NoisePersistence;
                    frequency *= _NoiseLacunarity;
                }

                return value / maxValue; // normalize
            }

            // ── Voronoi (Worley / Cellular) Noise ────────────────────────────

            /// Voronoi noise. Returns distance to nearest cell center [0,~0.7].
            float VoronoiNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float minDist = 8.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 point    = hash22(i + neighbor);
                        // Animate cell centers slightly
                        point = 0.5 + 0.5 * sin(point * 6.2831);
                        float2 diff = neighbor + point - f;
                        float  dist = dot(diff, diff);
                        minDist = min(minDist, dist);
                    }
                }

                return sqrt(minDist);
            }

            // ── Noise Dispatcher ─────────────────────────────────────────────

            /// Routes to the correct noise function based on _NoiseType.
            float SampleNoise(float2 uv)
            {
                float n = 0.0;
                int type = (int)_NoiseType;

                if      (type == 0) n = FBM(uv) * 0.5 + 0.5;          // FBM → [0,1]
                else if (type == 1) n = 1.0 - VoronoiNoise(uv) * 1.4; // Voronoi → [0,1]
                else if (type == 2) n = PerlinNoise(uv) * 0.5 + 0.5;  // Perlin → [0,1]
                else                n = ValueNoise(uv);                 // Value → [0,1]

                // Apply contrast: pow(n, contrast)
                n = saturate(n);
                n = pow(n, _NoiseContrast);

                return n;
            }

            // ================================================================
            // SDF FUNCTIONS
            // ================================================================

            /// SDF Circle. Returns signed distance (negative = inside).
            float sdCircle(float2 p, float r)
            {
                return length(p) - r;
            }

            /// SDF Box. b = half-extents.
            float sdBox(float2 p, float2 b)
            {
                float2 d = abs(p) - b;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            /// SDF Ring (annulus). r = outer radius, t = ring thickness.
            float sdRing(float2 p, float r, float t)
            {
                return abs(length(p) - r) - t;
            }

            /// SDF Cross. b = arm half-extents, r = corner rounding.
            float sdCross(float2 p, float2 b, float r)
            {
                p = abs(p);
                p = (p.y > p.x) ? p.yx : p.xy;
                float2 q = p - b;
                float  k = max(q.y, q.x);
                float2 w = (k > 0.0) ? q : float2(b.y - p.x, -k);
                return sign(k) * length(max(w, 0.0)) - r;
            }

            /// Evaluates the selected SDF shape and returns a [0,1] mask.
            float EvaluateSDF(float2 uv)
            {
                float2 p = uv - _SDFCenter.xy; // center the SDF

                float dist = 0.0;
                int shape = (int)_SDFShape;

                if      (shape == 0) dist = sdCircle(p, _SDFRadius);
                else if (shape == 1) dist = sdBox(p, float2(_SDFRadius, _SDFRadius));
                else if (shape == 2) dist = sdRing(p, _SDFRadius, _SDFRadius * 0.25);
                else if (shape == 3) dist = sdCross(p, float2(_SDFRadius, _SDFRadius * 0.3), 0.01);
                else                 return 1.0; // None — no mask

                // Convert SDF distance to smooth [0,1] mask
                // Inside shape = 1, outside = 0, with soft edge
                return 1.0 - smoothstep(-_SDFSoftness, _SDFSoftness, dist);
            }

            // ================================================================
            // COLOR FUNCTIONS
            // ================================================================

            /// Maps a [0,1] noise value to a final color.
            float3 RemapColor(float n)
            {
                // Apply brightness offset
                n = saturate(n + _ColorBrightness);

                int mode = (int)_ColorMode;

                if (mode == 1)
                {
                    // HSV mode: rotate hue based on noise
                    // Simple HSV → RGB for hue rotation
                    float hue = n;
                    float3 rgb = saturate(abs(frac(hue + float3(0.0, 0.333, 0.667)) * 6.0 - 3.0) - 1.0);
                    return rgb;
                }
                else
                {
                    // Two-color lerp
                    return lerp(_ColorA.rgb, _ColorB.rgb, n);
                }
            }

            // ================================================================
            // VERTEX SHADER
            // ================================================================

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            // ================================================================
            // FRAGMENT SHADER
            // ================================================================

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // 1. Apply UV transforms (tiling, offset, rotation)
                uv = TransformUV(uv);

                // 2. Apply trigonometric distortion
                uv = TrigDistortUV(uv);

                // 3. Sample noise at transformed UV
                float noise = SampleNoise(uv * _NoiseScale);

                // 4. Evaluate SDF mask
                float sdfMask = EvaluateSDF(IN.uv); // use original UV for SDF center

                // 5. Blend noise with SDF mask
                float finalValue = lerp(noise, noise * sdfMask, _SDFBlend);

                // 6. Remap to color
                float3 color = RemapColor(finalValue);

                // 7. Add subtle normal-based rim for 3D readability
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldNormal);
                float rim = 1.0 - saturate(dot(normalize(IN.worldNormal), float3(0,0,1)));
                color += rim * rim * 0.05;

                return half4(color, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"

    CustomEditor "TechArtToolkit.Editor.ProceduralNoiseLabShaderGUI"
}

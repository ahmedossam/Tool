// ============================================================================
// ProceduralNoise.hlsl
// Tech Art Toolkit — Shared HLSL Noise Library
//
// PURPOSE:
//   Cross-engine HLSL noise function library. Compatible with:
//   - Unity URP/HDRP (include via #include "ProceduralNoise.hlsl")
//   - Unreal Engine 5 (paste into Custom HLSL node)
//   - Standalone HLSL shaders (DirectX 11/12)
//
// FUNCTIONS:
//   Hash:       hash11, hash12, hash21, hash22, hash33
//   Noise:      valueNoise, perlinNoise, simplexNoise
//   Fractal:    fbm, ridgedFBM, domainWarpedFBM
//   Cellular:   voronoiNoise, voronoiEdge
//   Utility:    remap, smootherstep, quinticFade
//
// USAGE (Unity):
//   #include "Assets/TechArtToolkit/Shaders/ProceduralNoise.hlsl"
//   float n = fbm(uv * scale, octaves, persistence, lacunarity);
//
// USAGE (Unreal Custom Node):
//   Copy the desired function(s) into the Custom node's Code field.
//   Set the return type to float and add input pins as needed.
//
// PERFORMANCE NOTES:
//   - All functions are GPU-friendly (no branching in inner loops)
//   - Hash functions use only multiply/add/frac — no texture lookups
//   - FBM with 8 octaves costs ~8x a single noise sample
//   - Voronoi is more expensive than FBM — use sparingly on mobile
// ============================================================================

#ifndef PROCEDURAL_NOISE_HLSL
#define PROCEDURAL_NOISE_HLSL

// ============================================================================
// CONSTANTS
// ============================================================================

#define TAU     6.28318530718
#define PI      3.14159265359
#define SQRT2   1.41421356237
#define SQRT3   1.73205080757

// ============================================================================
// UTILITY FUNCTIONS
// ============================================================================

/// Remaps value from [inMin,inMax] to [outMin,outMax].
float remap(float value, float inMin, float inMax, float outMin, float outMax)
{
    return outMin + (outMax - outMin) * saturate((value - inMin) / (inMax - inMin));
}

/// Smoother version of smoothstep (Ken Perlin's quintic curve).
/// Eliminates second-derivative discontinuity at t=0 and t=1.
float quinticFade(float t)
{
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

float2 quinticFade2(float2 t)
{
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

/// Smooth minimum — blends two values with a smooth transition.
/// k controls the blend radius (larger k = smoother).
float smin(float a, float b, float k)
{
    float h = max(k - abs(a - b), 0.0) / k;
    return min(a, b) - h * h * k * 0.25;
}

// ============================================================================
// HASH FUNCTIONS
// ============================================================================
// All hash functions are deterministic (same input → same output).
// They use only arithmetic operations — no texture lookups.
// Output range: [0, 1] unless noted.

/// 1D → 1D hash.
float hash11(float p)
{
    p = frac(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return frac(p);
}

/// 2D → 1D hash.
float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

/// 1D → 2D hash.
float2 hash12(float p)
{
    float3 p3 = frac(float3(p, p, p) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

/// 2D → 2D hash.
float2 hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

/// 3D → 1D hash.
float hash31(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

/// 3D → 3D hash.
float3 hash33(float3 p)
{
    p = frac(p * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yxx) * p.zyx);
}

// ============================================================================
// VALUE NOISE
// ============================================================================

/// 2D Value Noise. Returns [0, 1].
/// Smooth interpolation between random values at integer lattice points.
float valueNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    float2 u = quinticFade2(f);

    float a = hash21(i + float2(0, 0));
    float b = hash21(i + float2(1, 0));
    float c = hash21(i + float2(0, 1));
    float d = hash21(i + float2(1, 1));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

/// 3D Value Noise. Returns [0, 1].
float valueNoise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    float3 u = quinticFade2(f.xy).xyy; // approximate

    float a = hash31(i + float3(0,0,0));
    float b = hash31(i + float3(1,0,0));
    float c = hash31(i + float3(0,1,0));
    float d = hash31(i + float3(1,1,0));
    float e = hash31(i + float3(0,0,1));
    float g = hash31(i + float3(1,0,1));
    float h = hash31(i + float3(0,1,1));
    float k = hash31(i + float3(1,1,1));

    return lerp(
        lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y),
        lerp(lerp(e,g,u.x), lerp(h,k,u.x), u.y),
        f.z);
}

// ============================================================================
// PERLIN NOISE (GRADIENT NOISE)
// ============================================================================

/// 2D Perlin Noise. Returns [-1, 1].
/// Higher quality than value noise — uses gradient vectors at lattice points.
float perlinNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    float2 u = quinticFade2(f);

    // Gradient vectors at four corners
    float2 ga = hash22(i + float2(0,0)) * 2.0 - 1.0;
    float2 gb = hash22(i + float2(1,0)) * 2.0 - 1.0;
    float2 gc = hash22(i + float2(0,1)) * 2.0 - 1.0;
    float2 gd = hash22(i + float2(1,1)) * 2.0 - 1.0;

    // Dot products with distance vectors
    float va = dot(ga, f - float2(0,0));
    float vb = dot(gb, f - float2(1,0));
    float vc = dot(gc, f - float2(0,1));
    float vd = dot(gd, f - float2(1,1));

    return lerp(lerp(va, vb, u.x), lerp(vc, vd, u.x), u.y);
}

/// 2D Perlin Noise normalized to [0, 1].
float perlinNoise01(float2 uv)
{
    return perlinNoise(uv) * 0.5 + 0.5;
}

// ============================================================================
// SIMPLEX NOISE (APPROXIMATION)
// ============================================================================

/// 2D Simplex-like noise. Returns [-1, 1].
/// Faster than Perlin, fewer directional artifacts, no patent issues.
float simplexNoise(float2 v)
{
    const float4 C = float4(
         0.211324865405187,   // (3.0 - sqrt(3.0)) / 6.0
         0.366025403784439,   // 0.5 * (sqrt(3.0) - 1.0)
        -0.577350269189626,   // -1.0 + 2.0 * C.x
         0.024390243902439);  // 1.0 / 41.0

    // Skew input space to determine simplex cell
    float2 i  = floor(v + dot(v, C.yy));
    float2 x0 = v - i + dot(i, C.xx);

    // Determine which simplex we're in
    float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
    float4 x12 = x0.xyxy + C.xxzz;
    x12.xy -= i1;

    // Permutations
    i = frac(i * (1.0 / 289.0)) * 289.0;
    float3 p = frac(
        (frac(i.y + float3(0.0, i1.y, 1.0)) * 34.0 + 1.0) *
        (frac(i.y + float3(0.0, i1.y, 1.0)) + i.x + float3(0.0, i1.x, 1.0))
    ) * 289.0;

    float3 m = max(0.5 - float3(dot(x0,x0), dot(x12.xy,x12.xy), dot(x12.zw,x12.zw)), 0.0);
    m = m * m;
    m = m * m;

    float3 x  = 2.0 * frac(p * C.www) - 1.0;
    float3 h  = abs(x) - 0.5;
    float3 ox = floor(x + 0.5);
    float3 a0 = x - ox;

    m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);

    float3 g;
    g.x  = a0.x  * x0.x   + h.x  * x0.y;
    g.yz = a0.yz * x12.xz  + h.yz * x12.yw;

    return 130.0 * dot(m, g);
}

/// 2D Simplex Noise normalized to [0, 1].
float simplexNoise01(float2 v)
{
    return simplexNoise(v) * 0.5 + 0.5;
}

// ============================================================================
// FRACTAL BROWNIAN MOTION (FBM)
// ============================================================================

/// Standard FBM — sums multiple octaves of Perlin noise.
/// Returns approximately [-1, 1] (normalized by maxAmplitude).
///
/// Parameters:
///   uv          - Input UV coordinates
///   octaves     - Number of noise layers (1–8 typical)
///   persistence - Amplitude multiplier per octave (0.5 = halved each layer)
///   lacunarity  - Frequency multiplier per octave (2.0 = doubled each layer)
float fbm(float2 uv, int octaves, float persistence, float lacunarity)
{
    float value     = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    float maxValue  = 0.0;

    for (int i = 0; i < octaves; i++)
    {
        value    += perlinNoise(uv * frequency) * amplitude;
        maxValue += amplitude;
        amplitude *= persistence;
        frequency *= lacunarity;
    }

    return value / maxValue;
}

/// FBM normalized to [0, 1].
float fbm01(float2 uv, int octaves, float persistence, float lacunarity)
{
    return fbm(uv, octaves, persistence, lacunarity) * 0.5 + 0.5;
}

/// Ridged FBM — inverts each octave to create sharp ridges.
/// Great for mountain ranges, lightning, cracks.
/// Returns [0, 1].
float ridgedFBM(float2 uv, int octaves, float persistence, float lacunarity)
{
    float value     = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    float maxValue  = 0.0;
    float weight    = 1.0;

    for (int i = 0; i < octaves; i++)
    {
        float n = 1.0 - abs(perlinNoise(uv * frequency));
        n = n * n * weight;
        value    += n * amplitude;
        maxValue += amplitude;
        weight    = saturate(n * 2.0);
        amplitude *= persistence;
        frequency *= lacunarity;
    }

    return value / maxValue;
}

/// Domain-warped FBM — uses one FBM to distort the input of another.
/// Creates organic, flowing patterns. More expensive than standard FBM.
/// Returns [0, 1].
float domainWarpedFBM(float2 uv, int octaves, float persistence,
                       float lacunarity, float warpStrength)
{
    // First FBM pass — generates warp offsets
    float2 warp = float2(
        fbm(uv + float2(0.0, 0.0), octaves, persistence, lacunarity),
        fbm(uv + float2(5.2, 1.3), octaves, persistence, lacunarity)
    );

    // Second FBM pass — sample at warped position
    float2 warpedUV = uv + warpStrength * warp;
    return fbm01(warpedUV, octaves, persistence, lacunarity);
}

// ============================================================================
// VORONOI / WORLEY / CELLULAR NOISE
// ============================================================================

/// 2D Voronoi noise.
/// Returns: x = distance to nearest cell center [0, ~0.7]
///          y = distance to second nearest center (for edge detection)
float2 voronoiNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);

    float minDist1 = 8.0;
    float minDist2 = 8.0;

    for (int y = -2; y <= 2; y++)
    {
        for (int x = -2; x <= 2; x++)
        {
            float2 neighbor = float2(x, y);
            float2 point    = hash22(i + neighbor);

            // Animate cell centers (optional — remove for static noise)
            // point = 0.5 + 0.5 * sin(point * TAU);

            float2 diff = neighbor + point - f;
            float  dist = dot(diff, diff); // squared distance

            if (dist < minDist1)
            {
                minDist2 = minDist1;
                minDist1 = dist;
            }
            else if (dist < minDist2)
            {
                minDist2 = dist;
            }
        }
    }

    return float2(sqrt(minDist1), sqrt(minDist2));
}

/// Voronoi cell distance only. Returns [0, ~0.7].
float voronoiDistance(float2 uv)
{
    return voronoiNoise(uv).x;
}

/// Voronoi edge detection. Returns [0, 1] where 1 = cell edge.
/// edgeWidth controls the thickness of the edge lines.
float voronoiEdge(float2 uv, float edgeWidth)
{
    float2 v = voronoiNoise(uv);
    float edge = v.y - v.x; // difference between 1st and 2nd nearest
    return 1.0 - smoothstep(0.0, edgeWidth, edge);
}

/// Voronoi with cell ID coloring. Returns a color per cell.
float3 voronoiCellColor(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);

    float  minDist = 8.0;
    float2 minPoint;
    float2 minCell;

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 neighbor = float2(x, y);
            float2 point    = hash22(i + neighbor);
            float2 diff     = neighbor + point - f;
            float  dist     = dot(diff, diff);

            if (dist < minDist)
            {
                minDist  = dist;
                minPoint = point;
                minCell  = i + neighbor;
            }
        }
    }

    // Generate a unique color per cell using the cell's hash
    return hash33(float3(minCell, 0.0));
}

// ============================================================================
// TILEABLE NOISE
// ============================================================================

/// Tileable 2D value noise. Tiles perfectly at period p.
float tileableValueNoise(float2 uv, float2 period)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    float2 u = quinticFade2(f);

    // Wrap cell coordinates to create tiling
    float2 i00 = fmod(i + float2(0,0), period);
    float2 i10 = fmod(i + float2(1,0), period);
    float2 i01 = fmod(i + float2(0,1), period);
    float2 i11 = fmod(i + float2(1,1), period);

    float a = hash21(i00);
    float b = hash21(i10);
    float c = hash21(i01);
    float d = hash21(i11);

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// ============================================================================
// NOISE COMBINERS
// ============================================================================

/// Turbulence noise — sum of absolute values of octaves.
/// Creates cloud-like patterns with sharp features.
float turbulence(float2 uv, int octaves, float persistence, float lacunarity)
{
    float value     = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    float maxValue  = 0.0;

    for (int i = 0; i < octaves; i++)
    {
        value    += abs(perlinNoise(uv * frequency)) * amplitude;
        maxValue += amplitude;
        amplitude *= persistence;
        frequency *= lacunarity;
    }

    return value / maxValue;
}

/// Marble pattern — uses sin + FBM for a veined marble look.
float marblePattern(float2 uv, int octaves, float turbulenceStrength)
{
    float t = turbulence(uv, octaves, 0.5, 2.0) * turbulenceStrength;
    return sin(uv.x * 10.0 + t) * 0.5 + 0.5;
}

/// Wood ring pattern — concentric rings with noise distortion.
float woodPattern(float2 uv, float ringFrequency, float distortionStrength)
{
    float dist = length(uv) * ringFrequency;
    float noise = fbm01(uv, 4, 0.5, 2.0) * distortionStrength;
    return frac(dist + noise);
}

#endif // PROCEDURAL_NOISE_HLSL

// ============================================================================
// SDFShapes.hlsl
// Tech Art Toolkit — Shared HLSL SDF (Signed Distance Field) Library
//
// PURPOSE:
//   Cross-engine HLSL library of Signed Distance Field functions.
//   Compatible with Unity URP/HDRP, Unreal Engine 5, and standalone HLSL.
//
//   SDF functions return the SIGNED DISTANCE from point p to the shape:
//     Negative = inside the shape
//     Zero     = exactly on the surface
//     Positive = outside the shape
//
//   This signed distance can be used to:
//   - Generate crisp, anti-aliased shapes (smoothstep around 0)
//   - Create outlines (band around 0)
//   - Blend shapes with boolean operations (min/max)
//   - Drive material blending, masking, and procedural effects
//
// PRIMITIVES (2D):
//   sdCircle, sdBox, sdRoundedBox, sdSegment, sdCapsule,
//   sdRing, sdCross, sdTriangle, sdStar, sdPie, sdArc,
//   sdEllipse, sdParallelogram, sdTrapezoid, sdHeart
//
// PRIMITIVES (3D):
//   sdSphere, sdBox3D, sdCylinder, sdCone, sdTorus,
//   sdCapsule3D, sdRoundedCylinder, sdOctahedron
//
// BOOLEAN OPERATIONS:
//   opUnion, opSubtract, opIntersect, opXor,
//   opSmoothUnion, opSmoothSubtract, opSmoothIntersect
//
// DOMAIN OPERATIONS:
//   opRepeat, opRepeatLimited, opSymmetry,
//   opRound, opOnion, opExtrude, opRevolve
//
// RENDERING HELPERS:
//   sdToMask, sdToOutline, sdToGradient, sdVisualize
//
// REFERENCE: Inigo Quilez — https://iquilezles.org/articles/distfunctions2d/
// ============================================================================

#ifndef SDF_SHAPES_HLSL
#define SDF_SHAPES_HLSL

// ============================================================================
// 2D SDF PRIMITIVES
// ============================================================================

// ── Circle ───────────────────────────────────────────────────────────────────

/// Signed distance to a circle centered at origin.
/// r = radius
float sdCircle(float2 p, float r)
{
    return length(p) - r;
}

// ── Box / Rectangle ──────────────────────────────────────────────────────────

/// Signed distance to an axis-aligned box centered at origin.
/// b = half-extents (half-width, half-height)
float sdBox(float2 p, float2 b)
{
    float2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

/// Signed distance to a rounded box.
/// b = half-extents, r = corner radius
float sdRoundedBox(float2 p, float2 b, float4 r)
{
    // r.xy = top-right/top-left radii, r.zw = bottom-right/bottom-left
    r.xy = (p.x > 0.0) ? r.xy : r.zw;
    r.x = (p.y > 0.0) ? r.x : r.y;
    float2 q = abs(p) - b + r.x;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
}

/// Signed distance to a rounded box with uniform corner radius.
float sdRoundedBoxUniform(float2 p, float2 b, float r)
{
    return sdRoundedBox(p, b, float4(r, r, r, r));
}

// ── Line Segment ─────────────────────────────────────────────────────────────

/// Signed distance to a line segment from point a to point b.
/// Returns distance to the nearest point on the segment.
float sdSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

// ── Capsule ───────────────────────────────────────────────────────────────────

/// Signed distance to a capsule (rounded line segment).
/// a, b = endpoints, r = radius
float sdCapsule(float2 p, float2 a, float2 b, float r)
{
    return sdSegment(p, a, b) - r;
}

/// Vertical capsule centered at origin.
/// h = half-height of the shaft, r = radius
float sdVerticalCapsule(float2 p, float h, float r)
{
    p.y -= clamp(p.y, -h, h);
    return length(p) - r;
}

// ── Ring / Annulus ────────────────────────────────────────────────────────────

/// Signed distance to a ring (hollow circle).
/// r = center radius, t = half-thickness
float sdRing(float2 p, float r, float t)
{
    return abs(length(p) - r) - t;
}

// ── Cross ─────────────────────────────────────────────────────────────────────

/// Signed distance to a cross/plus shape.
/// b = arm half-extents (x = arm length, y = arm width), r = corner rounding
float sdCross(float2 p, float2 b, float r)
{
    p = abs(p);
    p = (p.y > p.x) ? p.yx : p.xy;
    float2 q = p - b;
    float k = max(q.y, q.x);
    float2 w = (k > 0.0) ? q : float2(b.y - p.x, -k);
    return sign(k) * length(max(w, 0.0)) - r;
}

// ── Triangle ──────────────────────────────────────────────────────────────────

/// Signed distance to an equilateral triangle centered at origin.
/// r = circumradius
float sdEquilateralTriangle(float2 p, float r)
{
    const float k = sqrt(3.0);
    p.x = abs(p.x) - r;
    p.y = p.y + r / k;
    if (p.x + k * p.y > 0.0)
        p = float2(p.x - k * p.y, -k * p.x - p.y) / 2.0;
    p.x -= clamp(p.x, -2.0 * r, 0.0);
    return -length(p) * sign(p.y);
}

/// Signed distance to an arbitrary triangle with vertices a, b, c.
float sdTriangle(float2 p, float2 a, float2 b, float2 c)
{
    float2 e0 = b - a, e1 = c - b, e2 = a - c;
    float2 v0 = p - a, v1 = p - b, v2 = p - c;

    float2 pq0 = v0 - e0 * clamp(dot(v0, e0) / dot(e0, e0), 0.0, 1.0);
    float2 pq1 = v1 - e1 * clamp(dot(v1, e1) / dot(e1, e1), 0.0, 1.0);
    float2 pq2 = v2 - e2 * clamp(dot(v2, e2) / dot(e2, e2), 0.0, 1.0);

    float s = sign(e0.x * e2.y - e0.y * e2.x);
    float2 d = min(min(
                       float2(dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                       float2(dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                   float2(dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));

    return -sqrt(d.x) * sign(d.y);
}

// ── Star ──────────────────────────────────────────────────────────────────────

/// Signed distance to a 5-pointed star.
/// r = outer radius, rf = inner radius ratio (0.4 = typical star)
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

/// Signed distance to an N-pointed star.
/// r = outer radius, rf = inner radius ratio, n = number of points
float sdStarN(float2 p, float r, float rf, int n)
{
    float an = 3.14159 / float(n);
    float en = 3.14159 / rf;
    float2 acs = float2(cos(an), sin(an));
    float2 ecs = float2(cos(en), sin(en));

    float bn = fmod(atan2(p.x, p.y), 2.0 * an) - an;
    p = length(p) * float2(cos(bn), abs(sin(bn)));
    p -= r * acs;
    p += ecs * clamp(-dot(p, ecs), 0.0, r * acs.y / ecs.y);
    return length(p) * sign(p.x);
}

// ── Pie / Sector ──────────────────────────────────────────────────────────────

/// Signed distance to a pie/sector shape.
/// c = float2(sin, cos) of half-angle, r = radius
float sdPie(float2 p, float2 c, float r)
{
    p.x = abs(p.x);
    float l = length(p) - r;
    float m = length(p - c * clamp(dot(p, c), 0.0, r));
    return max(l, m * sign(c.y * p.x - c.x * p.y));
}

// ── Arc ───────────────────────────────────────────────────────────────────────

/// Signed distance to an arc.
/// sc = float2(sin, cos) of half-angle, ra = outer radius, rb = thickness
float sdArc(float2 p, float2 sc, float ra, float rb)
{
    p.x = abs(p.x);
    float k = (sc.y * p.x > sc.x * p.y) ? dot(p, sc) : length(p);
    return sqrt(dot(p, p) + ra * ra - 2.0 * ra * k) - rb;
}

// ── Ellipse ───────────────────────────────────────────────────────────────────

/// Signed distance to an ellipse (approximate, fast).
/// ab = semi-axes (half-width, half-height)
float sdEllipseApprox(float2 p, float2 ab)
{
    p = abs(p);
    if (p.x > p.y)
    {
        p = p.yx;
        ab = ab.yx;
    }
    float l = ab.y * ab.y - ab.x * ab.x;
    float m = ab.x * p.x / l;
    float m2 = m * m;
    float n = ab.y * p.y / l;
    float n2 = n * n;
    float c = (m2 + n2 - 1.0) / 3.0;
    float c3 = c * c * c;
    float q = c3 + m2 * n2 * 2.0;
    float d = c3 + m2 * n2;
    float g = m + m * n2;
    float co;
    if (d < 0.0)
    {
        float h = acos(q / c3) / 3.0;
        float s = cos(h);
        float t = sin(h) * sqrt(3.0);
        float rx = sqrt(-c * (s + t + 2.0) + m2);
        float ry = sqrt(-c * (s - t + 2.0) + m2);
        co = (ry + sign(l) * rx + abs(g) / (rx * ry) - m) / 2.0;
    }
    else
    {
        float h = 2.0 * m * n * sqrt(d);
        float s = sign(q + h) * pow(abs(q + h), 1.0 / 3.0);
        float u = sign(q - h) * pow(abs(q - h), 1.0 / 3.0);
        float rx = -s - u - c * 4.0 + 2.0 * m2;
        float ry = (s - u) * sqrt(3.0);
        float rm = sqrt(rx * rx + ry * ry);
        co = (ry / sqrt(rm - rx) + 2.0 * g / rm - m) / 2.0;
    }
    float2 r = ab * float2(co, sqrt(1.0 - co * co));
    return length(r - p) * sign(p.y - r.y);
}

// ── Heart ─────────────────────────────────────────────────────────────────────

/// Signed distance to a heart shape.
float sdHeart(float2 p)
{
    p.x = abs(p.x);
    if (p.y + p.x > 1.0)
        return sqrt(dot(p - float2(0.25, 0.75), p - float2(0.25, 0.75))) - sqrt(2.0) / 4.0;
    return sqrt(min(dot(p - float2(0.00, 1.00), p - float2(0.00, 1.00)),
                    dot(p - 0.5 * max(p.x + p.y, 0.0), p - 0.5 * max(p.x + p.y, 0.0)))) *
           sign(p.x - p.y);
}

// ============================================================================
// 3D SDF PRIMITIVES
// ============================================================================

float sdSphere(float3 p, float r)
{
    return length(p) - r;
}

float sdBox3D(float3 p, float3 b)
{
    float3 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, max(d.y, d.z)), 0.0);
}

float sdCylinder(float3 p, float h, float r)
{
    float2 d = abs(float2(length(p.xz), p.y)) - float2(r, h);
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}

float sdTorus(float3 p, float2 t)
{
    float2 q = float2(length(p.xz) - t.x, p.y);
    return length(q) - t.y;
}

float sdCapsule3D(float3 p, float3 a, float3 b, float r)
{
    float3 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) - r;
}

float sdOctahedron(float3 p, float s)
{
    p = abs(p);
    float m = p.x + p.y + p.z - s;
    float3 q;
    if (3.0 * p.x < m)
        q = p.xyz;
    else if (3.0 * p.y < m)
        q = p.yzx;
    else if (3.0 * p.z < m)
        q = p.zxy;
    else
        return m * 0.57735027;
    float k = clamp(0.5 * (q.z - q.y + s), 0.0, s);
    return length(float3(q.x, q.y - s + k, q.z - k));
}

// ============================================================================
// BOOLEAN OPERATIONS
// ============================================================================

/// Union: the combined shape of A and B.
float opUnion(float d1, float d2)
{
    return min(d1, d2);
}

/// Subtraction: A minus B (removes B from A).
float opSubtract(float d1, float d2)
{
    return max(d1, -d2);
}

/// Intersection: only the overlapping region of A and B.
float opIntersect(float d1, float d2)
{
    return max(d1, d2);
}

/// XOR: union minus intersection (non-overlapping parts only).
float opXor(float d1, float d2)
{
    return max(min(d1, d2), -max(d1, d2));
}

/// Smooth Union: blends A and B with a smooth transition.
/// k = blend radius (larger = smoother blend).
float opSmoothUnion(float d1, float d2, float k)
{
    float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0);
    return lerp(d2, d1, h) - k * h * (1.0 - h);
}

/// Smooth Subtraction: smoothly removes B from A.
float opSmoothSubtract(float d1, float d2, float k)
{
    float h = clamp(0.5 - 0.5 * (d2 + d1) / k, 0.0, 1.0);
    return lerp(d1, -d2, h) + k * h * (1.0 - h);
}

/// Smooth Intersection: smoothly intersects A and B.
float opSmoothIntersect(float d1, float d2, float k)
{
    float h = clamp(0.5 - 0.5 * (d2 - d1) / k, 0.0, 1.0);
    return lerp(d2, d1, h) + k * h * (1.0 - h);
}

// ============================================================================
// DOMAIN OPERATIONS
// ============================================================================

/// Infinite repetition: tiles the domain with period c.
float2 opRepeat(float2 p, float2 c)
{
    return fmod(p + 0.5 * c, c) - 0.5 * c;
}

/// Limited repetition: tiles only within [-lim, lim] repetitions.
float2 opRepeatLimited(float2 p, float c, float2 lim)
{
    return p - c * clamp(round(p / c), -lim, lim);
}

/// Mirror symmetry on X axis.
float2 opSymmetryX(float2 p)
{
    return float2(abs(p.x), p.y);
}

/// Mirror symmetry on both axes.
float2 opSymmetryXY(float2 p)
{
    return abs(p);
}

/// Rounds a shape outward by r (inflates it).
float opRound(float d, float r)
{
    return d - r;
}

/// Creates a shell/onion of thickness r around a shape.
float opOnion(float d, float r)
{
    return abs(d) - r;
}

// ============================================================================
// RENDERING HELPERS
// ============================================================================

/// Converts SDF distance to a [0,1] filled mask.
/// softness = anti-aliasing width (use fwidth(d) for screen-space AA).
float sdToMask(float d, float softness)
{
    return 1.0 - smoothstep(-softness, softness, d);
}

/// Converts SDF distance to an outline mask.
/// width = outline half-width, softness = edge softness.
float sdToOutline(float d, float width, float softness)
{
    return 1.0 - smoothstep(0.0, softness, abs(d) - width);
}

/// Returns a gradient based on distance (useful for glow effects).
/// falloff controls how quickly the gradient fades.
float sdToGradient(float d, float falloff)
{
    return exp(-max(d, 0.0) * falloff);
}

/// Combines fill + outline into a single color output.
/// fillColor = interior color, outlineColor = border color,
/// bgColor = background color.
float3 sdRender(float d, float3 fillColor, float3 outlineColor,
                float3 bgColor, float outlineWidth, float softness)
{
    float fill = sdToMask(d, softness);
    float outline = sdToOutline(d, outlineWidth, softness);
    float3 color = lerp(bgColor, outlineColor, outline);
    color = lerp(color, fillColor, fill);
    return color;
}

/// Debug visualization: maps SDF to a color field showing distance contours.
float3 sdVisualize(float d)
{
    // Contour lines
    float contour = abs(sin(d * 20.0 * 3.14159));

    float3 inside = float3(0.9, 0.4, 0.1);
    float3 outside = float3(0.1, 0.3, 0.7);
    float3 col = d < 0.0 ? inside : outside;

    col *= 1.0 - 0.3 * abs(d) * 5.0;
    col = lerp(col, float3(1, 1, 1), (1.0 - contour) * 0.3);
    col = lerp(col, float3(1, 1, 1), 1.0 - smoothstep(0.0, 0.01, abs(d)));

    return col;
}

#endif // SDF_SHAPES_HLSL

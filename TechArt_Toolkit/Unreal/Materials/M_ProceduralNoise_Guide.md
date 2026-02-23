# Unreal Engine 5 — Procedural Noise Material Guide

## Tech Art Toolkit — Module 1: Shader & Procedural Lab

---

## Overview

This document describes how to build `M_ProceduralNoise` — the Unreal Engine 5
material that powers the Shader & Procedural Lab module. It uses Custom HLSL
nodes to implement the same noise functions as the Unity shader, ensuring
cross-engine visual parity.

---

## Material Setup

### File: `M_ProceduralNoise`

**Type:** Material (not Material Function — we need the full material graph)
**Blend Mode:** Opaque
**Shading Model:** Unlit (for pure procedural output) or Default Lit (for PBR preview)
**Usage:** Used on a preview sphere/plane in the editor scene

---

## Material Graph Structure

```
[Material Output Node]
    ├── Base Color ← [Custom HLSL: FinalColor]
    ├── Roughness  ← 0.5 (constant)
    ├── Metallic   ← 0.0 (constant)
    └── Emissive   ← [Custom HLSL: FinalColor] (for Unlit mode)
```

---

## Step 1: Create Material Parameters

Right-click in the Material Graph → Add Parameter nodes:

### Scalar Parameters

| Parameter Name | Default | Min | Max | Description |
|---|---|---|---|---|
| `NoiseType` | 0 | 0 | 3 | 0=FBM, 1=Voronoi, 2=Perlin, 3=Value |
| `NoiseScale` | 3.0 | 0.1 | 20.0 | Noise frequency/zoom |
| `NoiseOctaves` | 4.0 | 1 | 8 | FBM octave count |
| `NoisePersistence` | 0.5 | 0.1 | 1.0 | FBM amplitude falloff |
| `NoiseLacunarity` | 2.0 | 1.0 | 4.0 | FBM frequency multiplier |
| `NoiseContrast` | 1.0 | 0.1 | 5.0 | Output contrast |
| `UVTilingX` | 1.0 | 0.1 | 10.0 | UV X tiling |
| `UVTilingY` | 1.0 | 0.1 | 10.0 | UV Y tiling |
| `UVOffsetX` | 0.0 | -5.0 | 5.0 | UV X offset |
| `UVOffsetY` | 0.0 | -5.0 | 5.0 | UV Y offset |
| `UVRotation` | 0.0 | -3.14 | 3.14 | UV rotation (radians) |
| `TrigFrequency` | 2.0 | 0.1 | 20.0 | Wave distortion frequency |
| `TrigAmplitude` | 0.1 | 0.0 | 0.5 | Wave distortion strength |
| `SDFShape` | 0 | 0 | 4 | 0=Circle, 1=Box, 2=Ring, 3=Cross, 4=None |
| `SDFRadius` | 0.35 | 0.01 | 0.9 | SDF shape size |
| `SDFSoftness` | 0.05 | 0.001 | 0.3 | SDF edge softness |
| `SDFBlend` | 1.0 | 0.0 | 1.0 | SDF mask blend strength |
| `ColorContrast` | 1.0 | 0.1 | 5.0 | Color contrast |
| `ColorBrightness` | 0.0 | -1.0 | 1.0 | Color brightness offset |

### Vector Parameters

| Parameter Name | Default | Description |
|---|---|---|
| `ColorA` | (0.05, 0.05, 0.15) | Dark color (noise = 0) |
| `ColorB` | (0.2, 0.6, 1.0) | Bright color (noise = 1) |
| `SDFCenter` | (0.5, 0.5, 0, 0) | SDF shape center in UV space |

---

## Step 2: Create the Custom HLSL Node

1. Right-click in Material Graph → **Custom** node
2. Set **Output Type** to `CMOT Float3`
3. Set **Description** to `ProceduralNoise`
4. Add the following **Inputs** (click + in the Inputs array):

| Input Name | Type |
|---|---|
| `UV` | Float2 |
| `NoiseType` | Float |
| `NoiseScale` | Float |
| `NoiseOctaves` | Float |
| `NoisePersistence` | Float |
| `NoiseLacunarity` | Float |
| `NoiseContrast` | Float |
| `UVTiling` | Float2 |
| `UVOffset` | Float2 |
| `UVRotation` | Float |
| `TrigFrequency` | Float |
| `TrigAmplitude` | Float |
| `SDFShape` | Float |
| `SDFRadius` | Float |
| `SDFSoftness` | Float |
| `SDFBlend` | Float |
| `SDFCenter` | Float2 |
| `ColorA` | Float3 |
| `ColorB` | Float3 |
| `ColorContrast` | Float |
| `ColorBrightness` | Float |

1. Paste the following HLSL code into the **Code** field:

```hlsl
// ── Hash Functions ────────────────────────────────────────────────────────
float hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float2 hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

// ── UV Transform ──────────────────────────────────────────────────────────
float2 TransformUV(float2 uv, float2 tiling, float2 offset, float rotation)
{
    uv = uv * tiling + offset;
    float s = sin(rotation), c = cos(rotation);
    uv -= 0.5;
    uv = float2(uv.x*c - uv.y*s, uv.x*s + uv.y*c);
    uv += 0.5;
    return uv;
}

// ── Trig Distortion ───────────────────────────────────────────────────────
float2 TrigDistort(float2 uv, float freq, float amp)
{
    // Use View.GameTime for animation (Unreal's built-in time)
    float t = View.GameTime;
    return uv + float2(
        sin(uv.y * freq + t) * amp,
        cos(uv.x * freq + t) * amp
    );
}

// ── Perlin Noise ──────────────────────────────────────────────────────────
float PerlinNoise(float2 uv)
{
    float2 i = floor(uv), f = frac(uv);
    float2 u = f*f*f*(f*(f*6.0-15.0)+10.0);
    float2 ga = hash22(i+float2(0,0))*2.0-1.0;
    float2 gb = hash22(i+float2(1,0))*2.0-1.0;
    float2 gc = hash22(i+float2(0,1))*2.0-1.0;
    float2 gd = hash22(i+float2(1,1))*2.0-1.0;
    return lerp(lerp(dot(ga,f-float2(0,0)),dot(gb,f-float2(1,0)),u.x),
                lerp(dot(gc,f-float2(0,1)),dot(gd,f-float2(1,1)),u.x),u.y);
}

// ── FBM ───────────────────────────────────────────────────────────────────
float FBM(float2 uv, int octaves, float persistence, float lacunarity)
{
    float v=0, a=0.5, f=1, mv=0;
    for(int i=0;i<octaves;i++){
        v+=PerlinNoise(uv*f)*a; mv+=a; a*=persistence; f*=lacunarity;
    }
    return v/mv;
}

// ── Voronoi ───────────────────────────────────────────────────────────────
float VoronoiNoise(float2 uv)
{
    float2 i=floor(uv), f=frac(uv);
    float md=8.0;
    for(int y=-1;y<=1;y++) for(int x=-1;x<=1;x++){
        float2 n=float2(x,y), p=hash22(i+n);
        p=0.5+0.5*sin(p*6.2831);
        float2 d=n+p-f;
        md=min(md,dot(d,d));
    }
    return sqrt(md);
}

// ── Noise Dispatcher ──────────────────────────────────────────────────────
float SampleNoise(float2 uv, float type, float scale,
                  float octaves, float persistence, float lacunarity)
{
    float2 suv = uv * scale;
    float n = 0;
    int t = (int)type;
    if      (t==0) n = FBM(suv,(int)octaves,persistence,lacunarity)*0.5+0.5;
    else if (t==1) n = 1.0 - VoronoiNoise(suv)*1.4;
    else if (t==2) n = PerlinNoise(suv)*0.5+0.5;
    else           n = hash21(floor(suv));
    return saturate(n);
}

// ── SDF Functions ─────────────────────────────────────────────────────────
float sdCircle(float2 p, float r) { return length(p)-r; }
float sdBox(float2 p, float2 b)
{
    float2 d=abs(p)-b;
    return length(max(d,0.0))+min(max(d.x,d.y),0.0);
}
float sdRing(float2 p, float r, float t) { return abs(length(p)-r)-t; }
float sdCross(float2 p, float2 b, float r)
{
    p=abs(p); p=(p.y>p.x)?p.yx:p.xy;
    float2 q=p-b; float k=max(q.y,q.x);
    float2 w=(k>0.0)?q:float2(b.y-p.x,-k);
    return sign(k)*length(max(w,0.0))-r;
}

float EvaluateSDF(float2 uv, float shape, float radius,
                  float softness, float2 center)
{
    float2 p = uv - center;
    float d = 0;
    int s = (int)shape;
    if      (s==0) d = sdCircle(p, radius);
    else if (s==1) d = sdBox(p, float2(radius,radius));
    else if (s==2) d = sdRing(p, radius, radius*0.25);
    else if (s==3) d = sdCross(p, float2(radius,radius*0.3), 0.005);
    else           return 1.0;
    return 1.0 - smoothstep(-softness, softness, d);
}

// ── Main ──────────────────────────────────────────────────────────────────
// 1. Transform UV
float2 uv_t = TransformUV(UV, UVTiling, UVOffset, UVRotation);

// 2. Trig distortion
uv_t = TrigDistort(uv_t, TrigFrequency, TrigAmplitude);

// 3. Sample noise
float noise = SampleNoise(uv_t, NoiseType, NoiseScale,
                           NoiseOctaves, NoisePersistence, NoiseLacunarity);

// 4. Apply contrast
noise = pow(saturate(noise), NoiseContrast);

// 5. SDF mask
float sdfMask = EvaluateSDF(UV, SDFShape, SDFRadius, SDFSoftness, SDFCenter);
float finalVal = lerp(noise, noise * sdfMask, SDFBlend);

// 6. Brightness
finalVal = saturate(finalVal + ColorBrightness);

// 7. Color remap
return lerp(ColorA, ColorB, finalVal);
```

---

## Step 3: Connect Nodes

Wire each parameter node to the corresponding Custom node input:

```
[Scalar Param: NoiseType]      → Custom.NoiseType
[Scalar Param: NoiseScale]     → Custom.NoiseScale
[Scalar Param: NoiseOctaves]   → Custom.NoiseOctaves
[Scalar Param: NoisePersistence] → Custom.NoisePersistence
[Scalar Param: NoiseLacunarity]  → Custom.NoiseLacunarity
[Scalar Param: NoiseContrast]  → Custom.NoiseContrast

[Append: UVTilingX, UVTilingY] → Custom.UVTiling
[Append: UVOffsetX, UVOffsetY] → Custom.UVOffset
[Scalar Param: UVRotation]     → Custom.UVRotation

[Scalar Param: TrigFrequency]  → Custom.TrigFrequency
[Scalar Param: TrigAmplitude]  → Custom.TrigAmplitude

[Scalar Param: SDFShape]       → Custom.SDFShape
[Scalar Param: SDFRadius]      → Custom.SDFRadius
[Scalar Param: SDFSoftness]    → Custom.SDFSoftness
[Scalar Param: SDFBlend]       → Custom.SDFBlend
[Vector Param: SDFCenter] → ComponentMask(RG) → Custom.SDFCenter

[Vector Param: ColorA] → ComponentMask(RGB) → Custom.ColorA
[Vector Param: ColorB] → ComponentMask(RGB) → Custom.ColorB
[Scalar Param: ColorContrast]  → Custom.ColorContrast
[Scalar Param: ColorBrightness]→ Custom.ColorBrightness

[TexCoord[0]]                  → Custom.UV

[Custom Output]                → Material.Base Color
[Custom Output]                → Material.Emissive Color (for Unlit mode)
```

---

## Step 4: Create Material Instance

1. Right-click `M_ProceduralNoise` → **Create Material Instance**
2. Name it `MI_ProceduralNoise_Default`
3. This is the instance referenced by `WBP_ShaderLab`
4. The widget modifies this instance at runtime via `SetScalarParameterValue`

---

## Step 5: Preview Setup

1. Place a **Sphere** static mesh in the level
2. Name it `TAT_PreviewSphere`
3. Assign `MI_ProceduralNoise_Default` as its material
4. Place a **SceneCapture2D** actor pointing at the sphere
5. Set capture resolution to 256×256
6. Assign the render target to the `PreviewImage` widget in `WBP_ShaderLab`

---

## Material Optimization Notes

| Concern | Solution |
|---|---|
| Custom HLSL compile time | Keep code minimal; split into Material Functions if reusing |
| Shader permutations | Use `StaticSwitchParameter` for noise type instead of dynamic int |
| Mobile compatibility | Replace Custom node with Material Function nodes for mobile |
| Instruction count | FBM with 8 octaves = ~80 ALU instructions — use 4 for mobile |
| Time node | Use `View.GameTime` in Custom HLSL, or connect `Time` node to input |

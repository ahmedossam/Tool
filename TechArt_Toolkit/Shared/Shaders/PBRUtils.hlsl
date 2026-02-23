// ============================================================================
// PBRUtils.hlsl
// Tech Art Toolkit — Shared HLSL PBR (Physically Based Rendering) Library
//
// PURPOSE:
//   Cross-engine HLSL library implementing the Cook-Torrance BRDF used in
//   modern game engines. Compatible with Unity URP/HDRP, Unreal Engine 5,
//   and standalone HLSL shaders.
//
//   This library demonstrates understanding of:
//   - Physically Based Rendering theory (microfacet model)
//   - The Cook-Torrance BRDF components (D, G, F)
//   - Energy conservation
//   - Metal/roughness workflow
//   - Image-Based Lighting (IBL) approximations
//
// BRDF COMPONENTS:
//   D — Normal Distribution Function (NDF): GGX/Trowbridge-Reitz
//   G — Geometry/Shadowing Function: Smith + Schlick-GGX
//   F — Fresnel Equation: Schlick approximation
//
// FUNCTIONS:
//   Core BRDF:    D_GGX, G_SchlickGGX, G_Smith, F_Schlick, F_SchlickRoughness
//   Full BRDF:    CookTorranceBRDF, DirectLighting
//   IBL:          EnvBRDFApprox, IBLDiffuse, IBLSpecular
//   Utility:      LinearToSRGB, SRGBToLinear, ACESFilm, ReinhardTonemap
//                 GetF0, LuminanceRec709, ValidatePBRAlbedo
//
// REFERENCE:
//   - Physically Based Rendering (Pharr, Jakob, Humphreys)
//   - Real Shading in Unreal Engine 4 (Brian Karis, Epic Games)
//   - Moving Frostbite to PBR (Lagarde & de Rousiers, EA Dice)
//   - https://learnopengl.com/PBR/Theory
// ============================================================================

#ifndef PBR_UTILS_HLSL
#define PBR_UTILS_HLSL

// ============================================================================
// CONSTANTS
// ============================================================================

#define PBR_PI 3.14159265359
#define PBR_INV_PI 0.31830988618
#define PBR_EPSILON 0.0001

// Minimum reflectance for dielectrics (4% = 0.04)
// This is the F0 value for most non-metallic surfaces
#define DIELECTRIC_F0 0.04

// ============================================================================
// COLOR SPACE CONVERSIONS
// ============================================================================

/// Converts linear color to sRGB (gamma 2.2 approximation).
/// Use for final output when render target is sRGB.
float3 LinearToSRGB(float3 linearColor)
{
    return pow(max(linearColor, 0.0), 1.0 / 2.2);
}

/// Converts sRGB color to linear (gamma 2.2 approximation).
/// Use when sampling albedo textures marked as sRGB.
float3 SRGBToLinear(float3 srgbColor)
{
    return pow(max(srgbColor, 0.0), 2.2);
}

/// Precise sRGB to linear conversion (IEC 61966-2-1 standard).
float3 SRGBToLinearPrecise(float3 c)
{
    return (c <= 0.04045) ? c / 12.92 : pow((c + 0.055) / 1.055, 2.4);
}

// ============================================================================
// TONE MAPPING
// ============================================================================

/// ACES Filmic tone mapping (Academy Color Encoding System).
/// Industry standard for cinematic HDR → LDR conversion.
/// Used in Unreal Engine 4/5 by default.
float3 ACESFilm(float3 x)
{
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
}

/// Reinhard tone mapping — simple, widely used.
float3 ReinhardTonemap(float3 hdr)
{
    return hdr / (hdr + float3(1.0, 1.0, 1.0));
}

/// Extended Reinhard — preserves luminance better.
float3 ReinhardExtended(float3 hdr, float maxWhite)
{
    float3 numerator = hdr * (1.0 + (hdr / (maxWhite * maxWhite)));
    return numerator / (1.0 + hdr);
}

/// Uncharted 2 / Hable filmic tone mapping.
float3 Uncharted2Tonemap(float3 x)
{
    float A = 0.15, B = 0.50, C = 0.10, D = 0.20, E = 0.02, F = 0.30;
    return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
}

// ============================================================================
// LUMINANCE
// ============================================================================

/// Perceptual luminance using Rec. 709 coefficients.
float LuminanceRec709(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

/// Perceptual luminance using Rec. 2020 coefficients (HDR displays).
float LuminanceRec2020(float3 color)
{
    return dot(color, float3(0.2627, 0.6780, 0.0593));
}

// ============================================================================
// PBR MATERIAL UTILITIES
// ============================================================================

/// Computes F0 (base reflectance at normal incidence) for a PBR material.
/// For dielectrics: F0 = 0.04 (4% reflectance)
/// For metals: F0 = albedo (tinted reflectance)
///
/// Parameters:
///   albedo   - Base color (linear)
///   metallic - Metalness value [0 = dielectric, 1 = metal]
float3 GetF0(float3 albedo, float metallic)
{
    return lerp(float3(DIELECTRIC_F0, DIELECTRIC_F0, DIELECTRIC_F0), albedo, metallic);
}

/// Validates whether an albedo value is within PBR-correct range.
/// Returns 1.0 if valid, 0.0 if out of range.
///
/// PBR Rules:
///   Dielectric albedo: 50–240 sRGB (0.04–0.9 linear)
///   Metal albedo: 180–255 sRGB (0.5–1.0 linear) — tinted reflectance
float ValidatePBRAlbedo(float3 albedo, float metallic)
{
    float lum = LuminanceRec709(albedo);

    if (metallic < 0.5)
    {
        // Dielectric: luminance should be 0.04–0.9
        return (lum >= 0.04 && lum <= 0.9) ? 1.0 : 0.0;
    }
    else
    {
        // Metal: luminance should be 0.5–1.0
        return (lum >= 0.5 && lum <= 1.0) ? 1.0 : 0.0;
    }
}

// ============================================================================
// BRDF — NORMAL DISTRIBUTION FUNCTION (D)
// ============================================================================

/// GGX / Trowbridge-Reitz Normal Distribution Function.
///
/// Describes the statistical distribution of microfacet normals.
/// Higher roughness = wider, flatter specular highlight.
/// Lower roughness = narrow, sharp specular highlight.
///
/// Parameters:
///   N         - Surface normal (normalized)
///   H         - Half-vector between view and light (normalized)
///   roughness - Perceptual roughness [0 = smooth, 1 = rough]
float D_GGX(float3 N, float3 H, float roughness)
{
    float a = roughness * roughness; // remap roughness to alpha
    float a2 = a * a;

    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;

    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PBR_PI * denom * denom;

    return a2 / max(denom, PBR_EPSILON);
}

// ============================================================================
// BRDF — GEOMETRY / SHADOWING FUNCTION (G)
// ============================================================================

/// Schlick-GGX geometry function for a single direction.
/// Approximates the probability that microfacets are not self-shadowed.
///
/// k is remapped differently for direct vs IBL lighting:
///   Direct:  k = (roughness + 1)^2 / 8
///   IBL:     k = roughness^2 / 2
float G_SchlickGGX(float NdotV, float k)
{
    return NdotV / (NdotV * (1.0 - k) + k);
}

/// Smith's method: combines geometry obstruction (view) and shadowing (light).
/// Uses Schlick-GGX for both directions.
///
/// Parameters:
///   N         - Surface normal
///   V         - View direction (toward camera)
///   L         - Light direction (toward light)
///   roughness - Perceptual roughness
float G_Smith(float3 N, float3 V, float3 L, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0; // k for direct lighting

    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);

    float ggx1 = G_SchlickGGX(NdotV, k);
    float ggx2 = G_SchlickGGX(NdotL, k);

    return ggx1 * ggx2;
}

/// Smith's method with IBL k remapping.
float G_SmithIBL(float3 N, float3 V, float3 L, float roughness)
{
    float a = roughness * roughness;
    float k = a / 2.0; // k for IBL

    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);

    return G_SchlickGGX(NdotV, k) * G_SchlickGGX(NdotL, k);
}

// ============================================================================
// BRDF — FRESNEL EQUATION (F)
// ============================================================================

/// Schlick's approximation of the Fresnel equation.
///
/// Describes how reflectance increases at grazing angles (Fresnel effect).
/// At normal incidence: F = F0
/// At grazing incidence: F → 1.0 (full reflectance)
///
/// Parameters:
///   cosTheta - Dot product of half-vector and view direction
///   F0       - Base reflectance at normal incidence
float3 F_Schlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(saturate(1.0 - cosTheta), 5.0);
}

/// Fresnel-Schlick with roughness term for IBL.
/// Roughness reduces the Fresnel effect at grazing angles.
/// Used for environment map sampling.
float3 F_SchlickRoughness(float cosTheta, float3 F0, float roughness)
{
    return F0 + (max(float3(1.0 - roughness, 1.0 - roughness, 1.0 - roughness), F0) - F0) * pow(saturate(1.0 - cosTheta), 5.0);
}

// ============================================================================
// FULL COOK-TORRANCE BRDF
// ============================================================================

/// Evaluates the full Cook-Torrance specular BRDF for a single light.
///
/// Cook-Torrance BRDF = (D * G * F) / (4 * NdotV * NdotL)
///
/// Parameters:
///   N         - Surface normal (normalized)
///   V         - View direction (toward camera, normalized)
///   L         - Light direction (toward light, normalized)
///   F0        - Base reflectance (from GetF0())
///   roughness - Perceptual roughness [0–1]
///
/// Returns: specular BRDF value (multiply by light color and NdotL)
float3 CookTorranceBRDF(float3 N, float3 V, float3 L, float3 F0, float roughness)
{
    float3 H = normalize(V + L);

    float D = D_GGX(N, H, roughness);
    float G = G_Smith(N, V, L, roughness);
    float3 F = F_Schlick(max(dot(H, V), 0.0), F0);

    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);

    float3 numerator = D * G * F;
    float denominator = 4.0 * NdotV * NdotL + PBR_EPSILON;

    return numerator / denominator;
}

/// Computes full direct lighting contribution for one light source.
///
/// Combines diffuse (Lambertian) and specular (Cook-Torrance) terms.
/// Respects energy conservation: kD + kS = 1 for dielectrics.
///
/// Parameters:
///   N           - Surface normal
///   V           - View direction
///   L           - Light direction
///   albedo      - Base color (linear)
///   metallic    - Metalness [0–1]
///   roughness   - Perceptual roughness [0–1]
///   lightColor  - Light color × intensity
///   lightRadius - Light attenuation radius (0 = directional)
///
/// Returns: final lit color contribution from this light
float3 DirectLighting(float3 N, float3 V, float3 L,
                      float3 albedo, float metallic, float roughness,
                      float3 lightColor, float lightRadius)
{
    float3 F0 = GetF0(albedo, metallic);
    float3 H = normalize(V + L);

    // Attenuation (point/spot lights)
    float attenuation = 1.0;
    if (lightRadius > 0.0)
    {
        float dist = length(L);
        attenuation = 1.0 / (dist * dist);
        attenuation *= saturate(1.0 - (dist / lightRadius));
    }

    float3 radiance = lightColor * attenuation;

    // Cook-Torrance specular
    float3 specular = CookTorranceBRDF(N, V, normalize(L), F0, roughness);

    // Fresnel for energy conservation
    float3 F = F_Schlick(max(dot(H, V), 0.0), F0);
    float3 kS = F;
    float3 kD = (1.0 - kS) * (1.0 - metallic); // metals have no diffuse

    // Lambertian diffuse
    float3 diffuse = kD * albedo * PBR_INV_PI;

    float NdotL = max(dot(N, normalize(L)), 0.0);

    return (diffuse + specular) * radiance * NdotL;
}

// ============================================================================
// IMAGE-BASED LIGHTING (IBL)
// ============================================================================

/// Approximate environment BRDF lookup (replaces a pre-integrated BRDF LUT).
/// Based on the Unreal Engine 4 approximation by Brian Karis.
///
/// Parameters:
///   roughness - Perceptual roughness
///   NdotV     - Dot product of normal and view direction
///
/// Returns: float2(scale, bias) for F0 integration
float2 EnvBRDFApprox(float roughness, float NdotV)
{
    // Karis 2014 approximation
    const float4 c0 = float4(-1.0, -0.0275, -0.572, 0.022);
    const float4 c1 = float4(1.0, 0.0425, 1.040, -0.040);
    float4 r = roughness * c0 + c1;
    float a004 = min(r.x * r.x, exp2(-9.28 * NdotV)) * r.x + r.y;
    return float2(-1.04, 1.04) * a004 + r.zw;
}

/// Computes IBL specular contribution using the split-sum approximation.
///
/// Parameters:
///   N           - Surface normal
///   V           - View direction
///   F0          - Base reflectance
///   roughness   - Perceptual roughness
///   envSample   - Pre-filtered environment map sample (at mip = roughness)
///
/// Returns: IBL specular color
float3 IBLSpecular(float3 N, float3 V, float3 F0, float roughness, float3 envSample)
{
    float NdotV = max(dot(N, V), 0.0);
    float3 F = F_SchlickRoughness(NdotV, F0, roughness);
    float2 envBRDF = EnvBRDFApprox(roughness, NdotV);
    return envSample * (F * envBRDF.x + envBRDF.y);
}

/// Computes IBL diffuse contribution (irradiance).
///
/// Parameters:
///   N           - Surface normal
///   albedo      - Base color (linear)
///   metallic    - Metalness
///   F0          - Base reflectance
///   roughness   - Perceptual roughness
///   irradiance  - Irradiance map sample (diffuse IBL)
///   ao          - Ambient occlusion [0–1]
///
/// Returns: IBL diffuse color
float3 IBLDiffuse(float3 N, float3 V, float3 albedo, float metallic,
                  float3 F0, float roughness, float3 irradiance, float ao)
{
    float NdotV = max(dot(N, V), 0.0);
    float3 F = F_SchlickRoughness(NdotV, F0, roughness);
    float3 kS = F;
    float3 kD = (1.0 - kS) * (1.0 - metallic);
    return kD * irradiance * albedo * ao;
}

// ============================================================================
// NORMAL MAPPING
// ============================================================================

/// Reconstructs the Z component of a normal map (BC5/RG format).
/// BC5 stores only R and G channels — Z must be reconstructed.
float3 ReconstructNormalZ(float2 rg)
{
    float3 n;
    n.xy = rg * 2.0 - 1.0;
    n.z = sqrt(max(1.0 - dot(n.xy, n.xy), 0.0));
    return normalize(n);
}

/// Transforms a tangent-space normal to world space using TBN matrix.
float3 TangentToWorld(float3 tangentNormal, float3 worldNormal,
                      float3 worldTangent, float3 worldBitangent)
{
    float3x3 TBN = float3x3(
        normalize(worldTangent),
        normalize(worldBitangent),
        normalize(worldNormal));
    return normalize(mul(tangentNormal, TBN));
}

// ============================================================================
// AMBIENT OCCLUSION
// ============================================================================

/// Applies ambient occlusion to both diffuse and specular IBL.
/// Specular AO is derived from diffuse AO using a roughness-based approximation.
float3 ApplyAO(float3 color, float ao, float roughness, float NdotV)
{
    // Specular AO: less AO effect on rough surfaces (they scatter light anyway)
    float specAO = saturate(pow(NdotV + ao, exp2(-16.0 * roughness - 1.0)) - 1.0 + ao);
    return color * lerp(ao, specAO, 0.5);
}

// ============================================================================
// UTILITY: PBR VALIDATION
// ============================================================================

/// Returns a debug color indicating PBR correctness of a material.
///   Green  = PBR-correct values
///   Yellow = borderline values
///   Red    = PBR-incorrect values
float3 PBRValidationColor(float3 albedo, float metallic, float roughness)
{
    float lum = LuminanceRec709(albedo);
    bool albedoValid;

    if (metallic < 0.5)
        albedoValid = (lum >= 0.04 && lum <= 0.9);
    else
        albedoValid = (lum >= 0.5 && lum <= 1.0);

    bool roughnessValid = (roughness >= 0.0 && roughness <= 1.0);
    bool metallicValid = (metallic == 0.0 || metallic == 1.0 ||
                          (metallic > 0.0 && metallic < 1.0)); // transitions allowed

    if (albedoValid && roughnessValid)
        return float3(0.2, 0.9, 0.2); // Green: correct
    else if (!albedoValid)
        return float3(0.9, 0.2, 0.2); // Red: albedo out of range
    else
        return float3(0.9, 0.8, 0.1); // Yellow: borderline
}

#endif // PBR_UTILS_HLSL

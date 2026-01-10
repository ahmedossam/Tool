# 6-Month Technical Artist Career Plan: PBR Texturing & Real-Time Rendering

This comprehensive guide covers essential concepts for Technical Artists focusing on Physically Based Rendering (PBR) texturing principles and real-time rendering concepts. Includes practical shader examples in HLSL/GLSL.

## Month 1: Foundations of Real-Time Rendering

### Key Concepts
- **Rendering Pipeline**: Vertex Processing → Rasterization → Fragment Processing → Output Merging
- **Coordinate Systems**: Object Space → World Space → View Space → Clip Space → Screen Space
- **Shaders**: Small programs running on GPU for vertex/fragment processing
- **Frame Buffer**: Color, depth, stencil buffers

### PBR Basics
- **Physically Based Rendering**: Simulates real-world light interaction
- **Bidirectional Reflectance Distribution Function (BRDF)**: Describes how light reflects off surfaces
- **Microfacet Theory**: Surface roughness affects light scattering

### Shader Example: Basic Lighting
```glsl
// GLSL Fragment Shader with Basic Lighting
#version 330 core

in vec3 Normal;
in vec3 FragPos;
in vec2 TexCoord;

uniform vec3 lightPos;
uniform vec3 lightColor;
uniform vec3 viewPos;
uniform sampler2D diffuseTexture;

out vec4 FragColor;

void main()
{
    // Ambient
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor;

    // Diffuse
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(lightPos - FragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;

    // Specular
    float specularStrength = 0.5;
    vec3 viewDir = normalize(viewPos - FragPos);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
    vec3 specular = specularStrength * spec * lightColor;

    vec3 result = (ambient + diffuse + specular) * texture(diffuseTexture, TexCoord).rgb;
    FragColor = vec4(result, 1.0);
}
```

## Month 2: PBR Texturing Principles

### PBR Texture Maps
- **Albedo/Base Color**: Surface color without lighting/shadows (sRGB)
- **Metallic**: Metalness value (0.0 = dielectric, 1.0 = metal)
- **Roughness**: Surface smoothness (0.0 = smooth/mirror, 1.0 = rough/diffuse)
- **Normal**: Surface normal vectors for detail without geometry
- **AO/Ambient Occlusion**: Simulated shadowing in crevices
- **Height/Displacement**: Surface height for parallax mapping

### Material Properties
- **Dielectrics** (non-metals): Low metallic, varying roughness, colored albedo
- **Metals**: High metallic (0.9-1.0), albedo tinted with metal color, varying roughness
- **Energy Conservation**: Specular + diffuse = 1.0 for dielectrics

### Shader Example: PBR Metal-Roughness
```glsl
// GLSL PBR Fragment Shader
#version 330 core

in vec3 WorldPos;
in vec3 Normal;
in vec2 TexCoord;

uniform vec3 camPos;
uniform vec3 lightPositions[4];
uniform vec3 lightColors[4];

uniform sampler2D albedoMap;
uniform sampler2D metallicMap;
uniform sampler2D roughnessMap;
uniform sampler2D normalMap;
uniform sampler2D aoMap;

const float PI = 3.14159265359;

out vec4 FragColor;

// PBR Functions
vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness*roughness;
    float a2 = a*a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;

    float nom   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return nom / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float nom   = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return nom / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

void main()
{
    vec3 albedo = pow(texture(albedoMap, TexCoord).rgb, vec3(2.2));
    float metallic = texture(metallicMap, TexCoord).r;
    float roughness = texture(roughnessMap, TexCoord).r;
    float ao = texture(aoMap, TexCoord).r;

    vec3 N = normalize(Normal);
    vec3 V = normalize(camPos - WorldPos);

    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo, metallic);

    vec3 Lo = vec3(0.0);
    for(int i = 0; i < 4; ++i)
    {
        vec3 L = normalize(lightPositions[i] - WorldPos);
        vec3 H = normalize(V + L);
        float distance = length(lightPositions[i] - WorldPos);
        float attenuation = 1.0 / (distance * distance);
        vec3 radiance = lightColors[i] * attenuation;

        float NDF = DistributionGGX(N, H, roughness);
        float G = GeometrySmith(N, V, L, roughness);
        vec3 F = fresnelSchlick(max(dot(H, V), 0.0), F0);

        vec3 numerator = NDF * G * F;
        float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0);
        vec3 specular = numerator / max(denominator, 0.001);

        vec3 kS = F;
        vec3 kD = vec3(1.0) - kS;
        kD *= 1.0 - metallic;

        float NdotL = max(dot(N, L), 0.0);
        Lo += (kD * albedo / PI + specular) * radiance * NdotL;
    }

    vec3 ambient = vec3(0.03) * albedo * ao;
    vec3 color = ambient + Lo;

    color = color / (color + vec3(1.0));
    color = pow(color, vec3(1.0/2.2));

    FragColor = vec4(color, 1.0);
}
```

## Month 3: Advanced Lighting & Shadows

### Lighting Types
- **Directional Lights**: Sun/moon, no attenuation, parallel rays
- **Point Lights**: Omnidirectional, quadratic attenuation
- **Spot Lights**: Conical, angular attenuation
- **Area Lights**: Realistic soft shadows, expensive

### Shadow Mapping
- **Shadow Maps**: Depth buffer from light's perspective
- **PCF (Percentage Closer Filtering)**: Soft shadow edges
- **CSM (Cascaded Shadow Maps)**: Multiple shadow maps for different distances

### Shader Example: Shadow Mapping
```glsl
// GLSL Shadow Mapping
#version 330 core

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in vec4 FragPosLightSpace;

uniform sampler2D shadowMap;
uniform vec3 lightPos;
uniform vec3 viewPos;

float ShadowCalculation(vec4 fragPosLightSpace)
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;
    float closestDepth = texture(shadowMap, projCoords.xy).r;
    float currentDepth = projCoords.z;
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    for(int x = -1; x <= 1; ++x)
    {
        for(int y = -1; y <= 1; ++y)
        {
            float pcfDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * texelSize).r;
            shadow += currentDepth - 0.005 > pcfDepth ? 1.0 : 0.0;
        }
    }
    shadow /= 9.0;
    if(projCoords.z > 1.0) shadow = 0.0;
    return shadow;
}

void main()
{
    vec3 color = texture(diffuseTexture, TexCoord).rgb;
    vec3 normal = normalize(Normal);
    vec3 lightColor = vec3(0.3);

    vec3 ambient = 0.3 * color;

    vec3 lightDir = normalize(lightPos - FragPos);
    float diff = max(dot(lightDir, normal), 0.0);
    vec3 diffuse = diff * lightColor;

    vec3 viewDir = normalize(viewPos - FragPos);
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = 0.0;
    vec3 halfwayDir = normalize(lightDir + viewDir);
    spec = pow(max(dot(normal, halfwayDir), 0.0), 64.0);
    vec3 specular = spec * lightColor;

    float shadow = ShadowCalculation(FragPosLightSpace);
    vec3 lighting = (ambient + (1.0 - shadow) * (diffuse + specular)) * color;

    FragColor = vec4(lighting, 1.0);
}
```

## Month 4: Post-Processing & Effects

### Common Post-Processing Effects
- **Tone Mapping**: HDR to LDR conversion
- **Bloom**: Bright areas bleed light
- **Motion Blur**: Camera/object movement blur
- **Depth of Field**: Focus effects
- **SSAO (Screen Space Ambient Occlusion)**: Ambient shadowing

### Compute Shaders
- General purpose GPU computing
- Image processing, physics simulation
- Not limited to graphics pipeline

### Shader Example: Bloom Effect
```glsl
// GLSL Bloom Fragment Shader
#version 330 core

in vec2 TexCoord;

uniform sampler2D scene;
uniform sampler2D bloomBlur;
uniform float bloomStrength;
uniform float exposure;

out vec4 FragColor;

void main()
{
    const float gamma = 2.2;
    vec3 hdrColor = texture(scene, TexCoord).rgb;
    vec3 bloomColor = texture(bloomBlur, TexCoord).rgb;

    hdrColor += bloomColor * bloomStrength;

    vec3 result = vec3(1.0) - exp(-hdrColor * exposure);
    result = pow(result, vec3(1.0 / gamma));
    FragColor = vec4(result, 1.0);
}
```

## Month 5: Optimization & Performance

### GPU Optimization Techniques
- **LOD (Level of Detail)**: Reduce complexity at distance
- **Culling**: Frustum, occlusion, backface culling
- **Batching**: Combine draw calls
- **Shader Variants**: Compile-time conditionals
- **Texture Atlasing**: Combine textures

### Profiling Tools
- **GPU Profilers**: NVIDIA Nsight, AMD GPU PerfStudio
- **Frame Time Analysis**: Identify bottlenecks
- **Shader Performance**: ALU vs memory bound

### Real-Time Rendering Constraints
- **Frame Budget**: 16.67ms @ 60fps
- **Draw Calls**: Minimize state changes
- **Texture Memory**: VRAM limitations
- **Shader Complexity**: Balance quality vs performance

## Month 6: Advanced Topics & Pipeline Integration

### Advanced Rendering Techniques
- **Deferred Rendering**: Decouple geometry from lighting
- **Forward+ Rendering**: Tiled lighting for many lights
- **Ray Tracing**: Accurate reflections/refractions
- **Global Illumination**: Indirect lighting approximation

### Pipeline Integration
- **Asset Pipeline**: Texture compression, LOD generation
- **Shader Pipeline**: Cross-compilation, optimization
- **Version Control**: Large binary assets
- **Quality Assurance**: Automated testing

### Career Development
- **Certifications**: Unreal/Unity Certified Developer
- **Networking**: GDC, SIGGRAPH attendance
- **Specialization**: Choose focus area (lighting, materials, tools)
- **Continuous Learning**: Follow industry blogs, research papers

### Final Project: PBR Material Editor Tool
Create a tool that:
- Imports PBR texture sets
- Generates shader variants
- Provides real-time preview
- Exports optimized materials

## HLSL Equivalents

### PBR Pixel Shader (HLSL)
```hlsl
// HLSL PBR Pixel Shader
struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float3 WorldPos : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD;
};

Texture2D albedoTexture : register(t0);
Texture2D metallicTexture : register(t1);
Texture2D roughnessTexture : register(t2);
Texture2D normalTexture : register(t3);
Texture2D aoTexture : register(t4);

SamplerState samplerState : register(s0);

cbuffer MaterialConstants : register(b0)
{
    float3 albedoColor;
    float metallic;
    float roughness;
    float ao;
};

float4 main(PS_INPUT input) : SV_TARGET
{
    float3 albedo = pow(albedoTexture.Sample(samplerState, input.TexCoord).rgb, 2.2f);
    float metallicValue = metallicTexture.Sample(samplerState, input.TexCoord).r;
    float roughnessValue = roughnessTexture.Sample(samplerState, input.TexCoord).r;
    float aoValue = aoTexture.Sample(samplerState, input.TexCoord).r;

    // PBR calculations (simplified)
    float3 N = normalize(input.Normal);
    float3 V = normalize(cameraPos - input.WorldPos);

    float3 F0 = lerp(float3(0.04f, 0.04f, 0.04f), albedo, metallicValue);

    // Lighting calculations here...

    return float4(albedo, 1.0f);
}
```

## Key Takeaways

1. **PBR Fundamentals**: Albedo, metallic, roughness workflow
2. **Real-Time Constraints**: Performance vs quality balance
3. **Shader Programming**: GLSL/HLSL syntax and best practices
4. **Lighting Models**: Physically accurate illumination
5. **Optimization**: GPU profiling and bottleneck identification
6. **Pipeline Integration**: Asset management and workflow tools

This 6-month plan provides a solid foundation for Technical Artists working with modern real-time rendering pipelines.

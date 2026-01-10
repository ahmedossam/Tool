// Basic HLSL Pixel Shader
// Applies color or texture to pixels

// Input structure from vertex shader
struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 TexCoord : TEXCOORD;
};

// Texture and sampler
Texture2D texture1 : register(t0);
SamplerState sampler1 : register(s0);

// Constant buffer
cbuffer PixelConstantBuffer : register(b1)
{
    float3 color;    // Solid color
    bool useTexture; // Whether to use texture
};

float4 main(PS_INPUT input) : SV_TARGET
{
    if (useTexture)
    {
        // Sample texture
        return texture1.Sample(sampler1, input.TexCoord);
    }
    else
    {
        // Use solid color
        return float4(color, 1.0f);
    }
}

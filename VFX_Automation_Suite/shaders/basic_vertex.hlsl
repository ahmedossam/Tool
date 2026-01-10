// Basic HLSL Vertex Shader
// Transforms vertices and passes data to pixel shader

// Input structure
struct VS_INPUT
{
    float3 Pos : POSITION;      // Vertex position
    float2 TexCoord : TEXCOORD; // Texture coordinates
};

// Output structure
struct VS_OUTPUT
{
    float4 Pos : SV_POSITION;   // Transformed position
    float2 TexCoord : TEXCOORD; // Texture coordinates
};

// Constant buffer
cbuffer ConstantBuffer : register(b0)
{
    matrix model;      // Model matrix
    matrix view;       // View matrix
    matrix projection; // Projection matrix
};

VS_OUTPUT main(VS_INPUT input)
{
    VS_OUTPUT output;

    // Transform vertex position
    output.Pos = mul(float4(input.Pos, 1.0f), model);
    output.Pos = mul(output.Pos, view);
    output.Pos = mul(output.Pos, projection);

    // Pass texture coordinates
    output.TexCoord = input.TexCoord;

    return output;
}

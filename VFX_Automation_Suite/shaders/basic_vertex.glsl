// Basic GLSL Vertex Shader
// Transforms vertices and passes data to fragment shader

#version 330 core

// Input attributes
layout(location = 0) in vec3 aPos;      // Vertex position
layout(location = 1) in vec2 aTexCoord; // Texture coordinates

// Uniforms
uniform mat4 model;      // Model matrix
uniform mat4 view;       // View matrix
uniform mat4 projection; // Projection matrix

// Outputs to fragment shader
out vec2 TexCoord;

void main() {
    // Transform vertex position
    gl_Position = projection * view * model * vec4(aPos, 1.0);

    // Pass texture coordinates
    TexCoord = aTexCoord;
}

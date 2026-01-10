// Basic GLSL Fragment Shader
// Applies color or texture to pixels

#version 330 core

// Inputs from vertex shader
in vec2 TexCoord;

// Uniforms
uniform sampler2D texture1; // Texture sampler
uniform vec3 color;         // Solid color (if no texture)
uniform bool useTexture;    // Whether to use texture or solid color

// Output
out vec4 FragColor;

void main() {
    if(useTexture) {
        // Sample texture
        FragColor = texture(texture1, TexCoord);
    } else {
        // Use solid color
        FragColor = vec4(color, 1.0);
    }
}

# Basic Shader Examples

This directory contains basic examples of HLSL (High Level Shading Language) and GLSL (OpenGL Shading Language) shaders for understanding fundamental concepts.

## GLSL Shaders

GLSL is used in OpenGL applications.

### Vertex Shader (basic_vertex.glsl)
Transforms vertex positions and passes texture coordinates.

### Fragment Shader (basic_fragment.glsl)
Applies a simple color or texture to fragments.

## HLSL Shaders

HLSL is used in DirectX applications.

### Vertex Shader (basic_vertex.hlsl)
Transforms vertex positions and passes texture coordinates.

### Pixel Shader (basic_pixel.hlsl)
Applies a simple color or texture to pixels.

## Usage

These are basic examples. In a real application:

- GLSL shaders are compiled at runtime using OpenGL functions
- HLSL shaders are compiled using DirectX or tools like FXC/DXC

For VFX pipelines, shaders are often used in:
- Custom renderers
- Post-processing effects
- Material definitions
- GPU-accelerated tools

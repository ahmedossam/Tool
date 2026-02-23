# Unity URP — Tech Art Toolkit Setup Guide

## Requirements

- Unity **2022.3 LTS** or newer
- **Universal Render Pipeline** (URP) package
- **VFX Graph** package (`com.unity.visualeffectgraph`)
- **Shader Graph** package (`com.unity.shadergraph`)

---

## Installation

### Step 1 — Create a URP Project

```
Unity Hub → New Project → 3D (URP) template
Name: TechArtToolkit_Unity
```

### Step 2 — Copy Toolkit Files

```
Copy the following into your project's Assets/ folder:

Assets/
└── TechArtToolkit/
    ├── Editor/
    │   ├── Core/
    │   │   ├── ModuleBase.cs
    │   │   └── TechArtToolkitWindow.cs
    │   └── Modules/
    │       ├── ShaderProceduralLab.cs
    │       ├── VFXPerformanceTester.cs
    │       ├── LightingLookDevTool.cs
    │       ├── AssetOptimizationTool.cs
    │       └── ProceduralEnvironmentGenerator.cs
    └── Shaders/
        ├── ProceduralNoiseLab.shader
        └── SDFShapesLab.shader
```

### Step 3 — Open the Toolkit

```
Unity Menu Bar → Tools → Tech Art Toolkit
```

---

## Module Quick-Start

### Module 1: Shader Lab

1. Create a material from `ProceduralNoiseLab.shader`
2. Assign it to a sphere in the scene (optional — preview is built-in)
3. Open Shader Lab tab → adjust sliders → preview updates live

### Module 2: VFX Tester

1. Create two VFX Graph assets (see `BUILD_PLAN.md` Phase 3)
2. Open VFX Tester tab → assign both assets
3. Click "Spawn Optimized" → observe metrics
4. Click "Switch to Unoptimized" → compare delta

### Module 3: Lighting & LookDev

1. Create a scene with a Directional Light, Reflection Probe, and URP Volume
2. Open Lighting tab → click "Auto-Detect Scene Objects"
3. Click a preset (e.g., "Outdoor Day") → scene updates
4. Click "PBR Validation Grid" → spawn metallic/roughness matrix

### Module 4: Asset Optimizer

1. Import a mesh with LOD Group (or create one)
2. Open Asset Optimizer tab → drag mesh into Slot A
3. Click "Analyze Meshes" → view triangle counts and LOD breakdown
4. Drag optimized version into Slot B → compare

### Module 5: Procedural Env

1. Open Procedural Env tab
2. Select a biome preset (e.g., "Forest")
3. Assign rock prefabs (3 variations recommended)
4. Click "Generate Terrain" → terrain appears in scene
5. Click "Scatter Rocks" → rocks placed on terrain
6. Click "Clear All" to reset

---

## Folder Structure (in Unity project)

```
Assets/TechArtToolkit/
├── Editor/                    ← All editor-only scripts (not in builds)
│   ├── Core/
│   │   ├── ModuleBase.cs
│   │   └── TechArtToolkitWindow.cs
│   └── Modules/
│       ├── ShaderProceduralLab.cs
│       ├── VFXPerformanceTester.cs
│       ├── LightingLookDevTool.cs
│       ├── AssetOptimizationTool.cs
│       └── ProceduralEnvironmentGenerator.cs
├── Shaders/
│   ├── ProceduralNoiseLab.shader
│   └── SDFShapesLab.shader
├── Materials/
│   ├── MAT_ProceduralNoise.mat
│   └── MAT_SDFShapes.mat
├── Prefabs/
│   ├── PreviewMeshes/
│   │   └── PreviewSphere.prefab
│   ├── VFXEffects/
│   │   ├── OptimizedFireEffect.vfx
│   │   └── UnoptimizedFireEffect.vfx
│   └── Environment/
│       ├── Rock_A.prefab
│       ├── Rock_B.prefab
│       ├── Rock_C.prefab
│       ├── Tree_A.prefab
│       └── Tree_B.prefab
└── Textures/
    ├── HDRIs/
    │   ├── HDRI_Neutral.exr
    │   ├──

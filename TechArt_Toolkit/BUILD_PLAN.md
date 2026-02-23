# 🔨 Step-by-Step Build Plan

## Cross-Engine Technical Art Toolkit

---

## Prerequisites

### Unity Setup

```
1. Install Unity Hub → https://unity.com/download
2. Install Unity 2022.3 LTS (Long Term Support)
3. Create new project → select "3D (URP)" template
4. Package Manager → install:
   - Universal RP (already included in template)
   - VFX Graph (com.unity.visualeffectgraph)
   - Shader Graph (com.unity.shadergraph)
   - ProBuilder (optional, for test meshes)
```

### Unreal Setup

```
1. Install Epic Games Launcher → https://www.unrealengine.com
2. Install Unreal Engine 5.3 or newer
3. Create new project → Games → Blank → with Starter Content
4. Edit → Plugins → enable:
   - Niagara (should be enabled by default)
   - PCG (Procedural Content Generation)
   - Editor Scripting Utilities
   - (Optional) HoudiniEngine
```

---

## PHASE 1: Unity Core Framework

**Estimated Time: 2–3 hours**

### Step 1.1 — Project Folder Setup

```
In your Unity project Assets/ folder, create:
Assets/
├── TechArtToolkit/
│   ├── Editor/
│   │   ├── Core/
│   │   └── Modules/
│   ├── Shaders/
│   ├── Materials/
│   ├── Prefabs/
│   │   ├── PreviewMeshes/
│   │   └── VFXEffects/
│   └── Textures/
│       └── HDRIs/
```

### Step 1.2 — Create ModuleBase.cs

```csharp
// File: Assets/TechArtToolkit/Editor/Core/ModuleBase.cs
// Abstract base class — all 5 modules inherit from this
// Key methods: OnEnable(), OnDisable(), DrawGUI(), GetModuleName()
```

→ See: `Unity/Editor/Core/ModuleBase.cs`

### Step 1.3 — Create TechArtToolkitWindow.cs

```csharp
// File: Assets/TechArtToolkit/Editor/Core/TechArtToolkitWindow.cs
// Main EditorWindow — hosts all modules as tabs
// Menu item: Tools → Tech Art Toolkit
// Key: Uses GUILayout.Toolbar() for tab switching
//      Calls activeModule.DrawGUI() in OnGUI()
```

→ See: `Unity/Editor/Core/TechArtToolkitWindow.cs`

### Step 1.4 — Verify Core Works

```
1. Open Unity → Window → General → Console (clear errors)
2. Click: Tools → Tech Art Toolkit
3. Window should open with 5 empty tabs
4. No compile errors in Console
```

---

## PHASE 2: Module 1 — Shader & Procedural Lab

**Estimated Time: 4–6 hours**

### Step 2.1 — Create ProceduralNoiseLab.shader

```
1. Right-click Assets/TechArtToolkit/Shaders/
2. Create → Shader → Unlit Shader
3. Rename to "ProceduralNoiseLab"
4. Replace contents with the provided HLSL code
   → See: Unity/Shaders/ProceduralNoiseLab.shader
5. Key properties to expose:
   _NoiseScale, _NoiseOctaves, _UVTiling, _UVOffset
   _SDFRadius, _SDFSoftness, _TrigFrequency, _ColorA, _ColorB
```

### Step 2.2 — Create SDFShapesLab.shader

```
1. Create → Shader → Unlit Shader → "SDFShapesLab"
2. Implement SDF circle, box, ring, cross functions
   → See: Unity/Shaders/SDFShapesLab.shader
```

### Step 2.3 — Create ShaderProceduralLab.cs

```csharp
// File: Assets/TechArtToolkit/Editor/Modules/ShaderProceduralLab.cs
// Key implementation:
//   - PreviewRenderUtility for isolated preview
//   - Material with ProceduralNoiseLab shader
//   - IMGUI sliders → MaterialPropertyBlock updates
//   - Enum popup for noise type and SDF shape
//   - Gradient field for color remapping
```

→ See: `Unity/Editor/Modules/ShaderProceduralLab.cs`

### Step 2.4 — Test Module 1

```
1. Open Tech Art Toolkit → Tab "Shader Lab"
2. Move noise scale slider → preview updates
3. Switch noise type → different pattern appears
4. Change SDF shape → shape changes in preview
5. Animate UV offset → pattern scrolls
```

---

## PHASE 3: Module 2 — VFX Performance Tester

**Estimated Time: 3–4 hours**

### Step 3.1 — Create VFX Graph Assets

```
1. Right-click Assets/TechArtToolkit/Prefabs/VFXEffects/
2. Create → Visual Effects → Visual Effect Graph
3. Create TWO graphs:
   a. "OptimizedFireEffect"
      - Particle count: ~200
      - Use texture atlas (4x4 flipbook)
      - Enable GPU culling
      - Simple unlit shader
   b. "UnoptimizedFireEffect"
      - Particle count: ~2000
      - Individual textures (no atlas)
      - No culling
      - Complex lit shader with multiple samples
```

### Step 3.2 — Create VFXPerformanceTester.cs

```csharp
// File: Assets/TechArtToolkit/Editor/Modules/VFXPerformanceTester.cs
// Key implementation:
//   - ObjectField for VFX Graph asset references
//   - Spawn/destroy VisualEffect in scene
//   - EditorApplication.update callback for metrics
//   - VisualEffect.aliveParticleCount for particle count
//   - FrameTimingManager for GPU time
//   - Side-by-side comparison table
```

→ See: `Unity/Editor/Modules/VFXPerformanceTester.cs`

### Step 3.3 — Test Module 2

```
1. Open Tech Art Toolkit → Tab "VFX Tester"
2. Assign optimized and unoptimized VFX Graph assets
3. Click "Spawn Optimized" → effect appears in scene
4. Observe particle count and FPS metrics
5. Click "Switch to Unoptimized" → metrics change
6. Verify comparison table shows delta values
```

---

## PHASE 4: Module 3 — Lighting & LookDev Tool

**Estimated Time: 3–4 hours**

### Step 4.1 — Set Up Test Scene

```
1. Create new scene: "LookDevScene"
2. Add objects:
   - Directional Light (sun)
   - Reflection Probe (set to Realtime)
   - Post-Process Volume (set to Global)
   - Row of spheres with PBR materials:
     * Sphere_Metal_Rough0, Sphere_Metal_Rough025, etc.
     * Sphere_Dielectric_Rough0, etc.
3. Assign URP Lit materials with varying metallic/roughness
```

### Step 4.2 — Create LightingLookDevTool.cs

```csharp
// File: Assets/TechArtToolkit/Editor/Modules/LightingLookDevTool.cs
// Key implementation:
//   - ObjectField references for Light, ReflectionProbe, Volume
//   - Sliders for all lighting parameters
//   - Preset buttons (Neutral, Studio, Outdoor Day, Night)
//   - SerializedObject for modifying Volume components
//   - PBR validation: check albedo values in [0.04, 0.9] range
```

→ See: `Unity/Editor/Modules/LightingLookDevTool.cs`

### Step 4.3 — Test Module 3

```
1. Open Tech Art Toolkit → Tab "Lighting & LookDev"
2. Assign scene light and volume references
3. Move exposure slider → scene brightness changes
4. Click "Studio" preset → lighting changes to 3-point
5. Adjust bloom → post-processing updates
6. Verify PBR spheres look correct under each preset
```

---

## PHASE 5: Module 4 — Asset Optimization Tool

**Estimated Time: 3–4 hours**

### Step 5.1 — Prepare Test Assets

```
1. Import a test mesh (e.g., a rock or character)
2. Create LOD Group:
   - LOD0: original mesh (~5000 tris)
   - LOD1: 50% reduction (~2500 tris)
   - LOD2: 25% reduction (~1250 tris)
   - LOD3: 10% reduction (~500 tris)
3. Import test textures at different resolutions:
   - 4096x4096 (unoptimized)
   - 1024x1024 (optimized)
4. Set compression: DXT1 vs BC7 for comparison
```

### Step 5.2 — Create AssetOptimizationTool.cs

```csharp
// File: Assets/TechArtToolkit/Editor/Modules/AssetOptimizationTool.cs
// Key implementation:
//   - ObjectField for Mesh, Prefab, Texture2D
//   - LODGroup.GetLODs() → display per-LOD triangle count
//   - Mesh.triangles.Length / 3 for triangle count
//   - TextureImporter for compression settings
//   - EditorUtility.GetStorageMemorySize() for texture memory
//   - Two-column comparison layout
```

→ See: `Unity/Editor/Modules/AssetOptimizationTool.cs`

### Step 5.3 — Test Module 4

```
1. Open Tech Art Toolkit → Tab "Asset Optimizer"
2. Drag a mesh with LOD Group into the Mesh field
3. Verify LOD table shows correct triangle counts
4. Drag a texture → verify resolution and format display
5. Load optimized vs unoptimized → compare delta
```

---

## PHASE 6: Module 5 — Procedural Environment Generator

**Estimated Time: 4–5 hours**

### Step 6.1 — Prepare Prefabs

```
1. Create rock prefabs (3–5 variations)
2. Create tree prefabs (2–3 variations)
3. Create grass texture for terrain detail
4. Place all in Assets/TechArtToolkit/Prefabs/
```

### Step 6.2 — Create ProceduralEnvironmentGenerator.cs

```csharp
// File: Assets/TechArtToolkit/Editor/Modules/ProceduralEnvironmentGenerator.cs
// Key implementation:
//   - Terrain creation via TerrainData + Terrain.CreateTerrainGameObject()
//   - FBM noise for heightmap generation
//   - Poisson disk sampling for rock placement
//   - TerrainData.SetDetailLayer() for grass
//   - TreeInstance[] for tree placement
//   - Undo.RegisterCreatedObjectUndo() for undo support
```

→ See: `Unity/Editor/Modules/ProceduralEnvironmentGenerator.cs`

### Step 6.3 — Test Module 5

```
1. Open Tech Art Toolkit → Tab "Procedural Env"
2. Set noise scale to 50, octaves to 4
3. Click "Generate Terrain" → terrain appears in scene
4. Assign rock prefabs, set density to 0.5
5. Click "Scatter Rocks" → rocks appear on terrain
6. Change seed → different layout generated
7. Click "Clear" → all generated objects removed
8. Verify Ctrl+Z (Undo) works correctly
```

---

## PHASE 7: Unity Shaders

**Estimated Time: 2–3 hours**

### Step 7.1 — ProceduralNoiseLab.shader

```
Already created in Phase 2.
Additional testing:
1. Create material from shader
2. Assign to a sphere in scene
3. Verify all properties appear in Inspector
4. Test each noise type visually
```

### Step 7.2 — SDFShapesLab.shader

```
1. Create material from SDFShapesLab shader
2. Test circle SDF → smooth circle on UV plane
3. Test box SDF → rounded rectangle
4. Test ring SDF → hollow circle
5. Verify softness parameter creates smooth edges
```

---

## PHASE 8: Unreal Engine 5 Setup

**Estimated Time: 4–6 hours**

### Step 8.1 — Create Editor Utility Widget

```
1. Content Browser → right-click → Editor Utilities →
   Editor Utility Widget
2. Name it "EUW_TechArtToolkit"
3. Open Widget Blueprint
4. Root widget: Vertical Box
5. Add Tab Widget (or manual button-based tab system):
   - Button row: [Shader Lab] [VFX Tester] [Lighting] [Assets] [Procedural]
   - Switcher widget below buttons
   - Each switcher slot = one module widget
```

→ See: `Unreal/Blueprints/EUW_TechArtToolkit_Logic.md`

### Step 8.2 — Create Module Widgets

```
For each module, create a child Widget Blueprint:
1. WBP_ShaderLab
2. WBP_VFXTester
3. WBP_LightingTool
4. WBP_AssetOptimizer
5. WBP_ProceduralEnv

Each widget:
- Has a vertical layout with labeled sections
- Uses Sliders, Spinboxes, Buttons, Text blocks
- Binds to Blueprint event graph for logic
```

### Step 8.3 — Create Material: M_ProceduralNoise

```
1. Content Browser → Material → "M_ProceduralNoise"
2. Open Material Editor
3. Add Custom HLSL node with noise functions
   → See: Unreal/Materials/M_ProceduralNoise_Guide.md
4. Expose parameters as Material Parameters:
   - NoiseScale (Scalar)
   - NoiseOctaves (Scalar)
   - UVTiling (Vector)
   - ColorA, ColorB (Vector)
5. Create Material Instance: MI_ProceduralNoise
6. Reference MI in WBP_ShaderLab for runtime editing
```

### Step 8.4 — Create Niagara Systems

```
1. Content Browser → Niagara System → "NS_OptimizedEffect"
   - Emitter: GPU Sprite
   - Spawn Rate: 50/s
   - Lifetime: 2s
   - Use Texture Atlas (SubUV)
   - Enable Visibility Culling
   
2. Content Browser → Niagara System → "NS_UnoptimizedEffect"
   - Emitter: CPU Sprite
   - Spawn Rate: 500/s
   - Lifetime: 5s
   - No atlas (individual textures)
   - No culling
   - Complex material with multiple texture samples
```

### Step 8.5 — Set Up PCG Graph

```
1. Content Browser → PCG Graph → "PCG_EnvironmentScatter"
2. Add nodes:
   - Input: Landscape Actor
   - Surface Sampler (density parameter exposed)
   - Density Filter
   - Transform Points (scale variation)
   - Static Mesh Spawner (rock mesh list)
3. Expose parameters:
   - Seed (int32)
   - Density (float)
   - ScaleMin, ScaleMax (float)
4. Place PCG Actor in level, assign graph
5. Reference in WBP_ProceduralEnv
```

### Step 8.6 — Run and Test EUW

```
1. Content Browser → right-click EUW_TechArtToolkit
2. "Run Editor Utility Widget"
3. Test each tab:
   - Shader Lab: move sliders → material updates
   - VFX Tester: spawn effects → metrics display
   - Lighting: adjust exposure → scene changes
   - Asset Optimizer: select mesh → stats appear
   - Procedural Env: click Generate → PCG runs
```

---

## PHASE 9: Shared HLSL Library

**Estimated Time: 1–2 hours**

### Step 9.1 — ProceduralNoise.hlsl

```
Implement shared noise functions usable in both engines:
- hash2(), hash3()          → deterministic hash functions
- valueNoise()              → smooth value noise
- perlinNoise()             → gradient noise
- fbm()                     → fractal Brownian motion
- voronoiNoise()            → cellular/Worley noise
- simplexNoise()            → simplex noise approximation
→ See: Shared/Shaders/ProceduralNoise.hlsl
```

### Step 9.2 — SDFShapes.hlsl

```
Implement SDF functions:
- sdCircle(), sdBox(), sdRing(), sdCross()
- sdSmooth*() variants with softness
- opUnion(), opSubtract(), opIntersect()
- opSmoothUnion() (polynomial smooth min)
→ See: Shared/Shaders/SDFShapes.hlsl
```

### Step 9.3 — PBRUtils.hlsl

```
Implement PBR utility functions:
- FresnelSchlick()
- DistributionGGX()
- GeometrySmith()
- EnvBRDFApprox()
→ See: Shared/Shaders/PBRUtils.hlsl
```

---

## PHASE 10: Polish & Portfolio Prep

**Estimated Time: 2–3 hours**

### Step 10.1 — Unity Polish

```
1. Add tooltips to all controls (GUIContent with tooltip)
2. Add help boxes (EditorGUILayout.HelpBox) for each module
3. Add "Reset to Defaults" button per module
4. Add keyboard shortcuts (e.g., R = reset, G = generate)
5. Style with custom GUISkin or EditorStyles
6. Add module icons to tab labels
```

### Step 10.2 — Unreal Polish

```
1. Style EUW with custom colors (dark theme)
2. Add tooltips to all widgets (ToolTipText property)
3. Add "Reset" button per module
4. Add status text (e.g., "Generated 247 rocks")
5. Add icons to tab buttons (using Image widgets)
```

### Step 10.3 — Documentation

```
1. Add XML doc comments to all C# public methods
2. Add Blueprint comments/reroute nodes in UE5
3. Take screenshots of each module for portfolio
4. Record a 2–3 minute demo video
5. Write portfolio description (see PORTFOLIO_DESCRIPTION.md)
```

### Step 10.4 — Version Control

```
1. Initialize Git repository
2. Add .gitignore for Unity and Unreal:
   - Unity: Library/, Temp/, Logs/, UserSettings/
   - Unreal: Binaries/, Build/, DerivedDataCache/, Intermediate/, Saved/
3. Commit with clear messages per phase
4. Push to GitHub with README and screenshots
```

---

## Build Timeline Summary

| Phase | Task | Time |
|-------|------|------|
| 1 | Unity Core Framework | 2–3h |
| 2 | Module 1: Shader Lab | 4–6h |
| 3 | Module 2: VFX Tester | 3–4h |
| 4 | Module 3: Lighting | 3–4h |
| 5 | Module 4: Asset Optimizer | 3–4h |
| 6 | Module 5: Procedural Env | 4–5h |
| 7 | Unity Shaders | 2–3h |
| 8 | Unreal Engine 5 | 4–6h |
| 9 | Shared HLSL Library | 1–2h |
| 10 | Polish & Portfolio | 2–3h |
| **Total** | | **28–40h** |

---

## Common Issues & Solutions

### Unity

| Issue | Solution |
|-------|----------|
| `PreviewRenderUtility` not rendering | Call `BeginPreview()` before `Render()`, `EndPreview()` after |
| Material not updating in editor | Call `SceneView.RepaintAll()` after property changes |
| Terrain not visible | Ensure Terrain layer is not hidden in Scene view |
| VFX Graph not playing in editor | Set `VisualEffect.pause = false` and call `Play()` |
| Undo not working for generated objects | Use `Undo.RegisterCreatedObjectUndo()` immediately after `Instantiate()` |

### Unreal

| Issue | Solution |
|-------|----------|
| EUW not finding actors | Use `GetAllActorsOfClass()` instead of direct references |
| PCG not regenerating | Call `GenerateLocal(true)` with `bForce = true` |
| Niagara metrics not updating | Use `SetTimer` node at 0.1s interval, not Tick |
| Material parameter not found | Check exact parameter name matches (case-sensitive) |
| Widget not opening | Ensure EUW is in Content Browser, not in a map |

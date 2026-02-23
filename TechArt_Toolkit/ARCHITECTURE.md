# 🏗️ Technical Art Toolkit — Full Architecture

## Design Philosophy

The toolkit is built around three core principles:

1. **Modularity** — Each panel is a self-contained module with a clear interface
2. **Cross-Engine Parity** — Every feature has an equivalent in both Unity and Unreal
3. **Portfolio Clarity** — Each module maps directly to a demonstrable Technical Art skill

---

## System Architecture

### Unity Architecture

```
TechArtToolkitWindow (EditorWindow)
│
├── Tab Navigation System
│   ├── Tab 0 → ShaderProceduralLab      : ModuleBase
│   ├── Tab 1 → VFXPerformanceTester     : ModuleBase
│   ├── Tab 2 → LightingLookDevTool      : ModuleBase
│   ├── Tab 3 → AssetOptimizationTool    : ModuleBase
│   └── Tab 4 → ProceduralEnvGenerator   : ModuleBase
│
├── ModuleBase (Abstract)
│   ├── OnEnable()
│   ├── OnDisable()
│   ├── DrawGUI()          ← Called by host window
│   ├── DrawHeader()
│   └── DrawFooter()
│
└── Shared Utilities
    ├── PerformanceProfiler  ← GPU/CPU timing
    ├── AssetUtils           ← LOD, mesh, texture helpers
    └── ShaderUtils          ← Material property helpers
```

### Unreal Architecture

```
EUW_TechArtToolkit (Editor Utility Widget)
│
├── Tab Widget (WBP_TabContainer)
│   ├── Tab 0 → WBP_ShaderLab
│   ├── Tab 1 → WBP_VFXTester
│   ├── Tab 2 → WBP_LightingTool
│   ├── Tab 3 → WBP_AssetOptimizer
│   └── Tab 4 → WBP_ProceduralEnv
│
├── Blueprint Function Library (BFL_TechArtUtils)
│   ├── GetTriangleCount(StaticMesh)
│   ├── GetTextureMemory(Texture2D)
│   ├── SpawnNiagaraEffect(System, Location)
│   ├── GetCurrentFPS()
│   └── SetPostProcessSettings(Volume, Settings)
│
└── Data Assets
    ├── DA_ShaderParameters    ← Exposed material params
    ├── DA_VFXProfiles         ← Optimized/unoptimized configs
    └── DA_EnvironmentPresets  ← PCG parameter presets
```

---

## Module 1: Shader & Procedural Lab

### What It Does

A real-time shader parameter editor that lets you manipulate procedural shading
properties — noise frequency, UV tiling, mask blending, SDF shape parameters,
and trigonometric animation — and see results update live on a preview mesh.

### Why It Exists

Demonstrates **procedural shading fundamentals** — the ability to write and
control shaders mathematically without relying on texture artists. This is a
core Technical Artist skill: bridging art and code through shader math.

### Skills Proven

- HLSL/GLSL shader authoring
- Procedural noise (FBM, Voronoi, Perlin)
- UV manipulation and tiling
- SDF (Signed Distance Field) shape generation
- Trigonometric animation (sin/cos waves)
- Real-time material property control via editor tools

### Unity Implementation

```
ShaderProceduralLab.cs (ModuleBase)
│
├── Preview Setup
│   ├── Creates a PreviewRenderUtility scene
│   ├── Spawns a sphere/plane preview mesh
│   └── Assigns ProceduralNoiseLab.shader material
│
├── Parameter Controls (IMGUI sliders)
│   ├── Noise Type     : Enum (FBM, Voronoi, Perlin, Simplex)
│   ├── Noise Scale    : float [0.1 – 20.0]
│   ├── Noise Octaves  : int   [1 – 8]
│   ├── UV Tiling      : Vector2
│   ├── UV Offset      : Vector2 (animated)
│   ├── SDF Shape      : Enum (Circle, Box, Ring, Cross)
│   ├── SDF Radius     : float [0.0 – 1.0]
│   ├── SDF Softness   : float [0.0 – 0.5]
│   ├── Trig Frequency : float [0.1 – 10.0]
│   ├── Trig Amplitude : float [0.0 – 1.0]
│   └── Color Remap    : Gradient
│
└── Live Preview
    ├── Renders to Texture2D via PreviewRenderUtility
    └── Displays in EditorGUI.DrawPreviewTexture()
```

**Shader: ProceduralNoiseLab.shader**

```hlsl
// Key shader properties exposed to editor:
_NoiseScale, _NoiseOctaves, _UVTiling, _UVOffset
_SDFRadius, _SDFSoftness, _TrigFrequency, _TrigAmplitude
_ColorA, _ColorB, _MaskBlend
```

### Unreal Implementation

```
WBP_ShaderLab (Widget Blueprint)
│
├── Material Instance Dynamic (MID)
│   └── M_ProceduralNoise (parent material)
│       ├── Noise functions via Material Functions
│       ├── SDF shapes via custom HLSL node
│       └── UV animation via Time node
│
├── Widget Controls
│   ├── Sliders → SetScalarParameterValue() on MID
│   ├── Color pickers → SetVectorParameterValue() on MID
│   └── Enum dropdowns → SetStaticSwitchParameterValue()
│
└── Preview
    └── 3D Widget or SceneCaptureComponent2D → UMG Image
```

---

## Module 2: VFX Performance Tester

### What It Does

Spawns particle effects (VFX Graph / Niagara) and displays real-time performance
metrics: particle count, overdraw estimation, frame time, and FPS. Allows
switching between an "optimized" and "unoptimized" version of the same effect
to demonstrate the performance delta.

### Why It Exists

Demonstrates **VFX optimization skills** — understanding the GPU cost of
particle systems and knowing how to reduce overdraw, particle count, and
shader complexity without sacrificing visual quality.

### Skills Proven

- VFX Graph (Unity) / Niagara (Unreal) authoring
- GPU overdraw analysis
- Particle budget management
- Performance profiling (FPS, frame time, GPU time)
- Optimization techniques (culling, LOD, atlas sheets)

### Unity Implementation

```
VFXPerformanceTester.cs (ModuleBase)
│
├── Effect Management
│   ├── Spawns VisualEffect component in scene
│   ├── Holds references to two VFX Graph assets:
│   │   ├── optimizedEffect   (low particle count, atlas UVs)
│   │   └── unoptimizedEffect (high count, overdraw, no culling)
│   └── Toggle button swaps active VFX Graph asset
│
├── Metrics Display (updated every frame via EditorApplication.update)
│   ├── Particle Count  : VisualEffect.aliveParticleCount
│   ├── FPS             : 1f / Time.deltaTime
│   ├── Frame Time (ms) : Time.deltaTime * 1000f
│   ├── GPU Time        : Rendering.FrameTimingManager
│   └── Overdraw Est.   : Custom depth-based approximation
│
└── Comparison Panel
    ├── Side-by-side metric table (optimized vs unoptimized)
    └── Color-coded delta (green = better, red = worse)
```

### Unreal Implementation

```
WBP_VFXTester (Widget Blueprint)
│
├── Niagara Component Reference
│   ├── NS_OptimizedEffect   (low GPU cost profile)
│   └── NS_UnoptimizedEffect (high GPU cost profile)
│
├── Stat Commands via Execute Console Command
│   ├── "stat GPU"           → GPU frame time
│   ├── "stat Niagara"       → Particle counts
│   └── "stat SceneRendering"→ Draw calls, overdraw
│
└── Custom Blueprint Metrics
    ├── GetGameFramerate() → FPS display
    ├── Niagara.GetParticleCount() → particle count
    └── Timer-based polling every 0.1s → update UMG text
```

---

## Module 3: Lighting & LookDev Tool

### What It Does

A lighting control panel that adjusts HDRI/sky settings, exposure, shadow
quality, and post-processing parameters. Shows how different lighting setups
affect PBR materials — demonstrating the relationship between light, material,
and final rendered appearance.

### Why It Exists

Demonstrates **lighting and look development skills** — understanding how
physically-based lighting interacts with PBR materials, and how to set up
a consistent, art-directable lighting environment.

### Skills Proven

- HDRI/IBL (Image-Based Lighting) setup
- Exposure and tone mapping control
- Shadow quality and cascade configuration
- Post-processing (bloom, vignette, color grading)
- PBR material validation under different lighting conditions
- LookDev workflow (neutral grey, studio, outdoor presets)

### Unity Implementation

```
LightingLookDevTool.cs (ModuleBase)
│
├── Scene References
│   ├── DirectionalLight (sun)
│   ├── ReflectionProbe (IBL)
│   ├── Volume (URP Post-Processing)
│   └── HDRISky material
│
├── Controls
│   ├── HDRI Rotation     : float [0 – 360°]
│   ├── Sky Exposure      : float [-4 – 4 EV]
│   ├── Sun Intensity     : float [0 – 10]
│   ├── Sun Color         : Color
│   ├── Sun Angle         : Vector2 (azimuth, elevation)
│   ├── Shadow Distance   : float [10 – 500m]
│   ├── Shadow Cascades   : int [1 – 4]
│   ├── Shadow Resolution : Enum (256 – 4096)
│   ├── Bloom Intensity   : float [0 – 1]
│   ├── Bloom Threshold   : float [0 – 2]
│   ├── Vignette Intensity: float [0 – 1]
│   └── Color Temperature : float [1500 – 20000K]
│
├── Presets
│   ├── "Neutral Grey"    → flat grey HDRI, no post
│   ├── "Studio"          → 3-point lighting setup
│   ├── "Outdoor Day"     → sun + sky + bloom
│   └── "Outdoor Night"   → moon + stars + low exposure
│
└── PBR Validation Panel
    ├── Spawns a row of spheres: metallic 0→1, roughness 0→1
    └── Validates albedo values are within PBR-correct range
```

### Unreal Implementation

```
WBP_LightingTool (Widget Blueprint)
│
├── Actor References (set via eyedropper in EUW)
│   ├── BP_Sky_Sphere or SkyAtmosphere
│   ├── DirectionalLight Actor
│   ├── SkyLight Actor
│   └── PostProcessVolume Actor
│
├── Controls → Set Actor Properties via Blueprint
│   ├── SkyLight.SourceCubemap → HDRI asset picker
│   ├── DirectionalLight.Intensity → float slider
│   ├── PostProcessVolume.Settings.BloomIntensity
│   ├── PostProcessVolume.Settings.AutoExposureBias
│   └── PostProcessVolume.Settings.ColorGradingLUT
│
└── Preset System
    └── Data Table (DT_LightingPresets) → row per preset
        → Apply Row → Set all parameters at once
```

---

## Module 4: Asset Optimization Tool

### What It Does

Analyzes selected meshes and textures, displaying LOD levels, triangle counts,
texture resolutions, and estimated draw call costs. Allows side-by-side
comparison of an optimized vs non-optimized asset to demonstrate the impact
of proper asset budgeting.

### Why It Exists

Demonstrates **asset pipeline and optimization skills** — understanding how
mesh complexity, texture resolution, and draw call count affect runtime
performance, and knowing how to set appropriate budgets for different
asset types.

### Skills Proven

- LOD (Level of Detail) generation and validation
- Triangle/polygon budget management
- Texture compression and resolution analysis
- Draw call optimization (batching, instancing)
- Asset pipeline knowledge (import settings, compression)
- Performance budgeting for different target platforms

### Unity Implementation

```
AssetOptimizationTool.cs (ModuleBase)
│
├── Asset Selection
│   ├── Object field for Mesh/Prefab selection
│   └── Object field for Texture2D selection
│
├── Mesh Analysis
│   ├── LOD Group detection → list all LOD levels
│   ├── Per-LOD triangle count via Mesh.triangles.Length / 3
│   ├── Vertex count via Mesh.vertexCount
│   ├── Sub-mesh count (= draw calls per instance)
│   ├── Has Read/Write enabled? (memory cost warning)
│   └── Skinned mesh bone count (if applicable)
│
├── Texture Analysis
│   ├── Resolution (width × height)
│   ├── Format (DXT1/DXT5/BC7/ASTC etc.)
│   ├── Mip map count
│   ├── Memory size estimate (resolution × bpp / 8)
│   ├── sRGB flag (correct for albedo, wrong for normal/mask)
│   └── Compression quality setting
│
├── Draw Call Estimator
│   ├── Counts unique materials on selected prefab
│   ├── Estimates GPU instancing savings
│   └── Flags non-batching-friendly setups
│
└── Comparison Panel
    ├── "Before" column (unoptimized asset)
    ├── "After" column (optimized asset)
    └── Delta row with color coding
```

### Unreal Implementation

```
WBP_AssetOptimizer (Widget Blueprint)
│
├── Asset Picker (SoftObjectPath → resolve at runtime)
│   ├── StaticMesh picker
│   └── Texture2D picker
│
├── Mesh Stats via Blueprint nodes
│   ├── StaticMesh.GetNumLODs()
│   ├── StaticMesh.GetNumTriangles(LODIndex)
│   ├── StaticMesh.GetNumVertices(LODIndex)
│   └── StaticMesh.GetNumSections(LODIndex) → draw calls
│
├── Texture Stats
│   ├── Texture2D.GetSizeX() / GetSizeY()
│   ├── Texture2D.CompressionSettings
│   └── Texture2D.LODBias
│
└── LOD Visualizer
    └── Spawn mesh in viewport → set ForcedLOD → screenshot
```

---

## Module 5: Procedural Environment Generator

### What It Does

Generates a simple environment — terrain heightmap, rock placement, and
foliage distribution — using procedural parameters. Exposes controls for
density, scale variation, seed, and biome type. Demonstrates how procedural
systems reduce manual placement work while maintaining artistic control.

### Why It Exists

Demonstrates **procedural content generation skills** — understanding how
to use noise functions, scatter algorithms, and rule-based placement to
generate game-ready environments efficiently.

### Skills Proven

- Procedural terrain generation (noise-based heightmaps)
- Scatter/placement algorithms (Poisson disk, grid jitter)
- Foliage density and variation control
- PCG (Procedural Content Generation) framework usage
- Houdini Engine HDA integration (optional)
- Seed-based reproducibility

### Unity Implementation

```
ProceduralEnvironmentGenerator.cs (ModuleBase)
│
├── Terrain Generation
│   ├── Creates Unity Terrain object
│   ├── Generates heightmap via layered FBM noise
│   │   ├── Seed          : int
│   │   ├── Scale         : float [10 – 500]
│   │   ├── Octaves       : int [1 – 8]
│   │   ├── Persistence   : float [0.1 – 1.0]
│   │   └── Lacunarity    : float [1.0 – 4.0]
│   └── Applies heightmap to TerrainData.SetHeights()
│
├── Rock Scatter
│   ├── Poisson disk sampling for placement positions
│   ├── Parameters:
│   │   ├── Rock Prefab   : GameObject
│   │   ├── Density       : float [0 – 1]
│   │   ├── Min Radius    : float (Poisson exclusion)
│   │   ├── Scale Min/Max : Vector2
│   │   └── Slope Mask    : float (no rocks on steep slopes)
│   └── Places via GameObject.Instantiate in editor
│
├── Foliage Distribution
│   ├── Uses TerrainData.SetDetailLayer() for grass
│   ├── Uses TreeInstance[] for trees
│   └── Parameters:
│       ├── Grass Density : float [0 – 1]
│       ├── Tree Density  : float [0 – 1]
│       └── Height Range  : Vector2 (min/max height for placement)
│
└── Controls
    ├── [Generate] button → runs full generation
    ├── [Clear] button    → removes all generated objects
    └── [Randomize Seed]  → new random seed
```

### Unreal Implementation

```
WBP_ProceduralEnv (Widget Blueprint)
│
├── PCG Graph Reference
│   └── PCGComponent on a placed Actor
│       ├── PCG_TerrainScatter graph
│       │   ├── Surface Sampler node
│   │   ├── Density Filter node
│   │   └── Static Mesh Spawner node
│   └── Exposed PCG Parameters:
│       ├── Seed, Density, ScaleMin, ScaleMax
│       └── SlopeMask, HeightRange
│
├── Widget Controls → Set PCG Parameters via Blueprint
│   ├── PCGComponent.SetIntegerAttribute("Seed", value)
│   ├── PCGComponent.SetFloatAttribute("Density", value)
│   └── PCGComponent.GenerateLocal() → re-run graph
│
└── (Optional) Houdini HDA
    └── HoudiniAssetComponent → SetParameterFloat()
        → CookHoudiniAsset() → bake to static meshes
```

---

## Data Flow Diagram

```
User Input (Slider/Button)
        │
        ▼
Module DrawGUI() / Widget Event
        │
        ▼
Parameter Validation
        │
        ├──[Unity]──────────────────────────────────────────────┐
        │  MaterialPropertyBlock.SetFloat()                      │
        │  VisualEffect.SetFloat()                               │
        │  TerrainData.SetHeights()                              │
        │  Light.intensity = value                               │
        └──[Unreal]─────────────────────────────────────────────┘
           MID.SetScalarParameterValue()
           NiagaraComponent.SetVariableFloat()
           PCGComponent.SetFloatAttribute()
           DirectionalLight.SetIntensity()
                    │
                    ▼
            Scene/Viewport Update
                    │
                    ▼
            Metrics Refresh (if applicable)
                    │
                    ▼
            UI Feedback (labels, color coding)
```

---

## Performance Considerations

### Unity

| Concern | Solution |
|---|---|
| Editor repaints | Use `Repaint()` only when values change, not every frame |
| Preview rendering | Use `PreviewRenderUtility` — isolated from game scene |
| Terrain generation | Run on background thread, apply on main thread |
| VFX metrics polling | Use `EditorApplication.update` at 10Hz, not 60Hz |
| Asset analysis | Cache results, only re-analyze on asset change |

### Unreal

| Concern | Solution |
|---|---|
| EUW performance | Avoid Tick in widgets; use timers at low frequency |
| PCG generation | Use `GenerateLocal()` not `GenerateHierarchy()` for speed |
| Niagara metrics | Use `stat Niagara` console command, parse output |
| Material preview | Use SceneCaptureComponent2D at low resolution (256px) |
| Blueprint overhead | Move heavy logic to C++ Blueprint Function Library |

---

## Extension Points

The toolkit is designed to be extended:

### Adding a New Unity Module

1. Create `NewModule.cs` inheriting from `ModuleBase`
2. Override `DrawGUI()` with IMGUI code
3. Register in `TechArtToolkitWindow._modules` array
4. Add tab label to `_tabLabels` array

### Adding a New Unreal Module

1. Create new Widget Blueprint `WBP_NewModule`
2. Add a new tab to `EUW_TechArtToolkit` tab widget
3. Assign `WBP_NewModule` as tab content
4. Add any new Blueprint functions to `BFL_TechArtUtils`

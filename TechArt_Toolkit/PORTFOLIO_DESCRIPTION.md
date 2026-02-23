# 🎨 Portfolio Description

## Cross-Engine Technical Art Toolkit

---

## Short Description (1–2 sentences — for portfolio thumbnails / ArtStation)

> A modular Technical Art Toolkit built in both **Unity URP** and **Unreal Engine 5**,
> demonstrating shader programming, VFX optimization, lighting/look development,
> asset pipeline analysis, and procedural content generation — the core skill set
> of a professional Technical Artist.

---

## Medium Description (1 paragraph — for portfolio project page)

> This toolkit is a self-initiated portfolio project designed to demonstrate the
> breadth of skills required for a Junior Technical Artist role at a game studio.
> Built across two industry-standard engines — Unity (URP, C# EditorWindow) and
> Unreal Engine 5 (Blueprint EUW) — it consists of five modular editor tools:
> a **Shader & Procedural Lab** for real-time noise/SDF/UV control, a **VFX
> Performance Tester** that compares optimized vs unoptimized Niagara/VFX Graph
> effects with live metrics, a **Lighting & LookDev Tool** with preset environments
> and PBR validation, an **Asset Optimization Tool** that analyzes LODs, triangle
> counts, and texture memory, and a **Procedural Environment Generator** using
> FBM noise and Poisson disk sampling. The project includes a shared HLSL library
> (noise, SDF, PBR functions) usable in both engines, and is fully documented
> with architecture diagrams, build plans, and optimization notes.

---

## Full Description (for GitHub README / portfolio case study)

### Project: Cross-Engine Technical Art Toolkit

**Role:** Solo Technical Artist / Tools Programmer
**Engines:** Unity 2022 LTS (URP) + Unreal Engine 5.3
**Languages:** C# (Unity Editor), Blueprint (Unreal), HLSL
**Duration:** ~35 hours of development

---

### Why I Built This

As a Junior Technical Artist, I wanted to demonstrate that I understand not just
*how* to use game engines, but *why* certain technical decisions matter in a
production environment. This toolkit was designed to answer the question every
studio asks: **"Can you build tools that make artists more productive?"**

Rather than building a single tool in one engine, I chose to implement the same
five modules in both Unity and Unreal Engine 5 — proving cross-engine knowledge
and the ability to translate technical concepts between different paradigms.

---

### What It Demonstrates

#### 1. Shader Programming & Procedural Math

The **Shader & Procedural Lab** (Module 1) implements five noise algorithms
(FBM, Voronoi, Perlin, Simplex, Value), UV manipulation (tiling, offset, rotation),
trigonometric wave distortion, and eight SDF (Signed Distance Field) shape
primitives — all in raw HLSL, without relying on pre-built node graphs.

The shared `ProceduralNoise.hlsl` and `SDFShapes.hlsl` libraries are
cross-engine compatible and demonstrate understanding of GPU math fundamentals:
hash functions, gradient noise, fractal layering, and smooth boolean operations.

**Skills shown:** HLSL authoring, procedural math, UV manipulation, SDF geometry,
cross-engine shader compatibility.

---

#### 2. VFX Optimization & GPU Profiling

The **VFX Performance Tester** (Module 2) spawns two versions of the same
particle effect — one optimized (GPU sim, texture atlas, LOD culling, <200
particles) and one intentionally unoptimized (CPU sim, no atlas, no culling,
>2000 particles) — and displays real-time metrics: FPS, frame time, GPU time,
particle count, draw calls, and overdraw estimation.

The side-by-side comparison table with color-coded deltas makes the performance
impact immediately visible, demonstrating that I understand *why* optimization
decisions matter, not just *what* to do.

**Skills shown:** VFX Graph / Niagara authoring, GPU profiling, overdraw analysis,
particle budget management, optimization techniques (atlas UVs, GPU culling, LOD).

---

#### 3. Lighting & Look Development

The **Lighting & LookDev Tool** (Module 3) provides six preset lighting
environments (Neutral Grey, Studio 3-Point, Outdoor Day, Golden Hour, Night,
Overcast) with full control over sun direction, HDRI exposure, shadow quality,
and post-processing (bloom, vignette, color grading, exposure).

The PBR Validation Grid spawns a 5×5 matrix of spheres with varying metallic
and roughness values, allowing artists to verify that materials look correct
under different lighting conditions — a workflow used in professional LookDev
pipelines.

**Skills shown:** HDRI/IBL setup, PBR material validation, post-processing,
shadow quality tuning, lighting presets, LookDev workflow.

---

#### 4. Asset Pipeline & Optimization

The **Asset Optimization Tool** (Module 4) analyzes meshes and textures,
displaying LOD levels, per-LOD triangle counts, vertex counts, sub-mesh
(draw call) counts, texture resolution, GPU memory usage, compression format,
and mip map configuration.

The two-column comparison layout (Slot A = unoptimized, Slot B = optimized)
with a platform budget selector (PC High/Mid, Console, Mobile) demonstrates
understanding of real-world asset budgets and the ability to communicate
optimization impact clearly to artists and leads.

**Skills shown:** LOD generation, triangle budgets, texture compression,
draw call analysis, platform-specific budgeting, asset pipeline knowledge.

---

#### 5. Procedural Content Generation

The **Procedural Environment Generator** (Module 5) generates terrain
heightmaps using layered FBM noise, scatters rocks using Poisson disk sampling
(which prevents clustering while maintaining natural-looking distribution),
and distributes foliage using slope and height masking.

All generation is seed-based (reproducible), undoable (Ctrl+Z), and
parameterized with biome presets (Alpine, Desert, Forest, Tundra, Coastal).
The Unreal version uses the PCG (Procedural Content Generation) framework
introduced in UE5.2.

**Skills shown:** Procedural terrain generation, Poisson disk sampling,
scatter algorithms, slope/height masking, PCG framework, seed-based
reproducibility, Houdini Engine integration (optional).

---

### Technical Highlights

#### Cross-Engine HLSL Library

The `Shared/Shaders/` directory contains three HLSL libraries usable in both
engines without modification:

- **ProceduralNoise.hlsl** — 15+ noise functions including FBM, ridged FBM,
  domain-warped FBM, Voronoi, tileable noise, turbulence, marble, and wood patterns
- **SDFShapes.hlsl** — 15+ 2D SDF primitives, 6 3D primitives, 7 boolean
  operations, and rendering helpers (mask, outline, gradient, visualization)
- **PBRUtils.hlsl** — Full Cook-Torrance BRDF (D_GGX, G_Smith, F_Schlick),
  IBL approximations, tone mapping (ACES, Reinhard, Uncharted 2), and
  PBR validation utilities

#### Modular Architecture

The Unity implementation uses an abstract `ModuleBase` class with a clean
lifecycle (`OnEnable`, `OnDisable`, `DrawGUI`, `OnDestroy`), shared IMGUI
helper methods, and a tab-based `TechArtToolkitWindow` that manages module
switching with `EditorPrefs` persistence.

This architecture makes adding new modules trivial — inherit from `ModuleBase`,
implement `DrawGUI()`, and register in the window's module list.

#### Performance-Conscious Design

Every module was designed with editor performance in mind:

- Metrics polling at 10Hz (not every frame)
- `PreviewRenderUtility` for isolated shader preview (no scene contamination)
- `MaterialPropertyBlock` for shader parameter updates (no material instance creation)
- `Undo.RegisterCreatedObjectUndo()` for all scene modifications
- Lazy style initialization (styles created once, not every `OnGUI()` call)

---

### What I Learned

Building this toolkit taught me several things that aren't obvious from
tutorials or documentation:

1. **PreviewRenderUtility is finicky** — it requires careful `BeginPreview`/
   `EndPreview` pairing and doesn't work well with URP's render pipeline
   without explicit camera setup.

2. **MaterialPropertyBlock vs Material Instance** — using `MaterialPropertyBlock`
   for editor preview avoids creating garbage material instances, but requires
   the shader to not use `CBUFFER_START(UnityPerMaterial)` for those properties.

3. **Poisson disk sampling in editor** — running Poisson disk sampling on a
   500m terrain at 3m minimum radius generates ~27,000 candidate points.
   Capping at `_rockMaxCount` and using early-exit is essential.

4. **UE5 EUW limitations** — Editor Utility Widgets cannot use Tick efficiently.
   Timer-based polling at 0.1s intervals is the correct pattern for live metrics.

5. **Cross-engine shader parity** — Unity's `CBUFFER_START(UnityPerMaterial)`
   and Unreal's Custom HLSL node have different scoping rules. The shared HLSL
   library needed to avoid engine-specific macros to remain truly cross-engine.

---

### Future Improvements

- [ ] **Shader Graph visual version** of ProceduralNoiseLab for artists who
      prefer node-based workflows
- [ ] **Render pipeline comparison** — add HDRP support alongside URP
- [ ] **Automated LOD generation** — use `Mesh.SimplifyMesh()` to auto-generate
      LODs from LOD0 within the Asset Optimization Tool
- [ ] **VFX recording** — capture before/after screenshots automatically
      for portfolio documentation
- [ ] **Houdini Engine integration** — connect the Procedural Environment
      Generator to a Houdini HDA for more complex scatter rules
- [ ] **CI/CD pipeline** — automated shader compilation tests using Unity's
      `ShaderUtil.CompileShaderVariant()`

---

## Skills Matrix

| Skill | Evidence in This Project |
|-------|--------------------------|
| HLSL Shader Programming | ProceduralNoiseLab.shader, SDFShapesLab.shader, Shared HLSL library |
| Procedural Math | FBM, Voronoi, SDF shapes, Poisson disk, trig distortion |
| Unity Editor Tools (C#) | TechArtToolkitWindow, 5 ModuleBase subclasses, IMGUI |
| Unreal Blueprint | EUW with 5 module widgets, BFL_TechArtUtils |
| VFX Optimization | Optimized vs unoptimized Niagara/VFX Graph comparison |
| GPU Profiling | FrameTimingManager, stat commands, overdraw analysis |
| Lighting & LookDev | 6 presets, PBR validation grid, post-processing control |
| Asset Pipeline | LOD analysis, texture compression, draw call budgeting |
| PCG / Procedural Systems | FBM terrain, Poisson scatter, biome presets |
| PBR Theory | Cook-Torrance BRDF, F0, energy conservation, IBL |
| Cross-Engine Knowledge | Same 5 modules implemented in Unity AND Unreal |
| Tool Architecture | Modular design, abstract base class, lifecycle management |
| Performance Profiling | 10Hz polling, budget thresholds, color-coded metrics |
| Documentation | Architecture diagrams, build plans, inline code comments |

---

## Tags (for portfolio search)

`Technical Artist` `Unity` `Unreal Engine 5` `HLSL` `Shader Programming`
`VFX` `Niagara` `VFX Graph` `Procedural Generation` `PCG` `PBR` `LookDev`
`Asset Optimization` `LOD` `Editor Tools` `C#` `Blueprint` `Tool Development`
`Game Development` `Junior Technical Artist` `Portfolio`

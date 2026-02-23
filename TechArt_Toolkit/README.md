# 🎨 Cross-Engine Technical Art Toolkit

### Unity URP + Unreal Engine 5 | Portfolio Project

> A modular, cross-engine Technical Art Toolkit demonstrating core skills used in professional game studios:
> shader development, VFX optimization, lighting/look development, asset profiling, and procedural generation.

---

## 🏗️ Tool Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     TECH ART TOOLKIT  ─  Master Window                      │
│                  [Unity: EditorWindow]  [Unreal: EUW Widget]                │
├─────────────┬──────────────┬─────────────┬──────────────┬───────────────────┤
│  MODULE 1   │   MODULE 2   │  MODULE 3   │   MODULE 4   │     MODULE 5      │
│  Shader &   │     VFX      │  Lighting & │    Asset     │   Procedural      │
│ Procedural  │ Performance  │  LookDev    │ Optimization │   Environment     │
│    Lab      │   Tester     │    Tool     │    Tool      │    Generator      │
├─────────────┼──────────────┼─────────────┼──────────────┼───────────────────┤
│ ShaderGraph │ VFX Graph    │ HDRI/Sky    │ LOD Viewer   │ PCG / Houdini     │
│ HLSL Noise  │ Niagara      │ Exposure    │ Tri Count    │ Terrain Gen       │
│ UV Control  │ Particle Cnt │ Shadow Qual │ Tex Res      │ Rock Scatter      │
│ SDF Shapes  │ Overdraw     │ Post-Proc   │ Draw Calls   │ Foliage Density   │
│ Trig Funcs  │ FPS Display  │ PBR Preview │ Opt Compare  │ Param Controls    │
├─────────────┴──────────────┴─────────────┴──────────────┴───────────────────┤
│                          CORE SYSTEMS LAYER                                  │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐  │
│   │ ModuleBase   │  │ Perf Profiler│  │ Asset Utils  │  │ Shader Utils  │  │
│   │ (Abstract)   │  │ (GPU/CPU)    │  │ (LOD/Mesh)   │  │ (HLSL Lib)    │  │
│   └──────────────┘  └──────────────┘  └──────────────┘  └───────────────┘  │
├─────────────────────────────────────────────────────────────────────────────┤
│                         ENGINE INTEGRATION LAYER                             │
│   ┌─────────────────────────────┐  ┌──────────────────────────────────────┐ │
│   │         UNITY (URP)         │  │         UNREAL ENGINE 5              │ │
│   │  C# EditorWindow            │  │  Editor Utility Widget (EUW)         │ │
│   │  Shader Graph + HLSL        │  │  Material Editor + HLSL              │ │
│   │  VFX Graph                  │  │  Niagara System                      │ │
│   │  URP Render Pipeline        │  │  PCG Framework / Houdini HDA         │ │
│   │  UnityEditor API            │  │  Blueprint Visual Scripting          │ │
│   └─────────────────────────────┘  └──────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📦 Project Structure

```
TechArt_Toolkit/
│
├── README.md                          ← This file
├── ARCHITECTURE.md                    ← Deep-dive architecture docs
├── BUILD_PLAN.md                      ← Step-by-step build guide
├── PORTFOLIO_DESCRIPTION.md           ← Portfolio-ready description
├── TODO.md                            ← Build progress tracker
│
├── Unity/                             ← Unity URP Implementation
│   ├── README.md                      ← Unity setup guide
│   ├── Editor/
│   │   ├── Core/
│   │   │   ├── ModuleBase.cs          ← Abstract base for all modules
│   │   │   └── TechArtToolkitWindow.cs← Main EditorWindow (tab host)
│   │   └── Modules/
│   │       ├── ShaderProceduralLab.cs ← Module 1
│   │       ├── VFXPerformanceTester.cs← Module 2
│   │       ├── LightingLookDevTool.cs ← Module 3
│   │       ├── AssetOptimizationTool.cs← Module 4
│   │       └── ProceduralEnvironmentGenerator.cs ← Module 5
│   └── Shaders/
│       ├── ProceduralNoiseLab.shader  ← URP procedural noise shader
│       └── SDFShapesLab.shader        ← URP SDF shapes shader
│
├── Unreal/                            ← Unreal Engine 5 Implementation
│   ├── README.md                      ← Unreal setup guide
│   ├── Blueprints/
│   │   └── EUW_TechArtToolkit_Logic.md← EUW Blueprint logic guide
│   ├── Materials/
│   │   └── M_ProceduralNoise_Guide.md ← Material function breakdown
│   └── Niagara/
│       └── NS_VFXTester_Guide.md      ← Niagara system setup guide
│
└── Shared/                            ← Cross-engine HLSL library
    └── Shaders/
        ├── ProceduralNoise.hlsl       ← Noise functions (FBM, Voronoi, etc.)
        ├── SDFShapes.hlsl             ← SDF shape functions
        └── PBRUtils.hlsl             ← PBR utility functions
```

---

## 🧩 Module Overview

| # | Module | Skill Demonstrated | Unity | Unreal |
|---|--------|-------------------|-------|--------|
| 1 | Shader & Procedural Lab | Shader programming, procedural math | Shader Graph + HLSL | Material Editor + HLSL |
| 2 | VFX Performance Tester | VFX optimization, GPU profiling | VFX Graph | Niagara |
| 3 | Lighting & LookDev Tool | Lighting, PBR, post-processing | URP Volume | Sky/Atmosphere + PP |
| 4 | Asset Optimization Tool | Asset pipeline, LOD, draw calls | MeshUtility API | Static Mesh Editor |
| 5 | Procedural Environment Generator | PCG, procedural systems | Terrain + Scatter | PCG Framework |

---

## 🚀 Quick Start

### Unity

1. Create a Unity 2022 LTS+ project with **Universal Render Pipeline**
2. Copy the `Unity/Editor/` folder into your project's `Assets/` directory
3. Copy `Unity/Shaders/` into `Assets/Shaders/`
4. Open the toolkit: **Tools → Tech Art Toolkit** in the Unity menu bar

### Unreal Engine 5

1. Create a UE5.3+ project
2. Follow `Unreal/README.md` to set up the Editor Utility Widget
3. Follow `Unreal/Blueprints/EUW_TechArtToolkit_Logic.md` for Blueprint setup
4. Right-click the EUW asset → **Run Editor Utility Widget**

---

## 🎯 Skills Demonstrated

| Skill Area | Evidence |
|---|---|
| **Shader Programming** | HLSL noise, SDF, UV manipulation, trig functions |
| **VFX Optimization** | Particle count, overdraw analysis, FPS comparison |
| **Lighting & LookDev** | HDRI control, exposure, shadow quality, PBR validation |
| **Asset Pipeline** | LOD generation, triangle budgets, texture compression |
| **Procedural Systems** | PCG terrain, scatter, foliage density parameters |
| **Tool Development** | C# EditorWindow, Blueprint EUW, modular architecture |
| **Cross-Engine Knowledge** | Unity URP + Unreal Engine 5 implementations |
| **Performance Profiling** | GPU/CPU metrics, draw call analysis |

---

## 📋 Requirements

### Unity

- Unity 2022.3 LTS or newer
- Universal Render Pipeline (URP) package
- VFX Graph package
- Shader Graph package

### Unreal Engine 5

- Unreal Engine 5.3 or newer
- PCG (Procedural Content Generation) plugin enabled
- Niagara plugin enabled
- (Optional) Houdini Engine plugin for HDA support

---

## 📝 License

MIT License — Free to use as a portfolio reference or learning resource.

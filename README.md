# Technical Art & VFX Tools

A portfolio of three tools built for game development and VFX pipelines.

---

## Tools

### 1. [TechArt Toolkit](TechArt_Toolkit/README.md)
Cross-engine technical art toolkit for Unity and Unreal Engine.

| Module | Description |
|--------|-------------|
| Shader Lab | Procedural textures using noise & SDF math |
| VFX Performance Tester | Compare optimized vs unoptimized effects |
| Lighting Tool | Quick look-dev lighting setups |
| Asset Optimizer | Mesh analysis and performance reporting |
| Procedural Generator | Terrain generation and object scattering |

**Stack:** C# (Unity EditorWindow), Unreal Blueprints, HLSL

---

### 2. [VFX Automation Suite](VFX_Automation_Suite/README.md)
Python automation toolkit for repetitive VFX pipeline tasks.

| Tool | Description |
|------|-------------|
| Batch Processor | Bulk resize, convert, and process images |
| Format Converter | Convert image sequences to video |
| Asset Organizer | Sort and structure messy project folders |
| Quality Checker | Validate files before delivery |

**Stack:** Python, tkinter, PIL, ffmpeg

---

### 3. [Spine Auto-Rig](SpineAutoRig/README.md)
AI-powered 2D character rigging assistant.

```
Image (PSD/PNG) → MediaPipe pose detection → Smart bone mapping → Spine JSON
```

- Detects 33 body landmarks automatically
- Maps layer names to bones with 50+ synonyms (multi-language)
- One-click Spine project export

**Stack:** Python, MediaPipe, OpenCV, Pillow, tkinter

---

## Setup

Each tool has its own `requirements.txt` or is self-contained. See individual READMEs for setup instructions.

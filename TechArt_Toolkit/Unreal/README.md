# Unreal Engine 5 — Tech Art Toolkit Setup Guide

## Requirements

- Unreal Engine **5.3** or newer
- Plugins enabled:
  - **Niagara** (enabled by default)
  - **PCG** (Procedural Content Generation Framework)
  - **Editor Scripting Utilities**
  - **Python Editor Script Plugin** (optional, for automation)
  - **Houdini Engine** (optional, for HDA support)

---

## Installation

### Step 1 — Create a UE5 Project

```
Epic Games Launcher → Unreal Engine 5.3
New Project → Games → Blank
Settings: With Starter Content, Desktop/Console, Maximum Quality
Name: TechArtToolkit_UE5
```

### Step 2 — Enable Required Plugins

```
Edit → Plugins → search and enable:
  ✓ Niagara                        (FX category)
  ✓ PCG                            (Procedural Generation category)
  ✓ Editor Scripting Utilities     (Scripting category)
  ✓ Python Editor Script Plugin    (Scripting category, optional)
Restart Editor when prompted.
```

### Step 3 — Create Content Folder Structure

```
Content Browser → right-click → New Folder

Content/
└── TechArtToolkit/
    ├── Blueprints/
    │   ├── EUW_TechArtToolkit.uasset      ← Main EUW (create this)
    │   ├── WBP_ShaderLab.uasset
    │   ├── WBP_VFXTester.uasset
    │   ├── WBP_LightingTool.uasset
    │   ├── WBP_AssetOptimizer.uasset
    │   ├── WBP_ProceduralEnv.uasset
    │   └── BFL_TechArtUtils.uasset        ← Blueprint Function Library
    ├── Materials/
    │   ├── M_ProceduralNoise.uasset
    │   ├── MI_ProceduralNoise_Default.uasset
    │   ├── M_Fire_Optimized.uasset
    │   └── M_Fire_Unoptimized.uasset
    ├── Niagara/
    │   ├── NS_OptimizedEffect.uasset
    │   └── NS_UnoptimizedEffect.uasset
    ├── PCG/
    │   └── PCG_EnvironmentScatter.uasset
    ├── Meshes/
    │   ├── SM_Rock_A.uasset
    │   ├── SM_Rock_B.uasset
    │   ├── SM_Rock_C.uasset
    │   ├── SM_Tree_A.uasset
    │   └── SM_PreviewSphere.uasset
    └── Textures/
        ├── T_Fire_Atlas_4x4.uasset
        ├── T_Fire_Unoptimized_01.uasset
        ├── T_Fire_Unoptimized_02.uasset
        └── T_HDRI_Neutral.uasset
```

### Step 4 — Create the Main EUW

```
Content/TechArtToolkit/Blueprints/
→ Right-click → Editor Utilities → Editor Utility Widget
→ Name: EUW_TechArtToolkit
→ Open and follow: Blueprints/EUW_TechArtToolkit_Logic.md
```

### Step 5 — Run the Toolkit

```
Content Browser → right-click EUW_TechArtToolkit
→ "Run Editor Utility Widget"
The toolkit window opens as a dockable panel.
```

---

## Module Quick-Start

### Module 1: Shader Lab

1. Create `M_ProceduralNoise` following `Materials/M_ProceduralNoise_Guide.md`
2. Create `MI_ProceduralNoise_Default` (Material Instance)
3. Place a sphere in the level, name it `TAT_PreviewSphere`
4. Assign `MI_ProceduralNoise_Default` to the sphere
5. Open Shader Lab tab → adjust sliders → sphere updates live

### Module 2: VFX Tester

1. Create `NS_OptimizedEffect` and `NS_UnoptimizedEffect`
   following `Niagara/NS_VFXTester_Guide.md`
2. Open VFX Tester tab → assign both Niagara assets
3. Click "Spawn Optimized" → observe metrics
4. Click "Spawn Unoptimized" → compare performance delta

### Module 3: Lighting & LookDev

1. Place in level: Directional Light, Sky Light, Post Process Volume
2. Open Lighting tab → assign actor references (or use eyedropper)
3. Click a preset → lighting updates
4. Adjust sliders for fine control

### Module 4: Asset Optimizer

1. Import a Static Mesh with multiple LODs
2. Open Asset Optimizer tab → select mesh from asset picker
3. Click "Analyze Mesh" → view triangle counts per LOD
4. Select a texture → click "Analyze Texture" → view memory usage

### Module 5: Procedural Env

1. Place a **PCG Volume** actor in the level
2. Create `PCG_EnvironmentScatter` graph (see Build Plan Phase 8.5)
3. Assign graph to the PCG Volume
4. Open Procedural Env tab → assign PCG Volume reference
5. Click "Generate" → PCG runs and places rocks/foliage

---

## Blueprint Function Library Setup

Create `BFL_TechArtUtils` as a **Blueprint Function Library**:

```
Content Browser → right-click → Blueprint Class
Parent Class: Blueprint Function Library
Name: BFL_TechArtUtils
```

Add these functions (see `Blueprints/EUW_TechArtToolkit_Logic.md`):

- `GetGameFPS() → Float`
- `FormatMemoryBytes(Bytes: Integer) → String`
- `EvaluateBudget(Value, Good, Bad) → LinearColor`
- `GetStaticMeshTriCount(Mesh, LODIndex) → Integer`
- `SetStatusText(Widget, Message, Color)`

---

## PCG Graph Setup

### PCG_EnvironmentScatter

```
Content Browser → right-click → PCG → PCG Graph
Name: PCG_EnvironmentScatter

Add nodes:
1. [Get Landscape Data]          ← Input: Landscape actor
2. [Surface Sampler]
     Point Density: 0.5          ← Exposed as parameter "Density"
3. [Density Filter]
     Lower Bound: 0.0
     Upper Bound: 1.0
4. [Transform Points]
     Scale Min: (0.5, 0.5, 0.5)  ← Exposed as "ScaleMin"
     Scale Max: (2.0, 2.0, 2.0)  ← Exposed as "ScaleMax"
     Rotation: Random (0-360° Y)
5. [Static Mesh Spawner]
     Mesh Entries:
       - SM_Rock_A (weight: 0.4)
       - SM_Rock_B (weight: 0.35)
       - SM_Rock_C (weight: 0.25)

Expose as PCG Parameters:
  - Seed (int32, default: 42)
  - Density (float, default: 0.5)
  - ScaleMin (float, default: 0.5)
  - ScaleMax (float, default: 2.0)
```

---

## Performance Notes

| Issue | Solution |
|-------|----------|
| EUW Tick overhead | Use `Set Timer by Function Name` at 0.1s, never Tick |
| PCG generation lag | Use `Generate Local` not `Generate Hierarchy` |
| Material preview | SceneCapture2D at 256×256, not full resolution |
| Niagara metrics | Use `stat Niagara` console command |
| Actor references | Use `Get All Actors Of Class` with fallback |
| Widget rebuild | Cache widget references in variables, don't search every frame |

---

## Common Issues

| Issue | Solution |
|-------|----------|
| EUW not opening | Ensure it's in Content Browser (not in a map), right-click → Run |
| PCG not generating | Check PCG Volume bounds cover the landscape |
| Material not updating | Verify parameter names match exactly (case-sensitive) |
| Niagara not spawning | Check Niagara plugin is enabled, restart editor |
| Slider not responding | Bind to `OnValueChanged`, not `OnMouseEnter` |
| Stats not showing | Run `stat fps` first to enable the stats system |

---

## .gitignore for Unreal Projects

Add to your `.gitignore`:

```
Binaries/
Build/
DerivedDataCache/
Intermediate/
Saved/
*.VC.db
*.opensdf
*.opendb
*.sdf
*.suo
*.xcworkspace
*.xcodeproj
```

Track these folders:

```
Config/
Content/
Source/
*.uproject

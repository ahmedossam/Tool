# Unreal Engine 5 — Editor Utility Widget (EUW) Blueprint Logic Guide

## Tech Art Toolkit — All 5 Modules

---

## Overview

This document describes the complete Blueprint logic for the Unreal Engine 5
implementation of the Tech Art Toolkit as an **Editor Utility Widget (EUW)**.

Since Blueprint graphs cannot be stored as text files, this guide provides:

- Widget hierarchy (what to create in the Designer tab)
- Event graph logic (what nodes to connect in the Graph tab)
- Variable declarations
- Function implementations
- Step-by-step node connection instructions

---

## Root Widget Setup

### File: `EUW_TechArtToolkit`

**Type:** Editor Utility Widget (right-click Content Browser → Editor Utilities → Editor Utility Widget)

### Designer Tab — Root Hierarchy

```
[Canvas Panel]
  └── [Vertical Box] (Fill, padding 8px)
        ├── [Border] (dark blue bg #1A2A3A, padding 8px)
        │     └── [Horizontal Box]
        │           ├── [Image] (toolkit icon, 24x24)
        │           ├── [Text Block] "🎨 Tech Art Toolkit — Unity URP + Unreal Engine 5"
        │           └── [Spacer]
        │
        ├── [Horizontal Box] (Tab Buttons Row, height 36px)
        │     ├── [Button] "Shader Lab"    → OnClicked → ShowTab(0)
        │     ├── [Button] "VFX Tester"   → OnClicked → ShowTab(1)
        │     ├── [Button] "Lighting"     → OnClicked → ShowTab(2)
        │     ├── [Button] "Asset Opt."   → OnClicked → ShowTab(3)
        │     └── [Button] "Procedural"   → OnClicked → ShowTab(4)
        │
        ├── [Border] (tab underline, height 2px, color #3388FF)
        │
        └── [Widget Switcher] (variable: TabSwitcher)
              ├── Slot 0: [WBP_ShaderLab]
              ├── Slot 1: [WBP_VFXTester]
              ├── Slot 2: [WBP_LightingTool]
              ├── Slot 3: [WBP_AssetOptimizer]
              └── Slot 4: [WBP_ProceduralEnv]
```

### Variables

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ActiveTabIndex` | Integer | 0 | Currently active tab |
| `TabButtons` | Button Array | — | References to all 5 tab buttons |

### Event Graph: `ShowTab` (Custom Event)

```
Input: TabIndex (Integer)

[Custom Event: ShowTab]
    │
    ├── [Set Active Widget Index]
    │     Target: TabSwitcher
    │     Active Widget Index: TabIndex
    │
    ├── [Set Variable: ActiveTabIndex] = TabIndex
    │
    └── [For Each Loop] (TabButtons array)
          │
          ├── [Branch] (Array Index == TabIndex)
          │     True:  [Set Button Style] → Active style (blue tint)
          │     False: [Set Button Style] → Inactive style (grey)
          │
          └── [Loop Body continues...]
```

### Event Graph: `Construct`

```
[Event Construct]
    │
    ├── [ShowTab] (TabIndex = 0)  ← Start on Shader Lab
    │
    └── [Print String] "Tech Art Toolkit loaded"
```

---

## MODULE 1: WBP_ShaderLab

### Designer Tab

```
[Scroll Box]
  └── [Vertical Box]
        ├── [Text Block] "🌊 Shader & Procedural Lab" (header)
        ├── [Text Block] description (grey, small)
        │
        ├── [Border] "Noise Parameters"
        │     ├── [ComboBox] NoiseTypeCombo (FBM, Voronoi, Perlin, Value)
        │     ├── [Slider + Text] NoiseScaleSlider (0.1 – 20.0)
        │     ├── [Slider + Text] NoiseOctavesSlider (1 – 8)
        │     ├── [Slider + Text] PersistenceSlider (0.1 – 1.0)
        │     └── [Slider + Text] LacunaritySlider (1.0 – 4.0)
        │
        ├── [Border] "UV Controls"
        │     ├── [Slider + Text] UVTilingXSlider
        │     ├── [Slider + Text] UVTilingYSlider
        │     ├── [Slider + Text] UVOffsetXSlider
        │     ├── [Slider + Text] UVOffsetYSlider
        │     └── [CheckBox] AnimateUVCheck
        │
        ├── [Border] "SDF Shape"
        │     ├── [ComboBox] SDFShapeCombo (Circle, Box, Ring, Cross, None)
        │     ├── [Slider + Text] SDFRadiusSlider
        │     └── [Slider + Text] SDFSoftnessSlider
        │
        ├── [Border] "Color"
        │     ├── [ColorPicker] ColorAWidget
        │     └── [ColorPicker] ColorBWidget
        │
        └── [Border] "Preview"
              └── [Image] PreviewImage (256x256, updated via SceneCapture)
```

### Variables

| Name | Type | Default |
|------|------|---------|
| `PreviewMID` | Material Instance Dynamic | — |
| `NoiseScale` | Float | 3.0 |
| `NoiseOctaves` | Float | 4.0 |
| `SDFRadius` | Float | 0.35 |
| `ColorA` | Linear Color | (0.05, 0.05, 0.15, 1) |
| `ColorB` | Linear Color | (0.2, 0.6, 1.0, 1) |

### Event Graph: `InitializePreview`

```
[Event Construct]
    │
    ├── [Create Dynamic Material Instance]
    │     Parent: M_ProceduralNoise
    │     → Set Variable: PreviewMID
    │
    ├── [Get All Actors Of Class: StaticMeshActor]
    │     → Find actor named "TAT_PreviewSphere"
    │     → [Set Material] (index 0, PreviewMID)
    │
    └── [UpdateAllParameters]
```

### Event Graph: `OnNoiseScaleChanged` (bound to slider)

```
[On Value Changed (NoiseScaleSlider)]
    │
    ├── [Set Variable: NoiseScale] = NewValue
    │
    ├── [Set Scalar Parameter Value]
    │     Target: PreviewMID
    │     Parameter Name: "NoiseScale"
    │     Value: NoiseScale
    │
    └── [Update Preview Text] (display current value)
```

### Event Graph: `UpdateAllParameters` (Custom Function)

```
[Function: UpdateAllParameters]
    │
    ├── [Set Scalar Parameter Value] "NoiseScale"    = NoiseScale
    ├── [Set Scalar Parameter Value] "NoiseOctaves"  = NoiseOctaves
    ├── [Set Scalar Parameter Value] "SDFRadius"     = SDFRadius
    ├── [Set Vector Parameter Value] "ColorA"        = ColorA
    └── [Set Vector Parameter Value] "ColorB"        = ColorB
```

---

## MODULE 2: WBP_VFXTester

### Designer Tab

```
[Scroll Box]
  └── [Vertical Box]
        ├── [Text Block] "⚡ VFX Performance Tester" (header)
        │
        ├── [Border] "Effect Assets"
        │     ├── [Asset Picker] OptimizedNiagaraAsset
        │     └── [Asset Picker] UnoptimizedNiagaraAsset
        │
        ├── [Border] "Controls"
        │     ├── [Button] "▶ Spawn Optimized"    → SpawnOptimized
        │     ├── [Button] "▶ Spawn Unoptimized"  → SpawnUnoptimized
        │     └── [Button] "■ Stop All"           → StopAll
        │
        ├── [Border] "Live Metrics" (updated by timer)
        │     ├── [Text Block] FPSText
        │     ├── [Text Block] FrameTimeText
        │     ├── [Text Block] ParticleCountText
        │     └── [Text Block] DrawCallsText
        │
        └── [Border] "Comparison Table"
              ├── [Text Block] "Metric | Optimized | Unoptimized | Delta"
              ├── [Text Block] FPSCompareText
              ├── [Text Block] ParticleCompareText
              └── [Text Block] DrawCallCompareText
```

### Variables

| Name | Type | Default |
|------|------|---------|
| `SpawnedOptimized` | Actor Reference | — |
| `SpawnedUnoptimized` | Actor Reference | — |
| `OptimizedFPS` | Float | 0 |
| `UnoptimizedFPS` | Float | 0 |
| `MetricsTimer` | Timer Handle | — |

### Event Graph: `SpawnOptimized`

```
[Button OnClicked: SpawnOptimized]
    │
    ├── [Branch] SpawnedOptimized != None
    │     True: [Destroy Actor] SpawnedOptimized
    │
    ├── [Spawn Actor From Class: NiagaraActor]
    │     Location: (0, 0, 100)
    │     → Set Variable: SpawnedOptimized
    │
    ├── [Get Niagara Component] from SpawnedOptimized
    │     → [Set Asset] = OptimizedNiagaraAsset
    │     → [Activate]
    │
    └── [Start Metrics Timer]
```

### Event Graph: `StartMetricsTimer`

```
[Function: StartMetricsTimer]
    │
    └── [Set Timer by Function Name]
          Function Name: "UpdateMetrics"
          Time: 0.1  (10Hz polling)
          Looping: True
          → Set Variable: MetricsTimer
```

### Event Graph: `UpdateMetrics` (called by timer)

```
[Function: UpdateMetrics]
    │
    ├── [Get Game Frame Rate]
    │     → [Format Text] "FPS: {0}"
    │     → [Set Text] FPSText
    │
    ├── [Execute Console Command] "stat Niagara"
    │     (outputs to viewport — parse manually or use custom C++ node)
    │
    ├── [Get Stat Value: "STAT_NiagaraNumParticles"]
    │     → [Format Text] "Particles: {0}"
    │     → [Set Text] ParticleCountText
    │
    └── [Update Comparison Table]
```

### Event Graph: `UpdateComparisonTable`

```
[Function: UpdateComparisonTable]
    │
    ├── [Calculate Delta FPS]
    │     = OptimizedFPS - UnoptimizedFPS
    │     → Color code: positive = green, negative = red
    │
    └── [Set Text] FPSCompareText
          = "{OptimizedFPS} | {UnoptimizedFPS} | {Delta}"
```

---

## MODULE 3: WBP_LightingTool

### Designer Tab

```
[Scroll Box]
  └── [Vertical Box]
        ├── [Text Block] "☀ Lighting & LookDev Tool" (header)
        │
        ├── [Border] "Scene References"
        │     ├── [Actor Picker] DirectionalLightRef
        │     ├── [Actor Picker] SkyLightRef
        │     └── [Actor Picker] PostProcessVolumeRef
        │
        ├── [Border] "Lighting Presets"
        │     ├── [Button] "Neutral Grey"   → ApplyPreset(0)
        │     ├── [Button] "Studio"         → ApplyPreset(1)
        │     ├── [Button] "Outdoor Day"    → ApplyPreset(2)
        │     ├── [Button] "Golden Hour"    → ApplyPreset(3)
        │     └── [Button] "Night"          → ApplyPreset(4)
        │
        ├── [Border] "Sun Controls"
        │     ├── [Slider] SunIntensitySlider (0 – 10)
        │     ├── [ColorPicker] SunColorPicker
        │     ├── [Slider] SunAzimuthSlider (0 – 360°)
        │     └── [Slider] SunElevationSlider (-10 – 90°)
        │
        ├── [Border] "Post-Processing"
        │     ├── [Slider] ExposureBiasSlider (-4 – 4 EV)
        │     ├── [Slider] BloomIntensitySlider (0 – 2)
        │     ├── [Slider] ColorTempSlider (1500 – 20000K)
        │     └── [Slider] SaturationSlider (-100 – 100)
        │
        └── [Border] "Shadow Settings"
              ├── [Slider] ShadowDistanceSlider
              └── [ComboBox] ShadowCascadesCombo
```

### Variables

| Name | Type | Default |
|------|------|---------|
| `SunLightRef` | Directional Light | — |
| `PPVolumeRef` | Post Process Volume | — |
| `SkyLightRef` | Sky Light | — |
| `SunIntensity` | Float | 3.14 |
| `SunAzimuth` | Float | 135.0 |
| `SunElevation` | Float | 55.0 |

### Event Graph: `OnSunIntensityChanged`

```
[Slider OnValueChanged: SunIntensitySlider]
    │
    ├── [Set Variable: SunIntensity] = NewValue
    │
    ├── [Branch] SunLightRef != None
    │     True:
    │       [Get Light Component] from SunLightRef
    │       → [Set Intensity] = SunIntensity
    │
    └── [Update Label Text]
```

### Event Graph: `OnSunDirectionChanged`

```
[Slider OnValueChanged: SunAzimuthSlider OR SunElevationSlider]
    │
    ├── [Set Variable: SunAzimuth/SunElevation] = NewValue
    │
    ├── [Make Rotator]
    │     Pitch: -SunElevation
    │     Yaw:   SunAzimuth
    │     Roll:  0
    │
    └── [Set Actor Rotation] SunLightRef = MakeRotator result
```

### Event Graph: `ApplyPreset`

```
[Custom Event: ApplyPreset]
    Input: PresetIndex (Integer)
    │
    ├── [Switch on Int] PresetIndex
    │     Case 0 (Neutral Grey):
    │       SunIntensity=0, ColorTemp=6500, Bloom=0, Exposure=0
    │     Case 1 (Studio):
    │       SunIntensity=2.5, ColorTemp=5600, Bloom=0.2, Exposure=0
    │     Case 2 (Outdoor Day):
    │       SunIntensity=3.14, ColorTemp=6500, Bloom=0.5, Exposure=0
    │     Case 3 (Golden Hour):
    │       SunIntensity=1.5, ColorTemp=3200, Bloom=0.8, Exposure=-0.5
    │     Case 4 (Night):
    │       SunIntensity=0.05, ColorTemp=8000, Bloom=1.0, Exposure=-2
    │
    ├── [Apply Sun Settings]
    ├── [Apply Post Process Settings]
    └── [Update All Sliders to match new values]
```

### Event Graph: `ApplyPostProcessSettings`

```
[Function: ApplyPostProcessSettings]
    │
    ├── [Branch] PPVolumeRef != None
    │     True:
    │       [Get Post Process Settings] from PPVolumeRef
    │       → [Set Bloom Intensity]
    │       → [Set Auto Exposure Bias]
    │       → [Set White Balance Temp]
    │       → [Set Color Saturation]
    │       → [Set Post Process Settings] back to PPVolumeRef
    │
    └── [Print String] "Post-process settings applied"
```

---

## MODULE 4: WBP_AssetOptimizer

### Designer Tab

```
[Scroll Box]
  └── [Vertical Box]
        ├── [Text Block] "📐 Asset Optimization Tool" (header)
        │
        ├── [Border] "Platform Budget"
        │     └── [ComboBox] PlatformCombo (PC High, PC Mid, Console, Mobile)
        │
        ├── [Border] "Mesh Analysis"
        │     ├── [Asset Picker] MeshAssetPicker (Static Mesh)
        │     ├── [Button] "🔍 Analyze Mesh" → AnalyzeMesh
        │     ├── [Text Block] TriCountText
        │     ├── [Text Block] VertCountText
        │     ├── [Text Block] LODCountText
        │     └── [Text Block] DrawCallText
        │
        ├── [Border] "Texture Analysis"
        │     ├── [Asset Picker] TextureAssetPicker (Texture2D)
        │     ├── [Button] "🔍 Analyze Texture" → AnalyzeTexture
        │     ├── [Text Block] TexResolutionText
        │     ├── [Text Block] TexFormatText
        │     ├── [Text Block] TexMemoryText
        │     └── [Text Block] TexMipsText
        │
        └── [Border] "Optimization Tips"
              └── [Text Block] TipText (cycling tips)
```

### Event Graph: `AnalyzeMesh`

```
[Button OnClicked: AnalyzeMesh]
    │
    ├── [Get Selected Asset] from MeshAssetPicker
    │     → Cast to StaticMesh
    │
    ├── [Get Num LODs] from StaticMesh
    │     → [Format Text] "LODs: {0}"
    │     → [Set Text] LODCountText
    │
    ├── [Get Num Triangles] (LOD 0)
    │     → [Format Text] "Triangles: {0}"
    │     → [Set Text] TriCountText
    │
    ├── [Get Num Vertices] (LOD 0)
    │     → [Format Text] "Vertices: {0}"
    │     → [Set Text] VertCountText
    │
    ├── [Get Num Sections] (LOD 0)  ← sections = draw calls
    │     → [Format Text] "Draw Calls: {0}"
    │     → [Set Text] DrawCallText
    │
    └── [Compare Against Platform Budget]
          → Color code each metric (green/yellow/red)
```

### Event Graph: `AnalyzeTexture`

```
[Button OnClicked: AnalyzeTexture]
    │
    ├── [Get Selected Asset] from TextureAssetPicker
    │     → Cast to Texture2D
    │
    ├── [Get Size X] → [Format Text] "Resolution: {W}×{H}"
    │   [Get Size Y]
    │
    ├── [Get Pixel Format] → [Format Text] "Format: {0}"
    │
    ├── [Calculate Memory]
    │     = SizeX * SizeY * BytesPerPixel * (HasMips ? 1.33 : 1.0)
    │     → [Format Text] "GPU Memory: {0} MB"
    │
    └── [Get Num Mips] → [Format Text] "Mip Levels: {0}"
```

---

## MODULE 5: WBP_ProceduralEnv

### Designer Tab

```
[Scroll Box]
  └── [Vertical Box]
        ├── [Text Block] "🌍 Procedural Environment Generator" (header)
        │
        ├── [Border] "Biome Preset"
        │     ├── [ComboBox] BiomeCombo (Alpine, Desert, Forest, Tundra, Coastal)
        │     └── [SpinBox] SeedSpinBox (0 – 99999)
        │
        ├── [Border] "PCG Parameters"
        │     ├── [Slider] DensitySlider (0 – 1)
        │     ├── [Slider] ScaleMinSlider (0.1 – 3.0)
        │     ├── [Slider] ScaleMaxSlider (0.1 – 3.0)
        │     ├── [Slider] SlopeMaxSlider (0 – 90°)
        │     └── [Slider] HeightMinSlider / HeightMaxSlider
        │
        ├── [Border] "Generation Controls"
        │     ├── [Button] "✨ Generate" → GenerateEnvironment
        │     ├── [Button] "🎲 Randomize Seed" → RandomizeSeed
        │     └── [Button] "🗑 Clear" → ClearEnvironment
        │
        └── [Border] "Stats"
              ├── [Text Block] RockCountText
              ├── [Text Block] TreeCountText
              └── [Text Block] ActiveSeedText
```

### Variables

| Name | Type | Default |
|------|------|---------|
| `PCGActorRef` | Actor Reference | — |
| `PCGComponent` | PCG Component | — |
| `Seed` | Integer | 42 |
| `Density` | Float | 0.5 |
| `ScaleMin` | Float | 0.5 |
| `ScaleMax` | Float | 2.0 |

### Event Graph: `GenerateEnvironment`

```
[Button OnClicked: GenerateEnvironment]
    │
    ├── [Branch] PCGActorRef == None
    │     True:
    │       [Get All Actors Of Class: PCGVolume]
    │       → Set PCGActorRef to first result
    │
    ├── [Get Component by Class: PCGComponent]
    │     from PCGActorRef
    │     → Set PCGComponent
    │
    ├── [Set PCG Attribute: "Seed"]     = Seed
    ├── [Set PCG Attribute: "Density"]  = Density
    ├── [Set PCG Attribute: "ScaleMin"] = ScaleMin
    ├── [Set PCG Attribute: "ScaleMax"] = ScaleMax
    │
    ├── [Generate Local] PCGComponent (bForce = True)
    │
    ├── [Get Generated Points Count]
    │     → [Format Text] "Generated: {0} points"
    │     → [Set Text] RockCountText
    │
    └── [Print String] "Environment generated with seed {Seed}"
```

### Event Graph: `RandomizeSeed`

```
[Button OnClicked: RandomizeSeed]
    │
    ├── [Random Integer in Range] (0, 99999)
    │     → Set Variable: Seed
    │
    ├── [Set Value] SeedSpinBox = Seed
    │
    └── [GenerateEnvironment]  ← auto-regenerate with new seed
```

### Event Graph: `OnBiomeChanged`

```
[ComboBox OnSelectionChanged: BiomeCombo]
    │
    ├── [Switch on String] SelectedOption
    │     "Alpine":  Density=0.3, ScaleMin=0.5, ScaleMax=3.0, SlopeMax=60
    │     "Desert":  Density=0.15, ScaleMin=0.3, ScaleMax=1.5, SlopeMax=15
    │     "Forest":  Density=0.7, ScaleMin=0.5, ScaleMax=2.0, SlopeMax=30
    │     "Tundra":  Density=0.1, ScaleMin=0.2, ScaleMax=0.8, SlopeMax=10
    │     "Coastal": Density=0.25, ScaleMin=0.3, ScaleMax=1.5, SlopeMax=20
    │
    ├── [Update All Sliders to new values]
    │
    └── [GenerateEnvironment]
```

---

## Blueprint Function Library: BFL_TechArtUtils

Create a **Blueprint Function Library** asset named `BFL_TechArtUtils` for
reusable utility functions across all modules.

### Functions to Implement

```
GetGameFPS() → Float
    [Get World Delta Seconds]
    → 1.0 / DeltaSeconds
    → Return Float

FormatMemoryBytes(Bytes: Integer) → String
    [Branch] Bytes < 1024
        → Return "{Bytes} B"
    [Branch] Bytes < 1048576
        → Return "{Bytes/1024} KB"
    → Return "{Bytes/1048576} MB"

EvaluateBudget(Value: Float, GoodThreshold: Float, BadThreshold: Float) → LinearColor
    [Branch] Value <= GoodThreshold → Return Green (0.2, 0.8, 0.2, 1)
    [Branch] Value >= BadThreshold  → Return Red   (0.9, 0.2, 0.2, 1)
    → Return Yellow (0.9, 0.7, 0.1, 1)

GetStaticMeshTriCount(Mesh: StaticMesh, LODIndex: Integer) → Integer
    [Get Num Triangles] Mesh, LODIndex
    → Return Integer

SetStatusText(Widget: UserWidget, Message: String, Color: LinearColor)
    [Find Widget by Name: "StatusText"] in Widget
    → [Set Text] = Message
    → [Set Color and Opacity] = Color
```

---

## Performance Notes for EUW

| Issue | Solution |
|-------|----------|
| Widget Tick overhead | Never use Tick in EUW — use Set Timer by Function Name at 0.1s |
| PCG regeneration lag | Call GenerateLocal(bForce=true) on a background thread if possible |
| Actor references lost | Use GetAllActorsOfClass() with a fallback, not hard references |
| Slider spam | Use OnMouseButtonUp instead of OnValueChanged to avoid per-frame updates |
| Memory leaks | Clear timer handles in Destruct event |

### Destruct Event (cleanup)

```
[Event Destruct]
    │
    ├── [Clear Timer by Handle] MetricsTimer
    ├── [Destroy Actor] SpawnedOptimized (if valid)
    └── [Destroy Actor] SpawnedUnoptimized (if valid)

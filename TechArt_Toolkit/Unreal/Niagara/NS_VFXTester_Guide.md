# Unreal Engine 5 — Niagara VFX Tester Setup Guide

## Tech Art Toolkit — Module 2: VFX Performance Tester

---

## Overview

This guide describes how to create two Niagara Systems for the VFX Performance
Tester module: one **optimized** and one **unoptimized** version of the same
fire/smoke effect. The performance delta between them demonstrates VFX
optimization skills.

---

## NS_OptimizedEffect — Setup Guide

### Target Metrics

| Metric | Target |
|--------|--------|
| Particle Count | < 200 active |
| GPU Time | < 0.5ms |
| Draw Calls | 1 (single emitter) |
| Overdraw | Low (opaque core, transparent edges only) |
| CPU Cost | Minimal (GPU simulation) |

### Step 1: Create the Niagara System

```
Content Browser → Right-click → FX → Niagara System
Select: "New system from selected emitter(s)"
Choose: "Fountain" template as starting point
Rename: NS_OptimizedEffect
```

### Step 2: Emitter Settings

```
Emitter Name: Optimized_Fire_Emitter
Sim Target: GPU Sim  ← CRITICAL for performance
Fixed Bounds: Enable (set to 200x200x400 cm)
Scalability: Enable LOD
```

### Step 3: Emitter Properties

```
[Emitter Properties]
  Local Space: Enabled
  Determinism: Enabled (for reproducible metrics)
  Interpolated Spawning: Enabled
```

### Step 4: Spawn Rate Module

```
[Spawn Rate]
  Spawn Rate: 50 particles/second
  (Low spawn rate = low particle count)
```

### Step 5: Initialize Particle Module

```
[Initialize Particle]
  Lifetime: 2.0 – 3.0 seconds (random range)
  Mass: 1.0
  Sprite Size: 20 – 40 cm (random range)
  Sprite Rotation: 0 – 360° (random)
  Color: (1.0, 0.4, 0.1, 1.0) → (0.2, 0.1, 0.0, 0.0) over lifetime
```

### Step 6: Velocity Module

```
[Add Velocity]
  Velocity: (Random in cone)
  Cone Angle: 20°
  Speed: 50 – 100 cm/s
  
[Drag]
  Drag: 2.0 (slows particles over time)
```

### Step 7: SubUV Animation (Texture Atlas) ← KEY OPTIMIZATION

```
[Sub UV Animation]
  Sub Image Size: (4, 4)  ← 4x4 = 16 frames in one texture
  Sub UV Blend Enabled: True
  Animation Mode: Linear
  Start Frame: 0
  End Frame: 15
  Playback Mode: Loop
  
[Sprite Renderer]
  Material: M_Fire_Optimized
    ← Uses a 4x4 atlas texture (1 texture sample = 16 animation frames)
    ← SubUV coordinates driven by SubUV Animation module
  Sub UV Blending Enabled: True
  Alignment: Velocity Aligned
  Facing Mode: Camera Facing
```

### Step 8: Visibility Culling ← KEY OPTIMIZATION

```
[Significance Handler]
  Enable Significance: True
  
[Scalability]
  LOD 0 (High):   Spawn Rate = 50,  Max Particles = 200
  LOD 1 (Medium): Spawn Rate = 25,  Max Particles = 100
  LOD 2 (Low):    Spawn Rate = 10,  Max Particles = 50
  LOD 3 (Culled): Spawn Rate = 0    (fully culled at distance)
  
  Distance Thresholds: 500, 1000, 2000 cm
```

### Step 9: Bounds & Culling

```
[Fixed Bounds]
  Min: (-100, -100, 0)
  Max: (100, 100, 400)
  
[Visibility]
  Cull Distance: 2000 cm
  Cull Distance Slack: 200 cm
```

### Step 10: Material — M_Fire_Optimized

```
Create Material: M_Fire_Optimized
  Blend Mode: Translucent
  Shading Model: Unlit
  
  Nodes:
  [Texture Sample: T_Fire_Atlas_4x4]  ← Single 512x512 atlas
    → [Multiply] × Particle Color
    → [Multiply] × Particle Alpha
    → Emissive Color output
    
  [Particle SubUV] → UV input of Texture Sample
  [Particle Color] → Color multiply
  
  Translucency:
    Lighting Mode: Surface ForwardShading
    Disable Depth Test: False  ← Keep depth test ON for optimization
    Responsive AA: False
```

---

## NS_UnoptimizedEffect — Setup Guide

### Target Metrics (intentionally bad)

| Metric | Target |
|--------|--------|
| Particle Count | > 2000 active |
| GPU Time | > 3ms |
| Draw Calls | 3+ (multiple emitters, no batching) |
| Overdraw | High (large transparent particles, many layers) |
| CPU Cost | High (CPU simulation) |

### Step 1: Create the Niagara System

```
Content Browser → Right-click → FX → Niagara System
Rename: NS_UnoptimizedEffect
```

### Step 2: Emitter Settings (intentionally bad)

```
Emitter Name: Unoptimized_Fire_Emitter
Sim Target: CPU Sim  ← Expensive for large counts
Fixed Bounds: Disabled  ← Forces dynamic bounds recalculation
Scalability: Disabled   ← No LOD, always full quality
```

### Step 3: Spawn Rate (intentionally high)

```
[Spawn Rate]
  Spawn Rate: 500 particles/second  ← 10x the optimized version
```

### Step 4: Initialize Particle (large particles = more overdraw)

```
[Initialize Particle]
  Lifetime: 5.0 – 8.0 seconds  ← Long lifetime = more particles alive
  Sprite Size: 80 – 200 cm     ← Large particles = high overdraw
  Color: (1.0, 0.4, 0.1, 0.8) → (0.2, 0.1, 0.0, 0.0)
```

### Step 5: No SubUV — Individual Textures (intentionally bad)

```
[Sprite Renderer]
  Material: M_Fire_Unoptimized
    ← Uses 4 SEPARATE texture samples (no atlas)
    ← Each sample = separate texture fetch
    ← More ALU instructions, more memory bandwidth
  Sub UV Blending: Disabled  ← No atlas
```

### Step 6: No Culling (intentionally bad)

```
[Scalability]: Disabled
[Visibility Culling]: Disabled
[Fixed Bounds]: Disabled
```

### Step 7: Additional Emitters (more draw calls)

```
Add Emitter 2: Smoke_Emitter
  Spawn Rate: 200/s
  Large grey particles (100-300cm)
  CPU Sim, no culling
  
Add Emitter 3: Ember_Emitter
  Spawn Rate: 300/s
  Small bright particles
  CPU Sim, no culling
  
Total: 3 emitters × separate draw calls = 3+ draw calls
```

### Step 8: Material — M_Fire_Unoptimized

```
Create Material: M_Fire_Unoptimized
  Blend Mode: Translucent
  Shading Model: Default Lit  ← Lit = more expensive than Unlit
  
  Nodes:
  [Texture Sample: T_Fire_Frame1]   ← 4 separate 512x512 textures
  [Texture Sample: T_Fire_Frame2]   ← (no atlas = 4x memory bandwidth)
  [Texture Sample: T_Fire_Frame3]
  [Texture Sample: T_Fire_Frame4]
  
  [Lerp] between frames based on Time
  → Emissive Color
  → Normal Map (from T_Fire_Normal)  ← Extra texture sample
  
  Translucency:
    Lighting Mode: Volumetric NonDirectional  ← Expensive
    Responsive AA: True  ← Extra pass
    Disable Depth Test: True  ← Overdraw not culled by depth
```

---

## Performance Comparison Reference

| Metric | NS_Optimized | NS_Unoptimized | Delta |
|--------|-------------|----------------|-------|
| Particle Count | ~150 | ~2500 | 16.7x more |
| Emitter Count | 1 | 3 | 3x more |
| Texture Samples | 1 (atlas) | 5 (separate) | 5x more |
| Sim Target | GPU | CPU | GPU vs CPU |
| Culling | Yes (LOD) | No | — |
| Sprite Size | 20–40cm | 80–200cm | 5x larger |
| Lifetime | 2–3s | 5–8s | 2.5x longer |
| Draw Calls | 1 | 3+ | 3x more |
| Overdraw | Low | Very High | ~10x more |

---

## Measuring Performance in UE5

### Method 1: Stat Commands (in-editor)

```
Open the viewport → press ` (backtick) to open console

stat fps              → Shows FPS and frame time
stat unit             → Shows Game/Draw/GPU thread times
stat gpu              → Shows per-pass GPU timing
stat Niagara          → Shows Niagara-specific stats:
                          STAT_NiagaraNumParticles
                          STAT_NiagaraSimulateGPU
                          STAT_NiagaraSimulateCPU
stat SceneRendering   → Shows draw calls, triangles, overdraw
```

### Method 2: GPU Visualizer

```
Window → Developer Tools → GPU Visualizer (Ctrl+Shift+,)
→ Shows per-pass GPU timing as a bar chart
→ Look for "Translucency" pass — this is where particles render
→ Compare optimized vs unoptimized translucency cost
```

### Method 3: Unreal Insights

```
Tools → Run Unreal Insights
→ Start recording
→ Spawn optimized effect → record 5 seconds
→ Swap to unoptimized → record 5 seconds
→ Stop recording → analyze in Insights viewer
→ Compare CPU/GPU thread timelines
```

### Method 4: RenderDoc Integration

```
1. Install RenderDoc (https://renderdoc.org)
2. In UE5: Edit → Plugins → enable RenderDoc
3. Press F12 to capture a frame
4. In RenderDoc: Pipeline State → Texture Viewer
5. Enable "Overlay: Quad Overdraw" to visualize overdraw
   → Red = high overdraw (bad), Blue = low overdraw (good)
```

---

## WBP_VFXTester Integration

The widget reads metrics using these Blueprint nodes:

```
[Get Game Frame Rate]
  → Divide 1.0 / result = frame time in seconds
  → Multiply × 1000 = frame time in milliseconds

[Execute Console Command: "stat Niagara"]
  → Outputs to viewport (visual only)
  → For programmatic access, use C++ or custom plugin

[Get World → Get Timer Manager → Get Timer Elapsed]
  → Manual frame timing

[Niagara Component → Get Num Active Particles]
  → Direct particle count (if accessible via Blueprint)
  → May require C++ Blueprint Function Library wrapper
```

### Custom C++ Node for Particle Count (optional)

```cpp
// In a Blueprint Function Library .h:
UFUNCTION(BlueprintCallable, Category="TechArtToolkit|VFX")
static int32 GetNiagaraParticleCount(UNiagaraComponent* NiagaraComp);

// In .cpp:
int32 UTechArtBlueprintLibrary::GetNiagaraParticleCount(UNiagaraComponent* NiagaraComp)
{
    if (!NiagaraComp) return 0;
    
    int32 TotalParticles = 0;
    UNiagaraSystem* System = NiagaraComp->GetAsset();
    if (!System) return 0;
    
    // Iterate emitters and sum particle counts
    FNiagaraSystemInstance* Instance = NiagaraComp->GetSystemInstance();
    if (Instance)
    {
        for (auto& Emitter : Instance->GetEmitters())
        {
            TotalParticles += Emitter->GetNumParticles();
        }
    }
    return TotalParticles;
}
```

---

## Optimization Checklist

### ✅ Optimized Effect Checklist

- [x] GPU simulation (not CPU)
- [x] Texture atlas (SubUV) — 1 texture for all frames
- [x] Frustum culling enabled
- [x] LOD/Scalability configured
- [x] Fixed bounds set
- [x] Unlit material (not Lit)
- [x] Depth test enabled (no unnecessary overdraw)
- [x] Single emitter (1 draw call)
- [x] Particle count < 200
- [x] Short lifetime (2–3s)
- [x] Small-medium sprite size

### ❌ Unoptimized Effect Anti-Patterns

- [x] CPU simulation
- [x] Individual textures (no atlas)
- [x] No culling
- [x] No LOD
- [x] Dynamic bounds
- [x] Lit material
- [x] Depth test disabled
- [x] Multiple emitters (3+ draw calls)
- [x] Particle count > 2000
- [x] Long lifetime (5–8s)
- [x] Large sprite size (high overdraw)

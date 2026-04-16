# SpineAutoRig

AI-powered 2D character auto-rigging tool. Takes a character image, detects the body pose using MediaPipe, and generates a ready-to-animate Spine project.

## Pipeline

```
Input Image (PSD/PNG)
    → AI Pose Detection (MediaPipe, 33 landmarks)
    → Smart Layer Mapping (50+ name synonyms, multi-language)
    → Spine JSON Export
```

## Folder Structure

```
SpineAutoRig/
├── main.py                        # Entry point — run this
├── requirements.txt
├── core/
│   ├── spine_rig_core.py          # Core rigging logic & bone mapping
│   ├── auto_rig_tool.py           # MediaPipe pose detection & automation
│   ├── rig_spine_export.py        # Spine JSON export
│   └── smart_layer_map.py        # Intelligent layer-to-bone name mapping
├── ui/
│   └── spine_rig_ui.py            # GUI (tkinter)
├── integrations/
│   └── krita_export_layers.py    # Krita plugin: export layers as PNG sheets
└── utils/
    └── image_cut_tools.py         # Image slicing utilities
```

## Setup

```bash
pip install -r requirements.txt
python main.py
```

## How It Works

1. **Load image** — PSD (with layers) or flat PNG
2. **Detect pose** — MediaPipe finds 33 body landmarks automatically
3. **Map layers** — Smart name mapping handles English, Spanish, and custom naming conventions
   - Example: `"brazo izq"` → `upper_arm_L`
4. **Export** — One-click Spine 2D JSON output, ready to animate

## Krita Integration

Use `integrations/krita_export_layers.py` as a Krita script to batch-export all layers as individual PNGs before running the auto-rig.

## Requirements

- Python 3.10+
- MediaPipe, OpenCV, Pillow (see `requirements.txt`)
- Spine 2D (for importing the generated project)

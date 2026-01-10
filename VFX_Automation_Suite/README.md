# VFX Automation Suite

A comprehensive automation toolkit for Technical VFX Artists, designed to streamline repetitive workflows across all major VFX software and pipelines.

## 🎯 Features

### 🔧 Core Tools

1. **Batch Processor**
   - Resize, convert, compress images
   - Add watermarks
   - Crop, rotate, flip operations
   - Parallel processing support
   - Custom operation chains

2. **Format Converter**
   - Image sequences ↔ Video
   - Video format conversion
   - Audio format conversion
   - 3D format conversion (FBX, OBJ, Alembic, USD)
   - Contact sheet generation
   - Sprite sheet creation
   - GIF creation from video

3. **Asset Organizer**
   - Organize by type, date, or project
   - Find and remove duplicates
   - Cleanup temp files
   - Create searchable asset database
   - Rename sequences with proper numbering
   - Generate thumbnails

4. **Quality Checker**
   - Check for missing frames
   - Validate naming conventions
   - Verify resolution requirements
   - Check file sizes
   - Detect corrupted files
   - Find duplicate filenames
   - Generate detailed QC reports

## 🚀 Installation

### Prerequisites

```bash
# Python 3.8 or higher required
python --version

# Install required packages
pip install -r requirements.txt
```

### Required Dependencies

```bash
pip install PySide6 Pillow numpy opencv-python psd-tools
```

### Optional Dependencies

For extended functionality:

```bash
# For pose detection in images
pip install mediapipe

# FFmpeg (for video conversion)
# Download from: https://ffmpeg.org/download.html
# Add to system PATH

# Blender (for 3D format conversion)
# Download from: https://www.blender.org/download/
```

## 📖 Usage

### GUI Application

Launch the graphical interface:

```bash
python VFX_Automation_Suite/main_gui.py
```

### Command Line / Scripting

Use tools programmatically:

```python
from pathlib import Path
from tools.batch_processor import BatchProcessor
from tools.format_converter import FormatConverter
from tools.asset_organizer import AssetOrganizer
from tools.quality_checker import QualityChecker

# Example: Batch resize images
processor = BatchProcessor()
result = processor.execute(
    files=[Path("image1.png"), Path("image2.png")],
    operations=[
        {"type": "resize", "width": 1920, "height": 1080},
        {"type": "convert", "format": "jpg", "quality": 90}
    ],
    output_dir="./output",
    parallel=True
)

# Example: Convert image sequence to video
converter = FormatConverter()
result = converter.execute(
    input_path="./frames",
    output_path="./output/video.mp4",
    conversion_type="image_sequence_to_video",
    options={"framerate": 24, "quality": 23}
)

# Example: Organize assets by type
organizer = AssetOrganizer()
result = organizer.execute(
    source_dir="./messy_folder",
    target_dir="./organized",
    mode="by_type",
    options={"copy": True}
)

# Example: Run quality checks
checker = QualityChecker()
result = checker.execute(
    source_dir="./assets",
    checks=["missing_frames", "resolution", "naming_convention"],
    output_report="./qc_report.json"
)
```

## 🔌 Plugin System

The suite uses an extensible plugin architecture. Create custom tools:

```python
from core.plugin_system import PluginBase

class MyCustomTool(PluginBase):
    name = "My Custom Tool"
    description = "Does something awesome"
    version = "1.0.0"
    category = "Custom"
    
    def execute(self, **kwargs):
        # Your tool logic here
        return {"status": "success"}
    
    def validate_inputs(self, **kwargs):
        # Validate inputs
        return True

# Register plugin
from core.plugin_system import get_plugin_manager
plugin_manager = get_plugin_manager()
plugin_manager.register_plugin(MyCustomTool)
```

## ⚙️ Configuration

Configuration is stored in `~/.vfx_automation/config.json`

### Key Settings

```json
{
  "paths": {
    "temp_dir": "~/VFX_Temp",
    "export_dir": "~/VFX_Export",
    "backup_dir": "~/VFX_Backup"
  },
  "software": {
    "ffmpeg": "ffmpeg",
    "blender": "blender",
    "imagemagick": "magick"
  },
  "quality_checks": {
    "min_resolution": [1920, 1080],
    "max_file_size_mb": 500
  }
}
```

## 📁 Project Structure

```
VFX_Automation_Suite/
├── core/
│   ├── config.py           # Configuration management
│   ├── logger.py           # Logging system
│   └── plugin_system.py    # Plugin architecture
├── tools/
│   ├── batch_processor.py  # Batch file processing
│   ├── format_converter.py # Format conversion
│   ├── asset_organizer.py  # Asset management
│   └── quality_checker.py  # Quality control
├── main_gui.py             # GUI application
├── requirements.txt        # Python dependencies
└── README.md              # This file
```

## 🎨 Supported Formats

### Images
- PNG, JPG, JPEG, TIF, TIFF, EXR, DPX, TGA

### Videos
- MP4, MOV, AVI, MKV, WEBM

### 3D Models
- FBX, OBJ, Alembic (.abc), USD (.usd, .usda, .usdc)
- Blender (.blend), Maya (.ma, .mb)

### Audio
- WAV, MP3, AAC, FLAC, OGG

### Project Files
- After Effects (.aep)
- Premiere Pro (.prproj)
- Nuke (.nk)
- Houdini (.hip)

## 🔥 Common Workflows

### Workflow 1: Prepare Assets for Delivery

```python
# 1. Organize files by type
organizer.execute(
    source_dir="./raw_assets",
    mode="by_type"
)

# 2. Run quality checks
checker.execute(
    source_dir="./organized",
    checks=["all"],
    output_report="./qc_report.json"
)

# 3. Batch process images
processor.execute(
    files=image_files,
    operations=[
        {"type": "resize", "width": 1920, "height": 1080},
        {"type": "watermark", "text": "CONFIDENTIAL"}
    ]
)
```

### Workflow 2: Convert Renders to Deliverables

```python
# 1. Convert image sequence to video
converter.execute(
    input_path="./renders/sequence",
    output_path="./delivery/final.mp4",
    conversion_type="image_sequence_to_video",
    options={"framerate": 24, "codec": "libx264", "quality": 18}
)

# 2. Create contact sheet for review
converter.execute(
    input_path="./renders/sequence",
    output_path="./delivery/contact_sheet.jpg",
    conversion_type="contact_sheet",
    options={"columns": 5, "thumb_size": 200}
)
```

### Workflow 3: Asset Database Creation

```python
# Create searchable database with thumbnails
organizer.execute(
    source_dir="./asset_library",
    target_dir="./database",
    mode="create_database",
    options={
        "include_thumbnails": True,
        "thumbnail_size": 200
    }
)
```

## 🐛 Troubleshooting

### FFmpeg not found
```bash
# Windows: Download from ffmpeg.org and add to PATH
# Mac: brew install ffmpeg
# Linux: sudo apt-get install ffmpeg
```

### Blender not found
```bash
# Add Blender to system PATH or update config.json:
{
  "software": {
    "blender": "/path/to/blender"
  }
}
```

### Import errors
```bash
# Reinstall dependencies
pip install --upgrade -r requirements.txt
```

## 🤝 Contributing

This is a modular system designed for extension. To add new tools:

1. Create a new plugin in `tools/`
2. Inherit from `PluginBase`
3. Implement `execute()` method
4. Register in `main_gui.py` or use programmatically

## 📝 License

MIT License - Feel free to use and modify for your projects

## 🎓 Tips for VFX Artists

1. **Batch Processing**: Use parallel processing for large file sets
2. **Quality Checks**: Run before final delivery to catch issues
3. **Asset Organization**: Maintain consistent naming conventions
4. **Backups**: Always work on copies, not originals
5. **Automation**: Chain tools together for complete workflows

## 🔮 Future Enhancements

- [ ] Render queue manager
- [ ] Project template generator
- [ ] Backup and sync system
- [ ] Notification system (email, Discord, Slack)
- [ ] Cloud storage integration
- [ ] Machine learning-based asset tagging
- [ ] Automated documentation generation
- [ ] Integration with production tracking systems

## 📧 Support

For issues, questions, or feature requests, please create an issue in the repository.

---

**Built for VFX Artists, by VFX Artists** 🎬✨

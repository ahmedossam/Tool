"""
VFX Automation Suite - Format Converter
Convert between various file formats (images, videos, 3D, audio)
"""

import subprocess
import shutil
from pathlib import Path
from typing import Dict, Any, List, Optional
import json

from core.plugin_system import PluginBase
from core.logger import get_logger
from core.config import get_config

logger = get_logger()
config = get_config()


class FormatConverter(PluginBase):
    """Universal format converter for VFX workflows"""

    name = "Format Converter"
    description = "Convert between image sequences, videos, 3D formats, and audio"
    version = "1.0.0"
    category = "File Processing"

    def __init__(self):
        super().__init__()
        self.ffmpeg = config.get('software.ffmpeg', 'ffmpeg')
        self.imagemagick = config.get('software.imagemagick', 'magick')

    def execute(self, **kwargs) -> Dict[str, Any]:
        """
        Execute format conversion

        Args:
            input_path: Input file or directory
            output_path: Output file or directory
            conversion_type: Type of conversion
            options: Additional conversion options
        """
        input_path = Path(kwargs.get('input_path'))
        output_path = Path(kwargs.get('output_path'))
        conversion_type = kwargs.get('conversion_type')
        options = kwargs.get('options', {})

        if not input_path.exists():
            raise FileNotFoundError(f"Input not found: {input_path}")

        logger.section(f"Format Conversion: {conversion_type}")
        logger.info(f"Input: {input_path}")
        logger.info(f"Output: {output_path}")

        # Route to appropriate converter
        converters = {
            'image_sequence_to_video': self.image_sequence_to_video,
            'video_to_image_sequence': self.video_to_image_sequence,
            'video_format': self.convert_video_format,
            'image_format': self.convert_image_format,
            'audio_format': self.convert_audio_format,
            '3d_format': self.convert_3d_format,
            'contact_sheet': self.create_contact_sheet,
            'gif_from_video': self.video_to_gif,
            'sprite_sheet': self.create_sprite_sheet
        }

        if conversion_type not in converters:
            raise ValueError(f"Unknown conversion type: {conversion_type}")

        result = converters[conversion_type](input_path, output_path, options)

        logger.success(f"Conversion complete: {output_path}")
        return result

    # ========== Image Sequence <-> Video ==========

    def image_sequence_to_video(self, input_dir: Path, output_file: Path,
                                options: Dict) -> Dict[str, Any]:
        """Convert image sequence to video"""
        framerate = options.get('framerate', 24)
        codec = options.get('codec', 'libx264')
        quality = options.get('quality', 23)  # CRF value
        pattern = options.get('pattern', '%04d.png')
        start_number = options.get('start_number', 0)

        # Find first image to get pattern
        images = sorted(list(input_dir.glob('*.png')) +
                        list(input_dir.glob('*.jpg')))
        if not images:
            raise FileNotFoundError("No images found in directory")

        # Build ffmpeg command
        input_pattern = str(input_dir / pattern)

        cmd = [
            self.ffmpeg,
            '-y',  # Overwrite output
            '-framerate', str(framerate),
            '-start_number', str(start_number),
            '-i', input_pattern,
            '-c:v', codec,
            '-crf', str(quality),
            '-pix_fmt', 'yuv420p',  # Compatibility
            str(output_file)
        ]

        logger.info(f"Running: {' '.join(cmd)}")

        result = subprocess.run(cmd, capture_output=True, text=True)

        if result.returncode != 0:
            logger.error(f"FFmpeg error: {result.stderr}")
            raise RuntimeError(f"FFmpeg conversion failed: {result.stderr}")

        return {
            "output_file": str(output_file),
            "frame_count": len(images),
            "framerate": framerate,
            "codec": codec
        }

    def video_to_image_sequence(self, input_file: Path, output_dir: Path,
                                options: Dict) -> Dict[str, Any]:
        """Convert video to image sequence"""
        format_ext = options.get('format', 'png')
        quality = options.get('quality', 2)  # For JPEG
        pattern = options.get('pattern', 'frame_%04d')
        start_number = options.get('start_number', 0)

        output_dir.mkdir(parents=True, exist_ok=True)
        output_pattern = str(output_dir / f"{pattern}.{format_ext}")

        cmd = [
            self.ffmpeg,
            '-i', str(input_file),
            '-start_number', str(start_number)
        ]

        if format_ext in ['jpg', 'jpeg']:
            cmd.extend(['-q:v', str(quality)])

        cmd.append(output_pattern)

        logger.info(f"Running: {' '.join(cmd)}")

        result = subprocess.run(cmd, capture_output=True, text=True)

        if result.returncode != 0:
            logger.error(f"FFmpeg error: {result.stderr}")
            raise RuntimeError(f"FFmpeg conversion failed: {result.stderr}")

        # Count output frames
        frames = list(output_dir.glob(f"*.{format_ext}"))

        return {
            "output_dir": str(output_dir),
            "frame_count": len(frames),
            "format": format_ext
        }

    # ========== Video Conversion ==========

    def convert_video_format(self, input_file: Path, output_file: Path,
                             options: Dict) -> Dict[str, Any]:
        """Convert video format"""
        codec = options.get('codec', 'libx264')
        quality = options.get('quality', 23)
        audio_codec = options.get('audio_codec', 'aac')
        audio_bitrate = options.get('audio_bitrate', '192k')
        resolution = options.get('resolution')  # e.g., "1920x1080"
        framerate = options.get('framerate')

        cmd = [
            self.ffmpeg,
            '-i', str(input_file),
            '-c:v', codec,
            '-crf', str(quality),
            '-c:a', audio_codec,
            '-b:a', audio_bitrate
        ]

        if resolution:
            cmd.extend(['-s', resolution])

        if framerate:
            cmd.extend(['-r', str(framerate)])

        cmd.append(str(output_file))

        logger.info(f"Running: {' '.join(cmd)}")

        result = subprocess.run(cmd, capture_output=True, text=True)

        if result.returncode != 0:
            raise RuntimeError(f"FFmpeg conversion failed: {result.stderr}")

        return {"output_file": str(output_file)}

    def video_to_gif(self, input_file: Path, output_file: Path,
                     options: Dict) -> Dict[str, Any]:
        """Convert video to optimized GIF"""
        fps = options.get('fps', 15)
        width = options.get('width', 480)

        # Two-pass for better quality
        palette_file = output_file.parent / "palette.png"

        # Generate palette
        cmd_palette = [
            self.ffmpeg,
            '-i', str(input_file),
            '-vf', f'fps={fps},scale={width}:-1:flags=lanczos,palettegen',
            str(palette_file)
        ]

        subprocess.run(cmd_palette, capture_output=True)

        # Generate GIF
        cmd_gif = [
            self.ffmpeg,
            '-i', str(input_file),
            '-i', str(palette_file),
            '-filter_complex', f'fps={fps},scale={width}:-1:flags=lanczos[x];[x][1:v]paletteuse',
            str(output_file)
        ]

        result = subprocess.run(cmd_gif, capture_output=True, text=True)

        # Clean up palette
        if palette_file.exists():
            palette_file.unlink()

        if result.returncode != 0:
            raise RuntimeError(f"GIF conversion failed: {result.stderr}")

        return {"output_file": str(output_file)}

    # ========== Image Conversion ==========

    def convert_image_format(self, input_file: Path, output_file: Path,
                             options: Dict) -> Dict[str, Any]:
        """Convert image format"""
        from PIL import Image

        quality = options.get('quality', 95)

        img = Image.open(input_file)

        # Handle transparency for JPEG
        if output_file.suffix.lower() in ['.jpg', '.jpeg'] and img.mode in ['RGBA', 'LA']:
            background = Image.new('RGB', img.size, (255, 255, 255))
            if img.mode == 'RGBA':
                background.paste(img, mask=img.split()[-1])
            else:
                background.paste(img)
            img = background

        save_kwargs = {}
        if output_file.suffix.lower() in ['.jpg', '.jpeg']:
            save_kwargs['quality'] = quality

        img.save(output_file, **save_kwargs)

        return {"output_file": str(output_file)}

    # ========== Audio Conversion ==========

    def convert_audio_format(self, input_file: Path, output_file: Path,
                             options: Dict) -> Dict[str, Any]:
        """Convert audio format"""
        codec = options.get('codec', 'aac')
        bitrate = options.get('bitrate', '192k')
        sample_rate = options.get('sample_rate', 44100)

        cmd = [
            self.ffmpeg,
            '-i', str(input_file),
            '-c:a', codec,
            '-b:a', bitrate,
            '-ar', str(sample_rate),
            str(output_file)
        ]

        result = subprocess.run(cmd, capture_output=True, text=True)

        if result.returncode != 0:
            raise RuntimeError(f"Audio conversion failed: {result.stderr}")

        return {"output_file": str(output_file)}

    # ========== 3D Format Conversion ==========

    def convert_3d_format(self, input_file: Path, output_file: Path,
                          options: Dict) -> Dict[str, Any]:
        """Convert 3D formats using Blender"""
        blender_path = config.get('software.blender', 'blender')

        # Create conversion script
        script = f"""
import bpy
import sys

# Clear scene
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()

# Import
input_file = r"{input_file}"
output_file = r"{output_file}"

input_ext = "{input_file.suffix.lower()}"
output_ext = "{output_file.suffix.lower()}"

# Import based on format
if input_ext == '.fbx':
    bpy.ops.import_scene.fbx(filepath=input_file)
elif input_ext == '.obj':
    bpy.ops.import_scene.obj(filepath=input_file)
elif input_ext in ['.abc', '.alembic']:
    bpy.ops.wm.alembic_import(filepath=input_file)
elif input_ext == '.usd':
    bpy.ops.wm.usd_import(filepath=input_file)

# Export based on format
if output_ext == '.fbx':
    bpy.ops.export_scene.fbx(filepath=output_file)
elif output_ext == '.obj':
    bpy.ops.export_scene.obj(filepath=output_file)
elif output_ext in ['.abc', '.alembic']:
    bpy.ops.wm.alembic_export(filepath=output_file)
elif output_ext == '.usd':
    bpy.ops.wm.usd_export(filepath=output_file)

print("Conversion complete")
"""

        script_file = output_file.parent / "convert_script.py"
        script_file.write_text(script)

        try:
            cmd = [
                blender_path,
                '--background',
                '--python', str(script_file)
            ]

            result = subprocess.run(cmd, capture_output=True, text=True)

            if result.returncode != 0:
                logger.warning("Blender conversion may have issues")

            return {"output_file": str(output_file)}

        finally:
            if script_file.exists():
                script_file.unlink()

    # ========== Utility Conversions ==========

    def create_contact_sheet(self, input_dir: Path, output_file: Path,
                             options: Dict) -> Dict[str, Any]:
        """Create contact sheet from images"""
        from PIL import Image

        columns = options.get('columns', 5)
        thumb_size = options.get('thumb_size', 200)
        spacing = options.get('spacing', 10)

        # Get all images
        images = sorted(list(input_dir.glob('*.png')) +
                        list(input_dir.glob('*.jpg')) +
                        list(input_dir.glob('*.jpeg')))

        if not images:
            raise FileNotFoundError("No images found")

        # Calculate grid
        rows = (len(images) + columns - 1) // columns

        # Create canvas
        canvas_width = columns * thumb_size + (columns + 1) * spacing
        canvas_height = rows * thumb_size + (rows + 1) * spacing

        canvas = Image.new(
            'RGB', (canvas_width, canvas_height), (255, 255, 255))

        # Place thumbnails
        for idx, img_path in enumerate(images):
            row = idx // columns
            col = idx % columns

            x = col * thumb_size + (col + 1) * spacing
            y = row * thumb_size + (row + 1) * spacing

            img = Image.open(img_path)
            img.thumbnail((thumb_size, thumb_size), Image.Resampling.LANCZOS)

            canvas.paste(img, (x, y))

        canvas.save(output_file, quality=95)

        return {
            "output_file": str(output_file),
            "image_count": len(images),
            "grid": f"{columns}x{rows}"
        }

    def create_sprite_sheet(self, input_dir: Path, output_file: Path,
                            options: Dict) -> Dict[str, Any]:
        """Create sprite sheet from images"""
        from PIL import Image

        columns = options.get('columns', 8)
        spacing = options.get('spacing', 0)

        # Get all images
        images = sorted(list(input_dir.glob('*.png')) +
                        list(input_dir.glob('*.jpg')))

        if not images:
            raise FileNotFoundError("No images found")

        # Get size from first image
        first_img = Image.open(images[0])
        sprite_width, sprite_height = first_img.size

        # Calculate grid
        rows = (len(images) + columns - 1) // columns

        # Create canvas
        canvas_width = columns * sprite_width + (columns - 1) * spacing
        canvas_height = rows * sprite_height + (rows - 1) * spacing

        canvas = Image.new('RGBA', (canvas_width, canvas_height), (0, 0, 0, 0))

        # Place sprites
        for idx, img_path in enumerate(images):
            row = idx // columns
            col = idx % columns

            x = col * (sprite_width + spacing)
            y = row * (sprite_height + spacing)

            img = Image.open(img_path)
            canvas.paste(img, (x, y))

        canvas.save(output_file)

        # Create metadata JSON
        metadata = {
            "sprite_size": [sprite_width, sprite_height],
            "grid": [columns, rows],
            "sprite_count": len(images),
            "spacing": spacing
        }

        metadata_file = output_file.with_suffix('.json')
        metadata_file.write_text(json.dumps(metadata, indent=2))

        return {
            "output_file": str(output_file),
            "metadata_file": str(metadata_file),
            "sprite_count": len(images)
        }

    def get_ui_config(self) -> Dict:
        """Return UI configuration"""
        return {
            "inputs": [
                {"name": "input_path", "type": "path", "label": "Input"},
                {"name": "output_path", "type": "path", "label": "Output"},
                {"name": "conversion_type", "type": "dropdown", "label": "Conversion Type",
                 "options": [
                     "image_sequence_to_video",
                     "video_to_image_sequence",
                     "video_format",
                     "image_format",
                     "audio_format",
                     "3d_format",
                     "contact_sheet",
                     "gif_from_video",
                     "sprite_sheet"
                 ]},
                {"name": "options", "type": "dict", "label": "Options"}
            ]
        }

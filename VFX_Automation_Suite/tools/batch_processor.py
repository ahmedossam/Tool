"""
VFX Automation Suite - Batch File Processor
Process multiple files with various operations
"""

import os
import shutil
from pathlib import Path
from typing import List, Callable, Dict, Any, Optional
from concurrent.futures import ThreadPoolExecutor, as_completed
from PIL import Image
import subprocess

from core.plugin_system import PluginBase
from core.logger import get_logger

logger = get_logger()


class BatchProcessor(PluginBase):
    """Batch process files with various operations"""

    name = "Batch Processor"
    description = "Process multiple files with resize, convert, watermark, etc."
    version = "1.0.0"
    category = "File Processing"

    def __init__(self):
        super().__init__()
        self.operations = {
            "resize": self.resize_image,
            "convert": self.convert_format,
            "watermark": self.add_watermark,
            "rename": self.rename_file,
            "compress": self.compress_image,
            "crop": self.crop_image,
            "rotate": self.rotate_image,
            "flip": self.flip_image,
            "grayscale": self.convert_grayscale,
            "normalize": self.normalize_image
        }

    def execute(self, **kwargs) -> Dict[str, Any]:
        """
        Execute batch processing

        Args:
            files: List of file paths
            operations: List of operation dicts with 'type' and params
            output_dir: Output directory
            parallel: Use parallel processing
            max_workers: Max parallel workers
        """
        files = kwargs.get('files', [])
        operations = kwargs.get('operations', [])
        output_dir = Path(kwargs.get('output_dir', './output'))
        parallel = kwargs.get('parallel', True)
        max_workers = kwargs.get('max_workers', 4)

        output_dir.mkdir(parents=True, exist_ok=True)

        results = {
            "processed": 0,
            "failed": 0,
            "errors": [],
            "output_files": []
        }

        logger.section(f"Batch Processing {len(files)} files")

        if parallel and len(files) > 1:
            results = self._process_parallel(
                files, operations, output_dir, max_workers)
        else:
            results = self._process_sequential(files, operations, output_dir)

        logger.success(f"Processed {results['processed']}/{len(files)} files")
        if results['failed'] > 0:
            logger.warning(f"Failed: {results['failed']} files")

        return results

    def _process_sequential(self, files: List[Path], operations: List[Dict],
                            output_dir: Path) -> Dict[str, Any]:
        """Process files sequentially"""
        results = {"processed": 0, "failed": 0,
                   "errors": [], "output_files": []}

        for i, file_path in enumerate(files, 1):
            logger.progress(i, len(files), f"Processing {file_path.name}")

            try:
                output_file = self._process_single_file(
                    file_path, operations, output_dir)
                results["output_files"].append(str(output_file))
                results["processed"] += 1
            except Exception as e:
                logger.error(f"Failed to process {file_path.name}: {e}")
                results["failed"] += 1
                results["errors"].append(
                    {"file": str(file_path), "error": str(e)})

        return results

    def _process_parallel(self, files: List[Path], operations: List[Dict],
                          output_dir: Path, max_workers: int) -> Dict[str, Any]:
        """Process files in parallel"""
        results = {"processed": 0, "failed": 0,
                   "errors": [], "output_files": []}

        with ThreadPoolExecutor(max_workers=max_workers) as executor:
            futures = {
                executor.submit(self._process_single_file, f, operations, output_dir): f
                for f in files
            }

            for i, future in enumerate(as_completed(futures), 1):
                file_path = futures[future]
                logger.progress(i, len(files), f"Processing {file_path.name}")

                try:
                    output_file = future.result()
                    results["output_files"].append(str(output_file))
                    results["processed"] += 1
                except Exception as e:
                    logger.error(f"Failed to process {file_path.name}: {e}")
                    results["failed"] += 1
                    results["errors"].append(
                        {"file": str(file_path), "error": str(e)})

        return results

    def _process_single_file(self, file_path: Path, operations: List[Dict],
                             output_dir: Path) -> Path:
        """Process a single file through all operations"""
        current_file = file_path
        temp_files = []

        try:
            for op in operations:
                op_type = op.get('type')
                if op_type not in self.operations:
                    raise ValueError(f"Unknown operation: {op_type}")

                # Execute operation
                result_file = self.operations[op_type](
                    current_file, op, output_dir)

                # Clean up temp file if not the original
                if current_file != file_path and current_file in temp_files:
                    current_file.unlink()

                current_file = result_file
                temp_files.append(result_file)

            return current_file

        except Exception as e:
            # Clean up temp files on error
            for temp_file in temp_files:
                if temp_file.exists() and temp_file != file_path:
                    temp_file.unlink()
            raise e

    # ========== Image Operations ==========

    def resize_image(self, file_path: Path, params: Dict, output_dir: Path) -> Path:
        """Resize image"""
        width = params.get('width')
        height = params.get('height')
        maintain_aspect = params.get('maintain_aspect', True)

        img = Image.open(file_path)

        if maintain_aspect:
            img.thumbnail((width or img.width, height or img.height),
                          Image.Resampling.LANCZOS)
        else:
            img = img.resize(
                (width or img.width, height or img.height), Image.Resampling.LANCZOS)

        output_file = output_dir / \
            f"{file_path.stem}_resized{file_path.suffix}"
        img.save(output_file)
        return output_file

    def convert_format(self, file_path: Path, params: Dict, output_dir: Path) -> Path:
        """Convert image format"""
        target_format = params.get('format', 'png').lower()
        quality = params.get('quality', 95)

        img = Image.open(file_path)

        # Handle transparency
        if target_format in ['jpg', 'jpeg'] and img.mode in ['RGBA', 'LA']:
            background = Image.new('RGB', img.size, (255, 255, 255))
            background.paste(img, mask=img.split()
                             [-1] if img.mode == 'RGBA' else None)
            img = background

        output_file = output_dir / f"{file_path.stem}.{target_format}"

        save_kwargs = {}
        if target_format in ['jpg', 'jpeg']:
            save_kwargs['quality'] = quality

        img.save(output_file, **save_kwargs)
        return output_file

    def add_watermark(self, file_path: Path, params: Dict, output_dir: Path) -> Path:
        """Add watermark to image"""
        watermark_text = params.get('text', 'WATERMARK')
        position = params.get('position', 'bottom-right')
        opacity = params.get('opacity', 128)

        from PIL import ImageDraw, ImageFont

        img = Image.open(file_path).convert('RGBA')

        # Create watermark layer
        watermark = Image.new('RGBA', img.size, (255, 255, 255, 0))
        draw = ImageDraw.Draw(watermark)

        # Try to use a font, fallback to default
        try:
            font = ImageFont.truetype("arial.ttf", 36)
        except:
            font = ImageFont.load_default()

        # Calculate position
        bbox = draw.textbbox((0, 0), watermark_text, font=font)
        text_width = bbox[2] - bbox[0]
        text_height = bbox[3] - bbox[1]

        positions = {
            'top-left': (10, 10),
            'top-right': (img.width - text_width - 10, 10),
            'bottom-left': (10, img.height - text_height - 10),
            'bottom-right': (img.width - text_width - 10, img.height - text_height - 10),
            'center': ((img.width - text_width) // 2, (img.height - text_height) // 2)
        }

        pos = positions.get(position, positions['bottom-right'])

        # Draw watermark
        draw.text(pos, watermark_text, fill=(
            255, 255, 255, opacity), font=font)

        # Composite
        result = Image.alpha_composite(img, watermark)

        output_file = output_dir / \
            f"{file_path.stem}_watermarked{file_path.suffix}"
        result.convert('RGB').save(output_file)
        return output_file

    def compress_image(self, file_path: Path, params: Dict, output_dir: Path) -> Path:
        """Compress image"""
        quality = params.get('quality', 85)
        optimize = params.get('optimize', True)

        img = Image.open(file_path)
        output_file = output_dir / \
            f"{file_path.stem}_compressed{file_path.suffix}"

        save_kwargs = {'quality': quality, 'optimize': optimize}
        img.save(output_file, **save_kwargs)
        return output_file

    def crop_image(self, file_path: Path, params: Dict, output_dir: Path) -> Path:
        """Crop image"""
        left = params.get('left', 0)
        top = params.get('top', 0)
        right = params.get('right')
        bottom = params.get('bottom')

        img = Image.open(file_path)

        if right is None:
            right = img.width
        if bottom is None:
            bottom = img.height

        cropped = img.crop((left, top, right, bottom))

        output_file = output_dir / \
            f"{file_path.stem}_cropped{file_path.suffix}"
        cropped.save(output_file)
        return output_file

    def rotate_image(self, file_path: Path, params: Dict, output_dir: Path) -> Path:
        """Rotate image"""
        angle = params.get('angle', 90)
        expand = params.get('expand', True)

        img = Image.open(file_path)
        rotated = img.rotate(angle, expand=expand)

        output_file = output_dir / \
            f"{file_path.stem}_rotated{file_path.suffix}"
        rotated.save(output_file)
        return output_file

    def flip_image(self, file_path: Path, params: Dict, output_dir: Path) -> Path:
        """Flip image"""
        direction = params.get('direction', 'horizontal')

        img = Image.open(file_path)

        if direction == 'horizontal':
            flipped = img.transpose(Image.FLIP_LEFT_RIGHT)
        else:
            flipped = img.transpose(Image.FLIP_TOP_BOTTOM)

        output_file = output_dir / \
            f"{file_path.stem}_flipped{file_path.suffix}"
        flipped.save(output_file)
        return output_file

    def convert_grayscale(self, file_path: Path, params: Dict, output_dir: Path) -> Path:
        """Convert to grayscale"""
        img = Image.open(file_path).convert('L')

        output_file = output_dir / \
            f"{file_path.stem}_grayscale{file_path.suffix}"
        img.save(output_file)
        return output_file

    def normalize_image(self, file_path: Path, params: Dict, output_dir: Path) -> Path:
        """Normalize image brightness/contrast"""
        from PIL import ImageEnhance

        brightness = params.get('brightness', 1.0)
        contrast = params.get('contrast', 1.0)

        img = Image.open(file_path)

        if brightness != 1.0:
            enhancer = ImageEnhance.Brightness(img)
            img = enhancer.enhance(brightness)

        if contrast != 1.0:
            enhancer = ImageEnhance.Contrast(img)
            img = enhancer.enhance(contrast)

        output_file = output_dir / \
            f"{file_path.stem}_normalized{file_path.suffix}"
        img.save(output_file)
        return output_file

    def rename_file(self, file_path: Path, params: Dict, output_dir: Path) -> Path:
        """Rename file with pattern"""
        pattern = params.get('pattern', '{name}')
        counter = params.get('counter', 1)

        # Available variables
        variables = {
            'name': file_path.stem,
            'ext': file_path.suffix,
            'counter': str(counter).zfill(4),
            'original': file_path.name
        }

        new_name = pattern.format(**variables)
        if not new_name.endswith(file_path.suffix):
            new_name += file_path.suffix

        output_file = output_dir / new_name
        shutil.copy2(file_path, output_file)
        return output_file

    def get_ui_config(self) -> Dict:
        """Return UI configuration"""
        return {
            "inputs": [
                {"name": "files", "type": "file_list", "label": "Input Files"},
                {"name": "output_dir", "type": "directory",
                    "label": "Output Directory"},
                {"name": "parallel", "type": "checkbox",
                    "label": "Parallel Processing", "default": True},
                {"name": "operations", "type": "operation_list", "label": "Operations"}
            ],
            "operations": list(self.operations.keys())
        }

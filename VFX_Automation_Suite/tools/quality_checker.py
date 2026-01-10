"""
VFX Automation Suite - Quality Checker
Validate assets for quality control (missing frames, naming, resolution, etc.)
"""

import re
from pathlib import Path
from typing import Dict, Any, List, Tuple
from collections import defaultdict
from PIL import Image

from core.plugin_system import PluginBase
from core.logger import get_logger
from core.config import get_config

logger = get_logger()
config = get_config()


class QualityChecker(PluginBase):
    """Quality control checks for VFX assets"""

    name = "Quality Checker"
    description = "Validate assets for missing frames, naming conventions, resolution, etc."
    version = "1.0.0"
    category = "Quality Control"

    def __init__(self):
        super().__init__()
        self.qc_config = config.get('quality_checks', {})

    def execute(self, **kwargs) -> Dict[str, Any]:
        """
        Execute quality checks

        Args:
            source_dir: Directory to check
            checks: List of checks to perform
            output_report: Path to save report
        """
        source_dir = Path(kwargs.get('source_dir'))
        checks = kwargs.get('checks', ['all'])
        output_report = kwargs.get('output_report')

        if not source_dir.exists():
            raise FileNotFoundError(
                f"Source directory not found: {source_dir}")

        logger.section("Quality Control Checks")

        # Available checks
        available_checks = {
            'missing_frames': self.check_missing_frames,
            'naming_convention': self.check_naming_convention,
            'resolution': self.check_resolution,
            'file_size': self.check_file_size,
            'color_space': self.check_color_space,
            'duplicates': self.check_duplicates,
            'corruption': self.check_corruption
        }

        # Determine which checks to run
        if 'all' in checks:
            checks_to_run = available_checks.keys()
        else:
            checks_to_run = [c for c in checks if c in available_checks]

        # Run checks
        results = {
            "source_dir": str(source_dir),
            "checks_performed": list(checks_to_run),
            "passed": True,
            "issues": [],
            "warnings": [],
            "statistics": {}
        }

        for check_name in checks_to_run:
            logger.info(f"\nRunning check: {check_name}")

            try:
                check_result = available_checks[check_name](source_dir)

                # Merge results
                if check_result.get('issues'):
                    results['issues'].extend(check_result['issues'])
                    results['passed'] = False

                if check_result.get('warnings'):
                    results['warnings'].extend(check_result['warnings'])

                if check_result.get('statistics'):
                    results['statistics'][check_name] = check_result['statistics']

                # Log summary
                issue_count = len(check_result.get('issues', []))
                warning_count = len(check_result.get('warnings', []))

                if issue_count > 0:
                    logger.error(
                        f"  ✗ {check_name}: {issue_count} issues found")
                elif warning_count > 0:
                    logger.warning(
                        f"  ⚠ {check_name}: {warning_count} warnings")
                else:
                    logger.success(f"  ✓ {check_name}: Passed")

            except Exception as e:
                logger.error(f"  ✗ {check_name}: Check failed - {e}")
                results['issues'].append({
                    "check": check_name,
                    "severity": "error",
                    "message": f"Check failed: {e}"
                })
                results['passed'] = False

        # Generate report
        logger.info("\n" + "="*60)
        logger.info("QUALITY CONTROL SUMMARY")
        logger.info("="*60)
        logger.info(f"Total Issues: {len(results['issues'])}")
        logger.info(f"Total Warnings: {len(results['warnings'])}")
        logger.info(
            f"Overall Status: {'PASSED' if results['passed'] else 'FAILED'}")
        logger.info("="*60)

        # Save report if requested
        if output_report:
            self._save_report(results, Path(output_report))

        return results

    # ========== Check Functions ==========

    def check_missing_frames(self, source_dir: Path) -> Dict[str, Any]:
        """Check for missing frames in image sequences"""
        result = {"issues": [], "warnings": [], "statistics": {}}

        # Find sequences
        sequences = self._find_sequences(source_dir)

        for seq_name, frames in sequences.items():
            if len(frames) < 2:
                continue

            # Extract frame numbers
            frame_numbers = sorted(
                [self._extract_frame_number(f.stem) for f in frames])

            if not frame_numbers or None in frame_numbers:
                continue

            # Check for gaps
            expected_range = range(frame_numbers[0], frame_numbers[-1] + 1)
            missing = set(expected_range) - set(frame_numbers)

            if missing:
                result['issues'].append({
                    "check": "missing_frames",
                    "severity": "error",
                    "sequence": seq_name,
                    "missing_frames": sorted(list(missing)),
                    "message": f"Sequence '{seq_name}' is missing {len(missing)} frames: {sorted(list(missing))[:10]}..."
                })

            result['statistics'][seq_name] = {
                "total_frames": len(frames),
                "missing_frames": len(missing),
                "frame_range": f"{frame_numbers[0]}-{frame_numbers[-1]}"
            }

        return result

    def check_naming_convention(self, source_dir: Path) -> Dict[str, Any]:
        """Check if files follow naming conventions"""
        result = {"issues": [], "warnings": [], "statistics": {}}

        # Define naming patterns
        patterns = {
            # name_0001.ext
            "sequence": r"^[a-zA-Z0-9_]+_\d{4,}\.[a-zA-Z0-9]+$",
            # SH_001_name.ext
            "shot": r"^[A-Z]{2,}_\d{3}_[a-zA-Z0-9_]+\.[a-zA-Z0-9]+$",
            "asset": r"^[a-zA-Z0-9_]+_v\d{3}\.[a-zA-Z0-9]+$"  # name_v001.ext
        }

        invalid_chars = r'[<>:"/\\|?*]'

        checked = 0
        invalid = 0

        for file_path in source_dir.rglob('*'):
            if not file_path.is_file():
                continue

            checked += 1
            filename = file_path.name

            # Check for invalid characters
            if re.search(invalid_chars, filename):
                result['issues'].append({
                    "check": "naming_convention",
                    "severity": "error",
                    "file": str(file_path.relative_to(source_dir)),
                    "message": f"Contains invalid characters: {filename}"
                })
                invalid += 1
                continue

            # Check if matches any pattern
            matches_pattern = any(re.match(pattern, filename)
                                  for pattern in patterns.values())

            if not matches_pattern:
                result['warnings'].append({
                    "check": "naming_convention",
                    "severity": "warning",
                    "file": str(file_path.relative_to(source_dir)),
                    "message": f"Does not match standard naming patterns: {filename}"
                })

        result['statistics'] = {
            "files_checked": checked,
            "invalid_names": invalid
        }

        return result

    def check_resolution(self, source_dir: Path) -> Dict[str, Any]:
        """Check image resolutions"""
        result = {"issues": [], "warnings": [], "statistics": {}}

        min_resolution = tuple(self.qc_config.get(
            'min_resolution', [1920, 1080]))

        resolutions = defaultdict(int)
        checked = 0
        below_min = 0

        for file_path in source_dir.rglob('*'):
            if not file_path.is_file():
                continue

            if file_path.suffix.lower() not in ['.png', '.jpg', '.jpeg', '.tif', '.tiff', '.exr']:
                continue

            try:
                with Image.open(file_path) as img:
                    width, height = img.size
                    checked += 1

                    resolutions[f"{width}x{height}"] += 1

                    # Check minimum resolution
                    if width < min_resolution[0] or height < min_resolution[1]:
                        result['issues'].append({
                            "check": "resolution",
                            "severity": "error",
                            "file": str(file_path.relative_to(source_dir)),
                            "resolution": f"{width}x{height}",
                            "message": f"Resolution {width}x{height} is below minimum {min_resolution[0]}x{min_resolution[1]}"
                        })
                        below_min += 1
            except Exception as e:
                result['warnings'].append({
                    "check": "resolution",
                    "severity": "warning",
                    "file": str(file_path.relative_to(source_dir)),
                    "message": f"Could not read image: {e}"
                })

        result['statistics'] = {
            "images_checked": checked,
            "below_minimum": below_min,
            "resolutions_found": dict(resolutions)
        }

        return result

    def check_file_size(self, source_dir: Path) -> Dict[str, Any]:
        """Check for unusually large or small files"""
        result = {"issues": [], "warnings": [], "statistics": {}}

        max_size_mb = self.qc_config.get('max_file_size_mb', 500)
        min_size_kb = 1  # Suspiciously small files

        checked = 0
        too_large = 0
        too_small = 0
        total_size_mb = 0

        for file_path in source_dir.rglob('*'):
            if not file_path.is_file():
                continue

            checked += 1
            size_bytes = file_path.stat().st_size
            size_mb = size_bytes / (1024 * 1024)
            size_kb = size_bytes / 1024

            total_size_mb += size_mb

            # Check if too large
            if size_mb > max_size_mb:
                result['warnings'].append({
                    "check": "file_size",
                    "severity": "warning",
                    "file": str(file_path.relative_to(source_dir)),
                    "size_mb": round(size_mb, 2),
                    "message": f"File is very large: {size_mb:.2f} MB"
                })
                too_large += 1

            # Check if suspiciously small
            if size_kb < min_size_kb:
                result['issues'].append({
                    "check": "file_size",
                    "severity": "error",
                    "file": str(file_path.relative_to(source_dir)),
                    "size_kb": round(size_kb, 2),
                    "message": f"File is suspiciously small: {size_kb:.2f} KB (possibly corrupted)"
                })
                too_small += 1

        result['statistics'] = {
            "files_checked": checked,
            "too_large": too_large,
            "too_small": too_small,
            "total_size_mb": round(total_size_mb, 2)
        }

        return result

    def check_color_space(self, source_dir: Path) -> Dict[str, Any]:
        """Check image color spaces"""
        result = {"issues": [], "warnings": [], "statistics": {}}

        color_modes = defaultdict(int)
        checked = 0

        for file_path in source_dir.rglob('*'):
            if not file_path.is_file():
                continue

            if file_path.suffix.lower() not in ['.png', '.jpg', '.jpeg', '.tif', '.tiff']:
                continue

            try:
                with Image.open(file_path) as img:
                    checked += 1
                    mode = img.mode
                    color_modes[mode] += 1

                    # Warn about unusual color modes
                    if mode not in ['RGB', 'RGBA', 'L', 'LA']:
                        result['warnings'].append({
                            "check": "color_space",
                            "severity": "warning",
                            "file": str(file_path.relative_to(source_dir)),
                            "color_mode": mode,
                            "message": f"Unusual color mode: {mode}"
                        })
            except Exception as e:
                pass

        result['statistics'] = {
            "images_checked": checked,
            "color_modes": dict(color_modes)
        }

        return result

    def check_duplicates(self, source_dir: Path) -> Dict[str, Any]:
        """Check for duplicate filenames (not content)"""
        result = {"issues": [], "warnings": [], "statistics": {}}

        filenames = defaultdict(list)

        for file_path in source_dir.rglob('*'):
            if not file_path.is_file():
                continue

            filenames[file_path.name].append(file_path)

        duplicates = {name: paths for name,
                      paths in filenames.items() if len(paths) > 1}

        for filename, paths in duplicates.items():
            result['warnings'].append({
                "check": "duplicates",
                "severity": "warning",
                "filename": filename,
                "locations": [str(p.relative_to(source_dir)) for p in paths],
                "message": f"Duplicate filename found in {len(paths)} locations: {filename}"
            })

        result['statistics'] = {
            "duplicate_names": len(duplicates),
            "total_duplicates": sum(len(paths) - 1 for paths in duplicates.values())
        }

        return result

    def check_corruption(self, source_dir: Path) -> Dict[str, Any]:
        """Check for corrupted image files"""
        result = {"issues": [], "warnings": [], "statistics": {}}

        checked = 0
        corrupted = 0

        for file_path in source_dir.rglob('*'):
            if not file_path.is_file():
                continue

            if file_path.suffix.lower() not in ['.png', '.jpg', '.jpeg', '.tif', '.tiff', '.exr']:
                continue

            checked += 1

            try:
                with Image.open(file_path) as img:
                    img.verify()  # Verify image integrity
            except Exception as e:
                result['issues'].append({
                    "check": "corruption",
                    "severity": "error",
                    "file": str(file_path.relative_to(source_dir)),
                    "message": f"File appears to be corrupted: {e}"
                })
                corrupted += 1

        result['statistics'] = {
            "images_checked": checked,
            "corrupted": corrupted
        }

        return result

    # ========== Helper Functions ==========

    def _find_sequences(self, directory: Path) -> Dict[str, List[Path]]:
        """Find image sequences in directory"""
        sequences = defaultdict(list)

        for file_path in directory.rglob('*'):
            if not file_path.is_file():
                continue

            if file_path.suffix.lower() not in ['.png', '.jpg', '.jpeg', '.tif', '.tiff', '.exr', '.dpx']:
                continue

            # Extract sequence name (remove frame number)
            seq_name = re.sub(r'\d{3,}', '#', file_path.stem)
            sequences[seq_name].append(file_path)

        return sequences

    def _extract_frame_number(self, filename: str) -> int:
        """Extract frame number from filename"""
        match = re.search(r'(\d{3,})', filename)
        if match:
            return int(match.group(1))
        return None

    def _save_report(self, results: Dict, output_path: Path):
        """Save QC report to file"""
        import json
        from datetime import datetime

        # Add timestamp
        results['report_generated'] = datetime.now().isoformat()

        # Save JSON
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(json.dumps(results, indent=2))

        # Also create human-readable text report
        text_report = output_path.with_suffix('.txt')

        with open(text_report, 'w') as f:
            f.write("="*80 + "\n")
            f.write("QUALITY CONTROL REPORT\n")
            f.write("="*80 + "\n\n")
            f.write(f"Generated: {results['report_generated']}\n")
            f.write(f"Source: {results['source_dir']}\n")
            f.write(
                f"Status: {'PASSED' if results['passed'] else 'FAILED'}\n\n")

            f.write(
                f"Checks Performed: {', '.join(results['checks_performed'])}\n\n")

            f.write("="*80 + "\n")
            f.write(f"SUMMARY\n")
            f.write("="*80 + "\n")
            f.write(f"Total Issues: {len(results['issues'])}\n")
            f.write(f"Total Warnings: {len(results['warnings'])}\n\n")

            if results['issues']:
                f.write("="*80 + "\n")
                f.write("ISSUES\n")
                f.write("="*80 + "\n")
                for issue in results['issues']:
                    f.write(
                        f"\n[{issue['check'].upper()}] {issue['severity'].upper()}\n")
                    f.write(f"  {issue['message']}\n")
                    if 'file' in issue:
                        f.write(f"  File: {issue['file']}\n")

            if results['warnings']:
                f.write("\n" + "="*80 + "\n")
                f.write("WARNINGS\n")
                f.write("="*80 + "\n")
                for warning in results['warnings']:
                    f.write(f"\n[{warning['check'].upper()}]\n")
                    f.write(f"  {warning['message']}\n")
                    if 'file' in warning:
                        f.write(f"  File: {warning['file']}\n")

            if results['statistics']:
                f.write("\n" + "="*80 + "\n")
                f.write("STATISTICS\n")
                f.write("="*80 + "\n")
                for check_name, stats in results['statistics'].items():
                    f.write(f"\n{check_name}:\n")
                    for key, value in stats.items():
                        f.write(f"  {key}: {value}\n")

        logger.success(f"Report saved: {output_path}")
        logger.info(f"Text report: {text_report}")

    def get_ui_config(self) -> Dict:
        """Return UI configuration"""
        return {
            "inputs": [
                {"name": "source_dir", "type": "directory",
                    "label": "Source Directory"},
                {"name": "checks", "type": "checklist", "label": "Checks to Perform",
                 "options": [
                     "all",
                     "missing_frames",
                     "naming_convention",
                     "resolution",
                     "file_size",
                     "color_space",
                     "duplicates",
                     "corruption"
                 ]},
                {"name": "output_report", "type": "file",
                    "label": "Output Report (optional)"}
            ]
        }

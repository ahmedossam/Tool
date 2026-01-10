"""
VFX Automation Suite - Asset Organizer
Organize, categorize, and manage VFX assets
"""

import os
import shutil
import hashlib
from pathlib import Path
from typing import Dict, Any, List, Set
from datetime import datetime
import json

from core.plugin_system import PluginBase
from core.logger import get_logger
from core.config import get_config

logger = get_logger()
config = get_config()


class AssetOrganizer(PluginBase):
    """Organize and manage VFX assets"""

    name = "Asset Organizer"
    description = "Organize files by type, find duplicates, create asset database"
    version = "1.0.0"
    category = "Asset Management"

    def __init__(self):
        super().__init__()
        self.formats = config.get('formats', {})

    def execute(self, **kwargs) -> Dict[str, Any]:
        """
        Execute asset organization

        Args:
            source_dir: Source directory to organize
            target_dir: Target directory for organized files
            mode: Organization mode (by_type, by_date, by_project, cleanup)
            options: Additional options
        """
        source_dir = Path(kwargs.get('source_dir'))
        target_dir = Path(kwargs.get('target_dir', source_dir / 'organized'))
        mode = kwargs.get('mode', 'by_type')
        options = kwargs.get('options', {})

        if not source_dir.exists():
            raise FileNotFoundError(
                f"Source directory not found: {source_dir}")

        logger.section(f"Asset Organization: {mode}")

        modes = {
            'by_type': self.organize_by_type,
            'by_date': self.organize_by_date,
            'by_project': self.organize_by_project,
            'find_duplicates': self.find_duplicates,
            'cleanup': self.cleanup_assets,
            'create_database': self.create_asset_database,
            'rename_sequence': self.rename_sequence
        }

        if mode not in modes:
            raise ValueError(f"Unknown organization mode: {mode}")

        result = modes[mode](source_dir, target_dir, options)

        logger.success("Organization complete")
        return result

    # ========== Organization Modes ==========

    def organize_by_type(self, source_dir: Path, target_dir: Path,
                         options: Dict) -> Dict[str, Any]:
        """Organize files by type (images, videos, 3D, etc.)"""
        copy_files = options.get('copy', True)
        create_structure = options.get('create_structure', True)

        target_dir.mkdir(parents=True, exist_ok=True)

        # Define categories
        categories = {
            'images': self.formats.get('image', []),
            'videos': self.formats.get('video', []),
            '3d_models': self.formats.get('3d', []),
            'audio': self.formats.get('audio', []),
            'projects': self.formats.get('project', []),
            'documents': ['.pdf', '.doc', '.docx', '.txt', '.md'],
            'archives': ['.zip', '.rar', '.7z', '.tar', '.gz']
        }

        stats = {category: 0 for category in categories}
        stats['other'] = 0
        stats['total'] = 0

        # Scan and organize
        for file_path in source_dir.rglob('*'):
            if not file_path.is_file():
                continue

            ext = file_path.suffix.lower()

            # Find category
            category = 'other'
            for cat_name, extensions in categories.items():
                if ext in extensions:
                    category = cat_name
                    break

            # Create category folder
            category_dir = target_dir / category
            category_dir.mkdir(exist_ok=True)

            # Determine destination
            dest_file = category_dir / file_path.name

            # Handle name conflicts
            counter = 1
            while dest_file.exists():
                dest_file = category_dir / \
                    f"{file_path.stem}_{counter}{file_path.suffix}"
                counter += 1

            # Copy or move
            if copy_files:
                shutil.copy2(file_path, dest_file)
            else:
                shutil.move(str(file_path), str(dest_file))

            stats[category] += 1
            stats['total'] += 1

            if stats['total'] % 100 == 0:
                logger.progress(stats['total'], stats['total'],
                                f"Organized {stats['total']} files")

        # Log statistics
        logger.info("\nOrganization Statistics:")
        for category, count in stats.items():
            if count > 0:
                logger.info(f"  {category}: {count} files")

        return {
            "target_dir": str(target_dir),
            "statistics": stats
        }

    def organize_by_date(self, source_dir: Path, target_dir: Path,
                         options: Dict) -> Dict[str, Any]:
        """Organize files by creation/modification date"""
        date_format = options.get('date_format', '%Y/%m')  # Year/Month
        use_creation_date = options.get('use_creation_date', False)
        copy_files = options.get('copy', True)

        target_dir.mkdir(parents=True, exist_ok=True)

        stats = {'total': 0, 'folders_created': set()}

        for file_path in source_dir.rglob('*'):
            if not file_path.is_file():
                continue

            # Get date
            if use_creation_date:
                timestamp = file_path.stat().st_ctime
            else:
                timestamp = file_path.stat().st_mtime

            date_obj = datetime.fromtimestamp(timestamp)
            date_folder = date_obj.strftime(date_format)

            # Create date folder
            dest_dir = target_dir / date_folder
            dest_dir.mkdir(parents=True, exist_ok=True)
            stats['folders_created'].add(str(dest_dir))

            # Copy or move
            dest_file = dest_dir / file_path.name

            # Handle conflicts
            counter = 1
            while dest_file.exists():
                dest_file = dest_dir / \
                    f"{file_path.stem}_{counter}{file_path.suffix}"
                counter += 1

            if copy_files:
                shutil.copy2(file_path, dest_file)
            else:
                shutil.move(str(file_path), str(dest_file))

            stats['total'] += 1

        return {
            "target_dir": str(target_dir),
            "files_organized": stats['total'],
            "folders_created": len(stats['folders_created'])
        }

    def organize_by_project(self, source_dir: Path, target_dir: Path,
                            options: Dict) -> Dict[str, Any]:
        """Organize files by project name (extracted from filename)"""
        separator = options.get('separator', '_')
        # Which part is project name
        project_index = options.get('project_index', 0)
        copy_files = options.get('copy', True)

        target_dir.mkdir(parents=True, exist_ok=True)

        stats = {'total': 0, 'projects': set()}

        for file_path in source_dir.rglob('*'):
            if not file_path.is_file():
                continue

            # Extract project name from filename
            parts = file_path.stem.split(separator)

            if len(parts) > project_index:
                project_name = parts[project_index]
            else:
                project_name = 'uncategorized'

            # Create project folder
            project_dir = target_dir / project_name
            project_dir.mkdir(exist_ok=True)
            stats['projects'].add(project_name)

            # Copy or move
            dest_file = project_dir / file_path.name

            counter = 1
            while dest_file.exists():
                dest_file = project_dir / \
                    f"{file_path.stem}_{counter}{file_path.suffix}"
                counter += 1

            if copy_files:
                shutil.copy2(file_path, dest_file)
            else:
                shutil.move(str(file_path), str(dest_file))

            stats['total'] += 1

        return {
            "target_dir": str(target_dir),
            "files_organized": stats['total'],
            "projects_found": len(stats['projects']),
            "projects": sorted(list(stats['projects']))
        }

    # ========== Duplicate Detection ==========

    def find_duplicates(self, source_dir: Path, target_dir: Path,
                        options: Dict) -> Dict[str, Any]:
        """Find duplicate files based on content hash"""
        delete_duplicates = options.get('delete_duplicates', False)
        move_to_folder = options.get('move_to_folder', True)

        logger.info("Scanning for duplicates...")

        # Calculate hashes
        file_hashes: Dict[str, List[Path]] = {}
        total_files = 0

        for file_path in source_dir.rglob('*'):
            if not file_path.is_file():
                continue

            total_files += 1

            # Calculate hash
            file_hash = self._calculate_file_hash(file_path)

            if file_hash not in file_hashes:
                file_hashes[file_hash] = []

            file_hashes[file_hash].append(file_path)

            if total_files % 100 == 0:
                logger.progress(total_files, total_files,
                                f"Scanned {total_files} files")

        # Find duplicates
        duplicates = {h: files for h, files in file_hashes.items()
                      if len(files) > 1}

        duplicate_count = sum(len(files) - 1 for files in duplicates.values())
        space_saved = 0

        logger.info(
            f"\nFound {len(duplicates)} sets of duplicates ({duplicate_count} duplicate files)")

        # Handle duplicates
        if delete_duplicates or move_to_folder:
            if move_to_folder:
                dup_dir = target_dir / 'duplicates'
                dup_dir.mkdir(parents=True, exist_ok=True)

            for file_hash, files in duplicates.items():
                # Keep first file, handle rest
                original = files[0]

                for duplicate in files[1:]:
                    file_size = duplicate.stat().st_size
                    space_saved += file_size

                    if delete_duplicates:
                        duplicate.unlink()
                        logger.info(f"Deleted: {duplicate.name}")
                    elif move_to_folder:
                        dest = dup_dir / duplicate.name
                        counter = 1
                        while dest.exists():
                            dest = dup_dir / \
                                f"{duplicate.stem}_{counter}{duplicate.suffix}"
                            counter += 1
                        shutil.move(str(duplicate), str(dest))

        # Create report
        report = {
            "total_files": total_files,
            "duplicate_sets": len(duplicates),
            "duplicate_files": duplicate_count,
            "space_saved_mb": space_saved / (1024 * 1024),
            "duplicates": []
        }

        for file_hash, files in duplicates.items():
            report["duplicates"].append({
                "hash": file_hash,
                "files": [str(f) for f in files],
                "size_mb": files[0].stat().st_size / (1024 * 1024)
            })

        # Save report
        report_file = target_dir / 'duplicate_report.json'
        report_file.parent.mkdir(parents=True, exist_ok=True)
        report_file.write_text(json.dumps(report, indent=2))

        logger.info(f"Report saved: {report_file}")

        return report

    def _calculate_file_hash(self, file_path: Path, chunk_size: int = 8192) -> str:
        """Calculate MD5 hash of file"""
        hasher = hashlib.md5()

        with open(file_path, 'rb') as f:
            while chunk := f.read(chunk_size):
                hasher.update(chunk)

        return hasher.hexdigest()

    # ========== Cleanup ==========

    def cleanup_assets(self, source_dir: Path, target_dir: Path,
                       options: Dict) -> Dict[str, Any]:
        """Clean up assets (remove empty folders, temp files, etc.)"""
        remove_empty_folders = options.get('remove_empty_folders', True)
        remove_temp_files = options.get('remove_temp_files', True)
        temp_patterns = options.get(
            'temp_patterns', ['*.tmp', '*.temp', '*~', '.DS_Store', 'Thumbs.db'])

        stats = {
            'empty_folders_removed': 0,
            'temp_files_removed': 0,
            'space_freed_mb': 0
        }

        # Remove temp files
        if remove_temp_files:
            for pattern in temp_patterns:
                for file_path in source_dir.rglob(pattern):
                    if file_path.is_file():
                        size = file_path.stat().st_size
                        file_path.unlink()
                        stats['temp_files_removed'] += 1
                        stats['space_freed_mb'] += size / (1024 * 1024)
                        logger.info(f"Removed temp file: {file_path.name}")

        # Remove empty folders
        if remove_empty_folders:
            # Walk bottom-up to remove nested empty folders
            for folder in sorted(source_dir.rglob('*'), reverse=True):
                if folder.is_dir() and not any(folder.iterdir()):
                    folder.rmdir()
                    stats['empty_folders_removed'] += 1
                    logger.info(f"Removed empty folder: {folder.name}")

        logger.info(f"\nCleanup complete:")
        logger.info(f"  Temp files removed: {stats['temp_files_removed']}")
        logger.info(
            f"  Empty folders removed: {stats['empty_folders_removed']}")
        logger.info(f"  Space freed: {stats['space_freed_mb']:.2f} MB")

        return stats

    # ========== Asset Database ==========

    def create_asset_database(self, source_dir: Path, target_dir: Path,
                              options: Dict) -> Dict[str, Any]:
        """Create searchable asset database with metadata"""
        include_thumbnails = options.get('include_thumbnails', True)
        thumbnail_size = options.get('thumbnail_size', 200)

        database = {
            "created": datetime.now().isoformat(),
            "source_dir": str(source_dir),
            "assets": []
        }

        thumb_dir = target_dir / 'thumbnails'
        if include_thumbnails:
            thumb_dir.mkdir(parents=True, exist_ok=True)

        total_files = sum(1 for _ in source_dir.rglob('*') if _.is_file())
        processed = 0

        for file_path in source_dir.rglob('*'):
            if not file_path.is_file():
                continue

            processed += 1
            logger.progress(processed, total_files,
                            f"Processing {file_path.name}")

            # Gather metadata
            stat = file_path.stat()

            asset_info = {
                "name": file_path.name,
                "path": str(file_path.relative_to(source_dir)),
                "size_mb": stat.st_size / (1024 * 1024),
                "modified": datetime.fromtimestamp(stat.st_mtime).isoformat(),
                "extension": file_path.suffix.lower(),
                "type": self._get_file_type(file_path)
            }

            # Add image-specific metadata
            if asset_info['type'] == 'image':
                try:
                    from PIL import Image
                    img = Image.open(file_path)
                    asset_info['resolution'] = f"{img.width}x{img.height}"
                    asset_info['format'] = img.format

                    # Create thumbnail
                    if include_thumbnails:
                        thumb_path = thumb_dir / f"{file_path.stem}_thumb.jpg"
                        img.thumbnail(
                            (thumbnail_size, thumbnail_size), Image.Resampling.LANCZOS)
                        img.convert('RGB').save(thumb_path, quality=85)
                        asset_info['thumbnail'] = str(
                            thumb_path.relative_to(target_dir))
                except:
                    pass

            database['assets'].append(asset_info)

        # Save database
        db_file = target_dir / 'asset_database.json'
        db_file.write_text(json.dumps(database, indent=2))

        logger.success(f"Database created: {db_file}")
        logger.info(f"Total assets: {len(database['assets'])}")

        return {
            "database_file": str(db_file),
            "asset_count": len(database['assets']),
            "thumbnail_dir": str(thumb_dir) if include_thumbnails else None
        }

    def _get_file_type(self, file_path: Path) -> str:
        """Determine file type from extension"""
        ext = file_path.suffix.lower()

        for type_name, extensions in self.formats.items():
            if ext in extensions:
                return type_name

        return 'other'

    # ========== Sequence Renaming ==========

    def rename_sequence(self, source_dir: Path, target_dir: Path,
                        options: Dict) -> Dict[str, Any]:
        """Rename image sequence with proper numbering"""
        pattern = options.get('pattern', 'frame_{counter:04d}')
        start_number = options.get('start_number', 0)
        extension = options.get('extension', None)  # Keep original if None
        copy_files = options.get('copy', True)

        target_dir.mkdir(parents=True, exist_ok=True)

        # Get all files
        files = sorted([f for f in source_dir.iterdir() if f.is_file()])

        renamed_count = 0

        for idx, file_path in enumerate(files):
            counter = start_number + idx

            # Generate new name
            new_name = pattern.format(
                counter=counter,
                name=file_path.stem,
                original=file_path.name
            )

            # Add extension
            if extension:
                new_name += f".{extension}"
            else:
                new_name += file_path.suffix

            dest_file = target_dir / new_name

            if copy_files:
                shutil.copy2(file_path, dest_file)
            else:
                shutil.move(str(file_path), str(dest_file))

            renamed_count += 1

            if renamed_count % 50 == 0:
                logger.progress(renamed_count, len(files),
                                f"Renamed {renamed_count} files")

        logger.success(f"Renamed {renamed_count} files")

        return {
            "target_dir": str(target_dir),
            "files_renamed": renamed_count,
            "pattern": pattern
        }

    def get_ui_config(self) -> Dict:
        """Return UI configuration"""
        return {
            "inputs": [
                {"name": "source_dir", "type": "directory",
                    "label": "Source Directory"},
                {"name": "target_dir", "type": "directory",
                    "label": "Target Directory"},
                {"name": "mode", "type": "dropdown", "label": "Organization Mode",
                 "options": [
                     "by_type",
                     "by_date",
                     "by_project",
                     "find_duplicates",
                     "cleanup",
                     "create_database",
                     "rename_sequence"
                 ]},
                {"name": "options", "type": "dict", "label": "Options"}
            ]
        }

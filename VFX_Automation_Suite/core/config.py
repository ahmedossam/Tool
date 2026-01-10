"""
VFX Automation Suite - Core Configuration
Centralized configuration management for all tools
"""

import os
import json
from pathlib import Path
from typing import Dict, Any


class Config:
    """Global configuration manager"""

    def __init__(self):
        self.config_dir = Path.home() / ".vfx_automation"
        self.config_file = self.config_dir / "config.json"
        self.config_dir.mkdir(exist_ok=True)

        self.default_config = {
            "version": "1.0.0",
            "paths": {
                "temp_dir": str(Path.home() / "VFX_Temp"),
                "export_dir": str(Path.home() / "VFX_Export"),
                "backup_dir": str(Path.home() / "VFX_Backup"),
                "templates_dir": str(Path.home() / "VFX_Templates")
            },
            "formats": {
                "image": [".png", ".jpg", ".jpeg", ".tif", ".tiff", ".exr", ".dpx", ".tga"],
                "video": [".mp4", ".mov", ".avi", ".mkv", ".webm"],
                "3d": [".fbx", ".obj", ".abc", ".usd", ".usda", ".usdc", ".blend", ".ma", ".mb"],
                "audio": [".wav", ".mp3", ".aac", ".flac", ".ogg"],
                "project": [".aep", ".prproj", ".nk", ".hip", ".blend"]
            },
            "software": {
                "after_effects": "",
                "premiere": "",
                "nuke": "",
                "houdini": "",
                "blender": "",
                "maya": "",
                "unity": "",
                "unreal": "",
                "ffmpeg": "ffmpeg",
                "imagemagick": "magick"
            },
            "quality_checks": {
                "check_naming": True,
                "check_resolution": True,
                "check_missing_frames": True,
                "check_color_space": True,
                "min_resolution": [1920, 1080],
                "max_file_size_mb": 500
            },
            "notifications": {
                "enabled": True,
                "email": "",
                "discord_webhook": "",
                "slack_webhook": ""
            },
            "render": {
                "max_concurrent": 2,
                "priority": "normal",
                "auto_retry": True,
                "retry_count": 3
            }
        }

        self.config = self.load_config()

    def load_config(self) -> Dict[str, Any]:
        """Load configuration from file or create default"""
        if self.config_file.exists():
            try:
                with open(self.config_file, 'r') as f:
                    loaded = json.load(f)
                    # Merge with defaults to add any new keys
                    return self._merge_configs(self.default_config, loaded)
            except Exception as e:
                print(f"Error loading config: {e}. Using defaults.")
                return self.default_config.copy()
        else:
            self.save_config(self.default_config)
            return self.default_config.copy()

    def _merge_configs(self, default: Dict, loaded: Dict) -> Dict:
        """Recursively merge loaded config with defaults"""
        result = default.copy()
        for key, value in loaded.items():
            if key in result and isinstance(result[key], dict) and isinstance(value, dict):
                result[key] = self._merge_configs(result[key], value)
            else:
                result[key] = value
        return result

    def save_config(self, config: Dict[str, Any] = None):
        """Save configuration to file"""
        if config is None:
            config = self.config

        try:
            with open(self.config_file, 'w') as f:
                json.dump(config, f, indent=2)
        except Exception as e:
            print(f"Error saving config: {e}")

    def get(self, key_path: str, default=None):
        """Get config value using dot notation (e.g., 'paths.temp_dir')"""
        keys = key_path.split('.')
        value = self.config

        for key in keys:
            if isinstance(value, dict) and key in value:
                value = value[key]
            else:
                return default

        return value

    def set(self, key_path: str, value: Any):
        """Set config value using dot notation"""
        keys = key_path.split('.')
        config = self.config

        for key in keys[:-1]:
            if key not in config:
                config[key] = {}
            config = config[key]

        config[keys[-1]] = value
        self.save_config()

    def ensure_directories(self):
        """Create all configured directories"""
        for path_key, path_value in self.config.get("paths", {}).items():
            Path(path_value).mkdir(parents=True, exist_ok=True)


# Global config instance
_config = None


def get_config() -> Config:
    """Get global config instance"""
    global _config
    if _config is None:
        _config = Config()
    return _config

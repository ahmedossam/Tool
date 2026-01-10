"""
VFX Automation Suite - Plugin System
Extensible plugin architecture for adding new tools
"""

import importlib
import inspect
from pathlib import Path
from typing import Dict, List, Type, Any
from abc import ABC, abstractmethod


class PluginBase(ABC):
    """Base class for all plugins"""

    name: str = "Unnamed Plugin"
    description: str = "No description"
    version: str = "1.0.0"
    category: str = "General"

    def __init__(self):
        self.enabled = True

    @abstractmethod
    def execute(self, **kwargs) -> Any:
        """Execute the plugin's main functionality"""
        pass

    def validate_inputs(self, **kwargs) -> bool:
        """Validate input parameters before execution"""
        return True

    def get_ui_config(self) -> Dict:
        """Return UI configuration for the plugin"""
        return {
            "inputs": [],
            "outputs": []
        }


class PluginManager:
    """Manages plugin discovery, loading, and execution"""

    def __init__(self, plugin_dirs: List[Path] = None):
        self.plugins: Dict[str, Type[PluginBase]] = {}
        self.plugin_instances: Dict[str, PluginBase] = {}
        self.plugin_dirs = plugin_dirs or []

    def add_plugin_directory(self, directory: Path):
        """Add a directory to search for plugins"""
        if directory not in self.plugin_dirs:
            self.plugin_dirs.append(directory)

    def discover_plugins(self):
        """Discover all plugins in registered directories"""
        for plugin_dir in self.plugin_dirs:
            if not plugin_dir.exists():
                continue

            for file_path in plugin_dir.glob("*.py"):
                if file_path.name.startswith("_"):
                    continue

                try:
                    self._load_plugin_from_file(file_path)
                except Exception as e:
                    print(f"Error loading plugin {file_path.name}: {e}")

    def _load_plugin_from_file(self, file_path: Path):
        """Load a plugin from a Python file"""
        module_name = file_path.stem
        spec = importlib.util.spec_from_file_location(module_name, file_path)

        if spec and spec.loader:
            module = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(module)

            # Find all PluginBase subclasses in the module
            for name, obj in inspect.getmembers(module):
                if (inspect.isclass(obj) and
                    issubclass(obj, PluginBase) and
                        obj is not PluginBase):

                    plugin_name = getattr(obj, 'name', name)
                    self.plugins[plugin_name] = obj
                    print(f"Loaded plugin: {plugin_name}")

    def register_plugin(self, plugin_class: Type[PluginBase]):
        """Manually register a plugin class"""
        plugin_name = plugin_class.name
        self.plugins[plugin_name] = plugin_class

    def get_plugin(self, name: str) -> PluginBase:
        """Get or create a plugin instance"""
        if name not in self.plugin_instances:
            if name not in self.plugins:
                raise ValueError(f"Plugin '{name}' not found")

            self.plugin_instances[name] = self.plugins[name]()

        return self.plugin_instances[name]

    def execute_plugin(self, name: str, **kwargs) -> Any:
        """Execute a plugin by name"""
        plugin = self.get_plugin(name)

        if not plugin.enabled:
            raise RuntimeError(f"Plugin '{name}' is disabled")

        if not plugin.validate_inputs(**kwargs):
            raise ValueError(f"Invalid inputs for plugin '{name}'")

        return plugin.execute(**kwargs)

    def list_plugins(self, category: str = None) -> List[Dict[str, str]]:
        """List all available plugins"""
        result = []

        for name, plugin_class in self.plugins.items():
            if category and plugin_class.category != category:
                continue

            result.append({
                "name": plugin_class.name,
                "description": plugin_class.description,
                "version": plugin_class.version,
                "category": plugin_class.category
            })

        return result

    def get_categories(self) -> List[str]:
        """Get all unique plugin categories"""
        categories = set()
        for plugin_class in self.plugins.values():
            categories.add(plugin_class.category)
        return sorted(list(categories))


# Global plugin manager
_plugin_manager = None


def get_plugin_manager() -> PluginManager:
    """Get global plugin manager instance"""
    global _plugin_manager
    if _plugin_manager is None:
        _plugin_manager = PluginManager()
    return _plugin_manager

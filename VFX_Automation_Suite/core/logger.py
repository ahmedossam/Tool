"""
VFX Automation Suite - Logging System
Centralized logging with file output and console display
"""

import logging
import sys
from pathlib import Path
from datetime import datetime
from typing import Optional


class VFXLogger:
    """Enhanced logger for VFX operations"""

    def __init__(self, name: str = "VFX_Automation", log_dir: Optional[Path] = None):
        self.name = name
        self.log_dir = log_dir or Path.home() / ".vfx_automation" / "logs"
        self.log_dir.mkdir(parents=True, exist_ok=True)

        # Create logger
        self.logger = logging.getLogger(name)
        self.logger.setLevel(logging.DEBUG)

        # Remove existing handlers
        self.logger.handlers.clear()

        # Console handler
        console_handler = logging.StreamHandler(sys.stdout)
        console_handler.setLevel(logging.INFO)
        console_format = logging.Formatter(
            '%(asctime)s [%(levelname)s] %(message)s',
            datefmt='%H:%M:%S'
        )
        console_handler.setFormatter(console_format)
        self.logger.addHandler(console_handler)

        # File handler
        log_file = self.log_dir / \
            f"{name}_{datetime.now().strftime('%Y%m%d')}.log"
        file_handler = logging.FileHandler(log_file, encoding='utf-8')
        file_handler.setLevel(logging.DEBUG)
        file_format = logging.Formatter(
            '%(asctime)s [%(levelname)s] [%(name)s] %(message)s',
            datefmt='%Y-%m-%d %H:%M:%S'
        )
        file_handler.setFormatter(file_format)
        self.logger.addHandler(file_handler)

        self.callbacks = []

    def add_callback(self, callback):
        """Add callback function for log messages (for GUI updates)"""
        self.callbacks.append(callback)

    def _notify_callbacks(self, level: str, message: str):
        """Notify all registered callbacks"""
        for callback in self.callbacks:
            try:
                callback(level, message)
            except Exception as e:
                print(f"Callback error: {e}")

    def debug(self, message: str):
        """Log debug message"""
        self.logger.debug(message)
        self._notify_callbacks("DEBUG", message)

    def info(self, message: str):
        """Log info message"""
        self.logger.info(message)
        self._notify_callbacks("INFO", message)

    def warning(self, message: str):
        """Log warning message"""
        self.logger.warning(message)
        self._notify_callbacks("WARNING", message)

    def error(self, message: str):
        """Log error message"""
        self.logger.error(message)
        self._notify_callbacks("ERROR", message)

    def critical(self, message: str):
        """Log critical message"""
        self.logger.critical(message)
        self._notify_callbacks("CRITICAL", message)

    def success(self, message: str):
        """Log success message (custom level)"""
        self.logger.info(f"✓ {message}")
        self._notify_callbacks("SUCCESS", message)

    def progress(self, current: int, total: int, message: str = ""):
        """Log progress"""
        percentage = (current / total * 100) if total > 0 else 0
        msg = f"Progress: {current}/{total} ({percentage:.1f}%) {message}"
        self.logger.info(msg)
        self._notify_callbacks("PROGRESS", msg)

    def section(self, title: str):
        """Log section header"""
        separator = "=" * 60
        self.logger.info(f"\n{separator}")
        self.logger.info(f"  {title}")
        self.logger.info(separator)
        self._notify_callbacks("SECTION", title)


# Global logger instance
_logger = None


def get_logger(name: str = "VFX_Automation") -> VFXLogger:
    """Get global logger instance"""
    global _logger
    if _logger is None:
        _logger = VFXLogger(name)
    return _logger

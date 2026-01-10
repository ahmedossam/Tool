"""
VFX Automation Suite - Main GUI Application
Qt-based interface for all automation tools
"""

from tools.quality_checker import QualityChecker
from tools.asset_organizer import AssetOrganizer
from tools.format_converter import FormatConverter
from tools.batch_processor import BatchProcessor
from core.plugin_system import get_plugin_manager
from core.logger import get_logger
from core.config import get_config
import sys
from pathlib import Path
from PySide6 import QtWidgets, QtCore, QtGui
from datetime import datetime

# Add core to path
sys.path.insert(0, str(Path(__file__).parent))


# Import tools

logger = get_logger()
config = get_config()


class VFXAutomationGUI(QtWidgets.QMainWindow):
    """Main application window"""

    def __init__(self):
        super().__init__()
        self.setWindowTitle("VFX Automation Suite")
        self.setMinimumSize(1200, 800)

        # Initialize plugin manager
        self.plugin_manager = get_plugin_manager()
        self._register_plugins()

        # Setup UI
        self._setup_ui()

        # Connect logger to console
        logger.add_callback(self._log_callback)

        logger.info("VFX Automation Suite started")

    def _register_plugins(self):
        """Register all available plugins"""
        self.plugin_manager.register_plugin(BatchProcessor)
        self.plugin_manager.register_plugin(FormatConverter)
        self.plugin_manager.register_plugin(AssetOrganizer)
        self.plugin_manager.register_plugin(QualityChecker)

    def _setup_ui(self):
        """Setup the user interface"""
        # Central widget
        central = QtWidgets.QWidget()
        self.setCentralWidget(central)

        # Main layout
        main_layout = QtWidgets.QHBoxLayout(central)

        # Left sidebar - Tool selection
        sidebar = self._create_sidebar()
        main_layout.addWidget(sidebar, 1)

        # Right panel - Tool interface
        self.tool_stack = QtWidgets.QStackedWidget()
        main_layout.addWidget(self.tool_stack, 3)

        # Create tool panels
        self._create_tool_panels()

        # Bottom console
        console_dock = self._create_console_dock()
        self.addDockWidget(QtCore.Qt.BottomDockWidgetArea, console_dock)

        # Menu bar
        self._create_menu_bar()

        # Status bar
        self.statusBar().showMessage("Ready")

    def _create_sidebar(self):
        """Create tool selection sidebar"""
        sidebar = QtWidgets.QWidget()
        layout = QtWidgets.QVBoxLayout(sidebar)

        # Logo/Title
        title = QtWidgets.QLabel("VFX Automation Suite")
        title.setStyleSheet(
            "font-size: 18px; font-weight: bold; padding: 10px;")
        layout.addWidget(title)

        # Tool categories
        categories = self.plugin_manager.get_categories()

        for category in categories:
            # Category header
            cat_label = QtWidgets.QLabel(category.upper())
            cat_label.setStyleSheet(
                "font-weight: bold; color: #888; padding: 10px 5px 5px 5px;")
            layout.addWidget(cat_label)

            # Tools in category
            plugins = self.plugin_manager.list_plugins(category)

            for plugin_info in plugins:
                btn = QtWidgets.QPushButton(f"  {plugin_info['name']}")
                btn.setStyleSheet("""
                    QPushButton {
                        text-align: left;
                        padding: 10px;
                        border: none;
                        background: transparent;
                    }
                    QPushButton:hover {
                        background: #e0e0e0;
                    }
                    QPushButton:pressed {
                        background: #d0d0d0;
                    }
                """)
                btn.setToolTip(plugin_info['description'])
                btn.clicked.connect(
                    lambda checked, name=plugin_info['name']: self._switch_tool(name))
                layout.addWidget(btn)

        layout.addStretch()

        # Settings button
        settings_btn = QtWidgets.QPushButton("⚙ Settings")
        settings_btn.clicked.connect(self._open_settings)
        layout.addWidget(settings_btn)

        return sidebar

    def _create_tool_panels(self):
        """Create interface panels for each tool"""
        # Welcome screen
        welcome = self._create_welcome_panel()
        self.tool_stack.addWidget(welcome)

        # Batch Processor
        batch_panel = self._create_batch_processor_panel()
        self.tool_stack.addWidget(batch_panel)

        # Format Converter
        converter_panel = self._create_format_converter_panel()
        self.tool_stack.addWidget(converter_panel)

        # Asset Organizer
        organizer_panel = self._create_asset_organizer_panel()
        self.tool_stack.addWidget(organizer_panel)

        # Quality Checker
        qc_panel = self._create_quality_checker_panel()
        self.tool_stack.addWidget(qc_panel)

    def _create_welcome_panel(self):
        """Create welcome/home panel"""
        panel = QtWidgets.QWidget()
        layout = QtWidgets.QVBoxLayout(panel)

        # Welcome message
        welcome = QtWidgets.QLabel("""
            <h1>Welcome to VFX Automation Suite</h1>
            <p>Select a tool from the sidebar to get started.</p>
            
            <h3>Available Tools:</h3>
            <ul>
                <li><b>Batch Processor</b> - Process multiple files with resize, convert, watermark, etc.</li>
                <li><b>Format Converter</b> - Convert between image sequences, videos, 3D formats</li>
                <li><b>Asset Organizer</b> - Organize files, find duplicates, create asset database</li>
                <li><b>Quality Checker</b> - Validate assets for missing frames, naming, resolution</li>
            </ul>
            
            <h3>Quick Start:</h3>
            <ol>
                <li>Select a tool from the left sidebar</li>
                <li>Configure the tool parameters</li>
                <li>Click "Execute" to run the tool</li>
                <li>Monitor progress in the console below</li>
            </ol>
        """)
        welcome.setWordWrap(True)
        welcome.setAlignment(QtCore.Qt.AlignTop)
        layout.addWidget(welcome)

        return panel

    def _create_batch_processor_panel(self):
        """Create Batch Processor interface"""
        panel = QtWidgets.QWidget()
        layout = QtWidgets.QVBoxLayout(panel)

        # Title
        title = QtWidgets.QLabel("<h2>Batch Processor</h2>")
        layout.addWidget(title)

        # Input files
        files_group = QtWidgets.QGroupBox("Input Files")
        files_layout = QtWidgets.QVBoxLayout(files_group)

        self.batch_files_list = QtWidgets.QListWidget()
        files_layout.addWidget(self.batch_files_list)

        btn_layout = QtWidgets.QHBoxLayout()
        add_files_btn = QtWidgets.QPushButton("Add Files")
        add_files_btn.clicked.connect(self._batch_add_files)
        add_folder_btn = QtWidgets.QPushButton("Add Folder")
        add_folder_btn.clicked.connect(self._batch_add_folder)
        clear_btn = QtWidgets.QPushButton("Clear")
        clear_btn.clicked.connect(self.batch_files_list.clear)

        btn_layout.addWidget(add_files_btn)
        btn_layout.addWidget(add_folder_btn)
        btn_layout.addWidget(clear_btn)
        files_layout.addLayout(btn_layout)

        layout.addWidget(files_group)

        # Output directory
        output_layout = QtWidgets.QHBoxLayout()
        output_layout.addWidget(QtWidgets.QLabel("Output Directory:"))
        self.batch_output_dir = QtWidgets.QLineEdit()
        output_layout.addWidget(self.batch_output_dir)
        browse_btn = QtWidgets.QPushButton("Browse")
        browse_btn.clicked.connect(
            lambda: self._browse_directory(self.batch_output_dir))
        output_layout.addWidget(browse_btn)
        layout.addLayout(output_layout)

        # Operations
        ops_group = QtWidgets.QGroupBox("Operations")
        ops_layout = QtWidgets.QVBoxLayout(ops_group)

        self.batch_operations = QtWidgets.QListWidget()
        ops_layout.addWidget(self.batch_operations)

        ops_btn_layout = QtWidgets.QHBoxLayout()
        add_op_btn = QtWidgets.QPushButton("Add Operation")
        add_op_btn.clicked.connect(self._batch_add_operation)
        remove_op_btn = QtWidgets.QPushButton("Remove")
        remove_op_btn.clicked.connect(
            lambda: self.batch_operations.takeItem(self.batch_operations.currentRow()))

        ops_btn_layout.addWidget(add_op_btn)
        ops_btn_layout.addWidget(remove_op_btn)
        ops_layout.addLayout(ops_btn_layout)

        layout.addWidget(ops_group)

        # Execute button
        execute_btn = QtWidgets.QPushButton("Execute Batch Processing")
        execute_btn.setStyleSheet("padding: 10px; font-weight: bold;")
        execute_btn.clicked.connect(self._execute_batch_processor)
        layout.addWidget(execute_btn)

        return panel

    def _create_format_converter_panel(self):
        """Create Format Converter interface"""
        panel = QtWidgets.QWidget()
        layout = QtWidgets.QVBoxLayout(panel)

        title = QtWidgets.QLabel("<h2>Format Converter</h2>")
        layout.addWidget(title)

        # Input
        input_layout = QtWidgets.QHBoxLayout()
        input_layout.addWidget(QtWidgets.QLabel("Input:"))
        self.converter_input = QtWidgets.QLineEdit()
        input_layout.addWidget(self.converter_input)
        browse_input_btn = QtWidgets.QPushButton("Browse")
        browse_input_btn.clicked.connect(
            lambda: self._browse_path(self.converter_input))
        input_layout.addWidget(browse_input_btn)
        layout.addLayout(input_layout)

        # Output
        output_layout = QtWidgets.QHBoxLayout()
        output_layout.addWidget(QtWidgets.QLabel("Output:"))
        self.converter_output = QtWidgets.QLineEdit()
        output_layout.addWidget(self.converter_output)
        browse_output_btn = QtWidgets.QPushButton("Browse")
        browse_output_btn.clicked.connect(
            lambda: self._browse_path(self.converter_output))
        output_layout.addWidget(browse_output_btn)
        layout.addLayout(output_layout)

        # Conversion type
        type_layout = QtWidgets.QHBoxLayout()
        type_layout.addWidget(QtWidgets.QLabel("Conversion Type:"))
        self.converter_type = QtWidgets.QComboBox()
        self.converter_type.addItems([
            "image_sequence_to_video",
            "video_to_image_sequence",
            "video_format",
            "image_format",
            "audio_format",
            "3d_format",
            "contact_sheet",
            "gif_from_video",
            "sprite_sheet"
        ])
        type_layout.addWidget(self.converter_type)
        layout.addLayout(type_layout)

        # Options
        options_group = QtWidgets.QGroupBox("Options")
        options_layout = QtWidgets.QFormLayout(options_group)

        self.converter_framerate = QtWidgets.QSpinBox()
        self.converter_framerate.setRange(1, 120)
        self.converter_framerate.setValue(24)
        options_layout.addRow("Framerate:", self.converter_framerate)

        self.converter_quality = QtWidgets.QSpinBox()
        self.converter_quality.setRange(1, 100)
        self.converter_quality.setValue(23)
        options_layout.addRow("Quality:", self.converter_quality)

        layout.addWidget(options_group)

        layout.addStretch()

        # Execute button
        execute_btn = QtWidgets.QPushButton("Convert")
        execute_btn.setStyleSheet("padding: 10px; font-weight: bold;")
        execute_btn.clicked.connect(self._execute_format_converter)
        layout.addWidget(execute_btn)

        return panel

    def _create_asset_organizer_panel(self):
        """Create Asset Organizer interface"""
        panel = QtWidgets.QWidget()
        layout = QtWidgets.QVBoxLayout(panel)

        title = QtWidgets.QLabel("<h2>Asset Organizer</h2>")
        layout.addWidget(title)

        # Source directory
        source_layout = QtWidgets.QHBoxLayout()
        source_layout.addWidget(QtWidgets.QLabel("Source Directory:"))
        self.organizer_source = QtWidgets.QLineEdit()
        source_layout.addWidget(self.organizer_source)
        browse_btn = QtWidgets.QPushButton("Browse")
        browse_btn.clicked.connect(
            lambda: self._browse_directory(self.organizer_source))
        source_layout.addWidget(browse_btn)
        layout.addLayout(source_layout)

        # Target directory
        target_layout = QtWidgets.QHBoxLayout()
        target_layout.addWidget(QtWidgets.QLabel("Target Directory:"))
        self.organizer_target = QtWidgets.QLineEdit()
        target_layout.addWidget(self.organizer_target)
        browse_btn2 = QtWidgets.QPushButton("Browse")
        browse_btn2.clicked.connect(
            lambda: self._browse_directory(self.organizer_target))
        target_layout.addWidget(browse_btn2)
        layout.addLayout(target_layout)

        # Mode
        mode_layout = QtWidgets.QHBoxLayout()
        mode_layout.addWidget(QtWidgets.QLabel("Organization Mode:"))
        self.organizer_mode = QtWidgets.QComboBox()
        self.organizer_mode.addItems([
            "by_type",
            "by_date",
            "by_project",
            "find_duplicates",
            "cleanup",
            "create_database",
            "rename_sequence"
        ])
        mode_layout.addWidget(self.organizer_mode)
        layout.addLayout(mode_layout)

        # Options
        self.organizer_copy = QtWidgets.QCheckBox(
            "Copy files (instead of move)")
        self.organizer_copy.setChecked(True)
        layout.addWidget(self.organizer_copy)

        layout.addStretch()

        # Execute button
        execute_btn = QtWidgets.QPushButton("Organize Assets")
        execute_btn.setStyleSheet("padding: 10px; font-weight: bold;")
        execute_btn.clicked.connect(self._execute_asset_organizer)
        layout.addWidget(execute_btn)

        return panel

    def _create_quality_checker_panel(self):
        """Create Quality Checker interface"""
        panel = QtWidgets.QWidget()
        layout = QtWidgets.QVBoxLayout(panel)

        title = QtWidgets.QLabel("<h2>Quality Checker</h2>")
        layout.addWidget(title)

        # Source directory
        source_layout = QtWidgets.QHBoxLayout()
        source_layout.addWidget(QtWidgets.QLabel("Source Directory:"))
        self.qc_source = QtWidgets.QLineEdit()
        source_layout.addWidget(self.qc_source)
        browse_btn = QtWidgets.QPushButton("Browse")
        browse_btn.clicked.connect(
            lambda: self._browse_directory(self.qc_source))
        source_layout.addWidget(browse_btn)
        layout.addLayout(source_layout)

        # Checks
        checks_group = QtWidgets.QGroupBox("Checks to Perform")
        checks_layout = QtWidgets.QVBoxLayout(checks_group)

        self.qc_checks = {}
        check_names = [
            ("all", "All Checks"),
            ("missing_frames", "Missing Frames"),
            ("naming_convention", "Naming Convention"),
            ("resolution", "Resolution"),
            ("file_size", "File Size"),
            ("color_space", "Color Space"),
            ("duplicates", "Duplicates"),
            ("corruption", "Corruption")
        ]

        for check_id, check_label in check_names:
            checkbox = QtWidgets.QCheckBox(check_label)
            if check_id == "all":
                checkbox.setChecked(True)
            self.qc_checks[check_id] = checkbox
            checks_layout.addWidget(checkbox)

        layout.addWidget(checks_group)

        # Output report
        report_layout = QtWidgets.QHBoxLayout()
        report_layout.addWidget(QtWidgets.QLabel("Output Report:"))
        self.qc_report = QtWidgets.QLineEdit()
        report_layout.addWidget(self.qc_report)
        browse_report_btn = QtWidgets.QPushButton("Browse")
        browse_report_btn.clicked.connect(
            lambda: self._browse_save_file(self.qc_report, "JSON Files (*.json)"))
        report_layout.addWidget(browse_report_btn)
        layout.addLayout(report_layout)

        layout.addStretch()

        # Execute button
        execute_btn = QtWidgets.QPushButton("Run Quality Checks")
        execute_btn.setStyleSheet("padding: 10px; font-weight: bold;")
        execute_btn.clicked.connect(self._execute_quality_checker)
        layout.addWidget(execute_btn)

        return panel

    def _create_console_dock(self):
        """Create console dock widget"""
        dock = QtWidgets.QDockWidget("Console", self)
        dock.setFeatures(QtWidgets.QDockWidget.DockWidgetMovable |
                         QtWidgets.QDockWidget.DockWidgetFloatable)

        self.console = QtWidgets.QTextEdit()
        self.console.setReadOnly(True)
        self.console.setMaximumHeight(200)

        dock.setWidget(self.console)
        return dock

    def _create_menu_bar(self):
        """Create menu bar"""
        menubar = self.menuBar()

        # File menu
        file_menu = menubar.addMenu("File")

        settings_action = QtWidgets.QAction("Settings", self)
        settings_action.triggered.connect(self._open_settings)
        file_menu.addAction(settings_action)

        file_menu.addSeparator()

        exit_action = QtWidgets.QAction("Exit", self)
        exit_action.triggered.connect(self.close)
        file_menu.addAction(exit_action)

        # Help menu
        help_menu = menubar.addMenu("Help")

        about_action = QtWidgets.QAction("About", self)
        about_action.triggered.connect(self._show_about)
        help_menu.addAction(about_action)

    # ========== Event Handlers ==========

    def _switch_tool(self, tool_name: str):
        """Switch to a tool panel"""
        tool_map = {
            "Batch Processor": 1,
            "Format Converter": 2,
            "Asset Organizer": 3,
            "Quality Checker": 4
        }

        index = tool_map.get(tool_name, 0)
        self.tool_stack.setCurrentIndex(index)
        self.statusBar().showMessage(f"Tool: {tool_name}")

    def _log_callback(self, level: str, message: str):
        """Callback for logger messages"""
        timestamp = datetime.now().strftime("%H:%M:%S")

        # Color based on level
        colors = {
            "DEBUG": "#888",
            "INFO": "#000",
            "WARNING": "#ff8800",
            "ERROR": "#ff0000",
            "SUCCESS": "#00aa00",
            "PROGRESS": "#0088ff"
        }

        color = colors.get(level, "#000")

        self.console.append(
            f'<span style="color: {color}">[{timestamp}] {message}</span>')

        # Auto-scroll
        scrollbar = self.console.verticalScrollBar()
        scrollbar.setValue(scrollbar.maximum())

    def _browse_directory(self, line_edit: QtWidgets.QLineEdit):
        """Browse for directory"""
        directory = QtWidgets.QFileDialog.getExistingDirectory(
            self, "Select Directory")
        if directory:
            line_edit.setText(directory)

    def _browse_path(self, line_edit: QtWidgets.QLineEdit):
        """Browse for file or directory"""
        path, _ = QtWidgets.QFileDialog.getOpenFileName(self, "Select File")
        if not path:
            path = QtWidgets.QFileDialog.getExistingDirectory(
                self, "Select Directory")
        if path:
            line_edit.setText(path)

    def _browse_save_file(self, line_edit: QtWidgets.QLineEdit, filter_str: str):
        """Browse for save file"""
        path, _ = QtWidgets.QFileDialog.getSaveFileName(
            self, "Save File", "", filter_str)
        if path:
            line_edit.setText(path)

    def _batch_add_files(self):
        """Add files to batch processor"""
        files, _ = QtWidgets.QFileDialog.getOpenFileNames(self, "Select Files")
        for file in files:
            self.batch_files_list.addItem(file)

    def _batch_add_folder(self):
        """Add folder to batch processor"""
        folder = QtWidgets.QFileDialog.getExistingDirectory(
            self, "Select Folder")
        if folder:
            from pathlib import Path
            for file in Path(folder).rglob('*'):
                if file.is_file():
                    self.batch_files_list.addItem(str(file))

    def _batch_add_operation(self):
        """Add operation to batch processor"""
        # Simple dialog for now
        op_type, ok = QtWidgets.QInputDialog.getItem(
            self, "Add Operation", "Operation Type:",
            ["resize", "convert", "watermark", "compress",
                "crop", "rotate", "flip", "grayscale"],
            0, False
        )

        if ok:
            self.batch_operations.addItem(op_type)

    # ========== Tool Execution ==========

    def _execute_batch_processor(self):
        """Execute batch processor"""
        files = [self.batch_files_list.item(
            i).text() for i in range(self.batch_files_list.count())]

        if not files:
            QtWidgets.QMessageBox.warning(
                self, "No Files", "Please add files to process")
            return

        output_dir = self.batch_output_dir.text() or "./output"

        # Get operations (simplified for now)
        operations = []
        for i in range(self.batch_operations.count()):
            op_type = self.batch_operations.item(i).text()
            operations.append({"type": op_type})

        try:
            result = self.plugin_manager.execute_plugin(
                "Batch Processor",
                files=[Path(f) for f in files],
                operations=operations,
                output_dir=output_dir,
                parallel=True
            )

            QtWidgets.QMessageBox.information(
                self, "Success",
                f"Processed {result['processed']} files\nFailed: {result['failed']}"
            )
        except Exception as e:
            QtWidgets.QMessageBox.critical(self, "Error", str(e))

    def _execute_format_converter(self):
        """Execute format converter"""
        input_path = self.converter_input.text()
        output_path = self.converter_output.text()

        if not input_path or not output_path:
            QtWidgets.QMessageBox.warning(
                self, "Missing Input", "Please specify input and output paths")
            return

        try:
            result = self.plugin_manager.execute_plugin(
                "Format Converter",
                input_path=input_path,
                output_path=output_path,
                conversion_type=self.converter_type.currentText(),
                options={
                    "framerate": self.converter_framerate.value(),
                    "quality": self.converter_quality.value()
                }
            )

            QtWidgets.QMessageBox.information(
                self, "Success", "Conversion complete!")
        except Exception as e:
            QtWidgets.QMessageBox.critical(self, "Error", str(e))

    def _execute_asset_organizer(self):
        """Execute asset organizer"""
        source_dir = self.organizer_source.text()
        target_dir = self.organizer_target.text()

        if not source_dir:
            QtWidgets.QMessageBox.warning(
                self, "Missing Input", "Please specify source directory")
            return

        try:
            result = self.plugin_manager.execute_plugin(
                "Asset Organizer",
                source_dir=source_dir,
                target_dir=target_dir or f"{source_dir}/organized",
                mode=self.organizer_mode.currentText(),
                options={"copy": self.organizer_copy.isChecked()}
            )

            QtWidgets.QMessageBox.information(
                self, "Success", "Organization complete!")
        except Exception as e:
            QtWidgets.QMessageBox.critical(self, "Error", str(e))

    def _execute_quality_checker(self):
        """Execute quality checker"""
        source_dir = self.qc_source.text()

        if not source_dir:
            QtWidgets.QMessageBox.warning(
                self, "Missing Input", "Please specify source directory")
            return

        # Get selected checks
        checks = [name for name, checkbox in self.qc_checks.items()
                  if checkbox.isChecked()]

        try:
            result = self.plugin_manager.execute_plugin(
                "Quality Checker",
                source_dir=source_dir,
                checks=checks,
                output_report=self.qc_report.text() or None
            )

            status = "PASSED" if result['passed'] else "FAILED"
            QtWidgets.QMessageBox.information(
                self, "Quality Check Complete",
                f"Status: {status}\nIssues: {len(result['issues'])}\nWarnings: {len(result['warnings'])}"
            )
        except Exception as e:
            QtWidgets.QMessageBox.critical(self, "Error", str(e))

    def _open_settings(self):
        """Open settings dialog"""
        QtWidgets.QMessageBox.information(
            self, "Settings", "Settings dialog coming soon!")

    def _show_about(self):
        """Show about dialog"""
        QtWidgets.QMessageBox.about(
            self, "About VFX Automation Suite",
            """
            <h2>VFX Automation Suite v1.0.0</h2>
            <p>Comprehensive automation tools for VFX artists</p>
            
            <p><b>Features:</b></p>
            <ul>
                <li>Batch file processing</li>
                <li>Format conversion</li>
                <li>Asset organization</li>
                <li>Quality control</li>
            </ul>
            
            <p>Built with Python and PySide6</p>
            """
        )


def main():
    """Main entry point"""
    app = QtWidgets.QApplication(sys.argv)

    # Set application style
    app.setStyle("Fusion")

    # Create and show main window
    window = VFXAutomationGUI()
    window.show()

    sys.exit(app.exec())


if __name__ == "__main__":
    main()

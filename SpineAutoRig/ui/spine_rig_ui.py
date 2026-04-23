"""
SpineAutoRig - PySide6 GUI
Main window for the Spine Auto-Rig tool.
"""
import sys
import os
import shutil
import traceback
from datetime import datetime

from PySide6 import QtWidgets, QtCore, QtGui
from PIL import Image

# Allow running standalone (python ui/spine_rig_ui.py) or as package
_HERE = os.path.dirname(os.path.abspath(__file__))
_ROOT = os.path.dirname(_HERE)
if _ROOT not in sys.path:
    sys.path.insert(0, _ROOT)

from core.smart_layer_map import smart_remap_name
from utils.image_cut_tools import auto_cut_parts_from_image
from core.rig_spine_export import SpineExporter

try:
    from psd_tools import PSDImage
    PSDTOOLS_AVAILABLE = True
except ImportError:
    PSDTOOLS_AVAILABLE = False

EXPORT_ROOT = "EXPORT"


def ts():
    return datetime.now().strftime("%H:%M:%S")


class AutoRigTool(QtWidgets.QWidget):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Spine Auto-Rig  |  PSD + PNG + Folder")
        self.resize(1050, 740)

        self.loaded_layers = []   # {name, width, height, x, y, image, source_path}
        self.doc_size = (1024, 1024)
        self.current_file = None
        self.export_root = os.path.abspath(EXPORT_ROOT)

        self._build_ui()
        self._check_deps()
        self.log("Ready. Import a PSD (preferred), PNG, or a folder of images.")

    # ------------------------------------------------------------------
    # Dependency check
    # ------------------------------------------------------------------
    def _check_deps(self):
        if not PSDTOOLS_AVAILABLE:
            self.log("⚠️  psd-tools not installed — PSD import disabled.")
            self.btn_psd.setEnabled(False)
            self.btn_psd.setToolTip("pip install psd-tools")

    # ------------------------------------------------------------------
    # UI layout
    # ------------------------------------------------------------------
    def _build_ui(self):
        main = QtWidgets.QHBoxLayout(self)

        # ── LEFT PANEL ──────────────────────────────────────────────
        left = QtWidgets.QVBoxLayout()

        # Import
        g_import = QtWidgets.QGroupBox("Import")
        gl = QtWidgets.QVBoxLayout(g_import)
        self.btn_psd = QtWidgets.QPushButton("📄  Import PSD  (preferred)")
        self.btn_png = QtWidgets.QPushButton("🖼️  Import PNG  (alpha auto-cut)")
        self.btn_folder = QtWidgets.QPushButton("📁  Import Folder of images")
        for b in (self.btn_psd, self.btn_png, self.btn_folder):
            b.setMinimumHeight(32)
            gl.addWidget(b)
        left.addWidget(g_import)

        # Options
        g_opts = QtWidgets.QGroupBox("Options")
        ol = QtWidgets.QVBoxLayout(g_opts)
        self.chk_smart = QtWidgets.QCheckBox("Smart rename & auto bone mapping")
        self.chk_smart.setChecked(True)
        self.chk_ik = QtWidgets.QCheckBox("Generate IK constraints (arms & legs)")
        self.chk_ik.setChecked(True)
        ol.addWidget(self.chk_smart)
        ol.addWidget(self.chk_ik)
        left.addWidget(g_opts)

        # Export
        g_export = QtWidgets.QGroupBox("Export")
        il = QtWidgets.QFormLayout(g_export)
        self.txt_name = QtWidgets.QLineEdit("character")
        self.btn_out = QtWidgets.QPushButton("Choose folder…")
        self.lbl_out = QtWidgets.QLabel(self.export_root)
        self.lbl_out.setWordWrap(True)
        il.addRow("Character name:", self.txt_name)
        il.addRow("Output root:", self.lbl_out)
        il.addRow("", self.btn_out)
        left.addWidget(g_export)

        self.btn_export = QtWidgets.QPushButton("⚡  Export Spine Project")
        self.btn_export.setEnabled(False)
        self.btn_export.setMinimumHeight(40)
        self.btn_export.setStyleSheet("font-weight: bold;")
        left.addWidget(self.btn_export)
        left.addStretch()
        main.addLayout(left, 1)

        # ── RIGHT PANEL ─────────────────────────────────────────────
        right_v = QtWidgets.QVBoxLayout()

        # Layer list
        g_parts = QtWidgets.QGroupBox("Detected layers  (click to preview)")
        pv = QtWidgets.QVBoxLayout(g_parts)
        self.list_parts = QtWidgets.QListWidget()
        self.list_parts.setSelectionMode(
            QtWidgets.QAbstractItemView.MultiSelection)
        pv.addWidget(self.list_parts)
        right_v.addWidget(g_parts, 3)

        # Preview
        g_preview = QtWidgets.QGroupBox("Preview")
        pvl = QtWidgets.QVBoxLayout(g_preview)
        self.lbl_preview = QtWidgets.QLabel("No preview")
        self.lbl_preview.setFixedSize(360, 280)
        self.lbl_preview.setAlignment(QtCore.Qt.AlignCenter)
        self.lbl_preview.setStyleSheet(
            "background:#222; color:#888; border:1px solid #444;")
        pvl.addWidget(self.lbl_preview)
        right_v.addWidget(g_preview, 2)

        # Log
        g_log = QtWidgets.QGroupBox("Console")
        lgl = QtWidgets.QVBoxLayout(g_log)
        self.log_text = QtWidgets.QTextEdit()
        self.log_text.setReadOnly(True)
        self.log_text.setStyleSheet(
            "font-family: monospace; font-size: 11px;")
        lgl.addWidget(self.log_text)
        right_v.addWidget(g_log, 2)

        main.addLayout(right_v, 2)

        # ── Signals ─────────────────────────────────────────────────
        self.btn_psd.clicked.connect(self.on_import_psd)
        self.btn_png.clicked.connect(self.on_import_png)
        self.btn_folder.clicked.connect(self.on_import_folder)
        self.btn_out.clicked.connect(self.on_choose_output)
        self.btn_export.clicked.connect(self.on_export)
        self.list_parts.itemSelectionChanged.connect(self.on_select_part)

    # ------------------------------------------------------------------
    # Helpers
    # ------------------------------------------------------------------
    def log(self, msg):
        line = f"[{ts()}] {msg}"
        self.log_text.append(line)
        print(line)

    def _reset(self):
        self.loaded_layers = []
        self.list_parts.clear()
        self.lbl_preview.setText("No preview")
        self.btn_export.setEnabled(False)

    def _pil_to_qimage(self, pil):
        data = pil.tobytes("raw", "RGBA")
        return QtGui.QImage(data, pil.width, pil.height,
                            QtGui.QImage.Format_RGBA8888)

    # ------------------------------------------------------------------
    # Import handlers
    # ------------------------------------------------------------------
    def on_import_psd(self):
        if not PSDTOOLS_AVAILABLE:
            QtWidgets.QMessageBox.warning(
                self, "Missing dependency",
                "psd-tools is not installed.\n\nRun:\n  pip install psd-tools")
            return
        path, _ = QtWidgets.QFileDialog.getOpenFileName(
            self, "Select PSD file", "", "PSD Files (*.psd)")
        if not path:
            return
        self._reset()
        self.current_file = path
        try:
            psd = PSDImage.open(path)
            self.doc_size = psd.size
            self.log(f"Opened PSD: {os.path.basename(path)}  size={self.doc_size}")

            def traverse(layer):
                if layer.is_group():
                    for child in layer:
                        traverse(child)
                    return
                if not getattr(layer, "visible", True):
                    return
                name_raw = layer.name or "layer"
                name = smart_remap_name(name_raw) if self.chk_smart.isChecked() else name_raw
                bbox = getattr(layer, "bbox", None)
                if bbox and isinstance(bbox, tuple) and len(bbox) == 4:
                    x1, y1, x2, y2 = bbox
                    w = max(1, x2 - x1)
                    h = max(1, y2 - y1)
                    cx = x1 + w / 2
                    cy = y1 + h / 2
                    spine_x = cx - self.doc_size[0] / 2
                    spine_y = self.doc_size[1] / 2 - cy
                else:
                    w = getattr(layer, "width", 100)
                    h = getattr(layer, "height", 100)
                    spine_x = spine_y = 0.0
                try:
                    pil = layer.composite().convert("RGBA")
                except Exception:
                    pil = None
                self.loaded_layers.append({
                    "name": name, "width": int(w), "height": int(h),
                    "x": float(spine_x), "y": float(spine_y),
                    "image": pil, "source_path": path
                })
                self.list_parts.addItem(name)

            for top in psd:
                traverse(top)

            self.log(f"✅ Imported {len(self.loaded_layers)} layers from PSD.")
            if self.loaded_layers:
                self.btn_export.setEnabled(True)
        except Exception as e:
            self.log(f"❌ PSD import failed: {e}")
            self.log(traceback.format_exc())

    def on_import_png(self):
        path, _ = QtWidgets.QFileDialog.getOpenFileName(
            self, "Select PNG", "", "PNG Files (*.png)")
        if not path:
            return
        self._reset()
        self.current_file = path
        try:
            parts = auto_cut_parts_from_image(path, min_area=200)
            if not parts:
                self.log("⚠️  No parts detected — image may have no alpha channel.")
                return
            maxw = max(p['x'] + p['w'] for p in parts)
            maxh = max(p['y'] + p['h'] for p in parts)
            self.doc_size = (max(maxw + 200, 1024), max(maxh + 200, 1024))

            for i, p in enumerate(parts):
                name = p.get("name", f"part_{i}")
                name = smart_remap_name(name) if self.chk_smart.isChecked() else name
                arr = p['image']
                pil = Image.fromarray(arr[..., [2, 1, 0, 3]])   # BGRA → RGBA
                cx = p['x'] + p['w'] / 2
                cy = p['y'] + p['h'] / 2
                spine_x = cx - self.doc_size[0] / 2
                spine_y = self.doc_size[1] / 2 - cy
                self.loaded_layers.append({
                    "name": name, "width": int(p['w']), "height": int(p['h']),
                    "x": float(spine_x), "y": float(spine_y),
                    "image": pil, "source_path": path
                })
                self.list_parts.addItem(name)

            self.log(f"✅ Auto-cut: {len(parts)} parts extracted from PNG.")
            if self.loaded_layers:
                self.btn_export.setEnabled(True)
        except Exception as e:
            self.log(f"❌ PNG import failed: {e}")
            self.log(traceback.format_exc())

    def on_import_folder(self):
        folder = QtWidgets.QFileDialog.getExistingDirectory(
            self, "Select folder containing images")
        if not folder:
            return
        self._reset()
        self.current_file = folder
        self.doc_size = (1024, 1024)
        files = sorted(f for f in os.listdir(folder)
                       if f.lower().endswith(('.png', '.jpg', '.jpeg')))
        for f in files:
            p = os.path.join(folder, f)
            try:
                pil = Image.open(p).convert("RGBA")
                name = os.path.splitext(f)[0]
                name = smart_remap_name(name) if self.chk_smart.isChecked() else name
                self.loaded_layers.append({
                    "name": name, "width": pil.width, "height": pil.height,
                    "x": 0.0, "y": 0.0, "image": pil, "source_path": p
                })
                self.list_parts.addItem(name)
            except Exception as e:
                self.log(f"⚠️  Could not load {f}: {e}")
        self.log(f"✅ Loaded {len(self.loaded_layers)} images from folder.")
        if self.loaded_layers:
            self.btn_export.setEnabled(True)

    def on_choose_output(self):
        d = QtWidgets.QFileDialog.getExistingDirectory(
            self, "Choose export root folder")
        if d:
            self.export_root = os.path.abspath(d)
            self.lbl_out.setText(self.export_root)

    # ------------------------------------------------------------------
    # Preview
    # ------------------------------------------------------------------
    def on_select_part(self):
        items = self.list_parts.selectedItems()
        if not items:
            self.lbl_preview.setText("No preview")
            return
        name = items[0].text()
        for L in self.loaded_layers:
            if L['name'] == name:
                img = L.get('image')
                if img is None:
                    self.lbl_preview.setText("No image data")
                    return
                qimg = self._pil_to_qimage(img)
                pix = QtGui.QPixmap.fromImage(qimg).scaled(
                    self.lbl_preview.size(),
                    QtCore.Qt.KeepAspectRatio,
                    QtCore.Qt.SmoothTransformation)
                self.lbl_preview.setPixmap(pix)
                return

    # ------------------------------------------------------------------
    # Export
    # ------------------------------------------------------------------
    def on_export(self):
        if not self.loaded_layers:
            QtWidgets.QMessageBox.warning(
                self, "No data", "Import a PSD, PNG, or image folder first.")
            return
        char = self.txt_name.text().strip() or "character"
        dest = os.path.join(self.export_root, char)
        images_folder = os.path.join(dest, "images")
        os.makedirs(images_folder, exist_ok=True)

        selected = [it.text() for it in self.list_parts.selectedItems()]
        if not selected:
            selected = [L['name'] for L in self.loaded_layers]

        # Save images
        exported = []
        for L in self.loaded_layers:
            if L['name'] not in selected:
                continue
            target = os.path.join(images_folder, f"{L['name']}.png")
            try:
                if isinstance(L.get('image'), Image.Image):
                    L['image'].save(target, "PNG")
                else:
                    shutil.copy2(L['source_path'], target)
                exported.append(L['name'])
            except Exception as e:
                self.log(f"⚠️  Could not save {L['name']}: {e}")

        self.log(f"Saved {len(exported)} images → {images_folder}")

        try:
            exporter = SpineExporter(
                layers=self.loaded_layers,
                selected=selected,
                doc_size=self.doc_size,
                enable_ik=self.chk_ik.isChecked()
            )
            json_path = os.path.join(dest, f"{char}.json")
            exporter.export(json_path)
            self.log(f"✅ Spine project exported to: {dest}")
            QtWidgets.QMessageBox.information(
                self, "Done! 🎉",
                f"Spine project exported to:\n{dest}\n\n"
                f"Open Spine → Import Data (Ctrl+I) → select the folder above.")
        except Exception as e:
            self.log(f"❌ Export failed: {e}")
            self.log(traceback.format_exc())
            QtWidgets.QMessageBox.critical(self, "Export failed", str(e))


# ------------------------------------------------------------------
# Entry point
# ------------------------------------------------------------------
def launch_ui():
    """Launch the Spine Auto-Rig GUI."""
    app = QtWidgets.QApplication.instance() or QtWidgets.QApplication(sys.argv)
    win = AutoRigTool()
    win.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    launch_ui()

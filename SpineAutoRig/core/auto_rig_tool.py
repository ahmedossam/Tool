# auto_rig_tool.py
import sys
import os
import shutil
import traceback
import json
from datetime import datetime
from PySide6 import QtWidgets, QtCore, QtGui
from PIL import Image
import numpy as np

from psd_tools import PSDImage

from smart_layer_map import smart_remap_name
from image_cut_tools import auto_cut_parts_from_image
from rig_spine_export import SpineExporter

EXPORT_ROOT = "EXPORT"


def ts(): return datetime.now().strftime("%H:%M:%S")


class AutoRigTool(QtWidgets.QWidget):
    def __init__(self):
        super().__init__()
        self.setWindowTitle(
            "Auto Spine Rig - PSD+PNG Hybrid (Alpha Cut fallback)")
        self.resize(980, 720)

        self.loaded_layers = []   # {name,width,height,x,y,image,source_path}
        self.doc_size = (1024, 1024)
        self.current_file = None

        self._build_ui()
        self.log("Ready. Import PSD (preferred) or PNG.")

    def _build_ui(self):
        main = QtWidgets.QHBoxLayout(self)

        # Left controls
        left = QtWidgets.QVBoxLayout()
        g1 = QtWidgets.QGroupBox("Import")
        gl = QtWidgets.QVBoxLayout(g1)
        self.btn_psd = QtWidgets.QPushButton("Import PSD (preferred)")
        self.btn_png = QtWidgets.QPushButton("Import PNG (alpha fallback)")
        self.btn_folder = QtWidgets.QPushButton("Import Images Folder")
        gl.addWidget(self.btn_psd)
        gl.addWidget(self.btn_png)
        gl.addWidget(self.btn_folder)
        left.addWidget(g1)

        opts = QtWidgets.QGroupBox("Options")
        ol = QtWidgets.QVBoxLayout(opts)
        self.chk_smart = QtWidgets.QCheckBox("Smart Rename & Auto Mapping")
        self.chk_smart.setChecked(True)
        self.chk_ik = QtWidgets.QCheckBox("Enable Arm IK (2-bone)")
        self.chk_ik.setChecked(True)
        ol.addWidget(self.chk_smart)
        ol.addWidget(self.chk_ik)
        left.addWidget(opts)

        io = QtWidgets.QGroupBox("Export")
        il = QtWidgets.QFormLayout(io)
        self.txt_name = QtWidgets.QLineEdit("character")
        self.btn_out = QtWidgets.QPushButton("Choose Export Root")
        self.lbl_out = QtWidgets.QLabel(os.path.abspath(EXPORT_ROOT))
        il.addRow("Character name:", self.txt_name)
        il.addRow(self.btn_out, self.lbl_out)
        left.addWidget(io)

        self.btn_export = QtWidgets.QPushButton(
            "Export Spine Project (Folder)")
        self.btn_export.setEnabled(False)
        left.addWidget(self.btn_export)
        left.addStretch()
        main.addLayout(left, 1)

        # Right panel: parts + preview + log
        right_v = QtWidgets.QVBoxLayout()

        parts_box = QtWidgets.QGroupBox(
            "Detected Parts / Layers (select which to rig)")
        pv = QtWidgets.QVBoxLayout(parts_box)
        self.list_parts = QtWidgets.QListWidget()
        self.list_parts.setSelectionMode(
            QtWidgets.QAbstractItemView.MultiSelection)
        pv.addWidget(self.list_parts)
        right_v.addWidget(parts_box, 3)

        preview_box = QtWidgets.QGroupBox("Preview")
        pvl = QtWidgets.QVBoxLayout(preview_box)
        self.lbl_preview = QtWidgets.QLabel()
        self.lbl_preview.setFixedSize(360, 360)
        self.lbl_preview.setAlignment(QtCore.Qt.AlignCenter)
        pvl.addWidget(self.lbl_preview)
        right_v.addWidget(preview_box, 2)

        log_box = QtWidgets.QGroupBox("Console")
        lgl = QtWidgets.QVBoxLayout(log_box)
        self.log_text = QtWidgets.QTextEdit()
        self.log_text.setReadOnly(True)
        lgl.addWidget(self.log_text)
        right_v.addWidget(log_box, 2)

        main.addLayout(right_v, 2)

        # Signals
        self.btn_psd.clicked.connect(self.on_import_psd)
        self.btn_png.clicked.connect(self.on_import_png)
        self.btn_folder.clicked.connect(self.on_import_folder)
        self.btn_out.clicked.connect(self.on_choose_output)
        self.btn_export.clicked.connect(self.on_export)
        self.list_parts.itemSelectionChanged.connect(self.on_select_part)

    def log(self, msg):
        line = f"[{ts()}] {msg}"
        self.log_text.append(line)
        print(line)

    def clear(self):
        self.loaded_layers = []
        self.list_parts.clear()
        self.lbl_preview.clear()
        self.btn_export.setEnabled(False)

    # ---------- Import PSD ----------
    def on_import_psd(self):
        try:
            path, _ = QtWidgets.QFileDialog.getOpenFileName(
                self, "Select PSD file", "", "PSD Files (*.psd)")
            if not path:
                return
            self.clear()
            self.current_file = path
            psd = PSDImage.open(path)
            self.doc_size = psd.size
            self.log(f"Opened PSD: {path} size={self.doc_size}")

            def traverse(layer):
                if layer.is_group():
                    for child in layer:
                        traverse(child)
                else:
                    if not getattr(layer, "visible", True):
                        return
                    name_raw = layer.name or "layer"
                    name = smart_remap_name(
                        name_raw) if self.chk_smart.isChecked() else name_raw
                    bbox = getattr(layer, "bbox", None)
                    if bbox and isinstance(bbox, tuple) and len(bbox) == 4:
                        x1, y1, x2, y2 = bbox
                        w = max(1, x2-x1)
                        h = max(1, y2-y1)
                        cx = x1 + w/2
                        cy = y1 + h/2
                        spine_x = cx - (self.doc_size[0]/2)
                        spine_y = (self.doc_size[1]/2) - cy
                    else:
                        w = getattr(layer, "width", 100)
                        h = getattr(layer, "height", 100)
                        spine_x, spine_y = 0, 0
                    try:
                        pil = layer.composite()
                        pil = pil.convert("RGBA")
                    except Exception:
                        pil = None
                    entry = {"name": name, "width": int(w), "height": int(h), "x": float(
                        spine_x), "y": float(spine_y), "image": pil, "source_path": path}
                    self.loaded_layers.append(entry)
                    self.list_parts.addItem(name)

            for top in psd:
                traverse(top)

            self.log(
                f"Imported {len(self.loaded_layers)} layers from PSD (using PSD priority).")
            if self.loaded_layers:
                self.btn_export.setEnabled(True)
        except Exception as e:
            self.log(f"PSD import failed: {e}")
            self.log(traceback.format_exc())

    # ---------- Import folder ----------
    def on_import_folder(self):
        folder = QtWidgets.QFileDialog.getExistingDirectory(
            self, "Select folder containing PNGs")
        if not folder:
            return
        self.clear()
        self.current_file = folder
        self.doc_size = (1024, 1024)
        files = sorted([f for f in os.listdir(folder)
                       if f.lower().endswith(('.png', '.jpg', '.jpeg'))])
        for f in files:
            p = os.path.join(folder, f)
            try:
                pil = Image.open(p).convert("RGBA")
                name = os.path.splitext(f)[0]
                name = smart_remap_name(
                    name) if self.chk_smart.isChecked() else name
                w, h = pil.size
                entry = {"name": name, "width": w, "height": h,
                         "x": 0, "y": 0, "image": pil, "source_path": p}
                self.loaded_layers.append(entry)
                self.list_parts.addItem(name)
            except Exception as e:
                self.log(f"Could not load {f}: {e}")
        self.log(f"Loaded {len(self.loaded_layers)} images from folder.")
        if self.loaded_layers:
            self.btn_export.setEnabled(True)

    # ---------- Import PNG (alpha auto-cut) ----------
    def on_import_png(self):
        path, _ = QtWidgets.QFileDialog.getOpenFileName(
            self, "Select PNG", "", "PNG Files (*.png)")
        if not path:
            return
        self.clear()
        self.current_file = path
        try:
            parts = auto_cut_parts_from_image(
                path, min_area=200)  # alpha-based
            if not parts:
                self.log("No parts detected in PNG.")
                return
            # estimate doc size big enough to position centers properly
            maxw = max([p['w'] + p['x'] for p in parts]) if parts else 1024
            maxh = max([p['h'] + p['y'] for p in parts]) if parts else 1024
            self.doc_size = (max(maxw+200, 1024), max(maxh+200, 1024))

            for i, p in enumerate(parts):
                name = p.get("name", f"part_{i}")
                name = smart_remap_name(
                    name) if self.chk_smart.isChecked() else name
                # convert numpy BGRA to PIL RGBA
                arr = p['image']
                pil = Image.fromarray(arr[..., [2, 1, 0, 3]])  # BGRA -> RGBA
                cx = p['x'] + p['w']/2
                cy = p['y'] + p['h']/2
                spine_x = cx - (self.doc_size[0]/2)
                spine_y = (self.doc_size[1]/2) - cy
                entry = {"name": name, "width": int(p['w']), "height": int(p['h']), "x": float(
                    spine_x), "y": float(spine_y), "image": pil, "source_path": path}
                self.loaded_layers.append(entry)
                self.list_parts.addItem(name)

            self.log(f"Auto-cut: created {len(parts)} parts from PNG (alpha).")
            if self.loaded_layers:
                self.btn_export.setEnabled(True)
        except Exception as e:
            self.log(f"PNG auto-cut failed: {e}")
            self.log(traceback.format_exc())

    def on_choose_output(self):
        d = QtWidgets.QFileDialog.getExistingDirectory(
            self, "Choose Export Root Folder")
        if not d:
            return
        self.lbl_out.setText(os.path.abspath(d))

    def on_select_part(self):
        items = self.list_parts.selectedItems()
        if not items:
            self.lbl_preview.clear()
            return
        name = items[0].text()
        for L in self.loaded_layers:
            if L['name'] == name:
                img = L.get('image')
                if img is None:
                    self.lbl_preview.setText("No preview")
                    return
                qimg = self.pil_to_qimage(img)
                pix = QtGui.QPixmap.fromImage(qimg).scaled(self.lbl_preview.size(
                ), QtCore.Qt.KeepAspectRatio, QtCore.Qt.SmoothTransformation)
                self.lbl_preview.setPixmap(pix)
                return

    def pil_to_qimage(self, pil):
        data = pil.tobytes("raw", "RGBA")
        qimg = QtGui.QImage(data, pil.width, pil.height,
                            QtGui.QImage.Format_RGBA8888)
        return qimg

    # ---------- Export ----------
    def on_export(self):
        try:
            if not self.loaded_layers:
                QtWidgets.QMessageBox.warning(
                    self, "No data", "Import PSD/PNG/images first.")
                return
            out_root = self.lbl_out.text().strip() or os.path.abspath(EXPORT_ROOT)
            char = self.txt_name.text().strip() or "character"
            dest = os.path.join(out_root, char)
            images_folder = os.path.join(dest, "images")
            os.makedirs(images_folder, exist_ok=True)

            selected = [it.text() for it in self.list_parts.selectedItems()]
            if not selected:
                selected = [L['name'] for L in self.loaded_layers]

            # copy/save images into images_folder
            exported = []
            for L in self.loaded_layers:
                if L['name'] in selected:
                    target = os.path.join(images_folder, f"{L['name']}.png")
                    try:
                        if isinstance(L.get('image'), Image.Image):
                            L['image'].save(target, "PNG")
                        else:
                            shutil.copy2(L.get('source_path'), target)
                        exported.append(L['name'])
                    except Exception as e:
                        self.log(f"Could not export image {L['name']}: {e}")

            self.log(f"Exported {len(exported)} images to {images_folder}")

            exporter = SpineExporter(layers=self.loaded_layers, selected=selected,
                                     doc_size=self.doc_size, enable_ik=self.chk_ik.isChecked())
            json_path = os.path.join(dest, f"{char}.json")
            exporter.export(json_path)

            QtWidgets.QMessageBox.information(
                self, "Done", f"Exported Spine project to:\n{dest}")
            self.log(f"Export complete: {dest}")
        except Exception as e:
            self.log(f"Export failed: {e}")
            self.log(traceback.format_exc())
            QtWidgets.QMessageBox.critical(
                self, "Error", f"Export failed:\n{e}")


def main():
    app = QtWidgets.QApplication(sys.argv)
    win = AutoRigTool()
    win.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()

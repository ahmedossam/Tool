"""
Krita Script: Export All Visible Layers as PNG
Usage: Tools → Scripts → Import Python Plugin from File → Select this file → Run

Or: Copy to Krita scripts folder and run from Scripts menu
"""

from krita import *
import os


def export_layers():
    """Export all visible layers as separate PNG files"""

    # Get active document
    doc = Krita.instance().activeDocument()

    if not doc:
        QMessageBox.critical(None, "Error", "No document open!")
        return

    # Ask for output folder
    output_dir = QFileDialog.getExistingDirectory(
        None,
        "Select Output Folder",
        os.path.expanduser("~")
    )

    if not output_dir:
        return

    # Get document name
    doc_name = doc.name().replace('.kra', '')

    # Create subfolder
    export_folder = os.path.join(output_dir, f"{doc_name}_layers")
    os.makedirs(export_folder, exist_ok=True)

    # Save current state
    original_file = doc.fileName()

    # Get all layers
    def get_all_layers(node, layers_list):
        """Recursively get all layers"""
        if node.type() == "paintlayer":
            layers_list.append(node)

        for child in node.childNodes():
            get_all_layers(child, layers_list)

    layers = []
    root = doc.rootNode()
    get_all_layers(root, layers)

    print(f"Found {len(layers)} layers")

    # Store original visibility
    original_visibility = {}
    for layer in layers:
        original_visibility[layer.name()] = layer.visible()

    exported_count = 0

    # Export each visible layer
    for layer in layers:
        if not original_visibility[layer.name()]:
            continue  # Skip hidden layers

        layer_name = layer.name()

        # Clean filename
        safe_name = "".join(c for c in layer_name if c.isalnum()
                            or c in (' ', '_', '-')).strip()
        safe_name = safe_name.replace(' ', '_')

        # Hide all layers
        for l in layers:
            l.setVisible(False)

        # Show only current layer
        layer.setVisible(True)

        # Refresh view
        doc.refreshProjection()

        # Export
        output_file = os.path.join(export_folder, f"{safe_name}.png")

        # Export layer bounds only
        bounds = layer.bounds()

        # Export document
        doc.exportImage(output_file, InfoObject())

        print(f"Exported: {safe_name}.png")
        exported_count += 1

    # Restore original visibility
    for layer in layers:
        layer.setVisible(original_visibility[layer.name()])

    doc.refreshProjection()

    # Show completion message
    QMessageBox.information(
        None,
        "Export Complete",
        f"Exported {exported_count} layers to:\n{export_folder}"
    )

    print(f"Export complete! {exported_count} layers saved to {export_folder}")


# Run the export
export_layers()

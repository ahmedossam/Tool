"""
Spine Auto-Rig Core Module
Handles PSD/PNG processing, CV detection, and rigging logic
"""

import os
import json
import hashlib
import numpy as np
from PIL import Image
import cv2

# Try import optional dependencies
try:
    from psd_tools import PSDImage
    PSDTOOLS_AVAILABLE = True
except ImportError:
    PSDTOOLS_AVAILABLE = False

try:
    import mediapipe as mp  # type: ignore
    MEDIAPIPE_AVAILABLE = True
except ImportError:
    MEDIAPIPE_AVAILABLE = False

# Alternative: OpenPose-like detection with OpenCV DNN
try:
    import cv2
    OPENCV_AVAILABLE = True
    # Try to check if DNN module is available
    try:
        cv2.dnn.readNetFromCaffe
        OPENCV_DNN_AVAILABLE = True
    except:
        OPENCV_DNN_AVAILABLE = False
except ImportError:
    OPENCV_AVAILABLE = False
    OPENCV_DNN_AVAILABLE = False


###############################################################################
# SMART NAME MAPPING - Maps common layer names to Spine bone names
###############################################################################
SMART_MAP = {
    "head": ["head", "face", "cabeza", "tete"],
    "neck": ["neck", "cuello", "cou"],
    "torso": ["torso", "body", "chest", "belly", "spine", "waist", "trunk", "cuerpo"],
    "hip": ["hip", "pelvis", "hips", "root", "cadera"],

    # Left arm
    "shoulder_L": ["left shoulder", "shoulder l", "l shoulder", "hombro izq"],
    "upper_arm_L": ["left arm", "arm l", "l arm", "left upper arm", "upper arm l", "brazo izq"],
    "forearm_L": ["left forearm", "l forearm", "forearm l", "lower arm l", "left lower arm"],
    "hand_L": ["left hand", "hand l", "l hand", "mano izq"],

    # Right arm
    "shoulder_R": ["right shoulder", "shoulder r", "r shoulder", "hombro der"],
    "upper_arm_R": ["right arm", "arm r", "r arm", "right upper arm", "upper arm r", "brazo der"],
    "forearm_R": ["right forearm", "r forearm", "forearm r", "lower arm r", "right lower arm"],
    "hand_R": ["right hand", "hand r", "r hand", "mano der"],

    # Left leg
    "upper_leg_L": ["left leg", "leg l", "thigh l", "upper leg l", "left thigh", "pierna izq"],
    "lower_leg_L": ["left shin", "shin l", "lower leg l", "left calf", "pantorrilla izq"],
    "foot_L": ["left foot", "foot l", "l foot", "pie izq"],

    # Right leg
    "upper_leg_R": ["right leg", "leg r", "thigh r", "upper leg r", "right thigh", "pierna der"],
    "lower_leg_R": ["right shin", "shin r", "lower leg r", "right calf", "pantorrilla der"],
    "foot_R": ["right foot", "foot r", "r foot", "pie der"]
}

# Parent hierarchy for humanoid rig
BONE_PARENTS = {
    "root": None,
    "hip": "root",
    "torso": "hip",
    "neck": "torso",
    "head": "neck",

    "shoulder_L": "torso",
    "upper_arm_L": "shoulder_L",
    "forearm_L": "upper_arm_L",
    "hand_L": "forearm_L",

    "shoulder_R": "torso",
    "upper_arm_R": "shoulder_R",
    "forearm_R": "upper_arm_R",
    "hand_R": "forearm_R",

    "upper_leg_L": "hip",
    "lower_leg_L": "upper_leg_L",
    "foot_L": "lower_leg_L",

    "upper_leg_R": "hip",
    "lower_leg_R": "upper_leg_R",
    "foot_R": "lower_leg_R"
}


###############################################################################
# CORE CLASS
###############################################################################
class SpineRigCore:
    def __init__(self, log_callback=None):
        self.log_callback = log_callback
        self.layers = []
        self.doc_width = 0
        self.doc_height = 0
        self.detected_pose = None

    def log(self, message):
        """Send log message to callback"""
        if self.log_callback:
            self.log_callback(message)

    ###########################################################################
    # SMART NAME MAPPING
    ###########################################################################
    def smart_remap_name(self, name, enable_smart=True):
        """Map layer name to standard bone name"""
        if not enable_smart:
            return name

        n = name.lower().replace("_", " ").replace("-", " ").strip()

        for target, synonyms in SMART_MAP.items():
            for syn in synonyms:
                if n == syn or syn in n:
                    return target

        return name

    ###########################################################################
    # PSD IMPORT
    ###########################################################################
    def import_psd(self, psd_path, smart_mapping=True, include_hidden=False,
                   merge_groups=False, group_prefix=True):
        """
        Import PSD file and extract layers with positions

        Args:
            psd_path: Path to PSD file
            smart_mapping: Enable smart name mapping
            include_hidden: Include hidden layers
            merge_groups: Merge group contents into single image
            group_prefix: Add group name as prefix to layer names
        """
        if not PSDTOOLS_AVAILABLE:
            raise Exception(
                "psd-tools not installed. Install with: pip install psd-tools")

        self.log(f"Opening PSD: {os.path.basename(psd_path)}")
        psd = PSDImage.open(psd_path)
        self.doc_width, self.doc_height = psd.size
        self.log(f"Document size: {self.doc_width}x{self.doc_height}px")

        doc_cx = self.doc_width / 2
        doc_cy = self.doc_height / 2

        self.layers = []
        self.groups_info = {}  # Track group information

        def extract_layer(layer, depth=0, parent_group=""):
            # Skip hidden layers unless requested
            if not include_hidden and not layer.visible:
                return

            # Skip layers with names starting with _
            if layer.name and layer.name.startswith('_'):
                return

            if hasattr(layer, 'is_group') and layer.is_group():
                group_name = layer.name if layer.name else f"Group_{depth}"
                self.log(f"{'  ' * depth}📁 Group: {group_name}")

                # Store group info
                self.groups_info[group_name] = {
                    "depth": depth,
                    "visible": layer.visible,
                    "parent": parent_group
                }

                if merge_groups:
                    # Merge all layers in group into single image
                    self.merge_group_layers(layer, group_name, doc_cx, doc_cy,
                                            depth, parent_group, smart_mapping)
                else:
                    # Process each layer in group separately
                    new_parent = f"{parent_group}/{group_name}" if parent_group else group_name
                    for sublayer in layer:
                        extract_layer(sublayer, depth + 1, new_parent)
            else:
                if not layer.name:
                    return

                try:
                    # Get bounding box
                    bbox = layer.bbox
                    if isinstance(bbox, tuple):
                        x1, y1, x2, y2 = bbox
                    else:
                        x1, y1 = bbox.x1, bbox.y1
                        x2, y2 = bbox.x2, bbox.y2

                    w = max(x2 - x1, 1)
                    h = max(y2 - y1, 1)

                    # Calculate center position
                    cx = x1 + w / 2
                    cy = y1 + h / 2

                    # Convert to Spine coordinates (centered, Y-flipped)
                    spine_x = cx - doc_cx
                    spine_y = doc_cy - cy

                    # Build layer name with group prefix if enabled
                    layer_name = layer.name
                    if group_prefix and parent_group:
                        # Use only the immediate parent group name
                        immediate_parent = parent_group.split('/')[-1]
                        layer_name = f"{immediate_parent}_{layer_name}"

                    # Smart remap name
                    mapped_name = self.smart_remap_name(
                        layer_name, smart_mapping)

                    # Try to get image data
                    try:
                        img = layer.topil()
                    except:
                        img = None

                    self.layers.append({
                        "name": mapped_name,
                        "original_name": layer.name,
                        "full_name": layer_name,
                        "group_path": parent_group,
                        "width": int(w),
                        "height": int(h),
                        "x": round(spine_x, 2),
                        "y": round(spine_y, 2),
                        "image_data": img
                    })

                    prefix = f"{parent_group}/" if parent_group else ""
                    self.log(
                        f"{'  ' * depth}✓ {prefix}{layer.name} → {mapped_name} @ ({spine_x:.1f}, {spine_y:.1f})")

                except Exception as e:
                    self.log(
                        f"{'  ' * depth}⚠️ Could not parse {layer.name}: {str(e)}")

        extract_layer(psd)

        if self.groups_info:
            self.log(f"\n📁 Found {len(self.groups_info)} groups:")
            for group_name, info in self.groups_info.items():
                self.log(f"  • {group_name} (depth: {info['depth']})")

        self.log(f"\n✅ Extracted {len(self.layers)} layers")
        return self.layers

    def merge_group_layers(self, group, group_name, doc_cx, doc_cy, depth,
                           parent_group, smart_mapping):
        """Merge all layers in a group into a single composite image"""
        try:
            # Get group bounding box
            bbox = group.bbox
            if isinstance(bbox, tuple):
                x1, y1, x2, y2 = bbox
            else:
                x1, y1 = bbox.x1, bbox.y1
                x2, y2 = bbox.x2, bbox.y2

            w = max(x2 - x1, 1)
            h = max(y2 - y1, 1)

            # Calculate center position
            cx = x1 + w / 2
            cy = y1 + h / 2
            spine_x = cx - doc_cx
            spine_y = doc_cy - cy

            # Try to composite the group
            try:
                img = group.composite()
            except:
                self.log(
                    f"{'  ' * depth}⚠️ Could not composite group {group_name}")
                return

            # Smart remap name
            mapped_name = self.smart_remap_name(group_name, smart_mapping)

            self.layers.append({
                "name": mapped_name,
                "original_name": group_name,
                "full_name": group_name,
                "group_path": parent_group,
                "width": int(w),
                "height": int(h),
                "x": round(spine_x, 2),
                "y": round(spine_y, 2),
                "image_data": img,
                "is_merged_group": True
            })

            self.log(
                f"{'  ' * depth}✓ Merged group '{group_name}' → {mapped_name}")

        except Exception as e:
            self.log(
                f"{'  ' * depth}⚠️ Could not merge group {group_name}: {str(e)}")

    ###########################################################################
    # PNG IMPORT WITH CV DETECTION
    ###########################################################################
    def import_png_with_detection(self, png_path, auto_segment=True):
        """Import PNG and use CV to detect body parts"""
        self.log(f"Opening PNG: {os.path.basename(png_path)}")

        img = Image.open(png_path).convert("RGBA")
        self.doc_width, self.doc_height = img.size
        self.log(f"Image size: {self.doc_width}x{self.doc_height}px")

        if auto_segment and MEDIAPIPE_AVAILABLE:
            self.log("Running pose detection...")
            self.layers = self.detect_and_segment_body_parts(img)
        else:
            # Fallback: treat whole image as single part
            self.log("No segmentation - using full image")
            self.layers = [{
                "name": "body",
                "original_name": "body",
                "width": self.doc_width,
                "height": self.doc_height,
                "x": 0,
                "y": 0,
                "image_data": img
            }]

        return self.layers

    def detect_and_segment_body_parts(self, pil_image):
        """Use MediaPipe to detect pose and segment body parts"""
        # Convert PIL to OpenCV format
        img_array = np.array(pil_image)
        img_rgb = cv2.cvtColor(img_array, cv2.COLOR_RGBA2RGB)

        # Initialize MediaPipe Pose
        mp_pose = mp.solutions.pose
        pose = mp_pose.Pose(static_image_mode=True,
                            min_detection_confidence=0.5)

        # Detect pose  (MediaPipe API uses .process(), not .detect())
        results = pose.process(img_rgb)

        if not results.pose_landmarks:
            self.log("⚠️ No pose detected - using fallback")
            return self.fallback_segmentation(pil_image)

        self.log("✓ Pose detected! Extracting body parts...")
        self.detected_pose = results.pose_landmarks

        # Get landmarks
        landmarks = results.pose_landmarks.landmark
        h, w = img_rgb.shape[:2]

        doc_cx = w / 2
        doc_cy = h / 2

        # Define body part regions using landmarks
        parts = self.define_body_regions(landmarks, w, h)

        # Extract each part
        extracted_layers = []

        for part_name, (x1, y1, x2, y2) in parts.items():
            # Ensure bounds are valid
            x1 = max(0, int(x1))
            y1 = max(0, int(y1))
            x2 = min(w, int(x2))
            y2 = min(h, int(y2))

            if x2 <= x1 or y2 <= y1:
                continue

            # Crop region
            part_img = pil_image.crop((x1, y1, x2, y2))

            # Calculate center in Spine coordinates
            cx = (x1 + x2) / 2
            cy = (y1 + y2) / 2
            spine_x = cx - doc_cx
            spine_y = doc_cy - cy

            extracted_layers.append({
                "name": part_name,
                "original_name": part_name,
                "width": x2 - x1,
                "height": y2 - y1,
                "x": round(spine_x, 2),
                "y": round(spine_y, 2),
                "image_data": part_img
            })

            self.log(f"  ✓ Extracted: {part_name}")

        pose.close()
        return extracted_layers

    def define_body_regions(self, landmarks, width, height):
        """Define bounding boxes for each body part from pose landmarks"""
        mp_pose = mp.solutions.pose.PoseLandmark

        def get_point(idx):
            lm = landmarks[idx]
            return lm.x * width, lm.y * height

        def expand_bbox(points, margin=0.1):
            """Create bbox from points with margin"""
            xs = [p[0] for p in points]
            ys = [p[1] for p in points]
            x1, x2 = min(xs), max(xs)
            y1, y2 = min(ys), max(ys)

            w = x2 - x1
            h = y2 - y1

            return (
                x1 - w * margin,
                y1 - h * margin,
                x2 + w * margin,
                y2 + h * margin
            )

        regions = {}

        # Head
        nose = get_point(mp_pose.NOSE)
        l_ear = get_point(mp_pose.LEFT_EAR)
        r_ear = get_point(mp_pose.RIGHT_EAR)
        regions["head"] = expand_bbox([nose, l_ear, r_ear], 0.3)

        # Torso
        l_shoulder = get_point(mp_pose.LEFT_SHOULDER)
        r_shoulder = get_point(mp_pose.RIGHT_SHOULDER)
        l_hip = get_point(mp_pose.LEFT_HIP)
        r_hip = get_point(mp_pose.RIGHT_HIP)
        regions["torso"] = expand_bbox(
            [l_shoulder, r_shoulder, l_hip, r_hip], 0.1)

        # Left arm
        l_elbow = get_point(mp_pose.LEFT_ELBOW)
        l_wrist = get_point(mp_pose.LEFT_WRIST)
        regions["upper_arm_L"] = expand_bbox([l_shoulder, l_elbow], 0.15)
        regions["forearm_L"] = expand_bbox([l_elbow, l_wrist], 0.15)
        regions["hand_L"] = expand_bbox(
            [l_wrist, get_point(mp_pose.LEFT_PINKY), get_point(mp_pose.LEFT_INDEX)], 0.2)

        # Right arm
        r_elbow = get_point(mp_pose.RIGHT_ELBOW)
        r_wrist = get_point(mp_pose.RIGHT_WRIST)
        regions["upper_arm_R"] = expand_bbox([r_shoulder, r_elbow], 0.15)
        regions["forearm_R"] = expand_bbox([r_elbow, r_wrist], 0.15)
        regions["hand_R"] = expand_bbox(
            [r_wrist, get_point(mp_pose.RIGHT_PINKY), get_point(mp_pose.RIGHT_INDEX)], 0.2)

        # Left leg
        l_knee = get_point(mp_pose.LEFT_KNEE)
        l_ankle = get_point(mp_pose.LEFT_ANKLE)
        regions["upper_leg_L"] = expand_bbox([l_hip, l_knee], 0.15)
        regions["lower_leg_L"] = expand_bbox([l_knee, l_ankle], 0.15)
        regions["foot_L"] = expand_bbox([l_ankle, get_point(
            mp_pose.LEFT_HEEL), get_point(mp_pose.LEFT_FOOT_INDEX)], 0.2)

        # Right leg
        r_knee = get_point(mp_pose.RIGHT_KNEE)
        r_ankle = get_point(mp_pose.RIGHT_ANKLE)
        regions["upper_leg_R"] = expand_bbox([r_hip, r_knee], 0.15)
        regions["lower_leg_R"] = expand_bbox([r_knee, r_ankle], 0.15)
        regions["foot_R"] = expand_bbox([r_ankle, get_point(
            mp_pose.RIGHT_HEEL), get_point(mp_pose.RIGHT_FOOT_INDEX)], 0.2)

        return regions

    def fallback_segmentation(self, pil_image):
        """Simple color-based segmentation fallback"""
        self.log("Using fallback color-based segmentation...")
        # Simple implementation: treat whole image as one part
        return [{
            "name": "body",
            "original_name": "body",
            "width": pil_image.width,
            "height": pil_image.height,
            "x": 0,
            "y": 0,
            "image_data": pil_image
        }]

    ###########################################################################
    # FOLDER IMPORT
    ###########################################################################
    def import_folder(self, folder_path, smart_mapping=True):
        """Import PNG images from folder"""
        self.log(f"Scanning folder: {folder_path}")

        self.layers = []
        image_files = [f for f in os.listdir(folder_path)
                       if f.lower().endswith(('.png', '.jpg', '.jpeg'))]

        for img_file in sorted(image_files):
            img_path = os.path.join(folder_path, img_file)
            img = Image.open(img_path).convert("RGBA")

            name = os.path.splitext(img_file)[0]
            mapped_name = self.smart_remap_name(name, smart_mapping)

            self.layers.append({
                "name": mapped_name,
                "original_name": name,
                "width": img.width,
                "height": img.height,
                "x": 0,
                "y": 0,
                "image_data": img
            })

            self.log(f"  ✓ {img_file} → {mapped_name}")

        self.log(f"Imported {len(self.layers)} images")
        return self.layers

    ###########################################################################
    # BONE GENERATION
    ###########################################################################
    def generate_bones(self, use_hierarchy=True):
        """Generate bone structure from layers"""
        bones = [{"name": "root", "x": 0, "y": 0}]

        for layer in self.layers:
            bone = {
                "name": layer["name"],
                "x": layer["x"],
                "y": layer["y"]
            }

            # Determine parent
            if use_hierarchy and layer["name"] in BONE_PARENTS:
                parent = BONE_PARENTS[layer["name"]]
                if parent:
                    bone["parent"] = parent
                else:
                    bone["parent"] = "root"
            else:
                bone["parent"] = "root"

            # Calculate length (distance from parent position)
            if "parent" in bone and bone["parent"] != "root":
                parent_layer = next(
                    (l for l in self.layers if l["name"] == bone["parent"]), None)
                if parent_layer:
                    dx = layer["x"] - parent_layer["x"]
                    dy = layer["y"] - parent_layer["y"]
                    length = int(np.sqrt(dx**2 + dy**2))
                    bone["length"] = max(length, 10)

            bones.append(bone)

        return bones

    def generate_slots(self):
        """Generate slots for attachments"""
        return [
            {
                "name": layer["name"],
                "bone": layer["name"],
                "attachment": layer["original_name"]
            }
            for layer in self.layers
        ]

    def generate_attachments(self):
        """Generate attachment definitions"""
        attachments = {}

        for layer in self.layers:
            attachments[layer["name"]] = {
                layer["original_name"]: {
                    "type": "region",
                    "name": layer["original_name"],
                    "x": 0,
                    "y": 0,
                    "rotation": 0,
                    "width": layer["width"],
                    "height": layer["height"]
                }
            }

        return attachments

    def generate_ik_constraints(self, create_ik=True):
        """Generate IK constraints for arms and legs"""
        if not create_ik:
            return []

        ik_list = []

        # Check which bones exist
        bone_names = [l["name"] for l in self.layers]

        # Left arm IK
        if all(b in bone_names for b in ["upper_arm_L", "forearm_L", "hand_L"]):
            ik_list.append({
                "name": "arm_L_ik",
                "order": len(ik_list),
                "bones": ["upper_arm_L", "forearm_L"],
                "target": "hand_L",
                "bendPositive": False,
                "mix": 1
            })

        # Right arm IK
        if all(b in bone_names for b in ["upper_arm_R", "forearm_R", "hand_R"]):
            ik_list.append({
                "name": "arm_R_ik",
                "order": len(ik_list),
                "bones": ["upper_arm_R", "forearm_R"],
                "target": "hand_R",
                "bendPositive": True,
                "mix": 1
            })

        # Left leg IK
        if all(b in bone_names for b in ["upper_leg_L", "lower_leg_L", "foot_L"]):
            ik_list.append({
                "name": "leg_L_ik",
                "order": len(ik_list),
                "bones": ["upper_leg_L", "lower_leg_L"],
                "target": "foot_L",
                "bendPositive": True,
                "mix": 1
            })

        # Right leg IK
        if all(b in bone_names for b in ["upper_leg_R", "lower_leg_R", "foot_R"]):
            ik_list.append({
                "name": "leg_R_ik",
                "order": len(ik_list),
                "bones": ["upper_leg_R", "lower_leg_R"],
                "target": "foot_R",
                "bendPositive": True,
                "mix": 1
            })

        return ik_list

    ###########################################################################
    # EXPORT
    ###########################################################################
    def export_spine_project(self, output_folder, character_name, create_ik=True, use_hierarchy=True):
        """Export complete Spine project with JSON and images"""
        if not self.layers:
            raise Exception("No layers to export. Import a file first.")

        # Create project structure
        project_folder = os.path.join(output_folder, character_name)
        images_folder = os.path.join(project_folder, "images")
        os.makedirs(images_folder, exist_ok=True)

        self.log(f"Creating project: {project_folder}")

        # Export images
        for layer in self.layers:
            if layer["image_data"]:
                # Use original_name for file, but could use full_name if needed
                img_filename = layer.get('full_name', layer['original_name'])
                # Sanitize filename
                img_filename = img_filename.replace(
                    '/', '_').replace('\\', '_')
                img_path = os.path.join(images_folder, f"{img_filename}.png")
                layer["image_data"].save(img_path, "PNG")
                self.log(f"  ✓ Saved: {img_filename}.png")

        # Generate Spine JSON
        spine_data = {
            "skeleton": {
                "hash": hashlib.md5(character_name.encode()).hexdigest()[:8],
                "spine": "4.1",
                "x": 0,
                "y": 0,
                "width": self.doc_width,
                "height": self.doc_height,
                "images": "./images/",
                "audio": ""
            },
            "bones": self.generate_bones(use_hierarchy),
            "slots": self.generate_slots(),
            "skins": [
                {
                    "name": "default",
                    "attachments": self.generate_attachments()
                }
            ],
            "animations": {
                "animation": {}
            }
        }

        # Add IK constraints
        ik = self.generate_ik_constraints(create_ik)
        if ik:
            spine_data["ik"] = ik
            self.log(f"  ✓ Added {len(ik)} IK constraints")

        # Save JSON
        json_path = os.path.join(project_folder, f"{character_name}.json")
        with open(json_path, 'w', encoding='utf-8') as f:
            json.dump(spine_data, f, indent=2, ensure_ascii=False)

        self.log(f"  ✓ Saved: {character_name}.json")

        # Create README
        self.create_readme(project_folder, character_name, len(self.layers))

        self.log(f"✅ Export complete! Project at: {project_folder}")
        return project_folder

    def create_readme(self, project_folder, char_name, layer_count):
        """Create import instructions"""
        group_info = ""
        if hasattr(self, 'groups_info') and self.groups_info:
            group_info = f"\n📁 PSD Groups: {len(self.groups_info)} groups were processed"
            merged = sum(1 for l in self.layers if l.get('is_merged_group'))
            if merged > 0:
                group_info += f"\n   • {merged} groups were merged into single images"

        readme = f"""
╔══════════════════════════════════════════════════════════════╗
║        SPINE IMPORT GUIDE - {char_name.upper()}
╚══════════════════════════════════════════════════════════════╝

✅ PROJECT READY FOR IMPORT

📁 Location: {project_folder}

📦 Contents:
   • {char_name}.json          ← Skeleton data
   • images/                    ← {layer_count} PNG files
   • README.txt                 ← This file{group_info}

═══════════════════════════════════════════════════════════════

🎯 HOW TO IMPORT IN SPINE EDITOR:

1. Open Spine Editor (4.x)

2. Import Project:
   → Spine Menu → Import Data (Ctrl+I)
   → Select: "Folder"  ⚠️ IMPORTANT - NOT "JSON file"!
   → Browse to: {project_folder}
   → Click Import

3. All images will load automatically!

═══════════════════════════════════════════════════════════════

💡 TIPS:

• Press F to frame skeleton in view
• Switch between Setup and Animate modes with Tab
• Use IK handles for easy posing
• Groups were {'merged' if any(l.get('is_merged_group') for l in self.layers) else 'expanded into individual bones'}

═══════════════════════════════════════════════════════════════
Generated by Spine Auto-Rig Tool
"""

        readme_path = os.path.join(project_folder, "README.txt")
        with open(readme_path, 'w', encoding='utf-8') as f:
            f.write(readme)


###############################################################################
# UTILITY FUNCTIONS
###############################################################################
def check_dependencies():
    """Check which optional dependencies are available"""
    deps = {
        "psd-tools": PSDTOOLS_AVAILABLE,
        "mediapipe": MEDIAPIPE_AVAILABLE,
        "opencv-python": True,  # Already imported
        "pillow": True,  # Already imported
        "numpy": True  # Already imported
    }
    return deps

# rig_spine_export.py
import json
import os

BONE_PARENT = {
    "root": None, "hip": "root", "torso": "hip", "neck": "torso", "head": "neck",
    "upper_arm_L": "torso", "forearm_L": "upper_arm_L", "hand_L": "forearm_L",
    "upper_arm_R": "torso", "forearm_R": "upper_arm_R", "hand_R": "forearm_R",
    "upper_leg_L": "hip", "lower_leg_L": "upper_leg_L", "foot_L": "lower_leg_L",
    "upper_leg_R": "hip", "lower_leg_R": "upper_leg_R", "foot_R": "lower_leg_R"
}

DEFAULT_BONES_ORDER = [
    "root", "hip", "torso", "neck", "head",
    "upper_arm_L", "forearm_L", "hand_L",
    "upper_arm_R", "forearm_R", "hand_R",
    "upper_leg_L", "lower_leg_L", "foot_L",
    "upper_leg_R", "lower_leg_R", "foot_R"
]

WEIGHT_MAP = {
    "head": "head", "torso": "torso", "hip": "hip",
    "upper_arm_L": "upper_arm_L", "forearm_L": "forearm_L", "hand_L": "hand_L",
    "upper_arm_R": "upper_arm_R", "forearm_R": "forearm_R", "hand_R": "hand_R",
    "upper_leg_L": "upper_leg_L", "lower_leg_L": "lower_leg_L", "foot_L": "foot_L",
    "upper_leg_R": "upper_leg_R", "lower_leg_R": "lower_leg_R", "foot_R": "foot_R"
}


class SpineExporter:
    def __init__(self, layers, selected, doc_size=(512, 512), enable_ik=True):
        self.layers = [L for L in layers if L['name'] in selected]
        self.selected = selected
        self.doc_w, self.doc_h = doc_size
        self.enable_ik = enable_ik

    def export(self, path):
        folder = os.path.dirname(path)
        os.makedirs(folder, exist_ok=True)

        skeleton = {"skeleton": {"spine": "4.1", "width": self.doc_w, "height": self.doc_h, "images": "./images/"},
                    "bones": [], "slots": [], "skins": [], "animations": {}}

        bones = []
        for name in DEFAULT_BONES_ORDER:
            b = {"name": name}
            parent = BONE_PARENT.get(name)
            if parent:
                b["parent"] = parent
            layer = next((L for L in self.layers if L['name'] == name), None)
            if layer:
                b["x"] = layer.get("x", 0)
                b["y"] = layer.get("y", 0)
                b["length"] = max(
                    10, int(max(layer.get("width", 50), layer.get("height", 50))/2))
            bones.append(b)
        skeleton["bones"] = bones

        slots = []
        default_skin = {}
        existing_slot_names = []

        def unique_slot(n):
            if n not in existing_slot_names:
                existing_slot_names.append(n)
                return n
            cnt = 1
            while True:
                new = f"{n}_{cnt}"
                if new not in existing_slot_names:
                    existing_slot_names.append(new)
                    return new
                cnt += 1

        for L in self.layers:
            name = L['name']
            target = WEIGHT_MAP.get(name, "torso")
            slot_name = unique_slot(name)
            slots.append(
                {"name": slot_name, "bone": target, "attachment": name})
            w = int(L.get("width", 100))
            h = int(L.get("height", 100))
            att = {"name": name, "type": "region", "x": 0, "y": 0,
                   "rotation": 0, "width": w, "height": h, "path": name}
            default_skin[slot_name] = {name: att}

        skeleton["slots"] = slots
        skeleton["skins"] = [{"name": "default", "attachments": default_skin}]

        if self.enable_ik:
            skeleton["ik"] = [
                {"name": "IK_L", "bones": [
                    "upper_arm_L", "forearm_L"], "target": "hand_L", "mix": 1},
                {"name": "IK_R", "bones": [
                    "upper_arm_R", "forearm_R"], "target": "hand_R", "mix": 1}
            ]

        with open(path, "w", encoding="utf-8") as f:
            json.dump(skeleton, f, indent=2, ensure_ascii=False)

        # write a helper mapping for debug
        with open(os.path.join(folder, "_weight_map.txt"), "w", encoding="utf-8") as wf:
            for L in self.layers:
                wf.write(
                    f"{L['name']} -> {WEIGHT_MAP.get(L['name'], 'torso')}\n")

        return True

# smart_layer_map.py
# Maps common layer names (English + Spanish) to standard Spine bone names.
# Used by both the GUI (auto_rig_tool) and the core pipeline (spine_rig_core).

MAP = {
    # Head / neck
    "head":         ["head", "face", "cabeza", "tete"],
    "neck":         ["neck", "cuello", "cou"],

    # Torso / hip
    "torso":        ["torso", "body", "chest", "belly", "spine", "waist",
                     "trunk", "cuerpo", "abdomen", "bod"],
    "hip":          ["hip", "pelvis", "hips", "root", "cadera"],

    # Left arm
    "shoulder_L":   ["left shoulder", "shoulder l", "l shoulder", "hombro izq"],
    "upper_arm_L":  ["left arm", "arm l", "l arm", "left upper arm",
                     "upper arm l", "brazo izq", "upper_arm_l", "left_upper_arm"],
    "forearm_L":    ["left forearm", "l forearm", "forearm l", "lower arm l",
                     "left lower arm", "forearm_l"],
    "hand_L":       ["left hand", "hand l", "l hand", "mano izq",
                     "hand_l", "left_hand"],

    # Right arm
    "shoulder_R":   ["right shoulder", "shoulder r", "r shoulder", "hombro der"],
    "upper_arm_R":  ["right arm", "arm r", "r arm", "right upper arm",
                     "upper arm r", "brazo der", "upper_arm_r", "right_upper_arm"],
    "forearm_R":    ["right forearm", "r forearm", "forearm r", "lower arm r",
                     "right lower arm", "forearm_r"],
    "hand_R":       ["right hand", "hand r", "r hand", "mano der",
                     "hand_r", "right_hand"],

    # Left leg
    "upper_leg_L":  ["left leg", "leg l", "thigh l", "upper leg l",
                     "left thigh", "pierna izq", "upper_leg_l"],
    "lower_leg_L":  ["left shin", "shin l", "lower leg l", "left calf",
                     "pantorrilla izq", "lower_leg_l"],
    "foot_L":       ["left foot", "foot l", "l foot", "pie izq", "foot_l"],

    # Right leg
    "upper_leg_R":  ["right leg", "leg r", "thigh r", "upper leg r",
                     "right thigh", "pierna der", "upper_leg_r"],
    "lower_leg_R":  ["right shin", "shin r", "lower leg r", "right calf",
                     "pantorrilla der", "lower_leg_r"],
    "foot_R":       ["right foot", "foot r", "r foot", "pie der", "foot_r"],
}


def smart_remap_name(name: str) -> str:
    """
    Map a layer name to a standard Spine bone name.
    Case-insensitive. Returns original name if no match found.
    """
    if not name:
        return name
    n = name.lower().replace("_", " ").replace("-", " ").strip()
    for target, synonyms in MAP.items():
        for s in synonyms:
            if s == n or s in n:
                return target
    return name

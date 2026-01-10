# smart_layer_map.py
MAP = {
    "head": ["head", "face"],
    "torso": ["body", "torso", "chest", "abdomen", "bod"],
    "hip": ["hip", "pelvis", "hips", "root"],
    "upper_arm_L": ["left arm", "arm l", "l arm", "left_upper_arm", "upper arm left", "shoulder l"],
    "forearm_L": ["left forearm", "lower arm l", "forearm_l"],
    "hand_L": ["left hand", "hand_l"],
    "upper_arm_R": ["right arm", "arm r", "r arm", "right_upper_arm", "shoulder r"],
    "forearm_R": ["right forearm", "lower arm r", "forearm_r"],
    "hand_R": ["right hand", "hand_r"],
    "upper_leg_L": ["left leg", "leg l", "thigh l"],
    "lower_leg_L": ["left shin", "shin l", "lower_leg_l"],
    "foot_L": ["left foot", "foot l"],
    "upper_leg_R": ["right leg", "leg r", "thigh r"],
    "lower_leg_R": ["right shin", "shin r", "lower_leg_r"],
    "foot_R": ["right foot", "foot r"]
}


def smart_remap_name(name):
    if not name:
        return name
    n = name.lower().replace("_", " ").replace("-", " ").strip()
    for target, synonyms in MAP.items():
        for s in synonyms:
            if s in n:
                return target
    return name

# image_cut_tools.py
import cv2
import numpy as np


def auto_cut_parts_from_image(path, min_area=50):
    """
    Alpha-based segmentation.
    Returns list of parts: {name,x,y,w,h,image(np BGRA)}
    """
    img = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    if img is None:
        raise Exception("Image load failed: " + path)
    if img.shape[2] < 4:
        # no alpha channel -> threshold
        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        _, alpha = cv2.threshold(gray, 10, 255, cv2.THRESH_BINARY)
    else:
        alpha = img[:, :, 3]

    # morphological open to remove tiny noise
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    alpha = cv2.morphologyEx(alpha, cv2.MORPH_OPEN, kernel, iterations=1)

    contours, _ = cv2.findContours(
        alpha, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    parts = []
    for i, cnt in enumerate(contours):
        area = cv2.contourArea(cnt)
        if area < min_area:
            continue
        x, y, w, h = cv2.boundingRect(cnt)
        # pad 2px
        x = max(0, x-2)
        y = max(0, y-2)
        w += 4
        h += 4
        part_img = img[y:y+h, x:x+w].copy()
        parts.append({"name": f"part_{i}", "x": int(x), "y": int(
            y), "w": int(w), "h": int(h), "image": part_img})
    parts.sort(key=lambda p: p['y'])  # top->down
    return parts

"""
Run:  python3 process_hearts.py <path_to_sprite_sheet>
Splits the 2-column × 3-row heart-bar sprite sheet into 6 individual PNGs
with the background removed, saved to Assets/Resources/Hearts/.

Expected grid (left→right, top→bottom):
  hearts_5  hearts_4
  hearts_3  hearts_2
  hearts_1  hearts_0
"""

import sys
import os
import numpy as np
from PIL import Image

LABELS = ["hearts_5", "hearts_4", "hearts_3", "hearts_2", "hearts_1", "hearts_0"]
OUT_DIR = os.path.join(os.path.dirname(__file__), "Assets", "Resources", "Hearts")
COLS, ROWS = 2, 3


def remove_checkerboard_bg(img: Image.Image) -> Image.Image:
    """Convert checkerboard (gray/white) background pixels to transparent."""
    img = img.convert("RGBA")
    data = np.array(img, dtype=np.float32)
    r, g, b, a = data[..., 0], data[..., 1], data[..., 2], data[..., 3]

    # Checkerboard squares are near-gray: R≈G≈B and both near 200 or 255.
    gray = (np.abs(r - g) < 15) & (np.abs(g - b) < 15)
    light = r > 170
    bg_mask = gray & light

    data[bg_mask, 3] = 0
    return Image.fromarray(data.astype(np.uint8), "RGBA")


def crop_to_content(img: Image.Image, padding: int = 6) -> Image.Image:
    data = np.array(img)
    alpha = data[..., 3]
    ys = np.where(np.any(alpha > 20, axis=1))[0]
    xs = np.where(np.any(alpha > 20, axis=0))[0]
    if len(ys) == 0 or len(xs) == 0:
        return img
    y0, y1 = max(0, ys[0] - padding), min(img.height, ys[-1] + padding + 1)
    x0, x1 = max(0, xs[0] - padding), min(img.width, xs[-1] + padding + 1)
    return img.crop((x0, y0, x1, y1))


def process(src_path: str) -> None:
    os.makedirs(OUT_DIR, exist_ok=True)

    img = Image.open(src_path).convert("RGBA")
    w, h = img.size
    cell_w, cell_h = w // COLS, h // ROWS

    idx = 0
    for row in range(ROWS):
        for col in range(COLS):
            x0, y0 = col * cell_w, row * cell_h
            x1, y1 = x0 + cell_w, y0 + cell_h
            cell = img.crop((x0, y0, x1, y1))

            cell = remove_checkerboard_bg(cell)
            cell = crop_to_content(cell)

            out = os.path.join(OUT_DIR, f"{LABELS[idx]}.png")
            cell.save(out, "PNG")
            print(f"  Saved {LABELS[idx]}.png  ({cell.width}×{cell.height})")
            idx += 1

    print(f"\nAll 6 heart sprites saved to:\n  {OUT_DIR}")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python3 process_hearts.py <path_to_sprite_sheet.png>")
        sys.exit(1)
    process(sys.argv[1])

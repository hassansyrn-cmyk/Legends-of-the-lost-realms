"""Extract isolated cutout-rig parts from a generated source board.

The source board may contain a light gray AI-preview checkerboard. Only the
edge-connected neutral background is removed, which protects ivory costume
details enclosed by the painted silhouette.
"""

from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


ASTER_ZONES = {
    "head": (0, 0, 430, 370),
    "torso": (400, 0, 1110, 625),
    "arm_back": (1130, 0, 1390, 500),
    "arm_front": (0, 340, 360, 880),
    "leg_back": (350, 470, 720, 1024),
    "leg_front": (880, 470, 1230, 1024),
    "sword": (1180, 350, 1536, 1024),
}


def is_preview_background(pixel: tuple[int, int, int, int]) -> bool:
    red, green, blue, _ = pixel
    return min(red, green, blue) >= 218 and max(red, green, blue) - min(red, green, blue) <= 14


def remove_connected_background(image: Image.Image) -> Image.Image:
    result = image.convert("RGBA")
    pixels = result.load()
    width, height = result.size
    queue: deque[tuple[int, int]] = deque()
    seen: set[tuple[int, int]] = set()
    for x in range(width):
        queue.append((x, 0))
        queue.append((x, height - 1))
    for y in range(height):
        queue.append((0, y))
        queue.append((width - 1, y))
    while queue:
        x, y = queue.popleft()
        if (x, y) in seen or not is_preview_background(pixels[x, y]):
            continue
        seen.add((x, y))
        red, green, blue, _ = pixels[x, y]
        pixels[x, y] = (red, green, blue, 0)
        if x: queue.append((x - 1, y))
        if x + 1 < width: queue.append((x + 1, y))
        if y: queue.append((x, y - 1))
        if y + 1 < height: queue.append((x, y + 1))
    return result


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    cleaned = remove_connected_background(Image.open(args.source))
    args.output.mkdir(parents=True, exist_ok=True)
    for name, zone in ASTER_ZONES.items():
        part = cleaned.crop(zone)
        bounds = part.getchannel("A").getbbox()
        if not bounds:
            raise SystemExit(f"No visible pixels found for {name}")
        padded = part.crop(bounds)
        padded.save(args.output / f"aster_{name}.png", optimize=True)
    print(f"Extracted {len(ASTER_ZONES)} modular Aster parts to {args.output}")


if __name__ == "__main__":
    main()

"""Rebuild runtime sheets with readable in-cell pose changes and fixed foot anchors.

The source paintings are intentionally preserved.  Motion is created by local,
feathered deformation inside each sprite (upper/lower body and side-to-side
silhouette changes), rather than translating or scaling the complete cell.
"""

from pathlib import Path
from io import BytesIO
import math
import subprocess

from PIL import Image, ImageEnhance


ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "app/src/main/res/drawable-nodpi"


def clean_source(filename: str) -> Image.Image:
    relative = f"app/src/main/res/drawable-nodpi/{filename}"
    content = subprocess.check_output(["git", "show", f"HEAD:{relative}"], cwd=ROOT)
    return Image.open(BytesIO(content)).convert("RGBA")


def frame(sheet: Image.Image, column: int, row: int, width: int, height: int) -> Image.Image:
    return sheet.crop((column * width, row * height, (column + 1) * width, (row + 1) * height))


def alpha_box(image: Image.Image):
    box = image.getchannel("A").getbbox()
    if box is None:
        raise RuntimeError("Sprite frame is empty")
    return box


def anchored_warp(source: Image.Image, bend: float, stride: float, breathe: float) -> Image.Image:
    """Warp the painted pose while keeping the bottom contact pixels stationary."""
    source = source.convert("RGBA")
    width, height = source.size
    contact = alpha_box(source)[3] - 1
    strip_height = 8
    mesh = []

    def offsets(y: float):
        normalized = max(0.0, min(1.0, (contact - y) / max(1.0, contact)))
        upper = max(0.0, min(1.0, (normalized - .28) / .72))
        lower = math.sin(max(0.0, min(1.0, normalized / .46)) * math.pi)
        return bend * upper * upper + stride * lower, breathe * math.sin(normalized * math.pi)

    # Pillow's mesh transform maps each destination strip to a source quadrilateral.
    for top in range(0, height, strip_height):
        bottom = min(height, top + strip_height)
        top_x, top_y = offsets(top)
        bottom_x, bottom_y = offsets(bottom)
        mesh.append((
            (0, top, width, bottom),
            (-top_x, top - top_y, -bottom_x, bottom - bottom_y,
             width - bottom_x, bottom - bottom_y, width - top_x, top - top_y),
        ))
    warped = source.transform(source.size, Image.Transform.MESH, mesh, Image.Resampling.BICUBIC)
    # Cubic sampling can leave faint edge pixels even when the authored frame had
    # breathing room. Keep a transparent guard band so adjacent cells never bleed.
    alpha = warped.getchannel("A")
    guard = Image.new("L", warped.size, 255)
    guard.paste(0, (0, 0, width, 3))
    guard.paste(0, (0, height - 3, width, height))
    guard.paste(0, (0, 0, 3, height))
    guard.paste(0, (width - 3, 0, width, height))
    alpha = Image.composite(alpha, Image.new("L", warped.size, 0), guard)
    warped.putalpha(alpha)
    return warped


def fit_bottom(image: Image.Image, cell_w: int, cell_h: int, margin: int = 10) -> Image.Image:
    box = alpha_box(image)
    subject = image.crop(box)
    scale = min((cell_w - margin * 2) / subject.width, (cell_h - margin * 2) / subject.height)
    size = (max(1, round(subject.width * scale)), max(1, round(subject.height * scale)))
    subject = subject.resize(size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
    canvas.alpha_composite(subject, ((cell_w - subject.width) // 2, cell_h - margin - subject.height))
    return canvas


def align_bottom(image: Image.Image, target_bottom: int) -> Image.Image:
    box = alpha_box(image)
    offset_y = target_bottom - box[3]
    canvas = Image.new("RGBA", image.size, (0, 0, 0, 0))
    canvas.alpha_composite(image, (0, offset_y))
    return canvas


def validate_pose_sheet(path: Path, cell_w: int, cell_h: int, rows: int, columns: int) -> None:
    """Fail generation if a pose loop starts resizing or losing its contact line."""
    sheet = Image.open(path).convert("RGBA")
    for row in range(rows):
        boxes = []
        for column in range(columns):
            pose = frame(sheet, column, row, cell_w, cell_h)
            box = alpha_box(pose)
            if box[0] <= 0 or box[1] <= 0 or box[2] >= cell_w or box[3] >= cell_h:
                raise RuntimeError(f"{path.name}: pose touches cell edge at row {row}, column {column}")
            boxes.append(box)
        bottoms = [box[3] for box in boxes]
        widths = [box[2] - box[0] for box in boxes]
        heights = [box[3] - box[1] for box in boxes]
        if max(bottoms) - min(bottoms) > max(8, int(cell_h * .09)):
            raise RuntimeError(f"{path.name}: unstable contact line in row {row}: {bottoms}")
        if (max(widths) - min(widths)) / max(widths) > .24:
            raise RuntimeError(f"{path.name}: unstable footprint width in row {row}: {widths}")
        if (max(heights) - min(heights)) / max(heights) > .24:
            raise RuntimeError(f"{path.name}: unstable footprint height in row {row}: {heights}")


def rebuild_aster() -> None:
    path = RES / "aster_motion_sheet.png"
    old = clean_source(path.name)
    cell = 256
    output = Image.new("RGBA", old.size, (0, 0, 0, 0))
    poses = {
        0: [(-3, 0, -2), (0, 1, 0), (4, -1, 3), (0, 0, 1), (-4, 1, -2), (0, -1, 0), (3, 0, 2), (0, 0, 0)],
        1: [(-12, 10, 1), (-6, -10, -2), (5, 13, 1), (12, -12, 0), (8, 10, -1), (0, -11, 2), (-9, 13, 0), (-14, -9, -1)],
        2: [(-16, -4, 0), (-8, 2, -1), (2, 7, 1), (13, 10, 0), (21, 5, -2), (13, -2, 1), (2, -7, 0), (-9, -3, 1)],
        3: [(14, -8, -1), (9, 5, 1), (2, 9, 0), (-7, 6, -2), (-13, 0, 1), (-7, -5, 0), (1, -8, -1), (8, -3, 0)],
    }
    for row in range(4):
        for column, pose in enumerate(poses[row]):
            source = frame(old, column, row, cell, cell)
            warped = align_bottom(anchored_warp(source, *pose), cell - 10)
            output.alpha_composite(warped, (column * cell, row * cell))
    output.save(path)
    validate_pose_sheet(path, cell, cell, 4, 8)


def rebuild_character_sheet(filename: str, cell: int, rows: int) -> None:
    path = RES / filename
    old = clean_source(path.name)
    output = Image.new("RGBA", old.size, (0, 0, 0, 0))
    # Six authored meanings: idle A, travel A, travel B, telegraph, strike, recoil.
    poses = [(0, 0, -2), (-10, 9, 1), (10, -9, 2), (-15, -3, 0), (22, 8, -1), (-18, 11, 2)]
    multiplier = cell / 256.0
    for row in range(rows):
        for column, pose in enumerate(poses):
            source = frame(old, column, row, cell, cell)
            scaled = tuple(value * multiplier for value in pose)
            warped = align_bottom(anchored_warp(source, *scaled), cell - 10)
            output.alpha_composite(warped, (column * cell, row * cell))
    output.save(path)
    validate_pose_sheet(path, cell, cell, rows, 6)


def rebuild_world() -> None:
    path = RES / "world_motion_sheet.png"
    old = clean_source(path.name)
    cell_w, cell_h = 192, 256
    output = Image.new("RGBA", old.size, (0, 0, 0, 0))
    for row in range(4):
        for column in range(4):
            source = frame(old, column, row, cell_w, cell_h)
            if row < 3:
                # Root/flame/spike silhouettes alternately open left and right.
                bend = (-12, -4, 8, 15)[column]
                stride = (5, -7, 9, -5)[column]
                warped = anchored_warp(source, bend, stride, (0, -2, 2, 0)[column])
            else:
                # Flag cloth visibly waves; the pole/base remains locked.
                warped = anchored_warp(source, (-9, 8, -12, 12)[column], 0, (0, 1, -1, 0)[column])
                if column >= 2:
                    rgb = ImageEnhance.Color(warped).enhance(1.22)
                    warped = ImageEnhance.Brightness(rgb).enhance(1.10)
            warped = align_bottom(warped, cell_h - 10)
            output.alpha_composite(warped, (column * cell_w, row * cell_h))
    output.save(path)
    validate_pose_sheet(path, cell_w, cell_h, 4, 4)


if __name__ == "__main__":
    rebuild_aster()
    rebuild_character_sheet("enemies_motion_sheet.png", 192, 8)
    rebuild_character_sheet("bosses_motion_sheet.png", 384, 3)
    rebuild_world()
    print("Rebuilt Aster, enemy, boss, trap, and checkpoint sheets with anchored pose changes.")
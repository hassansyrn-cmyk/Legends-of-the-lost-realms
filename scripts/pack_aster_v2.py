from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
RENDERS = ROOT / "art" / "blender" / "renders" / "aster_v2"
CELL = 256
ROWS = ("idle", "run", "jump", "attack")
FRAMES_PER_ROW = 8


def checker(size, tile=20):
    image = Image.new("RGBA", size, (20, 28, 32, 255))
    draw = ImageDraw.Draw(image)
    colors = ((24, 37, 42, 255), (34, 50, 55, 255))
    for y in range(0, size[1], tile):
        for x in range(0, size[0], tile):
            draw.rectangle((x, y, x + tile, y + tile), fill=colors[(x // tile + y // tile) % 2])
    return image


atlas = Image.new("RGBA", (CELL * FRAMES_PER_ROW, CELL * len(ROWS)), (0, 0, 0, 0))
for row_index, action in enumerate(ROWS):
    for frame_index in range(FRAMES_PER_ROW):
        source = RENDERS / action / f"{action}_{frame_index:02d}.png"
        if not source.exists():
            raise FileNotFoundError(source)
        frame = Image.open(source).convert("RGBA").resize((CELL, CELL), Image.Resampling.LANCZOS)
        atlas.alpha_composite(frame, (frame_index * CELL, row_index * CELL))

atlas_path = RENDERS / "aster_v2_motion_sheet.png"
atlas.save(atlas_path, optimize=True)

margin_left = 86
margin_top = 30
scale = .5
preview_size = (margin_left + int(atlas.width * scale), margin_top + int(atlas.height * scale))
preview = checker(preview_size)
small_atlas = atlas.resize((int(atlas.width * scale), int(atlas.height * scale)), Image.Resampling.LANCZOS)
preview.alpha_composite(small_atlas, (margin_left, margin_top))
draw = ImageDraw.Draw(preview)
draw.text((margin_left, 8), "ASTER V2 - PHASE 2 - BLENDER MOTION PROOF", fill=(219, 242, 244, 255))
for row_index, action in enumerate(ROWS):
    y = margin_top + int((row_index + .5) * CELL * scale) - 7
    draw.text((12, y), action.upper(), fill=(105, 225, 224, 255))
preview_path = RENDERS / "aster_v2_contact_sheet.png"
preview.save(preview_path, optimize=True)

motion_frames = []
for action in ROWS:
    action_frames = []
    for frame_index in range(FRAMES_PER_ROW):
        source = RENDERS / action / f"{action}_{frame_index:02d}.png"
        stage = checker((512, 544), tile=32)
        character = Image.open(source).convert("RGBA")
        stage.alpha_composite(character, (0, 32))
        stage_draw = ImageDraw.Draw(stage)
        stage_draw.text((18, 10), f"ASTER V2 - PHASE 2 - {action.upper()}", fill=(155, 242, 237, 255))
        action_frames.append(stage.convert("P", palette=Image.Palette.ADAPTIVE, colors=192))
    motion_frames.extend(action_frames)
    motion_frames.extend([action_frames[-1]] * 2)

gif_path = RENDERS / "aster_v2_motion_proof.gif"
motion_frames[0].save(gif_path, save_all=True, append_images=motion_frames[1:],
                      duration=90, loop=0, disposal=2, optimize=False)

print(atlas_path)
print(preview_path)
print(gif_path)

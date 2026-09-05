from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
RENDERS = ROOT / "art" / "blender" / "renders" / "aster_package_v1"
CELL = 256
ROWS = ("idle", "run", "jump", "attack")
FRAMES = 8


def checker(size, tile=20):
    image = Image.new("RGBA", size, (20, 28, 32, 255))
    draw = ImageDraw.Draw(image)
    colors = ((24, 37, 42, 255), (34, 50, 55, 255))
    for y in range(0, size[1], tile):
        for x in range(0, size[0], tile):
            draw.rectangle((x, y, x + tile, y + tile),
                           fill=colors[(x // tile + y // tile) % 2])
    return image


atlas = Image.new("RGBA", (CELL * FRAMES, CELL * len(ROWS)), (0, 0, 0, 0))
source_frames = {}
for row_index, action in enumerate(ROWS):
    source_frames[action] = []
    for frame_index in range(FRAMES):
        source = RENDERS / action / f"{action}_{frame_index:02d}.png"
        if not source.exists():
            raise FileNotFoundError(source)
        frame = Image.open(source).convert("RGBA")
        source_frames[action].append(frame)
        cell = frame.resize((CELL, CELL), Image.Resampling.LANCZOS)
        atlas.alpha_composite(cell, (frame_index * CELL, row_index * CELL))

atlas_path = RENDERS / "aster_motion_sheet.png"
atlas.save(atlas_path, optimize=True)

margin_left = 86
margin_top = 30
preview = checker((margin_left + 1024, margin_top + 512))
preview.alpha_composite(atlas.resize((1024, 512), Image.Resampling.LANCZOS),
                        (margin_left, margin_top))
draw = ImageDraw.Draw(preview)
draw.text((margin_left, 8), "ASTER FINAL RIG - ANDROID MOTION PROOF",
          fill=(219, 242, 244, 255))
for row_index, action in enumerate(ROWS):
    y = margin_top + int((row_index + .5) * CELL * .5) - 7
    draw.text((12, y), action.upper(), fill=(105, 225, 224, 255))
contact_path = RENDERS / "aster_rig_contact_sheet.png"
preview.save(contact_path, optimize=True)

gif_frames = []
for action in ROWS:
    for frame in source_frames[action]:
        stage = checker((512, 544), tile=32)
        stage.alpha_composite(frame, (0, 32))
        stage_draw = ImageDraw.Draw(stage)
        stage_draw.text((18, 10), f"ASTER FINAL RIG - {action.upper()}",
                        fill=(155, 242, 237, 255))
        gif_frames.append(stage.convert("P", palette=Image.Palette.ADAPTIVE, colors=192))

gif_path = RENDERS / "aster_rig_motion_proof.gif"
gif_frames[0].save(gif_path, save_all=True, append_images=gif_frames[1:],
                   duration=90, loop=0, disposal=2, optimize=False)

print(atlas_path)
print(contact_path)
print(gif_path)

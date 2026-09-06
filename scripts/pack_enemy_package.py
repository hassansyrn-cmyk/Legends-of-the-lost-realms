from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
RENDERS = ROOT / "art" / "blender" / "renders" / "enemy_package_v1"
RUNTIME = ROOT / "app" / "src" / "main" / "res" / "drawable-nodpi"
CHARACTERS = ("forest_goblin", "ember_demon", "forest_elemental")
ACTIONS = ("idle", "move", "attack", "hurt")
FRAMES = 12
ENEMY_CELL = 192
BOSS_CELL = 384


def checker(size, tile=18):
    image = Image.new("RGBA", size, (20, 28, 32, 255))
    draw = ImageDraw.Draw(image)
    colors = ((24, 37, 42, 255), (34, 50, 55, 255))
    for y in range(0, size[1], tile):
        for x in range(0, size[0], tile):
            draw.rectangle((x, y, x + tile, y + tile),
                           fill=colors[(x // tile + y // tile) % 2])
    return image


def source_frame(character, action, frame):
    path = RENDERS / character / action / f"{action}_{frame:02d}.png"
    if not path.exists():
        raise FileNotFoundError(path)
    return Image.open(path).convert("RGBA")


enemy_atlas = Image.new(
    "RGBA",
    (ENEMY_CELL * FRAMES, ENEMY_CELL * len(CHARACTERS) * len(ACTIONS)),
    (0, 0, 0, 0),
)
for character_index, character in enumerate(CHARACTERS):
    for action_index, action in enumerate(ACTIONS):
        row = character_index * len(ACTIONS) + action_index
        for frame_index in range(FRAMES):
            frame = source_frame(character, action, frame_index)
            cell = frame.resize((ENEMY_CELL, ENEMY_CELL), Image.Resampling.LANCZOS)
            enemy_atlas.alpha_composite(cell, (frame_index * ENEMY_CELL, row * ENEMY_CELL))

enemy_path = RENDERS / "enemy_rig_motion_sheet.png"
enemy_atlas.save(enemy_path, optimize=True)
enemy_atlas.save(RUNTIME / "enemy_rig_motion_sheet.png", optimize=True)

# The forest elemental doubles as the first upgraded boss. Eight high-resolution
# frames per state keep the atlas below common 4096px mobile texture limits.
boss_indices = (0, 2, 3, 5, 6, 8, 10, 11)
boss_atlas = Image.new(
    "RGBA", (BOSS_CELL * len(boss_indices), BOSS_CELL * len(ACTIONS)), (0, 0, 0, 0)
)
for action_index, action in enumerate(ACTIONS):
    for packed_index, source_index in enumerate(boss_indices):
        frame = source_frame("forest_elemental", action, source_index)
        cell = frame.resize((BOSS_CELL, BOSS_CELL), Image.Resampling.LANCZOS)
        boss_atlas.alpha_composite(cell, (packed_index * BOSS_CELL, action_index * BOSS_CELL))

boss_path = RENDERS / "forest_elemental_boss_sheet.png"
boss_atlas.save(boss_path, optimize=True)
boss_atlas.save(RUNTIME / "forest_elemental_boss_sheet.png", optimize=True)

label_width = 132
preview_cell = 96
contact = checker((label_width + preview_cell * FRAMES,
                   28 + preview_cell * len(CHARACTERS) * len(ACTIONS)))
draw = ImageDraw.Draw(contact)
draw.text((label_width, 7), "BLENDER RIG ENEMY MOTION PROOF", fill=(210, 244, 240, 255))
for character_index, character in enumerate(CHARACTERS):
    for action_index, action in enumerate(ACTIONS):
        row = character_index * len(ACTIONS) + action_index
        draw.text((8, 28 + row * preview_cell + 40),
                  f"{character.replace('_', ' ').upper()}\n{action.upper()}",
                  fill=(122, 231, 221, 255))
        for frame_index in range(FRAMES):
            frame = source_frame(character, action, frame_index)
            contact.alpha_composite(
                frame.resize((preview_cell, preview_cell), Image.Resampling.LANCZOS),
                (label_width + frame_index * preview_cell, 28 + row * preview_cell),
            )

contact_path = RENDERS / "enemy_rig_contact_sheet.png"
contact.save(contact_path, optimize=True)

for character in CHARACTERS:
    gif_frames = []
    for action in ACTIONS:
        for frame_index in range(FRAMES):
            source = source_frame(character, action, frame_index)
            stage = checker((320, 344), tile=24)
            stage.alpha_composite(source.resize((320, 320), Image.Resampling.LANCZOS), (0, 24))
            stage_draw = ImageDraw.Draw(stage)
            stage_draw.text((12, 6), f"{character.replace('_', ' ').upper()} - {action.upper()}",
                            fill=(180, 242, 235, 255))
            gif_frames.append(stage.convert("P", palette=Image.Palette.ADAPTIVE, colors=192))
    gif_path = RENDERS / f"{character}_motion_proof.gif"
    gif_frames[0].save(
        gif_path,
        save_all=True,
        append_images=gif_frames[1:],
        duration=78,
        loop=0,
        disposal=2,
        optimize=False,
    )

print(enemy_path)
print(boss_path)
print(contact_path)

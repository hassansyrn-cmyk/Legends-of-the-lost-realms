from pathlib import Path
from PIL import Image

ROOT = Path('/home/ubuntu/lost-realms')
ATLAS = ROOT / 'app/src/main/res/drawable-nodpi/boss_motion_atlas.png'
CELL = 544
COLS, ROWS = 8, 3
EXPECTED = (COLS * CELL, ROWS * CELL)

image = Image.open(ATLAS).convert('RGBA')
if image.size != EXPECTED:
    raise SystemExit(f'Unexpected atlas size: {image.size}; expected {EXPECTED}')

for row in range(ROWS):
    for col in range(COLS):
        frame = image.crop((col * CELL, row * CELL, (col + 1) * CELL, (row + 1) * CELL))
        alpha = frame.getchannel('A')
        bbox = alpha.getbbox()
        if bbox is None:
            raise SystemExit(f'Empty boss frame at row {row}, column {col}')
        left, top, right, bottom = bbox
        if left == 0 or right == CELL or top == 0 or bottom == CELL:
            raise SystemExit(
                f'Frame touches a cell edge at row {row}, column {col}: bbox={bbox}; possible crop'
            )
        opaque = 0
        magenta_opaque = 0
        black_opaque = 0
        for red, green, blue, a in frame.getdata():
            if a >= 220:
                opaque += 1
                if red >= 155 and blue >= 120 and green <= 105:
                    magenta_opaque += 1
                if red <= 12 and green <= 12 and blue <= 12:
                    black_opaque += 1
        if opaque == 0:
            raise SystemExit(f'No visible pixels in boss frame at row {row}, column {col}')
        if magenta_opaque / opaque > 0.01:
            raise SystemExit(f'Excess opaque magenta matte in row {row}, column {col}')
        if black_opaque / opaque > 0.28:
            raise SystemExit(f'Excess opaque black background in row {row}, column {col}')

print('Boss motion atlas alpha check: OK')
print('Verified 4352x1632 atlas, 24 non-empty frames, transparent margins, and no dominant opaque matte.')

from pathlib import Path
from PIL import Image

sheet = Image.open(Path('/home/ubuntu/lost-realms/app/src/main/res/drawable-nodpi/trap_platform_motion_sheet.png')).convert('RGBA')
for row in range(4):
    for column in range(6):
        cell = sheet.crop((column * 256, row * 256, (column + 1) * 256, (row + 1) * 256))
        alpha = cell.getchannel('A')
        bbox = alpha.getbbox()
        pixels = sum(value > 0 for value in alpha.getdata())
        print(f'row={row} column={column} alpha_bbox={bbox} alpha_pixels={pixels}')

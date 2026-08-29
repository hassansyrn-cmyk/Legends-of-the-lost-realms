from pathlib import Path
from PIL import Image

ROOT = Path('/home/ubuntu/lost-realms/app/src/main/res/drawable-nodpi')

run = Image.open(ROOT / 'action_fx_motion_atlas.png').convert('RGBA')
for frame in range(3):
    alpha = run.crop((frame * 544, 0, (frame + 1) * 544, 544)).getchannel('A')
    print(f'run_frame={frame} visible_bbox={alpha.getbbox()}')

aster = Image.open(ROOT / 'aster_run_sheet.png').convert('RGBA')
for frame in range(8):
    alpha = aster.crop((frame * 512, 0, (frame + 1) * 512, 512)).getchannel('A')
    print(f'aster_run_frame={frame} visible_bbox={alpha.getbbox()}')

boss = Image.open(ROOT / 'boss_motion_atlas.png').convert('RGBA')
for row in range(3):
    bounds = []
    for frame in range(8):
        alpha = boss.crop((frame * 544, row * 544, (frame + 1) * 544, (row + 1) * 544)).getchannel('A')
        bounds.append(alpha.getbbox())
    print(f'boss_row={row} visible_bboxes={bounds}')

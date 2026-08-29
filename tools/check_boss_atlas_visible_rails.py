from pathlib import Path
import numpy as np
from PIL import Image

atlas = np.asarray(Image.open('/home/ubuntu/lost-realms/app/src/main/res/drawable-nodpi/boss_motion_atlas.png').convert('RGBA'))
cell = 544
for row in range(3):
    for col in range(8):
        tile = atlas[row*cell:(row+1)*cell, col*cell:(col+1)*cell]
        alpha = tile[:, :, 3]
        max_run = 0
        max_row = -1
        for y in range(cell):
            run = 0
            row_best = 0
            for present in np.append(alpha[y] >= 128, False):
                if present:
                    run += 1
                    row_best = max(row_best, run)
                else:
                    run = 0
            if row_best > max_run:
                max_run, max_row = row_best, y
        print(f'cell {row},{col}: longest visible alpha run={max_run} px at y={max_row}')
        if max_run > 190:
            raise SystemExit(f'Visible guide rail remains in boss cell {row},{col}: {max_run}px')
print('Boss atlas visible-rail check: OK')

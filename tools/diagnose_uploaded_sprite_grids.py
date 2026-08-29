from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path('/home/ubuntu/lost-realms/art')


def report_alpha_sheet(name: str) -> None:
    rgba = np.asarray(Image.open(ROOT / name).convert('RGBA'))
    alpha = rgba[:, :, 3]
    # For the flattened trap sheet, treat bright neutral checkerboard as background.
    if name.startswith('uploaded_trap'):
        rgb = rgba[:, :, :3]
        foreground = ~((rgb.min(axis=2) >= 238) & ((rgb.max(axis=2) - rgb.min(axis=2)) <= 24))
    else:
        foreground = alpha > 16
    x_counts = foreground.sum(axis=0)
    y_counts = foreground.sum(axis=1)
    print(f'\n{name}: {rgba.shape[1]}x{rgba.shape[0]}')
    for columns in (6, 8):
        width = rgba.shape[1] // columns
        samples = [int(x_counts[min(rgba.shape[1] - 1, index * width)]) for index in range(columns + 1)]
        print(f'  candidate_columns={columns}, width={width}, boundary_alpha_counts={samples}')
    for rows in (3, 4):
        height = rgba.shape[0] // rows
        samples = [int(y_counts[min(rgba.shape[0] - 1, index * height)]) for index in range(rows + 1)]
        print(f'  candidate_rows={rows}, height={height}, boundary_alpha_counts={samples}')
    low_x = np.argsort(x_counts)[:24]
    low_y = np.argsort(y_counts)[:24]
    print(f'  lowest x-density positions={sorted(int(v) for v in low_x)}')
    print(f'  lowest y-density positions={sorted(int(v) for v in low_y)}')


for filename in ('uploaded_trap_platform_sheet.png', 'uploaded_checkpoint_sheet.png'):
    report_alpha_sheet(filename)

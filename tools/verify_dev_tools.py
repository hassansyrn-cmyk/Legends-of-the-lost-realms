from pathlib import Path

root = Path('/home/ubuntu/lost-realms/app/src/main/java/com/manus/lostrealms')
view = (root / 'GameView.java').read_text(encoding='utf-8')
renderer = (root / 'GameRenderer.java').read_text(encoding='utf-8')
ui = (root / 'UIRenderer.java').read_text(encoding='utf-8')

required_view = [
    'DEV_TOOLS = 9',
    'void drawDevTools(Canvas c)',
    'drawDevOverlay(c)',
    '"LEVEL " + (index + 1)',
    '"CLEAR SAVE"',
    'devStatsOverlay',
    'devCoordinatesOverlay',
    'if(a>=140&&a<150)startLevel(a-139)',
]
for marker in required_view:
    if marker not in view:
        raise SystemExit(f'Missing developer-tools marker: {marker}')
if 'case GameView.DEV_TOOLS:' not in renderer or 'case GameView.DEV_TOOLS:' not in ui:
    raise SystemExit('Developer tools are not routed through both renderers.')

print('Developer tools contract: OK')
print('Verified level jump actions, clear-save action, optional stats/coordinate overlays, and render routing.')

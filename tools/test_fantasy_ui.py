"""Source/asset guards only: not a substitute for Unity build/render verification."""
import json
import re
from pathlib import Path
from PIL import Image

root = Path("unity-3d")
assets = root / "Assets/Resources/FantasyUI"
source = (root / "Assets/Scripts/FantasyUI.cs").read_text()
names = set(re.findall(r'Sprite\("([^"]+)"\)', source))
names.update(["panel", "button", "button-active", "round", "round-primary",
              "portrait-frame", "minimap-frame", "joystick-base", "joystick-handle",
              "crest", "divider"])
names.update("icon-" + n for n in ["blade", "jump", "dodge", "power", "parry",
             "spell", "atlas", "sanctuary", "play", "restart", "pause", "new",
             "coin", "gem", "star"])
guids = set()
for name in names:
    path = assets / (name + ".png")
    im = Image.open(path)
    assert im.mode == "RGBA", path
    assert max(im.size) <= 512, path
    assert im.getextrema()[3][0] == 0, f"No transparent pixels: {path}"
    meta = Path(str(path) + ".meta").read_text()
    guid = re.search(r"guid: (\w+)", meta).group(1)
    assert guid not in guids, f"Duplicate GUID: {path}"
    guids.add(guid)
    assert "spriteMode: 1" in meta and "enableMipMap: 0" in meta, path
for font in ["Heading", "Body"]:
    assert (assets / f"Fonts/{font}.ttf").stat().st_size > 1000
    assert any((assets / f"Fonts/{font}-{suffix}.txt").exists() for suffix in ["LICENSE", "OFL"])
manifest = json.loads((root / "Packages/manifest.json").read_text())
assert manifest["dependencies"]["com.unity.ugui"] == "2.0.0"
assert "Screen.safeArea" in source and "GetWorldCorners" in source
assert "TextMeshProUGUI" in source and "VerticalLayoutGroup" in source
assert "FantasyPortrait.Capture" in source
print(f"Fantasy UI source/asset guards passed: {len(names)} transparent sprites, fonts, layout contracts.")
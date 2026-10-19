"""Prepare the licensed Arabic fonts for Unity's legacy Android text renderer.

Requires fonttools. Run from any directory after downloading the upstream TTFs
listed in Assets/Resources/UI/Fonts/NotoSansArabic-SOURCE.txt.
"""
from pathlib import Path
from fontTools.ttLib import TTFont
from fontTools.pens.ttGlyphPen import TTGlyphPen

fonts = Path(__file__).resolve().parents[1] / 'unity-3d/Assets/Resources/UI/Fonts'
for filename, family in [
    ('NotoSansArabicUI-Regular.ttf', 'Lost Realms Arabic UI'),
    ('LostRealmsArabicDisplay.ttf', 'Lost Realms Arabic Display'),
]:
    path = fonts / filename
    font = TTFont(path)
    glyph = 'invisibleBidiControl'
    if glyph not in font.getGlyphOrder():
        font.setGlyphOrder(font.getGlyphOrder() + [glyph])
    font['glyf'][glyph] = TTGlyphPen(None).glyph()
    font['hmtx'][glyph] = (0, 0)
    for table in font['cmap'].tables:
        if table.isUnicode():
            for codepoint in (0x202C, 0x202D):
                table.cmap[codepoint] = glyph
    # Derivative names avoid upstream reserved font names under SIL OFL.
    names = {1: family, 2: 'Regular', 3: family + ' Android UI 1',
             4: family, 6: family.replace(' ', '-'), 16: family, 17: 'Regular'}
    for record in font['name'].names:
        if record.nameID in names:
            record.string = names[record.nameID].encode(record.getEncoding())
    font.save(path)
    check = TTFont(path)
    for codepoint in (0x202C, 0x202D):
        name = check.getBestCmap()[codepoint]
        assert check['glyf'][name].numberOfContours == 0
        assert check['hmtx'][name] == (0, 0)
    print(f'{filename}: bidi controls have zero contours and zero advance')

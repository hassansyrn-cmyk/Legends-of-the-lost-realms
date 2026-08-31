"""Author fresh, state-aware runtime sprite sheets from simple vector primitives.

This deliberately does not read any existing runtime or premium sprite artwork.
The silhouettes are authored here at 3x resolution and downsampled to clean
anti-aliased transparent PNGs. Every pose has a semantic state and a locked
contact line, so a state change reads as animation rather than a warped image.
"""

from __future__ import annotations

import json
import math
from pathlib import Path
from typing import Iterable, Sequence

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "app/src/main/res/drawable-nodpi"
SCALE = 3


Color = tuple[int, int, int, int]


def rgba(rgb: tuple[int, int, int], alpha: int = 255) -> Color:
    return rgb[0], rgb[1], rgb[2], alpha


class Art:
    def __init__(self, width: int, height: int):
        self.width = width
        self.height = height
        self.image = Image.new("RGBA", (width * SCALE, height * SCALE), (0, 0, 0, 0))
        self.draw = ImageDraw.Draw(self.image)

    def xy(self, point: Sequence[float]) -> tuple[int, int]:
        return round(point[0] * SCALE), round(point[1] * SCALE)

    def box(self, box: Sequence[float]) -> tuple[int, int, int, int]:
        return tuple(round(value * SCALE) for value in box)  # type: ignore[return-value]

    def polygon(self, points: Iterable[Sequence[float]], fill: Color, outline: Color | None = None, width: float = 1):
        pts = [self.xy(point) for point in points]
        self.draw.polygon(pts, fill=fill)
        if outline:
            self.draw.line(pts + [pts[0]], fill=outline, width=max(1, round(width * SCALE)), joint="curve")

    def ellipse(self, box: Sequence[float], fill: Color, outline: Color | None = None, width: float = 1):
        self.draw.ellipse(self.box(box), fill=fill, outline=outline, width=max(1, round(width * SCALE)) if outline else 1)

    def rectangle(self, box: Sequence[float], fill: Color, outline: Color | None = None, width: float = 1):
        self.draw.rectangle(self.box(box), fill=fill, outline=outline, width=max(1, round(width * SCALE)) if outline else 1)

    def rounded(self, box: Sequence[float], radius: float, fill: Color, outline: Color | None = None, width: float = 1):
        self.draw.rounded_rectangle(
            self.box(box), radius=round(radius * SCALE), fill=fill, outline=outline,
            width=max(1, round(width * SCALE)) if outline else 1,
        )

    def line(self, points: Iterable[Sequence[float]], fill: Color, width: float):
        self.draw.line([self.xy(point) for point in points], fill=fill, width=max(1, round(width * SCALE)), joint="curve")

    def circle(self, center: Sequence[float], radius: float, fill: Color, outline: Color | None = None, width: float = 1):
        self.ellipse(
            (center[0] - radius, center[1] - radius, center[0] + radius, center[1] + radius),
            fill, outline, width,
        )

    def finish(self) -> Image.Image:
        clean = self.image.resize((self.width, self.height), Image.Resampling.LANCZOS)
        alpha = clean.getchannel("A").filter(ImageFilter.GaussianBlur(0.12))
        clean.putalpha(alpha)
        return clean


INK = rgba((14, 22, 34))
HIGHLIGHT = rgba((239, 248, 244))
HERO_CLOAK = rgba((30, 83, 91))
HERO_CLOAK_DARK = rgba((16, 43, 57))
HERO_CLOAK_LIGHT = rgba((66, 151, 149))
HERO_SCARF = rgba((238, 116, 64))
HERO_SKIN = rgba((218, 154, 111))
STEEL = rgba((191, 220, 224))
STEEL_LIGHT = rgba((238, 255, 247))


def centered_sheet(cell_w: int, cell_h: int, cols: int, rows: int, painter) -> Image.Image:
    sheet = Image.new("RGBA", (cell_w * cols, cell_h * rows), (0, 0, 0, 0))
    for row in range(rows):
        for column in range(cols):
            art = painter(row, column, cell_w, cell_h)
            sheet.alpha_composite(art.finish(), (column * cell_w, row * cell_h))
    return sheet


def draw_sword(art: Art, x: float, y: float, angle: float, length: float = 56):
    rad = math.radians(angle)
    tip = (x + math.cos(rad) * length, y + math.sin(rad) * length)
    side = (-math.sin(rad) * 5, math.cos(rad) * 5)
    art.polygon(
        [(x + side[0], y + side[1]), (tip[0], tip[1]),
         (tip[0] - side[0] * 0.6, tip[1] - side[1] * 0.6),
         (x - side[0], y - side[1])],
        STEEL_LIGHT, INK, 2,
    )
    art.line([(x, y), (x + math.cos(rad) * 18, y + math.sin(rad) * 18)], rgba((255, 207, 92)), 4)
    art.circle((x, y), 5, rgba((255, 190, 72)), INK, 2)


def draw_hero(row: int, frame: int, width: int, height: int) -> Art:
    art = Art(width, height)
    # All grounded poses share y=240. Limb positions carry the motion.
    phase = frame / 7.0
    walk = math.sin(phase * math.tau)
    opposite = -walk
    if row == 0:  # idle
        leg_a, leg_b, arm = -2 + walk * 2, 3 - walk * 2, 1.5 * walk
        cloak = 0
    elif row == 1:  # walk
        leg_a, leg_b, arm = 15 * walk, -15 * walk, -10 * walk
        cloak = -5 * walk
    elif row == 2:  # run
        leg_a, leg_b, arm = 24 * walk, -24 * walk, -14 * walk
        cloak = -12 * walk
    elif row == 3:  # attack
        leg_a, leg_b, arm = -7 + 4 * walk, 8 - 4 * walk, 0
        cloak = 7
    elif row == 4:  # jump
        leg_a, leg_b, arm = -18 + 6 * walk, 18 - 6 * walk, -7 * walk
        cloak = -18
    elif row == 5:  # hurt
        leg_a, leg_b, arm = -10 + 9 * walk, 10 - 9 * walk, 14 + 13 * walk
        cloak = 10 - 12 * walk
    else:  # defeat, kept broad enough to preserve a readable stable footprint
        leg_a, leg_b, arm = -17 + 8 * walk, 17 - 8 * walk, 20 + 10 * walk
        cloak = 20 - 10 * walk

    ground = 240
    hip_y = 176 if row != 6 else 202
    body_y = 115 if row != 6 else 142
    head_y = 76 if row != 6 else 111
    # Back cloak and scarf are drawn first, giving every pose a distinct trailing silhouette.
    art.polygon(
        [(82, body_y + 5), (34 + cloak, body_y + 38), (48 + cloak, ground - 4),
         (112, ground - 8), (125, body_y + 34)],
        HERO_CLOAK_DARK, INK, 3,
    )
    art.polygon(
        [(87, body_y + 7), (58 + cloak * .6, body_y + 35), (72, ground - 16),
         (108, ground - 12), (116, body_y + 28)],
        HERO_CLOAK, rgba((7, 31, 43)), 2,
    )
    art.polygon(
        [(58, 105), (31 + cloak, 117), (49 + cloak, 126), (72, 119)],
        HERO_CLOAK_LIGHT, INK, 2,
    )
    # Legs, with visible alternating stride rather than moving the whole body.
    knee_a = 150 + leg_a * .25
    knee_b = 150 + leg_b * .25
    art.line([(79, hip_y), (68 + leg_a, knee_a), (57 + leg_a * 1.25, ground - 3)], HERO_CLOAK_DARK, 14)
    art.line([(102, hip_y + 1), (108 + leg_b, knee_b), (119 + leg_b * 1.2, ground - 3)], HERO_CLOAK_DARK, 14)
    art.line([(79, hip_y), (68 + leg_a, knee_a), (57 + leg_a * 1.25, ground - 3)], STEEL, 4)
    art.line([(102, hip_y + 1), (108 + leg_b, knee_b), (119 + leg_b * 1.2, ground - 3)], STEEL, 4)
    art.ellipse((46 + leg_a * 1.2, ground - 9, 69 + leg_a * 1.2, ground + 1), INK)
    art.ellipse((108 + leg_b * 1.2, ground - 9, 131 + leg_b * 1.2, ground + 1), INK)
    # Torso and belt.
    art.polygon([(65, body_y + 20), (84, body_y - 2), (111, body_y + 16), (108, hip_y), (70, hip_y)],
                HERO_CLOAK, INK, 3)
    art.line([(68, hip_y - 4), (109, hip_y - 4)], rgba((233, 170, 74)), 6)
    art.circle((91, hip_y - 5), 5, rgba((97, 213, 185)), INK, 2)
    # Head, hood and scarf.
    art.circle((91, head_y), 22, HERO_SKIN, INK, 3)
    art.polygon([(65, head_y - 2), (80, head_y - 28), (105, head_y - 25), (116, head_y + 4), (101, head_y - 6)],
                HERO_CLOAK_DARK, INK, 3)
    art.line([(71, head_y + 12), (108, head_y + 8)], HERO_SCARF, 6)
    art.polygon([(70, head_y + 11), (45 + cloak * .5, head_y + 29), (75, head_y + 24)], HERO_SCARF, INK, 2)
    art.circle((101, head_y - 2), 2.5, INK)
    # Front arm and weapon form the readable state cue.
    elbow_x = 121 + arm
    elbow_y = body_y + 39 - (9 if row == 3 else 0)
    hand_x = 128 + arm * 1.05
    hand_y = body_y + (35 if row != 3 else 20)
    art.line([(101, body_y + 25), (elbow_x, elbow_y), (hand_x, hand_y)], HERO_CLOAK_DARK, 13)
    art.line([(101, body_y + 25), (elbow_x, elbow_y), (hand_x, hand_y)], HERO_CLOAK_LIGHT, 4)
    sword_angle = -48 + (frame - 3) * 15 if row == 3 else (-24 + arm * .35)
    if row == 5:
        sword_angle = 38
    if row == 6:
        sword_angle = 65
    # The attack arc is visible, but stays inside the same silhouette budget as
    # the idle/run poses so a sword swing cannot read as whole-character scaling.
    draw_sword(art, hand_x, hand_y, sword_angle, 32 if row == 3 else 46)
    return art


ENEMY_PALETTE = [
    ((74, 130, 82), (178, 213, 106)),
    ((215, 71, 42), (255, 176, 67)),
    ((147, 75, 47), (239, 165, 83)),
    ((79, 166, 207), (197, 247, 255)),
    ((128, 85, 193), (215, 172, 255)),
    ((63, 113, 109), (132, 239, 215)),
    ((142, 89, 59), (238, 165, 88)),
    ((71, 102, 153), (174, 177, 255)),
]


def draw_enemy(row: int, frame: int, width: int, height: int) -> Art:
    art = Art(width, height)
    base, light = ENEMY_PALETTE[row]
    primary, accent = rgba(base), rgba(light)
    ink = INK
    t = math.sin((frame / 6) * math.tau)
    action = frame >= 4
    hurt = frame == 5
    ground = 177
    # Flight is expressed through wing/body articulation, not vertical
    # translation, so the visual anchor remains deterministic for gameplay.
    bob = 0
    cx, cy = 96, ground - bob
    if row == 0:  # moss crawler
        art.ellipse((39, 81 + bob, 145, 169 + bob), primary, ink, 4)
        for x in (53, 78, 105, 130):
            art.polygon([(x - 9, 93 + bob), (x, 69 + bob), (x + 10, 94 + bob)], accent, ink, 2)
        claw = 19 if action else 10
        art.line([(55, 137 + bob), (33 - claw * .15, 164), (49, 174)], accent, 8)
        art.line([(128, 137 + bob), (159 + claw * .15, 164), (143, 174)], accent, 8)
        art.circle((80, 112 + bob), 4, rgba((245, 239, 154)), ink, 2)
    elif row == 1:  # ember moth
        wing = 11 if frame in (2, 4) else -4
        art.polygon([(83, 113 + bob), (27, 73 + wing + bob), (42, 137 + bob), (79, 147 + bob)], primary, ink, 3)
        art.polygon([(108, 113 + bob), (165, 73 - wing + bob), (150, 137 + bob), (112, 147 + bob)], primary, ink, 3)
        art.ellipse((73, 70 + bob, 118, 165 + bob), accent, ink, 4)
        art.line([(83, 80 + bob), (62, 55 + bob)], accent, 4)
        art.line([(108, 80 + bob), (132, 55 + bob)], accent, 4)
        art.circle((89, 99 + bob), 3, ink)
        art.circle((104, 99 + bob), 3, ink)
    elif row == 2:  # dune skirmisher
        stride = 13 * t
        art.polygon([(58, 80), (97, 60), (130, 92), (123, 154), (68, 154)], primary, ink, 4)
        art.polygon([(57, 81), (96, 45), (138, 80), (115, 101), (72, 100)], accent, ink, 3)
        art.circle((99, 82), 5, rgba((255, 226, 128)), ink, 2)
        art.line([(78, 143), (67 + stride, 174), (53 + stride, 174)], accent, 9)
        art.line([(107, 143), (119 - stride, 174), (134 - stride, 174)], accent, 9)
        draw_sword(art, 121 if not action else 139, 111, -42 if action else 28, 43)
    elif row == 3:  # frost sentinel
        art.polygon([(44, 151 + bob), (57, 75 + bob), (95, 55 + bob), (136, 77 + bob), (149, 151 + bob), (119, 174), (68, 174)],
                    primary, ink, 4)
        for x, y in ((59, 91), (95, 54), (130, 91), (71, 132), (122, 132)):
            art.polygon([(x, y + 30), (x + 12, y - 6), (x + 21, y + 30)], accent, ink, 2)
        arm = 28 if action else 12
        art.line([(60, 112), (32 - arm * .2, 145), (28 - arm * .2, 161)], accent, 10)
        art.line([(130, 112), (158 + arm * .2, 145), (162 + arm * .2, 161)], accent, 10)
    elif row == 4:  # wind wisp
        art.polygon([(42, 145 + bob), (66, 96 + bob), (104, 77 + bob), (146, 113 + bob),
                     (119, 160 + bob), (72, 171 + bob)], primary, ink, 4)
        art.line([(44, 126 + bob), (25, 106 + bob), (51, 106 + bob)], accent, 8)
        art.line([(128, 127 + bob), (165, 103 + bob), (145, 141 + bob)], accent, 8)
        art.circle((97, 112 + bob), 18, accent, ink, 3)
        art.circle((97, 112 + bob), 6, rgba((248, 238, 174)), ink, 2)
    elif row == 5:  # aegis guard
        art.rounded((61, 67, 119, 163), 16, primary, ink, 4)
        art.polygon([(65, 77), (90, 53), (119, 77), (108, 95), (75, 95)], accent, ink, 3)
        shield_x = 131 + (8 if action else 0)
        art.ellipse((shield_x - 25, 89, shield_x + 27, 162), accent, ink, 4)
        art.line([(shield_x - 12, 126), (shield_x + 15, 126)], rgba((29, 72, 92)), 4)
        art.line([(78, 157), (68, 175)], accent, 10)
        art.line([(104, 157), (117, 175)], accent, 10)
    elif row == 6:  # stone brute
        fist = 20 if action else 8
        art.rounded((39, 63, 148, 163), 24, primary, ink, 5)
        art.polygon([(53, 79), (83, 49), (121, 59), (137, 91), (70, 101)], accent, ink, 3)
        art.ellipse((18 - fist * .25, 111, 61 - fist * .25, 159), accent, ink, 4)
        art.ellipse((134 + fist * .25, 106, 177 + fist * .25, 155), accent, ink, 4)
        art.line([(69, 154), (59, 176)], accent, 15)
        art.line([(112, 154), (125, 176)], accent, 15)
    else:  # rune caster
        art.polygon([(55, 160), (72, 77), (98, 55), (126, 78), (143, 160)], primary, ink, 4)
        art.polygon([(72, 79), (98, 48), (126, 79), (113, 92), (84, 92)], accent, ink, 3)
        orb_y = 111 - (14 if action else 0)
        art.circle((145, orb_y), 18, accent, ink, 3)
        art.circle((145, orb_y), 7, rgba((241, 240, 177)), ink, 2)
        art.line([(72, 122), (44, 153), (32, 148)], accent, 9)
        art.line([(121, 122), (153, 146), (169, 141)], accent, 9)
    if hurt:
        art.line([(82, 54), (96, 68)], rgba((255, 243, 194)), 5)
        art.line([(96, 54), (82, 68)], rgba((255, 243, 194)), 5)
    return art


BOSS_PALETTE = [
    (rgba((82, 122, 78)), rgba((190, 224, 111))),
    (rgba((178, 78, 42)), rgba((255, 184, 66))),
    (rgba((116, 181, 221)), rgba((226, 250, 255))),
]


def draw_boss(row: int, frame: int, width: int, height: int) -> Art:
    art = Art(width, height)
    primary, accent = BOSS_PALETTE[row]
    ground = 365
    action = frame in (2, 3, 4)
    recover = frame == 5
    lean = 12 if frame in (3, 4) else (-8 if recover else 0)
    if row == 0:  # Heartwood Warden
        art.polygon([(104, 323), (128 + lean, 116), (192 + lean, 55), (260 + lean, 120),
                     (287, 323), (247, 363), (142, 363)], primary, INK, 8)
        for x in (129, 164, 205, 246):
            art.polygon([(x, 132), (x + 20, 42 + (frame % 2) * 12), (x + 40, 134)], accent, INK, 4)
        art.circle((196 + lean, 141), 32, accent, INK, 6)
        art.circle((206 + lean, 136), 7, rgba((73, 245, 177)), INK, 3)
        branch = 54 if action else 26
        art.line([(145, 190), (75 - branch * .2, 235), (44 - branch * .3, 289)], accent, 18)
        art.line([(243, 185), (309 + branch * .2, 230), (341 + branch * .3, 288)], accent, 18)
        art.line([(152, 318), (126, 358)], accent, 24)
        art.line([(231, 318), (262, 358)], accent, 24)
    elif row == 1:  # Sunscar Colossus
        art.polygon([(108, 330), (126 + lean, 108), (198 + lean, 58), (266 + lean, 113),
                     (291, 330), (249, 362), (143, 362)], primary, INK, 8)
        art.polygon([(132, 122), (198 + lean, 43), (270, 123), (229, 156), (164, 156)], accent, INK, 5)
        art.circle((204 + lean, 139), 29, rgba((255, 214, 91)), INK, 5)
        blade_x = 300 + (28 if action else 0)
        art.line([(245, 200), (blade_x, 108 if action else 174), (blade_x + 20, 44 if action else 120)], accent, 23)
        art.polygon([(blade_x - 16, 45 if action else 120), (blade_x + 20, 44 if action else 120),
                     (blade_x + 12, 16 if action else 91)], rgba((255, 238, 168)), INK, 4)
        art.line([(153, 320), (133, 359)], accent, 25)
        art.line([(234, 320), (267, 359)], accent, 25)
    else:  # Whiteout Matriarch
        art.polygon([(86, 327), (116 + lean, 119), (194 + lean, 45), (273 + lean, 119),
                     (304, 327), (256, 363), (133, 363)], primary, INK, 8)
        for x, y in ((123, 128), (158, 68), (200, 44), (242, 70), (275, 128)):
            art.polygon([(x, y + 60), (x + 22, y - 12), (x + 45, y + 60)], accent, INK, 4)
        art.circle((198 + lean, 134), 31, accent, INK, 5)
        art.circle((207 + lean, 130), 7, rgba((172, 243, 255)), INK, 3)
        wave = 54 if action else 22
        art.line([(139, 198), (68 - wave * .2, 235), (28 - wave * .25, 289)], accent, 17)
        art.line([(253, 198), (315 + wave * .2, 235), (345 + wave * .2, 289)], accent, 17)
        art.line([(153, 320), (125, 358)], accent, 23)
        art.line([(235, 320), (270, 358)], accent, 23)
    if recover:
        art.line([(117, 88), (143, 113)], rgba((255, 248, 202)), 8)
        art.line([(143, 88), (117, 113)], rgba((255, 248, 202)), 8)
    return art


def draw_world(row: int, frame: int, width: int, height: int) -> Art:
    art = Art(width, height)
    base_y = 246
    if row == 0:  # verdant thorn trap
        spread = (0, 8, 17, -4)[frame]
        art.ellipse((20, base_y - 18, 172, base_y + 3), rgba((39, 70, 49)), INK, 3)
        for x, top in ((47, 153), (77, 126 - spread), (105, 143 + spread), (135, 115)):
            art.line([(x, base_y - 8), (x - spread * .3, top)], rgba((64, 166, 105)), 12)
            art.polygon([(x - 7, top + 12), (x - spread * .3, top), (x + 9, top + 10)], rgba((126, 230, 151)), INK, 2)
    elif row == 1:  # dune fire turret
        art.ellipse((18, base_y - 19, 174, base_y + 3), rgba((85, 70, 49)), INK, 3)
        art.rounded((44, 132, 122, 237), 13, rgba((151, 113, 65)), INK, 4)
        art.ellipse((85, 153, 163, 220), rgba((230, 169, 70)), INK, 5)
        flame = (4, 8, 12, 16)[frame]
        art.polygon([(137, 184), (152 + flame, 174), (164 + flame, 192), (152 + flame, 208), (137, 202)],
                    rgba((255, 121 + frame * 15, 41)), INK, 3)
        art.polygon([(150, 185), (158 + flame, 188), (153, 199)], rgba((255, 239, 137)), None)
    elif row == 2:  # frozen spike trap
        art.ellipse((17, base_y - 18, 175, base_y + 3), rgba((61, 105, 129)), INK, 3)
        for x, h in ((35, 52 + frame * 4), (62, 78 - frame * 3), (90, 98 + frame * 5),
                     (120, 68 + frame * 3), (148, 43 + frame * 4)):
            art.polygon([(x - 14, base_y - 7), (x, base_y - h), (x + 16, base_y - 7)],
                        rgba((128, 221, 244)), INK, 3)
            art.line([(x, base_y - h + 8), (x + 7, base_y - 14)], rgba((231, 255, 255)), 3)
    else:  # checkpoint obelisk
        glow = (0, 8, 18, 34)[frame]
        art.ellipse((18, base_y - 17, 175, base_y + 3), rgba((50, 66, 76)), INK, 3)
        if frame > 0:
            spread = (0, 8, 18, 28)[frame]
            art.polygon(
                [(58, 151), (44 - spread, 124), (51 - spread, 175), (65, 190)],
                rgba((82, 230, 209), 185), INK, 2,
            )
            art.polygon(
                [(132, 151), (146 + spread, 124), (139 + spread, 175), (125, 190)],
                rgba((82, 230, 209), 185), INK, 2,
            )
        art.polygon([(46, base_y - 4), (53, 92), (95, 67), (138, 92), (146, base_y - 4)],
                    rgba((80, 91, 101)), INK, 4)
        art.polygon([(60, 91), (95, 60), (130, 91), (120, 185), (72, 185)],
                    rgba((141, 151, 159)), INK, 3)
        art.polygon([(79, 167), (95, 110 - glow), (111, 167)], rgba((88, 238, 216)), INK, 3)
        art.circle((95, 118 - glow), 12 + glow * .2, rgba((171, 255, 240), min(255, 150 + glow * 3)), None)
        art.polygon([(43, 92), (95, 51), (147, 92), (134, 108), (95, 76), (56, 108)],
                    rgba((105, 115, 123)), INK, 4)
        art.polygon([(75, 63), (95, 33 - frame * 2), (116, 63)], rgba((235, 190, 84)), INK, 3)
    return art


def validate_sheet(path: Path, cell_w: int, cell_h: int, rows: int, columns: int):
    sheet = Image.open(path).convert("RGBA")
    for row in range(rows):
        bounds = []
        for column in range(columns):
            pose = sheet.crop((column * cell_w, row * cell_h, (column + 1) * cell_w, (row + 1) * cell_h))
            box = pose.getchannel("A").getbbox()
            if box is None or box[0] <= 0 or box[1] <= 0 or box[2] >= cell_w or box[3] >= cell_h:
                raise RuntimeError(f"{path.name}: invalid edge/empty frame at {row},{column}: {box}")
            bounds.append(box)
        bottoms = [box[3] for box in bounds]
        widths = [box[2] - box[0] for box in bounds]
        heights = [box[3] - box[1] for box in bounds]
        if max(bottoms) - min(bottoms) > max(8, int(cell_h * .09)):
            raise RuntimeError(f"{path.name}: unstable bottom anchor row {row}: {bottoms}")
        if (max(widths) - min(widths)) / max(widths) > .30:
            raise RuntimeError(f"{path.name}: unstable footprint width row {row}: {widths}")
        if (max(heights) - min(heights)) / max(heights) > .30:
            raise RuntimeError(f"{path.name}: unstable footprint height row {row}: {heights}")


def rebuild_all():
    aster = centered_sheet(256, 256, 8, 7, draw_hero)
    enemies = centered_sheet(192, 192, 6, 8, draw_enemy)
    bosses = centered_sheet(384, 384, 6, 3, draw_boss)
    world = centered_sheet(192, 256, 4, 4, draw_world)
    outputs = [
        (RES / "aster_motion_sheet.png", aster, 256, 256, 7, 8),
        (RES / "enemies_motion_sheet.png", enemies, 192, 192, 8, 6),
        (RES / "bosses_motion_sheet.png", bosses, 384, 384, 3, 6),
        (RES / "world_motion_sheet.png", world, 192, 256, 4, 4),
    ]
    for path, sheet, cell_w, cell_h, rows, columns in outputs:
        sheet.save(path, optimize=True)
        validate_sheet(path, cell_w, cell_h, rows, columns)
        print(f"wrote {path.relative_to(ROOT)} {sheet.size}")


if __name__ == "__main__":
    rebuild_all()
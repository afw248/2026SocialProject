"""CupRice customer pixel portraits. Faces stay clear; ethnicity reads from
skin, hair silhouette, and a few clothing markers — not from a hair-mask over the eyes.
"""
from __future__ import annotations

import uuid
from dataclasses import dataclass
from pathlib import Path

from PIL import Image

W, H = 52, 68
SCALE = 5
OUTLINE = (32, 22, 16, 255)

# Distinct, separated palettes (fill, shade, blush)
SKIN = {
    "pale": ((255, 232, 210), (240, 200, 168), (255, 188, 176)),
    "east": ((255, 214, 170), (228, 176, 128), (248, 168, 148)),
    "olive": ((240, 196, 158), (214, 160, 118), (236, 156, 136)),
    "sea": ((204, 148, 96), (168, 112, 70), (214, 124, 96)),
    "gold": ((196, 132, 78), (156, 98, 56), (210, 112, 86)),
    "me": ((200, 156, 114), (164, 118, 82), (210, 128, 100)),
    "south": ((156, 96, 58), (118, 70, 42), (176, 88, 68)),
    "brown": ((110, 66, 42), (78, 46, 28), (140, 72, 56)),
    "deep": ((74, 44, 30), (48, 26, 18), (112, 58, 46)),
    "deep2": ((48, 30, 22), (30, 18, 14), (86, 48, 38)),
}

HAIR = {
    "black": ((22, 18, 20), (12, 10, 12)),
    "ink": ((18, 16, 28), (8, 8, 16)),
    "dark": ((42, 28, 22), (26, 16, 14)),
    "brown": ((96, 58, 36), (68, 38, 24)),
    "auburn": ((140, 68, 40), (104, 44, 28)),
    "gray": ((176, 172, 168), (132, 128, 124)),
    "silver": ((208, 208, 212), (156, 156, 160)),
    "mint": ((72, 168, 128), (44, 120, 92)),
    "rose": ((176, 56, 88), (132, 32, 60)),
    "navy": ((28, 72, 96), (16, 48, 68)),
    "burgundy": ((128, 36, 56), (92, 20, 40)),
    "teal": ((24, 108, 108), (12, 76, 76)),
}


class Canvas:
    def __init__(self, w: int, h: int):
        self.w, self.h = w, h
        self.px = [[None] * w for _ in range(h)]

    def set(self, x: int, y: int, color):
        if 0 <= x < self.w and 0 <= y < self.h and color is not None:
            self.px[y][x] = color

    def get(self, x: int, y: int):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[y][x]
        return None

    def fill_rect(self, x: int, y: int, w: int, h: int, color):
        for j in range(h):
            for i in range(w):
                self.set(x + i, y + j, color)

    def fill_ellipse(self, cx: int, cy: int, rx: int, ry: int, color):
        rx, ry = max(rx, 1), max(ry, 1)
        for y in range(cy - ry, cy + ry + 1):
            for x in range(cx - rx, cx + rx + 1):
                nx = (x - cx + 0.5) / rx
                ny = (y - cy + 0.5) / ry
                if nx * nx + ny * ny <= 1.04:
                    self.set(x, y, color)

    def shade_ellipse_right(self, cx: int, cy: int, rx: int, ry: int, color):
        rx, ry = max(rx, 1), max(ry, 1)
        for y in range(cy - ry, cy + ry + 1):
            for x in range(cx, cx + rx + 1):
                nx = (x - cx + 0.5) / rx
                ny = (y - cy + 0.5) / ry
                if nx * nx + ny * ny <= 1.04 and nx > 0.28:
                    self.set(x, y, color)

    def outline(self, color=OUTLINE):
        adds = []
        for y in range(self.h):
            for x in range(self.w):
                if self.px[y][x] is None:
                    continue
                for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                    nx, ny = x + dx, y + dy
                    if self.get(nx, ny) is None:
                        adds.append((nx, ny))
        for x, y in adds:
            if self.get(x, y) is None:
                self.set(x, y, color)

    def to_image(self, scale: int) -> Image.Image:
        img = Image.new("RGBA", (self.w, self.h), (0, 0, 0, 0))
        pix = img.load()
        for y in range(self.h):
            for x in range(self.w):
                c = self.px[y][x]
                if c is not None:
                    pix[x, y] = c if len(c) == 4 else (*c[:3], 255)
        if scale != 1:
            img = img.resize((self.w * scale, self.h * scale), Image.NEAREST)
        return img


@dataclass
class Spec:
    key: str
    name: str
    skin: str
    hair: str
    hair_style: str
    face: str
    clothes: tuple
    clothes_shade: tuple
    accent: tuple
    pants: tuple
    shoes: tuple
    body: str
    extras: tuple = ()


def darken(color, amt=28):
    return tuple(max(0, v - amt) for v in color[:3])


def draw_shadow(c: Canvas, cx: int):
    c.fill_ellipse(cx, 65, 12, 3, (40, 28, 22, 80))
    c.fill_ellipse(cx, 65, 8, 2, (40, 28, 22, 110))


def draw_legs(c: Canvas, cx: int, pants, shoes, wide: bool):
    gap = 2 if wide else 1
    lw = 5 if wide else 4
    left = cx - gap - lw
    right = cx + gap
    c.fill_rect(left, 48, lw, 14, pants)
    c.fill_rect(right, 48, lw, 14, pants)
    c.fill_rect(left - 1, 60, lw + 2, 5, shoes)
    c.fill_rect(right - 1, 60, lw + 2, 5, shoes)
    c.fill_rect(left - 1, 63, lw + 2, 2, darken(shoes, 30))


def draw_torso(c: Canvas, cx: int, fill, shade, accent, body: str, kind: str):
    tw = 18 if body == "m" else 16 if body == "f" else 17
    x = cx - tw // 2
    y = 32
    h = 18 if kind in ("tunic", "dress", "kurta") else 16
    c.fill_rect(x, y, tw, h, fill)
    c.fill_rect(x + tw - 4, y, 4, h, shade)
    c.fill_rect(x - 1, y, tw + 2, 5, fill)
    c.fill_rect(x + tw - 3, y, 4, 5, shade)

    if kind in ("jacket", "denim", "work"):
        c.fill_rect(x + 1, y, 3, 9, shade)
        c.fill_rect(x + tw - 4, y, 3, 9, darken(shade, 18))
        c.set(cx, y + 6, accent)
        c.set(cx, y + 10, accent)
        if kind == "work":
            c.fill_rect(x, y + 4, tw, 3, accent)
    elif kind == "hoodie":
        c.fill_rect(x + 2, y, tw - 4, 3, shade)
        c.fill_rect(cx - 1, y + 3, 2, 8, accent)
        c.fill_rect(x - 1, y - 1, 5, 4, fill)
        c.fill_rect(x + tw - 4, y - 1, 5, 4, shade)
    elif kind == "sailor":
        c.fill_rect(x, y, tw, 6, (245, 245, 248))
        c.fill_rect(x + 2, y + 2, tw - 4, 3, fill)
        c.fill_rect(x, y + 5, 4, 7, fill)
        c.fill_rect(x + tw - 4, y + 5, 4, 7, fill)
        c.fill_rect(cx - 1, y + 6, 2, 6, accent)
    elif kind == "stripe":
        for i in range(0, h, 3):
            c.fill_rect(x, y + i, tw, 1, (245, 245, 250))
    elif kind == "cardigan":
        c.fill_rect(x + 4, y + 2, tw - 8, h - 3, (248, 236, 220))
        c.fill_rect(x + 1, y, 4, h, fill)
        c.fill_rect(x + tw - 5, y, 4, h, shade)
    elif kind == "kurta":
        c.fill_rect(x + tw // 2 - 1, y, 2, h, accent)
        c.fill_rect(x, y, tw, 3, accent)
        c.fill_rect(x, y + h - 2, tw, 2, accent)
    elif kind == "dress":
        c.fill_rect(x - 1, y + 10, tw + 2, 9, fill)
        c.fill_rect(x + tw - 2, y + 10, 3, 9, shade)
    elif kind == "tunic":
        c.fill_rect(x - 1, y + 12, tw + 2, 6, fill)
        c.fill_rect(cx - 2, y + 2, 4, 3, accent)
    elif kind == "tee":
        c.fill_rect(cx - 2, y + 4, 4, 3, accent)

    arm_w = 4
    c.fill_rect(x - arm_w, y + 2, arm_w, 14, fill)
    c.fill_rect(x + tw, y + 2, arm_w, 14, shade)
    return x, tw, y


def draw_hands(c: Canvas, x: int, tw: int, y: int, skin_fill):
    c.fill_rect(x - 4, y + 15, 4, 4, skin_fill)
    c.fill_rect(x + tw, y + 15, 4, 4, skin_fill)


def draw_head(c: Canvas, cx: int, skin_fill, skin_shade, blush, face: str):
    rx, ry = (11, 12) if face in ("black", "south") else (10, 11)
    if face == "east":
        rx, ry = 10, 11
    c.fill_ellipse(cx, 18, rx, ry, skin_fill)
    c.shade_ellipse_right(cx, 18, rx, ry, skin_shade)
    c.fill_rect(cx - rx - 1, 18, 2, 4, skin_fill)
    c.fill_rect(cx + rx - 1, 18, 2, 4, skin_shade)
    c.fill_rect(cx - 7, 22, 3, 2, blush)
    c.fill_rect(cx + 4, 22, 3, 2, blush)
    c.fill_rect(cx - 2, 28, 4, 5, skin_fill)
    return rx, ry


def punch_face(c: Canvas, cx: int, cy: int, rx: int, ry: int, skin_fill, skin_shade, blush):
    """Clear hair off the face so bangs never become a mask."""
    c.fill_ellipse(cx, cy, rx, ry, skin_fill)
    c.shade_ellipse_right(cx, cy, rx, ry, skin_shade)
    c.fill_rect(cx - 7, cy + 4, 3, 2, blush)
    c.fill_rect(cx + 4, cy + 4, 3, 2, blush)


# Hair is drawn around the skull. Face oval is punched afterwards.
def hair_short(c, cx, fill, shade):
    c.fill_ellipse(cx, 10, 11, 7, fill)
    c.fill_rect(cx - 10, 8, 20, 5, fill)
    c.fill_rect(cx + 4, 7, 8, 6, shade)
    c.fill_rect(cx - 11, 14, 3, 7, fill)
    c.fill_rect(cx + 8, 14, 3, 7, shade)


def hair_messy(c, cx, fill, shade):
    hair_short(c, cx, fill, shade)
    c.fill_rect(cx - 5, 4, 4, 5, fill)
    c.fill_rect(cx + 1, 3, 3, 6, shade)
    c.fill_rect(cx + 6, 5, 3, 4, shade)


def hair_sidepart(c, cx, fill, shade):
    hair_short(c, cx, fill, shade)
    c.fill_rect(cx - 10, 7, 11, 4, fill)
    c.fill_rect(cx + 2, 6, 10, 5, shade)


def hair_bob(c, cx, fill, shade):
    c.fill_ellipse(cx, 10, 12, 8, fill)
    c.fill_rect(cx + 4, 7, 9, 8, shade)
    c.fill_rect(cx - 13, 16, 4, 10, fill)
    c.fill_rect(cx + 9, 16, 4, 10, shade)
    c.fill_rect(cx - 9, 7, 18, 3, fill)


def hair_long(c, cx, fill, shade):
    hair_bob(c, cx, fill, shade)
    c.fill_rect(cx - 14, 24, 5, 16, fill)
    c.fill_rect(cx + 9, 24, 5, 16, shade)
    c.fill_rect(cx - 13, 38, 4, 5, fill)
    c.fill_rect(cx + 9, 38, 4, 5, shade)


def hair_ponytail(c, cx, fill, shade):
    hair_short(c, cx, fill, shade)
    c.fill_rect(cx + 9, 12, 4, 4, shade)
    c.fill_rect(cx + 11, 14, 4, 16, shade)
    c.fill_rect(cx + 12, 28, 3, 6, fill)


def hair_bun(c, cx, fill, shade):
    hair_bob(c, cx, fill, shade)
    c.fill_ellipse(cx, 5, 6, 5, fill)
    c.fill_rect(cx + 1, 3, 5, 4, shade)


def hair_bangs(c, cx, fill, shade):
    hair_long(c, cx, fill, shade)
    # thin fringe above the eyes only
    c.fill_rect(cx - 8, 10, 16, 2, fill)
    c.fill_rect(cx + 2, 10, 8, 2, shade)


def hair_twin(c, cx, fill, shade):
    hair_bob(c, cx, fill, shade)
    c.fill_rect(cx - 15, 16, 4, 14, fill)
    c.fill_rect(cx + 11, 16, 4, 14, shade)
    c.fill_rect(cx - 16, 28, 5, 5, fill)
    c.fill_rect(cx + 11, 28, 5, 5, shade)


def hair_gray_wave(c, cx, fill, shade):
    hair_bob(c, cx, fill, shade)
    c.fill_rect(cx - 13, 12, 6, 4, fill)
    c.fill_rect(cx + 7, 12, 6, 4, shade)
    c.fill_ellipse(cx - 11, 22, 3, 4, fill)
    c.fill_ellipse(cx + 11, 22, 3, 4, shade)


def hair_afro(c, cx, fill, shade):
    c.fill_ellipse(cx, 12, 18, 16, fill)
    c.fill_ellipse(cx + 4, 10, 14, 14, shade)
    for ox, oy in (
        (-16, 6), (-14, 1), (-8, -2), (0, -3), (8, -1), (14, 4),
        (16, 12), (12, 20), (4, 22), (-6, 22), (-14, 16), (-17, 10),
    ):
        c.fill_rect(cx + ox, 12 + oy, 3, 3, fill if ox < 3 else shade)


def hair_puffs(c, cx, fill, shade):
    hair_short(c, cx, fill, shade)
    c.fill_ellipse(cx - 13, 9, 6, 6, fill)
    c.fill_ellipse(cx + 13, 9, 6, 6, shade)
    c.fill_ellipse(cx - 13, 9, 3, 3, shade)
    c.fill_ellipse(cx + 13, 9, 3, 3, fill)


def hair_twists(c, cx, fill, shade):
    hair_short(c, cx, fill, shade)
    for i, xx in enumerate(range(cx - 12, cx + 13, 4)):
        col = shade if i % 2 else fill
        c.fill_rect(xx, 7, 3, 5, col)
        c.fill_rect(xx, 20, 3, 14, col)
        c.fill_rect(xx, 33, 3, 3, darken(col, 20))


def hair_locs(c, cx, fill, shade):
    hair_short(c, cx, fill, shade)
    c.fill_ellipse(cx, 9, 12, 7, fill)
    for i, xx in enumerate((cx - 12, cx - 8, cx - 4, cx + 1, cx + 5, cx + 9)):
        col = shade if i % 2 else fill
        c.fill_rect(xx, 12, 3, 18, col)
        c.set(xx + 1, 30, darken(col, 24))


def hair_fade(c, cx, fill, shade):
    c.fill_ellipse(cx, 10, 10, 6, fill)
    c.fill_rect(cx + 2, 6, 9, 7, shade)
    c.fill_rect(cx - 6, 5, 10, 4, fill)
    c.fill_rect(cx - 10, 16, 2, 6, fill)
    c.fill_rect(cx + 8, 16, 2, 6, shade)


def hair_wavy(c, cx, fill, shade):
    hair_messy(c, cx, fill, shade)
    c.fill_rect(cx - 12, 16, 4, 10, fill)
    c.fill_rect(cx + 8, 16, 4, 10, shade)
    c.fill_rect(cx - 13, 24, 3, 4, fill)
    c.fill_rect(cx + 10, 24, 3, 4, shade)


HAIR_FN = {
    "short": hair_short,
    "messy": hair_messy,
    "sidepart": hair_sidepart,
    "bob": hair_bob,
    "long": hair_long,
    "ponytail": hair_ponytail,
    "bun": hair_bun,
    "bangs": hair_bangs,
    "twin": hair_twin,
    "gray_wave": hair_gray_wave,
    "afro": hair_afro,
    "puffs": hair_puffs,
    "twists": hair_twists,
    "locs": hair_locs,
    "fade": hair_fade,
    "wavy": hair_wavy,
}


def paint_hijab(c, cx, cloth, shade, skin_fill, skin_shade, blush):
    c.fill_ellipse(cx, 16, 14, 15, cloth)
    c.fill_rect(cx + 3, 6, 12, 18, shade)
    c.fill_rect(cx - 15, 22, 7, 18, cloth)
    c.fill_rect(cx + 8, 22, 7, 18, shade)
    c.fill_rect(cx - 14, 38, 6, 6, cloth)
    c.fill_rect(cx + 8, 38, 6, 6, shade)
    punch_face(c, cx, 19, 8, 9, skin_fill, skin_shade, blush)


def paint_kufi(c, cx, cap, shade):
    c.fill_ellipse(cx, 8, 10, 4, cap)
    c.fill_rect(cx - 10, 8, 20, 5, cap)
    c.fill_rect(cx + 2, 7, 9, 6, shade)
    c.fill_rect(cx - 9, 7, 18, 1, (255, 214, 96))
    for x in range(cx - 8, cx + 9, 4):
        c.set(x, 10, (255, 214, 96))


def draw_face(c, cx, extras, face: str, skin_fill, skin_shade):
    if face == "east":
        # narrow horizontal eyes — reads East Asian at this scale
        c.fill_rect(cx - 6, 17, 4, 1, (40, 28, 24))
        c.fill_rect(cx + 2, 17, 4, 1, (40, 28, 24))
        c.fill_rect(cx - 5, 18, 3, 1, (252, 252, 255))
        c.fill_rect(cx + 2, 18, 3, 1, (252, 252, 255))
        c.set(cx - 4, 18, (28, 22, 26))
        c.set(cx + 3, 18, (28, 22, 26))
        c.set(cx, 20, skin_shade)
        c.fill_rect(cx - 1, 23, 2, 1, (176, 92, 96))
    elif face == "south":
        c.fill_rect(cx - 6, 16, 3, 3, (252, 252, 255))
        c.fill_rect(cx + 3, 16, 3, 3, (252, 252, 255))
        c.set(cx - 5, 17, (36, 22, 18))
        c.set(cx + 4, 17, (36, 22, 18))
        c.set(cx - 6, 16, (255, 255, 255))
        c.set(cx + 3, 16, (255, 255, 255))
        c.fill_rect(cx - 1, 20, 2, 2, skin_shade)
        c.fill_rect(cx - 1, 24, 3, 1, (150, 70, 70))
        c.fill_rect(cx - 7, 15, 4, 1, (32, 20, 16))
        c.fill_rect(cx + 3, 15, 4, 1, (32, 20, 16))
    elif face == "black":
        c.fill_rect(cx - 6, 16, 4, 3, (252, 252, 255))
        c.fill_rect(cx + 2, 16, 4, 3, (252, 252, 255))
        c.fill_rect(cx - 5, 17, 2, 2, (24, 16, 14))
        c.fill_rect(cx + 3, 17, 2, 2, (24, 16, 14))
        c.set(cx - 6, 16, (255, 255, 255))
        c.set(cx + 2, 16, (255, 255, 255))
        c.fill_rect(cx - 1, 20, 3, 2, darken(skin_fill, 18))
        c.fill_rect(cx - 2, 23, 5, 2, (120, 58, 58))
        c.fill_rect(cx - 1, 24, 3, 1, (96, 42, 42))
        c.fill_rect(cx - 7, 15, 4, 1, (20, 14, 12))
        c.fill_rect(cx + 3, 15, 4, 1, (20, 14, 12))
    elif face == "west":
        c.fill_rect(cx - 6, 16, 3, 3, (252, 252, 255))
        c.fill_rect(cx + 3, 16, 3, 3, (252, 252, 255))
        c.set(cx - 5, 17, (40, 28, 24))
        c.set(cx + 4, 17, (40, 28, 24))
        c.set(cx - 6, 16, (255, 255, 255))
        c.set(cx + 3, 16, (255, 255, 255))
        c.fill_rect(cx, 19, 1, 3, skin_shade)
        c.fill_rect(cx - 1, 23, 3, 1, (188, 96, 96))
        c.fill_rect(cx - 7, 15, 4, 1, (72, 44, 32))
        c.fill_rect(cx + 3, 15, 4, 1, (72, 44, 32))
    elif face == "me":
        c.fill_rect(cx - 6, 16, 3, 3, (252, 252, 255))
        c.fill_rect(cx + 3, 16, 3, 3, (252, 252, 255))
        c.set(cx - 5, 17, (32, 22, 20))
        c.set(cx + 4, 17, (32, 22, 20))
        c.fill_rect(cx - 7, 14, 5, 1, (28, 18, 14))
        c.fill_rect(cx + 2, 14, 5, 1, (28, 18, 14))
        c.fill_rect(cx - 1, 20, 2, 2, skin_shade)
        c.fill_rect(cx - 1, 23, 3, 1, (160, 80, 80))
    else:  # sea
        c.fill_rect(cx - 5, 16, 3, 2, (252, 252, 255))
        c.fill_rect(cx + 2, 16, 3, 2, (252, 252, 255))
        c.set(cx - 4, 17, (32, 22, 18))
        c.set(cx + 3, 17, (32, 22, 18))
        c.set(cx, 20, skin_shade)
        c.fill_rect(cx - 1, 23, 3, 1, (168, 84, 80))
        c.fill_rect(cx - 6, 15, 3, 1, (36, 24, 18))
        c.fill_rect(cx + 3, 15, 3, 1, (36, 24, 18))

    if "smile" in extras:
        mouth = (168, 80, 86) if face not in ("black", "south") else (120, 58, 58)
        c.set(cx - 2, 23, mouth)
        c.set(cx + 2, 23, mouth)
    if "glasses" in extras:
        frame = (48, 44, 40)
        for x, y in (
            (cx - 7, 15), (cx - 2, 15), (cx - 7, 20), (cx - 2, 20),
            (cx - 7, 16), (cx - 7, 17), (cx - 7, 18), (cx - 7, 19),
            (cx - 2, 16), (cx - 2, 17), (cx - 2, 18), (cx - 2, 19),
            (cx + 1, 15), (cx + 6, 15), (cx + 1, 20), (cx + 6, 20),
            (cx + 1, 16), (cx + 1, 17), (cx + 1, 18), (cx + 1, 19),
            (cx + 6, 16), (cx + 6, 17), (cx + 6, 18), (cx + 6, 19),
            (cx - 1, 17), (cx, 17),
        ):
            c.set(x, y, frame)
    if "beard" in extras:
        beard = (36, 24, 20)
        c.fill_rect(cx - 6, 24, 12, 5, beard)
        c.fill_rect(cx - 4, 28, 8, 4, beard)
        c.fill_rect(cx - 2, 31, 4, 2, beard)
    if "stubble" in extras:
        c.fill_rect(cx - 5, 24, 10, 2, darken(skin_fill, 40))
    if "bindi" in extras:
        c.set(cx, 14, (196, 40, 48))
        c.set(cx, 13, (232, 180, 64))
    if "hoops" in extras:
        gold = (232, 180, 64)
        c.fill_rect(cx - 13, 20, 2, 4, gold)
        c.fill_rect(cx + 11, 20, 2, 4, gold)
        c.set(cx - 13, 24, gold)
        c.set(cx + 12, 24, gold)


def draw_bag(c, cx, color):
    c.fill_rect(cx + 13, 38, 6, 8, color)
    c.fill_rect(cx + 14, 36, 4, 3, darken(color, 30))


def draw_character(spec: Spec) -> Image.Image:
    c = Canvas(W, H)
    cx = 26
    skin_fill, skin_shade, blush = SKIN[spec.skin]
    hair_fill, hair_shade = HAIR[spec.hair]
    wide = spec.body in ("m", "elder")
    kind = "tee"
    for k in ("jacket", "hoodie", "work", "tunic", "cardigan", "sailor", "stripe",
              "kurta", "dress", "tee", "denim"):
        if k in spec.extras:
            kind = k
            break

    draw_shadow(c, cx)
    draw_legs(c, cx, spec.pants, spec.shoes, wide)
    x, tw, y = draw_torso(c, cx, spec.clothes, spec.clothes_shade, spec.accent, spec.body, kind)
    draw_hands(c, x, tw, y, skin_fill)
    rx, ry = draw_head(c, cx, skin_fill, skin_shade, blush, spec.face)

    if spec.hair_style == "hijab":
        paint_hijab(c, cx, hair_fill, hair_shade, skin_fill, skin_shade, blush)
        face_punched = True
    else:
        HAIR_FN[spec.hair_style](c, cx, hair_fill, hair_shade)
        punch_face(c, cx, 18, max(rx - 1, 8), max(ry - 1, 8), skin_fill, skin_shade, blush)
        face_punched = True
        if "kufi" in spec.extras:
            paint_kufi(c, cx, spec.accent, darken(spec.accent, 24))

    c.outline(OUTLINE)
    if face_punched:
        # keep face interior un-outlined
        pass
    draw_face(c, cx, spec.extras, spec.face, skin_fill, skin_shade)
    if "bag" in spec.extras:
        draw_bag(c, cx, spec.accent)
    return c.to_image(SCALE)


def rgb(r, g, b):
    return (r, g, b)


CUSTOMERS = [
    Spec("kimsangcheol", "김상철", "east", "gray", "sidepart", "east",
         rgb(52, 72, 110), rgb(36, 52, 82), rgb(220, 180, 80),
         rgb(58, 54, 52), rgb(36, 32, 30), "elder", ("jacket", "stubble")),
    Spec("parkyoungja", "박영자", "pale", "silver", "gray_wave", "east",
         rgb(196, 154, 118), rgb(168, 126, 92), rgb(180, 80, 70),
         rgb(90, 70, 58), rgb(64, 48, 40), "elder", ("cardigan", "glasses")),
    Spec("mina", "미나", "east", "black", "long", "east",
         rgb(232, 120, 148), rgb(196, 88, 118), rgb(255, 210, 220),
         rgb(72, 56, 80), rgb(48, 36, 52), "f", ("tee", "smile")),
    Spec("lara", "라라", "pale", "auburn", "twin", "east",
         rgb(255, 196, 72), rgb(220, 156, 40), rgb(255, 120, 90),
         rgb(80, 120, 160), rgb(48, 48, 70), "f", ("tee", "smile")),
    Spec("hyunwoo", "현우", "east", "ink", "messy", "east",
         rgb(64, 120, 196), rgb(44, 88, 156), rgb(240, 220, 80),
         rgb(48, 52, 70), rgb(32, 32, 40), "m", ("hoodie",)),
    Spec("nayoung", "나영", "east", "dark", "bob", "east",
         rgb(120, 196, 168), rgb(84, 156, 132), rgb(255, 240, 220),
         rgb(70, 64, 80), rgb(48, 40, 52), "f", ("cardigan", "smile")),
    Spec("leesujin", "이수진", "east", "black", "bangs", "east",
         rgb(248, 244, 236), rgb(216, 208, 196), rgb(220, 90, 80),
         rgb(64, 58, 72), rgb(40, 36, 48), "f", ("cardigan",)),
    Spec("junho", "준호", "east", "dark", "short", "east",
         rgb(88, 168, 96), rgb(60, 132, 72), rgb(210, 230, 120),
         rgb(70, 78, 62), rgb(48, 52, 40), "m", ("hoodie", "smile")),
    Spec("chen", "첸", "east", "black", "sidepart", "east",
         rgb(196, 64, 64), rgb(156, 40, 40), rgb(255, 210, 80),
         rgb(48, 48, 56), rgb(32, 32, 38), "m", ("jacket",)),
    Spec("wang", "왕", "sea", "gray", "short", "east",
         rgb(72, 88, 72), rgb(52, 64, 52), rgb(200, 80, 60),
         rgb(64, 56, 48), rgb(40, 34, 30), "elder", ("jacket", "stubble")),
    Spec("yuko", "유코", "pale", "ink", "bob", "east",
         rgb(40, 56, 120), rgb(28, 40, 92), rgb(220, 48, 64),
         rgb(48, 48, 64), rgb(32, 32, 44), "f", ("sailor", "smile")),
    Spec("marco", "마르코", "olive", "brown", "wavy", "west",
         rgb(48, 92, 148), rgb(32, 68, 116), rgb(245, 245, 250),
         rgb(70, 62, 58), rgb(48, 40, 36), "m", ("stripe", "smile")),
    Spec("aisha", "아이샤", "me", "teal", "hijab", "me",
         rgb(36, 92, 88), rgb(24, 68, 64), rgb(232, 196, 96),
         rgb(48, 56, 52), rgb(32, 36, 34), "f", ("tunic", "smile")),
    Spec("abdullah", "압둘라", "me", "black", "short", "me",
         rgb(48, 92, 72), rgb(32, 68, 52), rgb(212, 176, 72),
         rgb(56, 52, 48), rgb(36, 32, 28), "m", ("tunic", "kufi", "beard")),
    Spec("hasan", "하산", "gold", "dark", "fade", "me",
         rgb(72, 128, 120), rgb(48, 96, 90), rgb(230, 200, 110),
         rgb(52, 48, 56), rgb(34, 32, 38), "m", ("tee", "stubble")),
    Spec("fatima", "Fatima", "me", "burgundy", "hijab", "me",
         rgb(148, 52, 72), rgb(116, 36, 54), rgb(232, 196, 120),
         rgb(64, 44, 52), rgb(42, 30, 36), "f", ("tunic", "smile")),
    Spec("devi", "데비", "south", "black", "bun", "south",
         rgb(196, 72, 120), rgb(156, 48, 92), rgb(232, 180, 64),
         rgb(92, 44, 64), rgb(56, 32, 44), "f", ("kurta", "bindi", "hoops")),
    Spec("priya", "Priya", "gold", "dark", "long", "south",
         rgb(232, 140, 56), rgb(196, 108, 36), rgb(80, 48, 120),
         rgb(80, 40, 70), rgb(48, 28, 44), "f", ("kurta", "smile", "hoops")),
    Spec("sara", "사라", "pale", "brown", "ponytail", "east",
         rgb(120, 176, 92), rgb(88, 140, 68), rgb(240, 236, 180),
         rgb(64, 72, 56), rgb(40, 48, 36), "f", ("tee", "smile")),
    Spec("green", "그린", "east", "mint", "messy", "east",
         rgb(72, 160, 120), rgb(48, 124, 92), rgb(210, 255, 180),
         rgb(56, 72, 60), rgb(36, 48, 40), "m", ("hoodie",)),
    Spec("rosa", "로사", "olive", "rose", "bangs", "west",
         rgb(220, 96, 120), rgb(180, 68, 92), rgb(255, 210, 220),
         rgb(72, 52, 64), rgb(48, 32, 44), "f", ("dress", "smile")),
    Spec("leo", "Leo", "olive", "brown", "fade", "west",
         rgb(96, 140, 88), rgb(68, 108, 64), rgb(230, 220, 120),
         rgb(70, 68, 58), rgb(44, 42, 36), "m", ("tee", "smile")),
    Spec("nguyen", "응웬", "sea", "black", "short", "sea",
         rgb(40, 64, 96), rgb(28, 48, 72), rgb(232, 140, 48),
         rgb(48, 48, 56), rgb(32, 32, 38), "m", ("work", "bag")),
    Spec("bao", "Bao", "sea", "dark", "messy", "sea",
         rgb(48, 140, 168), rgb(32, 108, 132), rgb(255, 196, 72),
         rgb(52, 56, 64), rgb(34, 36, 42), "m", ("tee", "smile", "bag")),
    Spec("marcus", "마커스", "deep", "black", "fade", "black",
         rgb(232, 188, 56), rgb(196, 148, 32), rgb(40, 40, 48),
         rgb(48, 48, 56), rgb(28, 28, 32), "m", ("tee", "smile")),
    Spec("jasmine", "Jasmine", "deep2", "black", "twists", "black",
         rgb(148, 88, 188), rgb(112, 60, 156), rgb(232, 196, 80),
         rgb(48, 36, 60), rgb(32, 24, 40), "f", ("dress", "smile", "hoops")),
    Spec("tyler", "Tyler", "deep", "black", "short", "black",
         rgb(32, 112, 96), rgb(20, 84, 72), rgb(212, 176, 72),
         rgb(44, 48, 52), rgb(28, 30, 32), "m", ("tunic", "kufi")),
    Spec("keisha", "Keisha", "brown", "black", "puffs", "black",
         rgb(196, 56, 72), rgb(156, 36, 52), rgb(232, 188, 64),
         rgb(48, 36, 48), rgb(32, 24, 32), "f", ("jacket", "smile", "hoops")),
    Spec("darnell", "Darnell", "deep2", "black", "afro", "black",
         rgb(56, 140, 88), rgb(36, 108, 64), rgb(230, 220, 120),
         rgb(52, 56, 48), rgb(32, 36, 30), "m", ("hoodie", "smile")),
]


META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 512
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 512
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def main():
    root = Path(__file__).resolve().parents[4]
    out = root / "Assets" / "Resources" / "Craft" / "Sprites" / "Customers"
    out.mkdir(parents=True, exist_ok=True)
    folder_meta_path = out.parent / (out.name + ".meta")
    if not folder_meta_path.exists():
        folder_meta_path.write_text(FOLDER_META.format(guid=uuid.uuid4().hex), encoding="utf-8")

    images = []
    for spec in CUSTOMERS:
        img = draw_character(spec)
        path = out / f"portrait_{spec.key}.png"
        img.save(path)
        images.append(img)
        meta_path = out / f"portrait_{spec.key}.png.meta"
        if not meta_path.exists():
            meta_path.write_text(
                META_TEMPLATE.format(guid=uuid.uuid4().hex, sprite_id=uuid.uuid4().hex),
                encoding="utf-8",
            )
        print("wrote", path.name)

    cols = 6
    rows = (len(images) + cols - 1) // cols
    cw, ch = images[0].size
    sheet = Image.new("RGBA", (cols * cw, rows * ch), (40, 28, 22, 255))
    for i, img in enumerate(images):
        sheet.paste(img, ((i % cols) * cw, (i // cols) * ch), img)
    tools = Path(__file__).resolve().parent
    sheet_path = tools / "customer_portrait_sheet.png"
    sheet.save(sheet_path)
    print("sheet", sheet_path)


if __name__ == "__main__":
    main()

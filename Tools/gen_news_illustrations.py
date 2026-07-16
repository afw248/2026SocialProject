# -*- coding: utf-8 -*-
"""Readable 320x200 pixel news illustrations (drawn at native res, Point filter)."""
from PIL import Image, ImageDraw
import os

OUT = r"C:\Users\user\source\repos\2026GameProgramingTeamProject\2026GameProgramingTeamProject\2026SocialProject\Assets\Resources\Craft\News\Illustrations"
W, H = 320, 200


def canvas(bg=(0, 0, 0, 0)):
    return Image.new("RGBA", (W, H), bg)


def save(name, img):
    path = os.path.join(OUT, f"News_{name}.png")
    img.save(path)
    print("saved", name)


def rect(d, x, y, w, h, c):
    d.rectangle([x, y, x + w - 1, y + h - 1], fill=c)


def oval(d, x, y, w, h, c):
    d.ellipse([x, y, x + w - 1, y + h - 1], fill=c)


def line(d, xy, c, width=2):
    d.line(xy, fill=c, width=width)


# Colors
SKY = (168, 208, 238, 255)
SKY_DUSK = (255, 178, 128, 255)
SKY_NIGHT = (40, 50, 80, 255)
GRASS = (78, 148, 78, 255)
ROAD = (68, 68, 74, 255)
SIDEWALK = (170, 160, 145, 255)
WOOD = (158, 108, 58, 255)
DARK = (32, 28, 26, 255)
WHITE = (252, 250, 242, 255)
CREAM = (255, 236, 200, 255)
ORANGE = (232, 128, 42, 255)
GOLD = (222, 178, 52, 255)
RED = (206, 52, 48, 255)
GREEN = (52, 150, 68, 255)
TEAL = (32, 132, 136, 255)
BLUE = (58, 108, 178, 255)
PINK = (232, 140, 152, 255)
BROWN = (112, 72, 40, 255)
YELLOW = (246, 214, 68, 255)
GRAY = (118, 118, 124, 255)
PURPLE = (132, 78, 162, 255)
BEIGE = (236, 222, 192, 255)
SKIN_L = (214, 174, 132, 255)
SKIN_M = (180, 130, 90, 255)
SKIN_D = (90, 58, 38, 255)
SKIN_VD = (52, 32, 22, 255)


def person(d, x, y, skin, shirt, scale=1):
    """Person feet at (x,y). scale 1 ~= 12px tall."""
    s = scale
    # head
    oval(d, x - 3 * s, y - 20 * s, 7 * s, 7 * s, skin)
    # body
    rect(d, x - 4 * s, y - 14 * s, 8 * s, 9 * s, shirt)
    # legs
    rect(d, x - 3 * s, y - 5 * s, 3 * s, 5 * s, DARK)
    rect(d, x + 1 * s, y - 5 * s, 3 * s, 5 * s, DARK)
    # arms
    rect(d, x - 6 * s, y - 13 * s, 2 * s, 6 * s, shirt)
    rect(d, x + 4 * s, y - 13 * s, 2 * s, 6 * s, shirt)


def curry_pot(d, x, y, hot=True):
    """Curry pot centered roughly at x,y top of pot."""
    # body
    oval(d, x - 18, y, 36, 22, ORANGE)
    rect(d, x - 16, y + 8, 32, 14, ORANGE)
    oval(d, x - 16, y + 16, 32, 10, (180, 90, 30, 255))
    # rim
    oval(d, x - 18, y - 2, 36, 10, BROWN)
    oval(d, x - 14, y, 28, 6, GOLD)
    # handles
    rect(d, x - 22, y + 6, 5, 8, DARK)
    rect(d, x + 17, y + 6, 5, 8, DARK)
    if hot:
        # steam
        for i, ox in enumerate([-6, 0, 6]):
            oval(d, x + ox - 2, y - 14 - i * 3, 5, 8, (220, 220, 220, 160))


def bowl(d, x, y, rim, food, w=18, h=10):
    oval(d, x, y, w, h, rim)
    oval(d, x + 3, y + 2, w - 6, h - 4, food)


def shop(d, x, y, w, h, wall, awning, window_lit=True):
    rect(d, x, y, w, h, wall)
    # awning stripes
    rect(d, x - 2, y, w + 4, 10, awning)
    for i in range(0, w + 4, 8):
        rect(d, x - 2 + i, y, 4, 10, WHITE if awning != WHITE else (200, 200, 200, 255))
    # window
    wx, wy, ww, wh = x + 6, y + 16, w - 20, h - 36
    rect(d, wx, wy, ww, wh, CREAM if window_lit else (90, 100, 110, 255))
    rect(d, wx + 3, wy + 3, ww - 6, wh - 6, (160, 200, 230, 255) if window_lit else (50, 55, 65, 255))
    # door
    rect(d, x + w - 16, y + h - 28, 12, 28, DARK)
    rect(d, x + w - 14, y + h - 18, 3, 3, GOLD)  # knob


def closed_sign(d, x, y):
    rect(d, x, y, 28, 14, RED)
    rect(d, x + 2, y + 2, 24, 10, WHITE)
    # simple "X" to read as closed
    line(d, [(x + 6, y + 4), (x + 22, y + 10)], RED, 2)
    line(d, [(x + 22, y + 4), (x + 6, y + 10)], RED, 2)


# ========== SCENES ==========

def muslim_positive():
    img = canvas(SKY)
    d = ImageDraw.Draw(img)
    rect(d, 0, 120, W, 80, GRASS)
    # restaurant
    shop(d, 20, 50, 110, 90, WOOD, TEAL, True)
    # crescent + signboard
    rect(d, 30, 40, 90, 16, TEAL)
    oval(d, 100, 42, 14, 12, GOLD)
    oval(d, 105, 44, 10, 9, TEAL)
    # bowls in window
    bowl(d, 40, 78, TEAL, ORANGE)
    bowl(d, 68, 78, GOLD, GREEN)
    # long queue
    skins = [SKIN_M, SKIN_D, SKIN_L, SKIN_M, SKIN_VD, SKIN_L]
    shirts = [BLUE, GREEN, RED, PURPLE, TEAL, ORANGE]
    for i, sx in enumerate([150, 175, 200, 225, 250, 275]):
        person(d, sx, 165, skins[i], shirts[i], 1)
    # ground path
    rect(d, 140, 155, 160, 8, SIDEWALK)
    save("Muslim_Positive", img)


def korean_negative():
    img = canvas(BEIGE)
    d = ImageDraw.Draw(img)
    # stall roof
    rect(d, 20, 20, 280, 18, WOOD)
    rect(d, 30, 10, 8, 30, WOOD)
    rect(d, 280, 10, 8, 30, WOOD)
    # table
    rect(d, 30, 90, 260, 20, WOOD)
    # veggies piles — cabbage, sprouts, tofu, chili
    oval(d, 45, 55, 40, 35, GREEN)
    oval(d, 55, 62, 18, 16, (70, 170, 80, 255))
    rect(d, 100, 60, 45, 30, WHITE)
    rect(d, 105, 65, 35, 20, CREAM)
    oval(d, 160, 55, 40, 35, YELLOW)
    oval(d, 215, 55, 38, 35, RED)
    oval(d, 225, 65, 14, 12, (180, 40, 40, 255))
    # price board with down arrow
    rect(d, 120, 120, 70, 50, WHITE)
    rect(d, 122, 122, 66, 46, RED)
    rect(d, 145, 130, 20, 8, WHITE)
    rect(d, 150, 138, 10, 18, WHITE)
    # arrow head
    d.polygon([(140, 156), (170, 156), (155, 168)], fill=WHITE)
    person(d, 60, 185, SKIN_L, BLUE)
    person(d, 260, 185, SKIN_M, GREEN)
    save("Korean_Negative", img)


def hindu_curry():
    img = canvas((255, 218, 155, 255))
    d = ImageDraw.Draw(img)
    rect(d, 0, 130, W, 70, (175, 135, 85, 255))
    curry_pot(d, 160, 70, hot=True)
    # spice jars
    for i, (x, c) in enumerate([(40, RED), (80, GOLD), (240, GREEN), (280, BROWN)]):
        rect(d, x, 110, 22, 40, c)
        rect(d, x, 100, 22, 12, WOOD)
        oval(d, x + 4, 104, 14, 6, GOLD)
    # lotus
    oval(d, 145, 145, 30, 14, PINK)
    oval(d, 152, 138, 16, 12, PINK)
    rect(d, 157, 150, 6, 10, GOLD)
    # veggies
    oval(d, 50, 160, 24, 16, GREEN)
    oval(d, 250, 160, 24, 16, (250, 170, 50, 255))
    save("Hindu_Curry", img)


def vegan_rise():
    img = canvas((215, 242, 215, 255))
    d = ImageDraw.Draw(img)
    # plant stem + leaves
    rect(d, 155, 30, 12, 110, GREEN)
    oval(d, 90, 50, 70, 30, GREEN)
    oval(d, 165, 75, 80, 28, (70, 165, 80, 255))
    oval(d, 85, 100, 75, 28, GREEN)
    # veins
    line(d, [(100, 65), (150, 65)], (40, 100, 50, 255), 2)
    line(d, [(175, 89), (230, 89)], (40, 100, 50, 255), 2)
    # tofu
    rect(d, 30, 130, 70, 50, CREAM)
    rect(d, 38, 138, 54, 34, WHITE)
    rect(d, 48, 148, 10, 10, (220, 220, 200, 255))
    rect(d, 70, 155, 10, 10, (220, 220, 200, 255))
    # badge
    oval(d, 230, 125, 70, 55, GREEN)
    oval(d, 245, 138, 40, 30, WHITE)
    oval(d, 255, 148, 20, 14, GREEN)
    save("Vegan_Rise", img)


def seasian_fusion():
    img = canvas((255, 232, 195, 255))
    d = ImageDraw.Draw(img)
    rect(d, 0, 140, W, 60, (128, 92, 55, 255))
    # mat
    rect(d, 40, 110, 240, 30, WOOD)
    # shared bowls
    bowl(d, 70, 85, WHITE, GREEN, 36, 22)
    bowl(d, 140, 75, TEAL, ORANGE, 40, 24)
    bowl(d, 215, 85, ORANGE, YELLOW, 36, 22)
    # rice on center
    oval(d, 150, 80, 20, 12, WHITE)
    # chopsticks
    line(d, [(120, 70), (200, 55)], BROWN, 3)
    line(d, [(125, 78), (205, 63)], BROWN, 3)
    person(d, 60, 185, SKIN_D, RED)
    person(d, 160, 188, SKIN_L, BLUE)
    person(d, 260, 185, SKIN_M, GREEN)
    save("SEAsian_Fusion", img)


def multiculture_unity():
    img = canvas((220, 232, 248, 255))
    d = ImageDraw.Draw(img)
    # certificate
    rect(d, 80, 15, 160, 120, WHITE)
    rect(d, 88, 22, 144, 16, BLUE)
    rect(d, 95, 50, 120, 6, DARK)
    rect(d, 95, 65, 90, 5, GRAY)
    rect(d, 95, 78, 100, 5, GRAY)
    # seal
    oval(d, 175, 95, 40, 32, GOLD)
    oval(d, 185, 103, 18, 14, RED)
    skins = [SKIN_D, SKIN_L, SKIN_VD, SKIN_M]
    shirts = [BLUE, GREEN, RED, PURPLE]
    for i, sx in enumerate([50, 120, 190, 260]):
        person(d, sx, 185, skins[i], shirts[i])
    save("Multiculture_Unity", img)


def discrimination():
    img = canvas((32, 38, 48, 255))
    d = ImageDraw.Draw(img)
    # monitor bezel
    rect(d, 50, 20, 220, 110, GRAY)
    rect(d, 60, 30, 200, 90, (22, 28, 38, 255))
    # angry post card
    rect(d, 75, 45, 55, 50, RED)
    rect(d, 140, 45, 100, 12, (95, 95, 105, 255))
    rect(d, 140, 65, 85, 12, (95, 95, 105, 255))
    rect(d, 140, 85, 70, 12, (70, 70, 80, 255))
    # stand
    rect(d, 145, 130, 30, 20, DARK)
    rect(d, 110, 150, 100, 12, DARK)
    # warning triangle
    d.polygon([(270, 35), (300, 35), (285, 70)], fill=YELLOW)
    rect(d, 282, 42, 6, 16, DARK)
    rect(d, 282, 60, 6, 5, DARK)
    save("Discrimination", img)


def halal_scandal_false():
    img = canvas((250, 228, 218, 255))
    d = ImageDraw.Draw(img)
    rect(d, 60, 15, 200, 140, WHITE)
    rect(d, 75, 28, 170, 22, TEAL)
    # seal
    oval(d, 120, 70, 80, 60, GOLD)
    oval(d, 140, 88, 40, 30, WHITE)
    # crack
    line(d, [(160, 55), (175, 90), (155, 125), (180, 150)], RED, 3)
    line(d, [(162, 55), (177, 90), (157, 125), (182, 150)], RED, 2)
    # FAKE stamp
    rect(d, 20, 155, 80, 30, RED)
    rect(d, 26, 161, 68, 18, WHITE)
    line(d, [(40, 165), (70, 175)], RED, 3)
    line(d, [(70, 165), (40, 175)], RED, 3)
    save("Halal_Scandal_False", img)


def school_food_edu():
    img = canvas((244, 232, 210, 255))
    d = ImageDraw.Draw(img)
    # blackboard
    rect(d, 30, 15, 260, 85, (42, 88, 52, 255))
    rect(d, 45, 30, 50, 8, WHITE)
    rect(d, 45, 48, 100, 8, WHITE)
    rect(d, 45, 66, 70, 8, WHITE)
    # chalk bowl
    oval(d, 200, 40, 50, 30, WHITE)
    oval(d, 210, 48, 30, 16, (42, 88, 52, 255))
    # trays
    rect(d, 60, 115, 200, 35, WOOD)
    rect(d, 75, 122, 35, 22, CREAM)
    rect(d, 120, 122, 35, 22, GREEN)
    rect(d, 165, 122, 35, 22, ORANGE)
    rect(d, 210, 122, 30, 22, RED)
    person(d, 90, 185, PINK, BLUE)
    person(d, 160, 185, SKIN_D, GREEN)
    person(d, 230, 185, SKIN_L, RED)
    save("School_Food_Edu", img)


def muslim_travel_ban():
    img = canvas((188, 202, 218, 255))
    d = ImageDraw.Draw(img)
    rect(d, 0, 120, W, 80, (82, 88, 94, 255))
    # passport
    rect(d, 25, 35, 70, 95, (28, 82, 48, 255))
    rect(d, 35, 45, 50, 14, GOLD)
    rect(d, 40, 70, 40, 40, CREAM)
    oval(d, 50, 80, 20, 20, (28, 82, 48, 255))
    # barrier arm
    rect(d, 130, 40, 14, 110, RED)
    rect(d, 144, 85, 120, 14, WHITE)
    rect(d, 144, 105, 120, 14, RED)
    # closed
    rect(d, 200, 40, 70, 35, RED)
    rect(d, 208, 48, 54, 8, WHITE)
    rect(d, 208, 62, 40, 8, WHITE)
    person(d, 120, 185, SKIN_D, TEAL)
    save("Muslim_Travel_Ban", img)


def hindu_temple_dispute():
    """한산한 채식 커리 거리 — 닫힌 가게, 빈 테이블, 멀리 사원."""
    img = canvas(SKY_DUSK)
    d = ImageDraw.Draw(img)
    # road + sidewalk
    rect(d, 0, 118, W, 22, ROAD)
    rect(d, 0, 140, W, 60, SIDEWALK)
    # distant temple (right)
    rect(d, 235, 35, 70, 85, ORANGE)
    # dome + spire
    oval(d, 245, 10, 50, 40, GOLD)
    rect(d, 266, 2, 8, 20, GOLD)
    oval(d, 264, 0, 12, 10, GOLD)
    # temple doors
    rect(d, 250, 75, 16, 45, DARK)
    rect(d, 275, 75, 16, 45, DARK)
    # columns
    rect(d, 242, 55, 8, 65, (200, 120, 50, 255))
    rect(d, 290, 55, 8, 65, (200, 120, 50, 255))

    # LEFT curry shop — closed
    shop(d, 15, 45, 95, 85, (198, 148, 88, 255), RED, window_lit=False)
    closed_sign(d, 40, 95)
    # empty curry pot silhouette in dark window
    oval(d, 35, 70, 28, 16, (100, 70, 40, 255))

    # MIDDLE veg shop — closed shutter
    shop(d, 120, 50, 90, 80, (175, 155, 125, 255), GREEN, window_lit=False)
    rect(d, 130, 70, 50, 35, GRAY)  # shutter
    for yy in range(72, 102, 5):
        line(d, [(132, yy), (178, yy)], (90, 90, 95, 255), 1)
    closed_sign(d, 140, 100)

    # empty sidewalk tables
    for tx in (30, 85, 145):
        rect(d, tx, 148, 32, 8, WOOD)
        rect(d, tx + 4, 156, 5, 14, WOOD)
        rect(d, tx + 22, 156, 5, 14, WOOD)
        # empty cold bowls
        bowl(d, tx + 6, 140, TEAL if tx != 145 else ORANGE, (170, 155, 140, 255), 18, 10)

    # fallen flyer
    rect(d, 200, 155, 22, 16, WHITE)
    rect(d, 204, 160, 14, 4, RED)

    # one lonely person walking away toward temple
    person(d, 220, 185, SKIN_D, BROWN)
    # tiny second figure far
    person(d, 300, 175, SKIN_M, GRAY, scale=1)

    # quiet leaves
    oval(d, 100, 25, 12, 8, GREEN)
    oval(d, 180, 20, 10, 7, GREEN)
    save("Hindu_Temple_Dispute", img)


def vegan_greenwash():
    img = canvas((232, 248, 228, 255))
    d = ImageDraw.Draw(img)
    # package
    rect(d, 70, 25, 180, 110, GREEN)
    rect(d, 95, 45, 130, 70, WHITE)
    # leaf logo
    oval(d, 130, 55, 60, 40, GREEN)
    oval(d, 145, 65, 30, 22, (70, 165, 80, 255))
    line(d, [(160, 70), (160, 95)], (40, 100, 50, 255), 2)
    # big red X
    line(d, [(100, 40), (220, 120)], RED, 6)
    line(d, [(220, 40), (100, 120)], RED, 6)
    # meat peek under
    rect(d, 110, 150, 100, 30, (175, 85, 75, 255))
    rect(d, 120, 158, 80, 14, PINK)
    save("Vegan_Greenwash", img)


def seasian_labor_strike():
    img = canvas((172, 188, 205, 255))
    d = ImageDraw.Draw(img)
    rect(d, 0, 120, W, 80, (92, 98, 104, 255))
    # factory
    rect(d, 15, 45, 130, 85, GRAY)
    rect(d, 30, 15, 22, 40, DARK)
    rect(d, 70, 10, 22, 45, DARK)
    # smoke
    oval(d, 28, 2, 28, 16, (170, 170, 175, 180))
    oval(d, 68, 0, 30, 14, (170, 170, 175, 160))
    rect(d, 35, 60, 30, 25, (175, 205, 230, 255))
    rect(d, 85, 60, 30, 25, (175, 205, 230, 255))
    # picketers
    for i, sx in enumerate([175, 220, 265]):
        rect(d, sx, 55, 6, 55, WOOD)
        rect(d, sx - 18, 40, 42, 24, YELLOW)
        rect(d, sx - 10, 48, 26, 8, RED)
        person(d, sx + 3, 175, [SKIN_D, SKIN_M, SKIN_L][i], [BLUE, TEAL, GREEN][i])
    save("SEAsian_Labor_Strike", img)


def soulfood_fest():
    img = canvas((255, 198, 125, 255))
    d = ImageDraw.Draw(img)
    rect(d, 0, 130, W, 70, GRASS)
    # banner
    rect(d, 50, 15, 220, 40, PURPLE)
    rect(d, 60, 22, 200, 26, GOLD)
    for x in range(75, 250, 20):
        oval(d, x, 30, 8, 8, RED)
    # food table
    rect(d, 55, 105, 210, 22, WOOD)
    # fried chicken mound
    oval(d, 70, 75, 50, 35, ORANGE)
    oval(d, 80, 82, 28, 20, (200, 140, 55, 255))
    # greens
    oval(d, 140, 70, 55, 40, GREEN)
    oval(d, 150, 80, 35, 22, (40, 100, 50, 255))
    # pie
    oval(d, 215, 78, 45, 32, RED)
    oval(d, 225, 86, 25, 16, (180, 50, 50, 255))
    skins = [SKIN_VD, SKIN_D, SKIN_VD, SKIN_D]
    for i, sx in enumerate([55, 120, 190, 260]):
        person(d, sx, 185, skins[i], [YELLOW, BLUE, RED, GREEN][i])
    save("SoulFood_Fest", img)


def korean_harvest_fest():
    img = canvas((255, 222, 165, 255))
    d = ImageDraw.Draw(img)
    rect(d, 0, 130, W, 70, (108, 152, 72, 255))
    # moon
    oval(d, 240, 15, 55, 55, YELLOW)
    oval(d, 255, 25, 28, 28, (255, 222, 165, 255))
    # table + songpyeon
    rect(d, 70, 115, 180, 22, WOOD)
    oval(d, 95, 90, 35, 28, PINK)
    oval(d, 145, 85, 35, 32, WHITE)
    oval(d, 195, 90, 35, 28, GREEN)
    oval(d, 105, 95, 10, 8, GREEN)
    oval(d, 155, 92, 10, 8, GREEN)
    # hanbok figures
    # left
    oval(d, 35, 140, 16, 16, SKIN_L)
    rect(d, 28, 155, 30, 35, RED)
    # right
    oval(d, 270, 140, 16, 16, SKIN_L)
    rect(d, 263, 155, 30, 35, BLUE)
    save("Korean_Harvest_Fest", img)


def halal_kitchen_edu():
    img = canvas((244, 238, 228, 255))
    d = ImageDraw.Draw(img)
    # counter
    rect(d, 20, 90, 280, 50, WOOD)
    rect(d, 20, 80, 280, 12, (178, 138, 88, 255))
    # separated boards
    rect(d, 40, 100, 60, 30, (200, 180, 140, 255))
    rect(d, 130, 100, 60, 30, (175, 200, 155, 255))
    rect(d, 220, 100, 60, 30, CREAM)
    rect(d, 48, 108, 40, 6, TEAL)
    rect(d, 138, 108, 40, 6, GREEN)
    # knives
    rect(d, 55, 120, 30, 4, GRAY)
    rect(d, 145, 120, 30, 4, GRAY)
    # chef
    rect(d, 145, 20, 35, 22, WHITE)
    oval(d, 150, 40, 24, 22, SKIN_L)
    rect(d, 145, 60, 35, 25, WHITE)
    # crescent badge
    oval(d, 250, 155, 45, 35, GOLD)
    oval(d, 265, 162, 28, 24, (244, 238, 228, 255))
    save("Halal_Kitchen_Edu", img)


def seafood_shortage():
    img = canvas((145, 195, 225, 255))
    d = ImageDraw.Draw(img)
    rect(d, 0, 130, W, 70, (45, 90, 125, 255))
    for x in range(0, 320, 24):
        oval(d, x, 122, 30, 14, (70, 135, 175, 255))
    # crate
    rect(d, 70, 60, 160, 70, WOOD)
    rect(d, 80, 70, 140, 50, (185, 210, 228, 255))
    # one fish only
    oval(d, 110, 85, 50, 22, BLUE)
    d.polygon([(110, 96), (95, 88), (95, 104)], fill=BLUE)
    oval(d, 145, 90, 6, 6, WHITE)
    rect(d, 147, 92, 3, 3, DARK)
    # ice cubes empty space
    for p in [(180, 80), (200, 95), (170, 105), (210, 85)]:
        rect(d, p[0], p[1], 10, 8, WHITE)
    # shortage arrow
    rect(d, 265, 25, 20, 50, RED)
    d.polygon([(250, 75), (300, 75), (275, 100)], fill=RED)
    save("Seafood_Shortage", img)


def unity_city_campaign():
    img = canvas((150, 190, 232, 255))
    d = ImageDraw.Draw(img)
    rect(d, 0, 120, W, 80, (82, 92, 102, 255))
    # skyline
    for x, h in [(10, 70), (50, 95), (95, 60), (140, 85), (190, 75), (235, 100), (280, 55)]:
        rect(d, x, 120 - h, 30, h, GRAY)
        for wy in range(120 - h + 8, 115, 14):
            rect(d, x + 6, wy, 6, 6, YELLOW)
            rect(d, x + 18, wy, 6, 6, YELLOW)
    # banner
    rect(d, 40, 125, 240, 35, PURPLE)
    rect(d, 55, 133, 40, 18, WHITE)
    rect(d, 110, 133, 40, 18, WHITE)
    rect(d, 165, 133, 40, 18, WHITE)
    rect(d, 220, 133, 40, 18, GOLD)
    # hearts
    oval(d, 90, 20, 18, 14, RED)
    oval(d, 100, 20, 18, 14, RED)
    d.polygon([(90, 30), (118, 30), (104, 48)], fill=RED)
    oval(d, 200, 15, 16, 12, PINK)
    oval(d, 210, 15, 16, 12, PINK)
    d.polygon([(200, 24), (226, 24), (213, 40)], fill=PINK)
    skins = [SKIN_D, SKIN_L, SKIN_VD, SKIN_M]
    for i, sx in enumerate([60, 130, 200, 270]):
        person(d, sx, 190, skins[i], [BLUE, GREEN, RED, TEAL][i])
    save("Unity_City_Campaign", img)


def main():
    os.makedirs(OUT, exist_ok=True)
    muslim_positive()
    korean_negative()
    hindu_curry()
    vegan_rise()
    seasian_fusion()
    multiculture_unity()
    discrimination()
    halal_scandal_false()
    school_food_edu()
    muslim_travel_ban()
    hindu_temple_dispute()
    vegan_greenwash()
    seasian_labor_strike()
    soulfood_fest()
    korean_harvest_fest()
    halal_kitchen_edu()
    seafood_shortage()
    unity_city_campaign()
    print("all 18 redrawn at 320x200")


if __name__ == "__main__":
    main()

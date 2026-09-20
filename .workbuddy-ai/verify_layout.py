"""
Headless layout verifier for TowerDefense Unity HUD (Arknights-style rewrite).

Replicates the exact TdLayout anchor math and the Build() positioning in
HudTopBar.cs / HudDeployBar.cs, then reports for every element:
  - its rect in canvas coords (1280x720, y-up origin = bottom-left)
  - sibling overlaps (same parent)  -> hard failure if > 0.5px in both axes
  - out-of-band violations          -> solid elements must stay inside the band;
                                       decor (Glow/Sheen/AccentLine) may bleed a
                                       few px and is reported as a note, not a fail

This is the only way to catch real geometry bugs without a C# compiler / Unity.

Coordinate system matches UGUI local space after CanvasScaler lock-width 1280:
canvas bottom-left = (0,0), y grows upward.
"""

CANVAS_W = 1280.0
CANVAS_H = 720.0

# Tokens (mirror TdTheme.cs)
TOP_BAR_H = 64.0
BOTTOM_BAR_H = 112.0
CARD_W = 118.0
CARD_H = 86.0
CARD_GAP = 14.0
SPACE_LG = 16.0
SPACE_MD = 12.0


# ---------------------------------------------------------------
# TdLayout mirror
# ---------------------------------------------------------------
def rect_from(parent, anchor, pivot, pos, size):
    px, py, pw, ph = parent
    ax_p = anchor[0] * pw
    ay_p = anchor[1] * ph
    pivot_x = ax_p + pos[0]
    pivot_y = ay_p + pos[1]
    left = pivot_x - pivot[0] * size[0]
    bottom = pivot_y - pivot[1] * size[1]
    return (px + left, py + bottom, size[0], size[1])


def band(top, h):
    if top:
        return (0.0, CANVAS_H - h, CANVAS_W, h)
    return (0.0, 0.0, CANVAS_W, h)


def inleft(parent, left, w, h):
    return rect_from(parent, (0.0, 0.5), (0.0, 0.5), (left, 0.0), (w, h))


def inright(parent, right, w, h):
    return rect_from(parent, (1.0, 0.5), (1.0, 0.5), (-right, 0.0), (w, h))


def incenter(parent, cx, w, h):
    return rect_from(parent, (0.5, 0.5), (0.5, 0.5), (cx, 0.0), (w, h))


def at(parent, x, y, w, h):
    # anchor (0,1) top-left, pivot (0,0.5) -> y = dist from top to center
    return rect_from(parent, (0.0, 1.0), (0.0, 0.5), (x, -y), (w, h))


def atright(parent, right, y, w, h):
    # anchor (1,1) top-right, pivot (1,0.5) -> right = dist from right to center
    return rect_from(parent, (1.0, 1.0), (1.0, 0.5), (-right, -y), (w, h))


def atbottom(parent, x, bottom, w, h):
    # anchor (0,0) bottom-left, pivot (0,0.5) -> bottom = dist from bottom to center
    return rect_from(parent, (0.0, 0.0), (0.0, 0.5), (x, bottom), (w, h))


def atcenter(parent, x, y, w, h):
    # anchor (0,1) top-left, pivot (0.5,0.5)
    return rect_from(parent, (0.0, 1.0), (0.5, 0.5), (x, -y), (w, h))


def anchor(parent, a, pivot, pos, size):
    return rect_from(parent, a, pivot, pos, size)


def overlap(a, b):
    ax0, ay0, aw, ah = a
    bx0, by0, bw, bh = b
    ix = max(0.0, min(ax0 + aw, bx0 + bw) - max(ax0, bx0))
    iy = max(0.0, min(ay0 + ah, by0 + bh) - max(ay0, by0))
    return ix, iy


def report(name, r):
    x0, y0, w, h = r
    print(f"  {name:18s} x[{x0:7.1f},{x0+w:7.1f}] y[{y0:7.1f},{y0+h:7.1f}]  ({w:.0f}x{h:.0f})")


def check_siblings(title, items):
    print(f"\n== {title} ==")
    for n, r, _s in items:
        report(n, r)
    print("  -- sibling overlaps (threshold 0.5px; solid vs solid) --")
    found = False
    for i in range(len(items)):
        for j in range(i + 1, len(items)):
            ix, iy = overlap(items[i][1], items[j][1])
            if ix > 0.5 and iy > 0.5:
                # decor (Glow/Sheen/AccentLine) is transparent and intentionally
                # sits behind/around solids; its overlap with another element is
                # by design, not a geometry bug.
                if not items[i][2] or not items[j][2]:
                    print(f"     (decor) {items[i][0]} x {items[j][0]} : {ix:.1f} x {iy:.1f}  [intended halo]")
                    continue
                print(f"  !! {items[i][0]} x {items[j][0]} : {ix:.1f} x {iy:.1f} overlap")
                found = True
    if not found:
        print("  (none)")
    return found


def out_of_band(title, band_rect, items, tol_solid=0.5, tol_decor=14.0):
    print(f"  -- out-of-band (band {tuple(round(v,1) for v in band_rect)}) --")
    bx0, by0, bw, bh = band_rect
    bx1, by1 = bx0 + bw, by0 + bh
    found = False
    for n, r, solid in items:
        x0, y0, w, h = r
        x1, y1 = x0 + w, y0 + h
        t = tol_solid if solid else tol_decor
        bad = (x0 < bx0 - t or y0 < by0 - t or x1 > bx1 + t or y1 > by1 + t)
        if bad:
            tag = "" if solid else "   [decor bleed - allowed]"
            print(f"  !! {n} outside band: x[{x0:.1f},{x1:.1f}] y[{y0:.1f},{y1:.1f}]{tag}")
            if solid:
                found = True
    if not found:
        print("  (none)")
    return found


# ---------------------------------------------------------------
# HudTopBar  (top band, content = Fill(band))
# ---------------------------------------------------------------
def topbar():
    b = band(True, TOP_BAR_H)          # (0, 656, 1280, 64)
    content = b
    fails = []

    items = []
    # left: spirit chip
    spirit = inleft(content, SPACE_LG, 168.0, 48.0)
    items.append(("Spirit", spirit, True))

    # center: round module + children
    round_mod = incenter(content, 0.0, 268.0, 50.0)
    items.append(("RoundModule", round_mod, True))
    round_ch = [
        ("  RoundEng", at(round_mod, 0.0, 34.0, 268.0, 22.0), True),
        ("  RoundTitle", at(round_mod, 0.0, 14.0, 268.0, 16.0), True),
        ("  RoundBar", at(round_mod, 34.0, 4.0, 200.0, 2.0), True),
    ]

    # right cluster
    enemy = inright(content, 404.0, 100.0, 40.0)
    barrier = inright(content, 290.0, 100.0, 40.0)
    divider = atright(content, 278.0, 32.0, 1.0, 36.0)
    speedBox = inright(content, 96.0, 170.0, 48.0)
    pause = inright(content, SPACE_LG, 64.0, 48.0)
    items += [
        ("Enemy", enemy, True), ("Barrier", barrier, True),
        ("Divider", divider, True), ("SpeedBox", speedBox, True),
        ("Pause", pause, True),
    ]

    # speed buttons (children of speedBox)
    speed_ch = []
    for i in range(4):
        speed_ch.append((f"  Speed{i}", at(speedBox, i * 44.0, 24.0, 38.0, 34.0), True))

    print("\n########## HudTopBar ##########")
    fails.append(check_siblings("TopBand siblings", items))
    fails.append(out_of_band("TopBand", b, items))
    fails.append(check_siblings("RoundModule children", round_ch))
    fails.append(check_siblings("SpeedBox children", speed_ch))
    fails.append(out_of_band("SpeedBox", speedBox, speed_ch))
    return any(fails)


# ---------------------------------------------------------------
# HudDeployBar  (bottom band)
# ---------------------------------------------------------------
def deploybar():
    b = band(False, BOTTOM_BAR_H)      # (0, 0, 1280, 112)
    fails = []

    items = []
    # left cluster + children
    lc = inleft(b, SPACE_LG, 150.0, 64.0)
    items.append(("LeftCluster", lc, True))
    lc_ch = [
        ("  Count", at(lc, 0.0, 40.0, 150.0, 14.0), True),
        ("  Targeting", atbottom(lc, 0.0, 0.0, 150.0, 30.0), True),
    ]

    # card group
    n = 5
    total = n * CARD_W + (n - 1) * CARD_GAP
    groupH = CARD_H + SPACE_MD
    group = incenter(b, 0.0, total, groupH)
    items.append(("CardGroup", group, True))
    cards = []
    for i in range(n):
        cards.append((f"Card{i}",
                      anchor(group, (0.0, 0.0), (0.0, 0.0),
                             (i * (CARD_W + CARD_GAP), 0.0), (CARD_W, CARD_H)),
                      True))
    items += cards

    # barrage cluster + children
    bc = inright(b, SPACE_LG, 86.0, 86.0)
    items.append(("BarrageCluster", bc, True))
    bc_ch = [
        ("  Glow", atcenter(bc, 43.0, 43.0, 96.0, 96.0), False),
        ("  Shell", atcenter(bc, 43.0, 43.0, 70.0, 70.0), True),
        ("  Hint", atbottom(bc, 0.0, 2.0, 86.0, 12.0), True),
    ]

    # Card0 internals (the card that previously overflowed)
    c0 = cards[0][1]
    c0_ch = [
        ("  TopBand", at(c0, 0.0, 2.0, CARD_W, 4.0), True),
        ("  Icon", atcenter(c0, CARD_W * 0.5, 32.0, 30.0, 30.0), True),
        ("  Name", at(c0, 0.0, 55.0, CARD_W, 16.0), True),
        ("  Coin", at(c0, 30.0, 73.0, 13.0, 13.0), True),
        ("  Cost", at(c0, 46.0, 73.0, 56.0, 16.0), True),
        ("  Role", at(c0, 65.0, 10.0, 48.0, 12.0), True),
        ("  IdxBadge", at(c0, 5.0, 13.0, 22.0, 18.0), True),
    ]

    print("\n########## HudDeployBar ##########")
    # CardGroup is a parent container (scaling transform), not a visual sibling;
    # exclude it from the sibling-overlap check but still report + bound-check it.
    for n, r, _s in [it for it in items if it[0] == "CardGroup"]:
        report("[container] " + n, r)
    bottom_siblings = [it for it in items if it[0] != "CardGroup"]
    fails.append(check_siblings("BottomBand siblings", bottom_siblings))
    fails.append(out_of_band("BottomBand", b, items))
    fails.append(check_siblings("LeftCluster children", lc_ch))
    fails.append(check_siblings("CardGroup cards", cards))
    fails.append(check_siblings("Card0 internals", c0_ch))
    fails.append(out_of_band("Card0", c0, c0_ch))
    fails.append(check_siblings("BarrageCluster children", bc_ch))
    fails.append(out_of_band("BarrageCluster (vs bottom band)", b, bc_ch))
    return any(fails)


if __name__ == "__main__":
    print("=" * 60)
    print(" HUD GEOMETRY VERIFIER (Arknights-style rewrite)")
    print("=" * 60)
    f_top = topbar()
    f_bot = deploybar()
    print("\n" + "=" * 60)
    if f_top or f_bot:
        print(" RESULT: FAIL - see !! marks above")
    else:
        print(" RESULT: PASS - no sibling overlaps, no solid out-of-band")
    print("=" * 60)

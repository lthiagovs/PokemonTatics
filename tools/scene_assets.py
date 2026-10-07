import argparse
import io
import os
import re
import sys
import xml.etree.ElementTree as ET
from datetime import date

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow is required: pip install Pillow")

DESCRIPTION = """Slice the downloaded reference sheets into game-ready atlases.

    new_assets/Maps/*.png     -> Content/Environment/dungeons.png + Data/dungeons.xml
    Material Symbols          -> Content/UI/icons.png             + Data/icons.xml
    new_assets/Effects/*.png  -> Content/Effects/*.png            + Data/effects.xml
    new_assets/Icons.png      -> Content/UI/items.png             + Data/items.xml

    python tools/scene_assets.py dungeons
    python tools/scene_assets.py icons
    python tools/scene_assets.py effects
    python tools/scene_assets.py items
    python tools/scene_assets.py all
"""

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "new_assets")
CONTENT = os.path.join(ROOT, "Content")
DATA = os.path.join(ROOT, "Data")
MGCB = os.path.join(CONTENT, "Content.mgcb")

GRID_COLOR = (128, 255, 255)
SHEET_BG = (0, 128, 128)
COLOR_KEY = (255, 0, 255)
EMPTY_COLORS = (SHEET_BG, GRID_COLOR, COLOR_KEY)
KEY_TOLERANCE = 0.02
TILE = 24
PITCH = 25
GROUP = 3

WALL_GROUP = 1
WALL_ALT_GROUP = 2
WALL_CORNER_ROW = 5
CORNER_CELLS = [(0, 0), (1, 0), (0, 1), (1, 1)]
GROUND_GROUP = 4
MAX_GROUPS = 11

THEMES_PER_ROW = 8
THEME_COLUMNS = 9
THEME_ROWS = 5

DECOR_GROUPS = [5, 6]
DECOR_CELL = (1, 1)


MGCB_BLOCK = """#begin {asset}
/importer:TextureImporter
/processor:TextureProcessor
/processorParam:ColorKeyColor=255,0,255,255
/processorParam:ColorKeyEnabled=False
/processorParam:GenerateMipmaps=False
/processorParam:PremultiplyAlpha=True
/processorParam:ResizeToPowerOfTwo=False
/processorParam:MakeSquare=False
/processorParam:TextureFormat=Color
/build:{asset}

"""


def register_assets(assets):
    text = io.open(MGCB, encoding="utf-8-sig").read()
    added = []
    for asset in assets:
        if f"/build:{asset}" in text:
            continue
        text = text.rstrip("\n") + "\n\n" + MGCB_BLOCK.format(asset=asset)
        added.append(asset)
    if added:
        io.open(MGCB, "w", encoding="utf-8-sig", newline="\r\n").write(text)
    return added


def write_xml(root, path):
    ET.indent(root, space="  ")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    ET.ElementTree(root).write(path, encoding="utf-8", xml_declaration=True)
    raw = io.open(path, encoding="utf-8").read()
    io.open(path, "w", encoding="utf-8", newline="\r\n").write(raw)


def theme_name(path):
    label = os.path.basename(path).split(" - ")[-1]
    label = os.path.splitext(label)[0]
    label = re.sub(r"\(\d+\)$", "", label).strip()
    return label


def detect_grid(image):
    width, height = image.size
    px = image.convert("RGB").load()
    columns = [x for x in range(width)
               if sum(1 for y in range(height) if px[x, y] == GRID_COLOR) > height * 0.35]
    rows = [y for y in range(height)
            if sum(1 for x in range(width) if px[x, y] == GRID_COLOR) > width * 0.25]
    if len(columns) < 4 or len(rows) < 4:
        return None
    return columns[0] + 1, rows[0] + 1


def block_is_full(image, origin_x, origin_y, column_group, row_group):
    px = image.convert("RGB").load()
    for row in range(GROUP):
        for col in range(GROUP):
            x = origin_x + (column_group * GROUP + col) * PITCH
            y = origin_y + (row_group * GROUP + row) * PITCH
            if x + TILE > image.width or y + TILE > image.height:
                return False
            solid = sum(1 for dx in range(0, TILE, 3) for dy in range(0, TILE, 3)
                        if px[x + dx, y + dy] not in EMPTY_COLORS)
            if solid < 30:
                return False
    return True


def find_ground_group(image, origin_x, origin_y):
    best = None
    for group in range(2, MAX_GROUPS):
        if group == WALL_GROUP:
            continue
        if not block_is_full(image, origin_x, origin_y, group, 0):
            continue
        tiles = read_block(image, origin_x, origin_y, group, 0)
        keyed = max(key_share(tile) for tile in tiles)
        preferred = 0 if group == GROUND_GROUP else 1
        if best is None or (keyed, preferred) < best[0]:
            best = ((keyed, preferred), group)
    return best[1] if best else None


def strip_key(tile):
    rgba = tile.convert("RGBA")
    raw = bytearray(rgba.tobytes())
    for i in range(0, len(raw), 4):
        if (raw[i], raw[i + 1], raw[i + 2]) == COLOR_KEY:
            raw[i] = raw[i + 1] = raw[i + 2] = raw[i + 3] = 0
    rgba.frombytes(bytes(raw))
    return rgba


def key_share(tile):
    raw = tile.convert("RGBA").tobytes()
    clear = sum(1 for i in range(3, len(raw), 4) if raw[i] < 128)
    return clear / float(len(raw) // 4)


def mean_colour(tile):
    raw = tile.convert("RGBA").tobytes()
    red = green = blue = count = 0
    for i in range(0, len(raw), 4):
        if raw[i + 3] < 128:
            continue
        red += raw[i]
        green += raw[i + 1]
        blue += raw[i + 2]
        count += 1
    if count == 0:
        return "000000"
    return "%02x%02x%02x" % (red // count, green // count, blue // count)


def read_block(image, origin_x, origin_y, column_group, row_group):
    tiles = []
    for row in range(GROUP):
        for col in range(GROUP):
            x = origin_x + (column_group * GROUP + col) * PITCH
            y = origin_y + (row_group * GROUP + row) * PITCH
            tiles.append(strip_key(image.crop((x, y, x + TILE, y + TILE))))
    return tiles


def read_cell(image, origin_x, origin_y, column_group, row_group, col, row):
    x = origin_x + (column_group * GROUP + col) * PITCH
    y = origin_y + (row_group * GROUP + row) * PITCH
    if x + TILE > image.width or y + TILE > image.height:
        return None
    crop = image.crop((x, y, x + TILE, y + TILE))
    px = crop.convert("RGB").load()
    solid = sum(1 for dx in range(0, TILE, 3) for dy in range(0, TILE, 3)
                if px[dx, dy] not in EMPTY_COLORS)
    return strip_key(crop) if solid >= 30 else None


def read_optional_block(image, origin_x, origin_y, column_group, row_group):
    tiles = []
    for row in range(GROUP):
        for col in range(GROUP):
            tiles.append(read_cell(image, origin_x, origin_y, column_group, row_group, col, row))
    return tiles


def read_corners(image, origin_x, origin_y):
    return [read_cell(image, origin_x, origin_y, WALL_GROUP, WALL_CORNER_ROW, col, row)
            for col, row in CORNER_CELLS]


def read_decor(image, origin_x, origin_y):
    col, row = DECOR_CELL
    found = []
    for group in DECOR_GROUPS:
        tile = read_cell(image, origin_x, origin_y, group, 0, col, row)
        if tile is not None:
            found.append(tile)
    return found


def cmd_dungeons(args):
    import glob

    sources = sorted(glob.glob(os.path.join(SOURCE, "Maps", "*.png")))
    if not sources:
        sys.exit(f"no sheets in {os.path.relpath(os.path.join(SOURCE, 'Maps'), ROOT)}")

    themes = []
    skipped = []

    for path in sources:
        name = theme_name(path)
        image = Image.open(path).convert("RGBA")
        grid = detect_grid(image)
        if grid is None:
            skipped.append((name, "no grid detected"))
            continue

        origin_x, origin_y = grid
        if not block_is_full(image, origin_x, origin_y, WALL_GROUP, 0):
            skipped.append((name, "no wall block"))
            continue

        ground_group = find_ground_group(image, origin_x, origin_y)
        if ground_group is None:
            skipped.append((name, "no ground block"))
            continue

        ground_tiles = read_block(image, origin_x, origin_y, ground_group, 0)
        wall_tiles = read_block(image, origin_x, origin_y, WALL_GROUP, 0)
        keyed = max(key_share(tile) for tile in ground_tiles + wall_tiles)
        if keyed > KEY_TOLERANCE:
            skipped.append((name, f"colour key in ground/wall ({keyed:.0%})"))
            continue

        themes.append({
            "name": name,
            "ground": ground_tiles,
            "walls": wall_tiles,
            "wall_alt": read_optional_block(image, origin_x, origin_y, WALL_ALT_GROUP, 0),
            "corners": read_corners(image, origin_x, origin_y),
            "decor": read_decor(image, origin_x, origin_y),
            "note": "" if ground_group == GROUND_GROUP else f"ground at group {ground_group}",
        })

    if not themes:
        sys.exit("nothing extracted")

    columns = THEMES_PER_ROW
    rows = (len(themes) + columns - 1) // columns
    cell_w = THEME_COLUMNS * TILE
    cell_h = THEME_ROWS * TILE
    atlas = Image.new("RGBA", (columns * cell_w, rows * cell_h), (0, 0, 0, 0))

    root = ET.Element("Dungeons")
    root.set("generated", date.today().isoformat())
    root.set("texture", "Environment/dungeons")
    root.set("tile", str(TILE))

    for index, theme in enumerate(themes):
        base_x = (index % columns) * cell_w
        base_y = (index // columns) * cell_h
        for slot, tile in enumerate(theme["ground"]):
            atlas.alpha_composite(tile, (base_x + slot * TILE, base_y))
        for slot, tile in enumerate(theme["walls"]):
            atlas.alpha_composite(tile, (base_x + slot * TILE, base_y + TILE))
        alt_mask = 0
        for slot, tile in enumerate(theme["wall_alt"]):
            if tile is None:
                continue
            atlas.alpha_composite(tile, (base_x + slot * TILE, base_y + TILE * 2))
            alt_mask |= 1 << slot
        for slot, tile in enumerate(theme["decor"]):
            atlas.alpha_composite(tile, (base_x + slot * TILE, base_y + TILE * 3))
        corner_count = 0
        for slot, tile in enumerate(theme["corners"]):
            if tile is None:
                break
            atlas.alpha_composite(tile, (base_x + slot * TILE, base_y + TILE * 4))
            corner_count += 1

        node = ET.SubElement(root, "Theme")
        node.set("name", theme["name"])
        node.set("x", str(base_x))
        node.set("y", str(base_y))
        node.set("decor", str(len(theme["decor"])))
        node.set("wallAlt", str(alt_mask))
        node.set("corners", str(corner_count))
        node.set("wall", mean_colour(theme["walls"][4]))
        node.set("ground", mean_colour(theme["ground"][4]))

    target = os.path.join(CONTENT, "Environment", "dungeons.png")
    os.makedirs(os.path.dirname(target), exist_ok=True)
    atlas.save(target)
    write_xml(root, os.path.join(DATA, "dungeons.xml"))
    added = register_assets(["Environment/dungeons.png"])

    print(f"  themes      {len(themes)}")
    print(f"  atlas       {atlas.width}x{atlas.height} -> Content/Environment/dungeons.png")
    print(f"  index       Data/dungeons.xml")
    print(f"  mgcb        {len(added)} new entry(ies)")
    for theme in themes:
        if theme["note"]:
            print(f"    {theme['name']}: {theme['note']}")
    if skipped:
        print(f"  skipped     {len(skipped)}")
        for name, why in skipped:
            print(f"    {name}: {why}")
    return 0


def trim(tile):
    box = tile.getbbox()
    return tile.crop(box) if box else tile


ICON_SOURCE = ("https://raw.githubusercontent.com/google/material-design-icons/master/png/"
               "{category}/{name}/materialiconsoutlined/48dp/2x/outline_{name}_black_48dp.png")

ICON_CATEGORIES = ["action", "navigation", "av", "content", "image", "toggle",
                   "alert", "social", "maps", "hardware", "device", "editor"]

ICON_NAMES = ["settings", "close", "refresh", "play_arrow", "favorite", "bolt",
              "shield", "star", "info", "volume_up", "volume_off", "music_note",
              "fullscreen", "fullscreen_exit", "power_settings_new", "check",
              "leaderboard", "emoji_events"]

ICON_VARIANTS = ((48, 16, 3), (36, 12, 3), (24, 12, 2))
ICON_CACHE = os.path.join(ROOT, "tools", ".cache", "icons")


def fetch_icon(name):
    import urllib.error
    import urllib.request

    os.makedirs(ICON_CACHE, exist_ok=True)
    cached = os.path.join(ICON_CACHE, name + ".png")
    if os.path.isfile(cached):
        return cached

    for category in ICON_CATEGORIES:
        url = ICON_SOURCE.format(category=category, name=name)
        try:
            with urllib.request.urlopen(url, timeout=30) as response:
                if response.status != 200:
                    continue
                payload = response.read()
        except (urllib.error.URLError, urllib.error.HTTPError, TimeoutError):
            continue

        io.open(cached, "wb").write(payload)
        return cached

    return None


def whiten(tile):
    rgba = tile.convert("RGBA")
    raw = bytearray(rgba.tobytes())
    for i in range(0, len(raw), 4):
        raw[i] = raw[i + 1] = raw[i + 2] = 255
    rgba.frombytes(bytes(raw))
    return rgba


def cmd_icons(args):
    icons = []
    missing = []

    for name in ICON_NAMES:
        path = fetch_icon(name)
        if path is None:
            missing.append(name)
            continue

        source = Image.open(path).convert("RGBA")
        for size, base, upscale in ICON_VARIANTS:
            if source.width % base != 0:
                missing.append(f"{name} @{size} (source {source.width}px)")
                continue

            image = source.reduce(source.width // base) if source.width != base else source
            image = trim(whiten(image))
            if upscale > 1:
                image = image.resize((image.width * upscale, image.height * upscale), Image.NEAREST)
            icons.append((name, size, image))

    if not icons:
        sys.exit("no icons downloaded")

    padding = 2
    columns = 8
    cell = max(size for size, _base, _up in ICON_VARIANTS) + padding
    rows = (len(icons) + columns - 1) // columns
    atlas = Image.new("RGBA", (columns * cell + padding, rows * cell + padding), (0, 0, 0, 0))

    root = ET.Element("Icons")
    root.set("generated", date.today().isoformat())
    root.set("texture", "UI/icons")

    for index, (name, size, image) in enumerate(icons):
        x = padding + (index % columns) * cell
        y = padding + (index // columns) * cell
        atlas.alpha_composite(image, (x, y))

        node = ET.SubElement(root, "Icon")
        node.set("name", name)
        node.set("size", str(size))
        node.set("x", str(x))
        node.set("y", str(y))
        node.set("w", str(image.width))
        node.set("h", str(image.height))

    target = os.path.join(CONTENT, "UI", "icons.png")
    os.makedirs(os.path.dirname(target), exist_ok=True)
    atlas.save(target)
    write_xml(root, os.path.join(DATA, "icons.xml"))
    added = register_assets(["UI/icons.png"])

    print(f"  icons       {len(icons)} ({len(ICON_NAMES)} names x {len(ICON_VARIANTS)} variants)")
    if missing:
        print(f"  missing     {len(missing)}")
        for name in missing:
            print(f"    {name}")
    print(f"  atlas       {atlas.width}x{atlas.height} -> Content/UI/icons.png")
    print(f"  index       Data/icons.xml")
    print(f"  mgcb        {len(added)} new entry(ies)")
    return 0


EFFECT_DIR = os.path.join(SOURCE, "Effects")
EFFECT_BG = (0, 128, 128)
EFFECT_TOLERANCE = 24
EFFECT_MIN_FRAMES = 4
EFFECT_MIN_RUN = 10
EFFECT_GAP = 3

EFFECT_PICKS = [
    ("acid", "Poison", 2, 0),
    ("flare", "Dragon", 14, 0),
    ("wave", "Dragon", 9, 0),
    ("spark", "Dragon", 5, 0),
    ("frost", "Dragon", 19, 0),
    ("quake", "Dragon", 15, 0),
    ("rock", "Dragon", 16, 0),
    ("nova", "Dragon", 18, 0),
    ("sludge", "Poison", 4, 11),
    ("sting", "Poison", 14, 0),
    ("gale", "Dragon", 0, 0),
    ("glimmer", "Poison", 13, 0),
    ("mindblast", "Dragon", 2, 0),
    ("hex", "Dragon", 11, 0),
    ("void", "Dragon", 10, 0),
    ("orb", "Dragon", 12, 0),
    ("slash", "Dragon", 20, 0),
    ("impact", "Dragon", 3, 0),
    ("burn", "Dragon", 4, 0),
    ("venom", "Poison", 10, 7),
    ("jolt", "Dragon", 6, 0),
    ("sparkle", "Poison", 15, 0),
]


def effect_mask(image):
    raw = image.convert("RGB").tobytes()
    width, height = image.size
    mask = bytearray(width * height)
    for i in range(width * height):
        o = i * 3
        delta = (abs(raw[o] - EFFECT_BG[0]) + abs(raw[o + 1] - EFFECT_BG[1])
                 + abs(raw[o + 2] - EFFECT_BG[2]))
        mask[i] = 1 if delta > EFFECT_TOLERANCE else 0
    return mask, width, height


def effect_strips(image):
    mask, width, height = effect_mask(image)

    bands = []
    start = None
    for y in range(height):
        row = mask[y * width:(y + 1) * width]
        filled = any(row)
        if filled and start is None:
            start = y
        elif not filled and start is not None:
            if y - start >= 12:
                bands.append((start, y))
            start = None

    strips = []
    for y0, y1 in bands:
        columns = [any(mask[y * width + x] for y in range(y0, y1)) for x in range(width)]

        runs = []
        run_start = None
        gap = 0
        for x, filled in enumerate(columns):
            if filled:
                if run_start is None:
                    run_start = x
                gap = 0
            elif run_start is not None:
                gap += 1
                if gap >= EFFECT_GAP:
                    runs.append((run_start, x - gap + 1))
                    run_start = None
                    gap = 0
        if run_start is not None:
            runs.append((run_start, width))

        runs = [r for r in runs if r[1] - r[0] >= EFFECT_MIN_RUN]
        if len(runs) >= EFFECT_MIN_FRAMES:
            strips.append((y0, y1, runs))

    return strips


def is_label(tile):
    if tile.width < tile.height * 1.8:
        return False

    raw = tile.convert("RGBA").tobytes()
    shades = set()
    for i in range(0, len(raw), 4):
        if raw[i + 3] < 128:
            continue
        shades.add((raw[i] // 32, raw[i + 1] // 32, raw[i + 2] // 32))
        if len(shades) > 6:
            return False

    return True


def cmd_effects(args):
    import glob

    sheets = {}
    for path in sorted(glob.glob(os.path.join(EFFECT_DIR, "*.png"))):
        tag = "Dragon" if "Dragon" in os.path.basename(path) else "Poison"
        sheets[tag] = Image.open(path).convert("RGBA")

    if not sheets:
        sys.exit(f"no sheets in {os.path.relpath(EFFECT_DIR, ROOT)}")

    indexed = {tag: effect_strips(image) for tag, image in sheets.items()}

    root = ET.Element("Effects")
    root.set("generated", date.today().isoformat())

    written = []
    missing = []

    for name, tag, index, limit in EFFECT_PICKS:
        strips = indexed.get(tag, [])
        if index >= len(strips):
            missing.append(f"{name} ({tag} #{index})")
            continue

        y0, y1, runs = strips[index]
        if limit > 0: runs = runs[:limit]
        sheet = sheets[tag]

        frames = []
        for a, b in runs:
            crop = clear_colour(strip_key(sheet.crop((a, y0, b, y1))), EFFECT_BG)
            box = crop.getbbox()
            frames.append(crop.crop(box) if box else crop)

        peak = max(f.width * f.height for f in frames)
        kept = [f for f in frames
                if f.width * f.height >= peak * 0.05
                and not is_label(f)]
        if len(kept) >= EFFECT_MIN_FRAMES:
            frames = kept

        cell = max(max(f.width for f in frames), max(f.height for f in frames))

        out = Image.new("RGBA", (cell, cell * len(frames)), (0, 0, 0, 0))
        for frame, image in enumerate(frames):
            out.alpha_composite(image, ((cell - image.width) // 2, frame * cell + (cell - image.height) // 2))

        target = os.path.join(CONTENT, "Effects", name + ".png")
        os.makedirs(os.path.dirname(target), exist_ok=True)
        out.save(target)

        node = ET.SubElement(root, "Effect")
        node.set("name", name)
        node.set("frames", str(len(frames)))
        node.set("cell", str(cell))
        written.append((name, cell, len(runs)))

    if not written:
        sys.exit("no effects extracted")

    write_xml(root, os.path.join(DATA, "effects.xml"))
    added = register_assets([f"Effects/{name}.png" for name, _cell, _frames in written])

    print(f"  effects     {len(written)}")
    for name, cell, frames in written:
        print(f"    {name:8s} {cell}px x {frames} frames")
    if missing:
        print(f"  missing     {len(missing)}")
        for name in missing:
            print(f"    {name}")
    print(f"  index       Data/effects.xml")
    print(f"  mgcb        {len(added)} new entry(ies)")
    return 0


def clear_colour(tile, colour):
    rgba = tile.convert("RGBA")
    raw = bytearray(rgba.tobytes())
    for i in range(0, len(raw), 4):
        if (abs(raw[i] - colour[0]) + abs(raw[i + 1] - colour[1])
                + abs(raw[i + 2] - colour[2])) <= EFFECT_TOLERANCE:
            raw[i] = raw[i + 1] = raw[i + 2] = raw[i + 3] = 0
    rgba.frombytes(bytes(raw))
    return rgba


ITEM_SHEET = os.path.join(SOURCE, "Icons.png")
ITEM_BG = (0, 128, 128)
ITEM_BAND = (285, 350)
ITEM_UPSCALE = 3

ITEM_PICKS = [
    (3, "power_band", "POWER BAND", "ATTACK", 25),
    (4, "guard_shell", "GUARD SHELL", "DEFENCE", 30),
    (13, "focus_sash", "FOCUS SASH", "HEALTH", 20),
    (53, "swift_feather", "SWIFT FEATHER", "SPEED", 25),
    (41, "mystic_orb", "MYSTIC ORB", "SPECIAL_POWER", 30),
    (44, "giant_crown", "GIANT CROWN", "GIANT", 35),
    (40, "blast_seed", "BLAST SEED", "SPLASH", 35),
    (73, "ember_charm", "EMBER CHARM", "BURN_HIT", 30),
    (67, "toxic_leaf", "TOXIC LEAF", "POISON_HIT", 30),
    (11, "static_band", "STATIC BAND", "PARALYSIS_HIT", 25),
    (21, "life_ring", "LIFE RING", "LIFESTEAL", 15),
    (50, "lucky_coin", "LUCKY COIN", "MANA_ON_KILL", 2),
]


def item_boxes(image):
    from collections import deque

    raw = image.convert("RGB").tobytes()
    width, height = image.size

    def solid(x, y):
        o = (y * width + x) * 3
        return (abs(raw[o] - ITEM_BG[0]) + abs(raw[o + 1] - ITEM_BG[1])
                + abs(raw[o + 2] - ITEM_BG[2])) > 20

    seen = bytearray(width * height)
    boxes = []

    for y in range(ITEM_BAND[0], min(ITEM_BAND[1], height)):
        for x in range(width):
            if not solid(x, y) or seen[y * width + x]:
                continue

            queue = deque([(y, x)])
            seen[y * width + x] = 1
            x0 = x1 = x
            y0 = y1 = y
            area = 0

            while queue:
                cy, cx = queue.popleft()
                area += 1
                x0 = min(x0, cx)
                x1 = max(x1, cx)
                y0 = min(y0, cy)
                y1 = max(y1, cy)

                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        ny, nx = cy + dy, cx + dx
                        if 0 <= ny < height and 0 <= nx < width and not seen[ny * width + nx] and solid(nx, ny):
                            seen[ny * width + nx] = 1
                            queue.append((ny, nx))

            w, h = x1 - x0 + 1, y1 - y0 + 1
            if 8 <= w <= 22 and 8 <= h <= 22 and area > 40:
                boxes.append((x0, y0, w, h))

    boxes.sort(key=lambda b: (b[1] // 14, b[0]))
    return boxes


def cmd_items(args):
    if not os.path.isfile(ITEM_SHEET):
        sys.exit(f"missing {os.path.relpath(ITEM_SHEET, ROOT)}")

    sheet = Image.open(ITEM_SHEET).convert("RGBA")
    boxes = item_boxes(sheet)

    cells = []
    missing = []

    for index, key, name, effect, value in ITEM_PICKS:
        if index >= len(boxes):
            missing.append(f"{key} (#{index} of {len(boxes)})")
            continue

        x, y, w, h = boxes[index]
        crop = clear_colour(sheet.crop((x, y, x + w, y + h)), ITEM_BG)
        box = crop.getbbox()
        if box:
            crop = crop.crop(box)

        crop = crop.resize((crop.width * ITEM_UPSCALE, crop.height * ITEM_UPSCALE), Image.NEAREST)
        cells.append((key, name, effect, value, crop))

    if not cells:
        sys.exit("no items extracted")

    padding = 2
    columns = 6
    cell = max(max(c[4].width for c in cells), max(c[4].height for c in cells)) + padding
    rows = (len(cells) + columns - 1) // columns
    atlas = Image.new("RGBA", (columns * cell + padding, rows * cell + padding), (0, 0, 0, 0))

    root = ET.Element("Items")
    root.set("generated", date.today().isoformat())
    root.set("texture", "UI/items")

    for index, (key, name, effect, value, image) in enumerate(cells):
        x = padding + (index % columns) * cell
        y = padding + (index // columns) * cell
        atlas.alpha_composite(image, (x, y))

        node = ET.SubElement(root, "Item")
        node.set("id", key)
        node.set("name", name)
        node.set("effect", effect)
        node.set("value", str(value))
        node.set("x", str(x))
        node.set("y", str(y))
        node.set("w", str(image.width))
        node.set("h", str(image.height))

    target = os.path.join(CONTENT, "UI", "items.png")
    os.makedirs(os.path.dirname(target), exist_ok=True)
    atlas.save(target)
    write_xml(root, os.path.join(DATA, "items.xml"))
    added = register_assets(["UI/items.png"])

    print(f"  items       {len(cells)}")
    if missing:
        print(f"  missing     {len(missing)}")
        for key in missing:
            print(f"    {key}")
    print(f"  atlas       {atlas.width}x{atlas.height} -> Content/UI/items.png")
    print(f"  index       Data/items.xml")
    print(f"  mgcb        {len(added)} new entry(ies)")
    return 0


def cmd_all(args):
    cmd_dungeons(args)
    print()
    cmd_icons(args)
    print()
    cmd_effects(args)
    print()
    cmd_items(args)
    return 0


def main():
    parser = argparse.ArgumentParser(
        description=DESCRIPTION, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("dungeons", help="build the dungeon tile atlas").set_defaults(func=cmd_dungeons)
    sub.add_parser("icons", help="build the Material icon atlas").set_defaults(func=cmd_icons)
    sub.add_parser("effects", help="slice the attack effect sheets").set_defaults(func=cmd_effects)
    sub.add_parser("items", help="slice the item icon sheet").set_defaults(func=cmd_items)
    sub.add_parser("all", help="build everything").set_defaults(func=cmd_all)

    args = parser.parse_args()
    sys.exit(args.func(args) or 0)


if __name__ == "__main__":
    main()

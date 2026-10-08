import argparse
import hashlib
import io
import json
import os
import re
import shutil
import sys
import tempfile
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile
from concurrent.futures import ThreadPoolExecutor
from datetime import date, datetime, timezone
from threading import Lock

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow is required: pip install Pillow")

DESCRIPTION = """Build the game's Pokemon roster and sprites from public data.

    stats / types / evolutions : https://pokeapi.co
    sprites / portraits        : https://sprites.pmdcollab.org

Run with no arguments for the interactive menu.

    python tools/pokemon_assets.py
    python tools/pokemon_assets.py fetch --range 1-151
    python tools/pokemon_assets.py stats
    python tools/pokemon_assets.py delete --range 152-251
    python tools/pokemon_assets.py report --prune
    python tools/pokemon_assets.py clear --yes
"""

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CONTENT = os.path.join(ROOT, "Content")
POKEMON_DIR = os.path.join(CONTENT, "Pokemons")
ROSTER_XML = os.path.join(ROOT, "Data", "pokemons.xml")
CACHE_DIR = os.path.join(ROOT, "tools", ".cache")
LOG_FILE = os.path.join(ROOT, "tools", "import.log")

COLUMNS = 3
ROWS = 5
PORTRAIT_SIZE = 40

RENDER_SCALE = 2

GAME_ROW_TO_PMD = [0, 4, 6, 5, 7]
PMD_DIRECTIONS = 8

COST_TIERS = [(300, 6), (350, 8), (420, 10), (490, 13)]
COST_TOP = 16
COST_NOT_PURCHASABLE = 0

EVOLUTION_LEVEL_DIVISOR = 4
EVOLUTION_LEVEL_MIN = 2
EVOLUTION_LEVEL_STEP = 2
EVOLUTION_LEVEL_FALLBACK = [6, 12, 18]
EVOLUTION_POWER_WEIGHT = 1.1
EVOLUTION_LEVEL_CAP = 12

TYPE_NAMES = {
    "normal": "NORMAL", "fire": "FIRE", "water": "WATER", "electric": "ELECTRIC",
    "grass": "GRASS", "ice": "ICE", "fighting": "FIGHT", "poison": "POISON",
    "ground": "GROUND", "flying": "FLY", "psychic": "PSYCHIC", "bug": "BUG",
    "rock": "ROCK", "ghost": "GHOST", "dragon": "DRAGON", "dark": "DARK",
    "steel": "STEEL", "fairy": "FAIRY",
}

POKEAPI = "https://pokeapi.co/api/v2"
SPRITECOLLAB = "https://raw.githubusercontent.com/PMDCollab/SpriteCollab/master"
USER_AGENT = "PokemonTactics-assets/1.0"

USE_CACHE = True
POLITE_DELAY = 0.05

ANSI = {
    "reset": "\033[0m", "bold": "\033[1m", "dim": "\033[2m",
    "cyan": "\033[96m", "green": "\033[92m", "yellow": "\033[93m",
    "red": "\033[91m", "grey": "\033[90m", "blue": "\033[94m",
}


def enable_ansi():
    if os.name != "nt":
        return sys.stdout.isatty()
    try:
        import ctypes
        kernel32 = ctypes.windll.kernel32
        handle = kernel32.GetStdHandle(-11)
        mode = ctypes.c_uint32()
        kernel32.GetConsoleMode(handle, ctypes.byref(mode))
        kernel32.SetConsoleMode(handle, mode.value | 0x0004)
        return True
    except Exception:
        return False


def can_print(text):
    try:
        text.encode(sys.stdout.encoding or "ascii")
        return True
    except Exception:
        return False


COLOR = enable_ansi()
FANCY = can_print("█░─·")

BAR_FULL, BAR_EMPTY = ("█", "░") if FANCY else ("#", ".")
LINE = "─" if FANCY else "-"
DOT = "·" if FANCY else "*"


def paint(text, color):
    if not COLOR or color not in ANSI:
        return text
    return ANSI[color] + text + ANSI["reset"]


def clear_screen():
    os.system("cls" if os.name == "nt" else "clear")


def rule(width=66):
    return paint(LINE * width, "grey")


class Progress:
    active = None
    WIDTH = 30

    def __init__(self, title, total):
        self.title = title
        self.total = max(1, total)
        self.count = 0
        self.label = ""
        self._lock = Lock()
        self._width = 0
        Progress.active = self
        self.draw()

    def advance(self, label="", step=1):
        with self._lock:
            self.count += step
            self.label = label
            self.draw()

    def draw(self):
        ratio = min(1.0, self.count / self.total)
        filled = int(self.WIDTH * ratio)
        bar = BAR_FULL * filled + BAR_EMPTY * (self.WIDTH - filled)
        text = (f"  {self.title:<16} {paint(bar, 'cyan')} "
                f"{self.count:>4}/{self.total:<4} {paint(self.label[:22], 'grey')}")
        self._width = len(text)
        sys.stdout.write("\r" + text + " " * 6)
        sys.stdout.flush()

    def erase(self):
        sys.stdout.write("\r" + " " * (self._width + 8) + "\r")
        sys.stdout.flush()

    def finish(self):
        self.count = self.total
        self.label = ""
        self.draw()
        sys.stdout.write("\n")
        sys.stdout.flush()
        Progress.active = None


def emit(text):
    bar = Progress.active
    if bar:
        bar.erase()
    print(text, flush=True)
    if bar:
        bar.draw()


class Report:
    def __init__(self):
        self._lock = Lock()
        self.failures = []
        self.warnings = []
        self.imported = []
        self.lines = []

    def info(self, message):
        with self._lock:
            self.lines.append(message)
            emit("  " + message)

    def ok(self, dex, name, detail):
        with self._lock:
            self.imported.append((dex, name, detail))
            self.lines.append(f"[ok]   #{dex:04d} {name}: {detail}")

    def warn(self, dex, name, stage, detail):
        with self._lock:
            self.warnings.append((dex, name, stage, detail))
            self.lines.append(f"[warn] #{dex:04d} {name}: {stage}: {detail}")
            emit("  " + paint(f"warn  #{dex:04d} {name}: {detail}", "yellow"))

    def fail(self, dex, name, stage, detail):
        with self._lock:
            self.failures.append((dex, name, stage, detail))
            self.lines.append(f"[FAIL] #{dex:04d} {name}: {stage}: {detail}")
            emit("  " + paint(f"fail  #{dex:04d} {name}: {stage}: {detail}", "red"))

    def flush(self, header):
        os.makedirs(os.path.dirname(LOG_FILE), exist_ok=True)
        stamp = datetime.now(timezone.utc).isoformat(timespec="seconds")
        with io.open(LOG_FILE, "a", encoding="utf-8") as handle:
            handle.write(f"\n===== {stamp} {header} =====\n")
            handle.write("\n".join(self.lines) + "\n")

    def summary(self):
        print()
        print("  " + rule())
        imported = paint(str(len(self.imported)), "green")
        warned = paint(str(len(self.warnings)), "yellow" if self.warnings else "grey")
        failed = paint(str(len(self.failures)), "red" if self.failures else "grey")
        print(f"  imported {imported}    warnings {warned}    failures {failed}")

        if self.failures:
            print()
            for dex, name, stage, detail in self.failures:
                print("  " + paint(f"#{dex:04d} {name:14} {stage:10} {detail}", "red"))

        print("  " + paint(f"log: {os.path.relpath(LOG_FILE, ROOT)}", "grey"))
        return 1 if self.failures else 0


def cache_path(url):
    digest = hashlib.sha1(url.encode("utf-8")).hexdigest()[:16]
    tail = re.sub(r"[^A-Za-z0-9._-]", "_", url)[-60:]
    return os.path.join(CACHE_DIR, f"{digest}_{tail}")


def http_get(url, attempts=3, timeout=25):
    os.makedirs(CACHE_DIR, exist_ok=True)
    cached = cache_path(url)
    if USE_CACHE and os.path.isfile(cached):
        return io.open(cached, "rb").read()

    last = None
    for attempt in range(attempts):
        try:
            request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
            with urllib.request.urlopen(request, timeout=timeout) as response:
                payload = response.read()
            handle, temp = tempfile.mkstemp(dir=CACHE_DIR)
            with os.fdopen(handle, "wb") as out:
                out.write(payload)
            os.replace(temp, cached)
            time.sleep(POLITE_DELAY)
            return payload
        except urllib.error.HTTPError as error:
            if error.code == 404:
                raise FileNotFoundError(f"404 {url}") from error
            last = error
        except Exception as error:
            last = error
        time.sleep(0.6 * (attempt + 1))

    raise RuntimeError(f"{type(last).__name__}: {last}")


def http_json(url):
    return json.loads(http_get(url))


def display_name(api_name):
    return "-".join(part.capitalize() for part in api_name.split("-"))


def folder_name(api_name):
    return re.sub(r"[^A-Za-z0-9]", "", display_name(api_name)) or api_name


def compute_cost(base_total, purchasable):
    if not purchasable:
        return COST_NOT_PURCHASABLE
    for threshold, cost in COST_TIERS:
        if base_total < threshold:
            return cost
    return COST_TOP


def compute_evolution_level(real_level, stage):
    if real_level:
        return max(EVOLUTION_LEVEL_MIN, round(real_level / EVOLUTION_LEVEL_DIVISOR))
    index = min(stage, len(EVOLUTION_LEVEL_FALLBACK) - 1)
    return EVOLUTION_LEVEL_FALLBACK[index]


def find_in_chain(node, api_name):
    if node["species"]["name"] == api_name:
        return node
    for child in node["evolves_to"]:
        found = find_in_chain(child, api_name)
        if found:
            return found
    return None


def flatten_chain(node, stage=0, out=None):
    if out is None:
        out = []

    nxt = node["evolves_to"][0] if node["evolves_to"] else None
    real_level = None
    if nxt:
        for detail in nxt["evolution_details"]:
            if detail.get("min_level"):
                real_level = detail["min_level"]
                break

    out.append({
        "api_name": node["species"]["name"],
        "stage": stage,
        "evolves_to": nxt["species"]["name"] if nxt else None,
        "real_level": real_level,
    })

    if nxt:
        flatten_chain(nxt, stage + 1, out)
    return out


def fetch_species_entry(api_name, chain_info):
    pokemon = http_json(f"{POKEAPI}/pokemon/{api_name}")
    species = http_json(f"{POKEAPI}/pokemon-species/{api_name}")

    stats = {entry["stat"]["name"]: entry["base_stat"] for entry in pokemon["stats"]}
    primary = pokemon["types"][0]["type"]["name"]
    if primary not in TYPE_NAMES:
        raise ValueError(f"unknown type: {primary}")

    return {
        "dex": species["id"],
        "api_name": api_name,
        "name": display_name(api_name),
        "folder": folder_name(api_name),
        "type": TYPE_NAMES[primary],
        "hp": stats["hp"],
        "atk": stats["attack"],
        "spatk": stats["special-attack"],
        "def": stats["defense"],
        "spdef": stats["special-defense"],
        "speed": stats["speed"],
        "base_total": sum(stats.values()),
        "cost": 0,
        "playable": False,
        "legendary": species["is_legendary"] or species["is_mythical"],
        "evolves_to": display_name(chain_info["evolves_to"]) if chain_info["evolves_to"] else None,
        "evolution_level": (
            compute_evolution_level(chain_info["real_level"], chain_info["stage"])
            if chain_info["evolves_to"] else 0
        ),
        "slice": 0,
        "scale": 0,
        "foot_y": 0,
        "foot_w": 0,
    }


def paste_frame(target, frame, cell, cell_x, cell_y):
    if frame.width > cell or frame.height > cell:
        raise ValueError(f"frame {frame.width}x{frame.height} does not fit cell {cell}")

    target.alpha_composite(frame, (cell_x + (cell - frame.width) // 2,
                                   cell_y + (cell - frame.height) // 2))


def walk_frame_size(anim_data_bytes):
    root = ET.fromstring(anim_data_bytes)
    anims = root.find("Anims")
    if anims is None:
        raise ValueError("AnimData.xml has no <Anims>")
    for anim in anims:
        if anim.findtext("Name") == "Walk":
            return int(anim.findtext("FrameWidth")), int(anim.findtext("FrameHeight"))
    raise ValueError("AnimData.xml has no Walk animation")


def pick_frames(count):
    if count <= 1:
        return [0, 0, 0]
    if count == 2:
        return [1, 0, 1]
    if count == 3:
        return [0, 1, 2]
    step_a = max(1, round(count / 3))
    step_b = min(count - 1, round(count * 2 / 3))
    if step_b == step_a:
        step_b = min(count - 1, step_a + 1)
    return [step_a, 0, step_b]


def compose_moveset(sheet_bytes, anim_data_bytes):
    frame_w, frame_h = walk_frame_size(anim_data_bytes)
    sheet = Image.open(io.BytesIO(sheet_bytes)).convert("RGBA")

    frame_count = sheet.width // frame_w
    direction_count = sheet.height // frame_h
    if frame_count < 1:
        raise ValueError(f"Walk-Anim.png smaller than one frame ({sheet.width}x{sheet.height})")
    if direction_count < PMD_DIRECTIONS:
        raise ValueError(f"Walk-Anim.png has {direction_count} directions, expected {PMD_DIRECTIONS}")

    columns = pick_frames(frame_count)
    cell = max(frame_w, frame_h)
    out = Image.new("RGBA", (cell * COLUMNS, cell * ROWS), (0, 0, 0, 0))

    for row, pmd_row in enumerate(GAME_ROW_TO_PMD):
        for col, frame_index in enumerate(columns):
            left = frame_index * frame_w
            top = pmd_row * frame_h
            frame = sheet.crop((left, top, left + frame_w, top + frame_h))
            paste_frame(out, frame, cell, col * cell, row * cell)

    return out, cell, frame_count, columns


def foot_metrics(moveset, cell):
    idle = moveset.crop((cell, 0, cell * 2, cell))
    box = idle.getbbox()
    if box is None:
        return cell, cell // 2
    return box[3], max(1, box[2] - box[0])


def fit_square(image, side):
    source = max(image.width, image.height)
    square = Image.new("RGBA", (source, source), (0, 0, 0, 0))
    square.alpha_composite(image, ((source - image.width) // 2, (source - image.height) // 2))

    if source == side:
        return square
    if source < side and side % source == 0:
        return square.resize((side, side), Image.NEAREST)
    if source > side and source % side == 0:
        return square.reduce(source // side)
    return square


def compose_portrait(portrait_bytes):
    return fit_square(Image.open(io.BytesIO(portrait_bytes)).convert("RGBA"), PORTRAIT_SIZE)


def portrait_from_moveset(moveset, cell):
    face = moveset.crop((cell, 0, cell * 2, cell))
    box = face.getbbox()
    if box:
        face = face.crop(box)
    return fit_square(face, PORTRAIT_SIZE)


def download_assets(entry, report, keep_existing):
    dex = entry["dex"]
    target_dir = os.path.join(POKEMON_DIR, entry["folder"])
    moveset_path = os.path.join(target_dir, "moveset.png")
    portrait_path = os.path.join(target_dir, "portrait.png")

    if keep_existing and os.path.isfile(moveset_path):
        existing = Image.open(moveset_path).convert("RGBA")
        cell = existing.width // COLUMNS
        entry["slice"] = cell
        entry["scale"] = RENDER_SCALE
        entry["foot_y"], entry["foot_w"] = foot_metrics(existing, cell)
        return True

    try:
        sheet_bytes = http_get(f"{SPRITECOLLAB}/sprite/{dex:04d}/Walk-Anim.png")
        anim_bytes = http_get(f"{SPRITECOLLAB}/sprite/{dex:04d}/AnimData.xml")
    except FileNotFoundError:
        report.fail(dex, entry["name"], "sprite", "no Walk-Anim.png on Sprite Collab")
        return False
    except Exception as error:
        report.fail(dex, entry["name"], "sprite", str(error))
        return False

    try:
        moveset, cell, frame_count, columns = compose_moveset(sheet_bytes, anim_bytes)
    except Exception as error:
        report.fail(dex, entry["name"], "moveset", str(error))
        return False

    entry["slice"] = cell
    entry["scale"] = RENDER_SCALE
    entry["foot_y"], entry["foot_w"] = foot_metrics(moveset, cell)

    os.makedirs(target_dir, exist_ok=True)
    moveset.save(moveset_path)

    try:
        compose_portrait(http_get(f"{SPRITECOLLAB}/portrait/{dex:04d}/Normal.png")).save(portrait_path)
    except FileNotFoundError:
        portrait_from_moveset(moveset, cell).save(portrait_path)
        report.warn(dex, entry["name"], "portrait", "no Normal.png, derived from the sprite")
    except Exception as error:
        portrait_from_moveset(moveset, cell).save(portrait_path)
        report.warn(dex, entry["name"], "portrait", f"{error}, derived from the sprite")

    report.ok(dex, entry["name"],
              f"cell {cell} x{entry['scale']} ({cell * entry['scale']}px), "
              f"{frame_count} frames -> columns {columns}")
    return True


def attr_int(node, name, fallback=None):
    raw = node.get(name) if node is not None else None
    if raw is None or not re.fullmatch(r"-?\d+", raw.strip()):
        if fallback is None:
            tag = node.tag if node is not None else "?"
            raise ValueError(f"attribute '{name}' missing or not a number on <{tag}>")
        return fallback
    return int(raw)


def read_existing_roster():
    if not os.path.isfile(ROSTER_XML):
        return {}
    root = ET.parse(ROSTER_XML).getroot()
    existing = {}
    for node in root.findall("Pokemon"):
        stats = node.find("Stats")
        sprite = node.find("Sprite")
        evolution = node.find("Evolution")
        existing[node.get("name")] = {
            "dex": attr_int(node, "dex", 0),
            "name": node.get("name"),
            "folder": sprite.get("folder") if sprite is not None else node.get("name"),
            "type": node.get("type"),
            "cost": 0,
            "playable": True,
            "legendary": node.get("legendary", "false") == "true",
            "hp": attr_int(stats, "hp"), "atk": attr_int(stats, "atk"),
            "spatk": attr_int(stats, "spatk"), "def": attr_int(stats, "def"),
            "spdef": attr_int(stats, "spdef"), "speed": attr_int(stats, "speed"),
            "base_total": attr_int(node, "baseTotal", 0),
            "slice": attr_int(sprite, "slice", 40) if sprite is not None else 40,
            "scale": attr_int(sprite, "scale", 3) if sprite is not None else 3,
            "foot_y": attr_int(sprite, "footY", 0) if sprite is not None else 0,
            "foot_w": attr_int(sprite, "footW", 0) if sprite is not None else 0,
            "evolves_to": evolution.get("to") if evolution is not None else None,
            "evolution_level": 0,
        }

    derive_roster_fields(list(existing.values()))
    return existing


def write_roster(entries):
    entries = sorted(entries, key=lambda item: item["dex"])
    known = {entry["name"] for entry in entries}

    root = ET.Element("Pokedex")
    root.set("generated", date.today().isoformat())
    root.set("source", "pokeapi.co + sprites.pmdcollab.org")

    for entry in entries:
        node = ET.SubElement(root, "Pokemon")
        node.set("dex", str(entry["dex"]))
        node.set("name", entry["name"])
        node.set("type", entry["type"])
        node.set("baseTotal", str(entry["base_total"]))
        if entry["legendary"]:
            node.set("legendary", "true")

        stats = ET.SubElement(node, "Stats")
        for key in ("hp", "atk", "spatk", "def", "spdef", "speed"):
            stats.set(key, str(entry[key]))

        sprite = ET.SubElement(node, "Sprite")
        sprite.set("folder", entry["folder"])
        sprite.set("slice", str(entry["slice"]))
        sprite.set("scale", str(entry["scale"]))
        sprite.set("footY", str(entry["foot_y"]))
        sprite.set("footW", str(entry["foot_w"]))

        if entry["evolves_to"] and entry["evolves_to"] in known:
            evolution = ET.SubElement(node, "Evolution")
            evolution.set("to", entry["evolves_to"])

    ET.indent(root, space="  ")
    os.makedirs(os.path.dirname(ROSTER_XML), exist_ok=True)
    ET.ElementTree(root).write(ROSTER_XML, encoding="utf-8", xml_declaration=True)

    raw = io.open(ROSTER_XML, encoding="utf-8").read()
    io.open(ROSTER_XML, "w", encoding="utf-8", newline="\r\n").write(raw)


def parse_range(text):
    match = re.fullmatch(r"\s*(\d+)\s*-\s*(\d+)\s*", text) or re.fullmatch(r"\s*(\d+)\s*", text)
    if not match:
        raise argparse.ArgumentTypeError("use --range 1-151 or --range 25")
    first = int(match.group(1))
    last = int(match.group(2)) if match.lastindex == 2 else first
    if first < 1 or last < first:
        raise argparse.ArgumentTypeError("invalid range")
    return first, last


def resolve_chains(first, last, report):
    planned = {}
    bar = Progress("chains", last - first + 1)

    for dex in range(first, last + 1):
        bar.advance(f"#{dex}")
        try:
            species = http_json(f"{POKEAPI}/pokemon-species/{dex}")
        except FileNotFoundError:
            report.fail(dex, f"#{dex}", "pokedex", "not on PokeAPI")
            continue
        except Exception as error:
            report.fail(dex, f"#{dex}", "pokedex", str(error))
            continue

        try:
            chain = http_json(species["evolution_chain"]["url"])
        except Exception as error:
            report.fail(dex, species["name"], "evolution", str(error))
            continue

        start = find_in_chain(chain["chain"], species["name"])
        if start is None:
            report.warn(dex, species["name"], "evolution", "missing from its own chain")
            continue

        for info in flatten_chain(start):
            planned.setdefault(info["api_name"], info)

    bar.finish()
    return planned


def resolve_costs_and_levels(entries):
    targets = {entry["evolves_to"] for entry in entries if entry["evolves_to"]}
    for entry in entries:
        entry["playable"] = entry["name"] not in targets
        entry["cost"] = compute_cost(entry["base_total"], entry["playable"])

    by_name = {entry["name"]: entry for entry in entries}
    for entry in entries:
        if not entry["playable"]:
            continue
        previous = 0
        current = entry
        while current["evolves_to"] and current["evolves_to"] in by_name:
            nxt = by_name[current["evolves_to"]]

            ratio = nxt["base_total"] / max(1, current["base_total"])
            scaled = current["evolution_level"] * (1 + max(0.0, ratio - 1.0) * EVOLUTION_POWER_WEIGHT)

            current["evolution_level"] = min(EVOLUTION_LEVEL_CAP,
                                             max(int(round(scaled)),
                                                 previous + EVOLUTION_LEVEL_STEP,
                                                 EVOLUTION_LEVEL_MIN))
            previous = current["evolution_level"]
            current = nxt


def cmd_fetch(args):
    global USE_CACHE
    USE_CACHE = not args.refresh

    first, last = args.range
    report = Report()
    report.info(f"pokedex #{first}..#{last}  (sprites: {'no' if args.skip_sprites else 'yes'})")

    planned = resolve_chains(first, last, report)
    report.info(f"{len(planned)} species in the chains (evolutions outside the range included)")

    entries = []
    bar = Progress("stats", len(planned))

    def load(api_name):
        bar.advance(display_name(api_name))
        try:
            return fetch_species_entry(api_name, planned[api_name])
        except Exception as error:
            report.fail(0, display_name(api_name), "stats", str(error))
            return None

    with ThreadPoolExecutor(max_workers=args.workers) as pool:
        for entry in pool.map(load, sorted(planned)):
            if entry:
                entries.append(entry)
    bar.finish()

    resolve_costs_and_levels(entries)

    if args.no_legendary:
        before = len(entries)
        legendary = {entry["name"] for entry in entries if entry["legendary"]}
        entries = [entry for entry in entries if not entry["legendary"]]
        for entry in entries:
            if entry["evolves_to"] in legendary:
                entry["evolves_to"] = None
                entry["evolution_level"] = 0
        report.info(f"legendaries removed: {before - len(entries)}")

    if not args.skip_sprites:
        sprite_bar = Progress("sprites", len(entries))

        def grab(entry):
            sprite_bar.advance(entry["name"])
            return entry, download_assets(entry, report, args.keep_existing)

        with ThreadPoolExecutor(max_workers=args.workers) as pool:
            results = list(pool.map(grab, entries))
        sprite_bar.finish()

        usable = [entry for entry, ok in results if ok]
        dropped = [entry for entry, ok in results if not ok]

        names = {entry["name"] for entry in usable}
        for entry in usable:
            if entry["evolves_to"] and entry["evolves_to"] not in names:
                report.warn(entry["dex"], entry["name"], "evolution",
                            f"{entry['evolves_to']} has no sprite, link dropped")
                entry["evolves_to"] = None
                entry["evolution_level"] = 0
        entries = usable
        if dropped:
            report.info(f"{len(dropped)} without sprites, left out of the roster")

    merged = read_existing_roster()
    for entry in entries:
        merged[entry["name"]] = entry

    if not merged:
        report.info("nothing to write")
        report.flush(f"fetch {first}-{last}")
        return report.summary()

    write_roster(list(merged.values()))

    playable = sum(1 for entry in merged.values() if entry["cost"] > 0)
    report.info(f"roster: {len(merged)} species ({playable} purchasable) "
                f"-> {os.path.relpath(ROSTER_XML, ROOT)}")

    report.flush(f"fetch {first}-{last}")
    return report.summary()


def cmd_sprites(args):
    global USE_CACHE
    USE_CACHE = not args.refresh

    report = Report()
    entries = list(read_existing_roster().values())
    if not entries:
        report.info("the roster is empty, nothing to download")
        report.flush("sprites")
        return report.summary()

    bar = Progress("sprites", len(entries))

    def grab(entry):
        bar.advance(entry["name"])
        return download_assets(entry, report, args.keep_existing)

    with ThreadPoolExecutor(max_workers=args.workers) as pool:
        list(pool.map(grab, entries))
    bar.finish()

    report.flush("sprites")
    return report.summary()


def resolve_sprite_dir(source, workspace):
    if os.path.isdir(source):
        return source
    with zipfile.ZipFile(source) as archive:
        archive.extractall(workspace)
    for current, _dirs, files in os.walk(workspace):
        if "Walk-Anim.png" in files and "AnimData.xml" in files:
            return current
    raise SystemExit("no Walk-Anim.png + AnimData.xml inside the zip")


def cmd_import(args):
    workspace = tempfile.mkdtemp(prefix="pmd_")
    try:
        sprite_dir = resolve_sprite_dir(args.sprite, workspace)
        sheet_bytes = io.open(os.path.join(sprite_dir, "Walk-Anim.png"), "rb").read()
        anim_bytes = io.open(os.path.join(sprite_dir, "AnimData.xml"), "rb").read()
        moveset, cell, frame_count, columns, _ = compose_moveset(sheet_bytes, anim_bytes)

        target_dir = os.path.join(POKEMON_DIR, args.name)
        os.makedirs(target_dir, exist_ok=True)
        moveset.save(os.path.join(target_dir, "moveset.png"))

        portrait_path = os.path.join(target_dir, "portrait.png")
        if args.portrait:
            compose_portrait(io.open(args.portrait, "rb").read()).save(portrait_path)
            note = "from " + os.path.basename(args.portrait)
        elif os.path.isfile(portrait_path):
            note = "kept"
        else:
            portrait_from_moveset(moveset, cell).save(portrait_path)
            note = "derived from the sprite"

        print(f"  {args.name}: cell {cell} x{RENDER_SCALE}, {frame_count} frames -> "
              f"columns {columns}, portrait {note}")
        print("  " + paint(f"add the entry to {os.path.relpath(ROSTER_XML, ROOT)}", "yellow"))
    finally:
        shutil.rmtree(workspace, ignore_errors=True)
    return 0


def each_moveset():
    if not os.path.isdir(POKEMON_DIR):
        return
    for name in sorted(os.listdir(POKEMON_DIR)):
        path = os.path.join(POKEMON_DIR, name, "moveset.png")
        if os.path.isfile(path):
            yield name, path


def derive_roster_fields(entries):
    targets = {entry["evolves_to"] for entry in entries if entry["evolves_to"]}
    for entry in entries:
        entry["playable"] = entry["name"] not in targets
        entry["cost"] = compute_cost(entry["base_total"], entry["playable"])
    return entries


def cmd_report(args):
    roster = read_existing_roster()
    by_folder = {entry["folder"]: entry for entry in roster.values()}
    problems = []
    orphans = []

    header = (f"  {'folder':16} {'moveset':12} {'cell':6} {'scale':6} "
              f"{'screen':7} {'portrait':9} roster")
    print(paint(header, "bold"))

    for name, path in each_moveset():
        sheet = Image.open(path)
        entry = by_folder.get(name)

        cell = sheet.width // COLUMNS
        scale = entry["scale"] if entry else RENDER_SCALE

        if sheet.width % COLUMNS or sheet.height % ROWS:
            problems.append(f"{name}: {sheet.width}x{sheet.height} does not split into {COLUMNS}x{ROWS}")
        elif sheet.height // ROWS != cell:
            problems.append(f"{name}: cells are not square ({cell} x {sheet.height // ROWS})")

        if entry and entry["slice"] != cell:
            problems.append(f"{name}: xml says slice {entry['slice']}, file has {cell}")

        portrait_path = os.path.join(POKEMON_DIR, name, "portrait.png")
        portrait = "-"
        if os.path.isfile(portrait_path):
            size = Image.open(portrait_path).size
            portrait = f"{size[0]}x{size[1]}"
            if size[0] != size[1]:
                problems.append(f"{name}: portrait {size[0]}x{size[1]} is not square")

        if entry is None:
            if args.prune:
                orphans.append(name)
            else:
                problems.append(f"{name}: folder has no roster entry")

        print(f"  {name:16} {sheet.width}x{sheet.height:<8} {cell:<6} x{scale:<5} "
              f"{cell * scale:<7} {portrait:9} "
              f"{'yes' if entry else 'NO'}")

    missing = [entry["folder"] for entry in roster.values()
               if not os.path.isfile(os.path.join(POKEMON_DIR, entry["folder"], "moveset.png"))]
    problems += [f"{folder}: in the roster but has no sprite" for folder in missing]

    if orphans:
        for folder in orphans:
            shutil.rmtree(os.path.join(POKEMON_DIR, folder), ignore_errors=True)
        print()
        print("  " + paint(f"{len(orphans)} orphan folder(s) removed: {', '.join(orphans)}", "yellow"))

    playable = sum(1 for entry in roster.values() if entry["cost"] > 0)
    print()
    print(f"  roster: {len(roster)} species, {playable} purchasable")
    if problems:
        print()
        print("  " + paint(f"{len(problems)} problem(s):", "red"))
        for problem in problems:
            print("  " + paint("  " + problem, "red"))
        return 1
    print("  " + paint("no integrity problems", "green"))
    return 0


def remove_assets(folders):
    if not folders:
        return
    for folder in folders:
        shutil.rmtree(os.path.join(POKEMON_DIR, folder), ignore_errors=True)


def cmd_delete(args):
    first, last = args.range
    roster = read_existing_roster()
    if not roster:
        print("  " + paint("roster is empty", "yellow"))
        return 0

    doomed = {name: entry for name, entry in roster.items()
              if first <= entry["dex"] <= last}
    if not doomed:
        print("  " + paint(f"no species in #{first}..#{last}", "yellow"))
        return 0

    for name in doomed:
        roster.pop(name)

    relinked = []
    for entry in roster.values():
        if entry["evolves_to"] in doomed:
            relinked.append(f"{entry['name']} -> {entry['evolves_to']}")
            entry["evolves_to"] = None
            entry["evolution_level"] = 0

    remaining = list(roster.values())
    before = {entry["name"] for entry in remaining if entry["cost"] > 0}
    resolve_costs_and_levels(remaining)
    promoted = sorted({entry["name"] for entry in remaining if entry["cost"] > 0} - before)

    remove_assets([entry["folder"] for entry in doomed.values()])

    if remaining:
        write_roster(remaining)
    elif os.path.isfile(ROSTER_XML):
        os.remove(ROSTER_XML)

    print(f"  removed {paint(str(len(doomed)), 'yellow')} species from #{first}..#{last}")
    print("    " + ", ".join(sorted(doomed)))
    if relinked:
        print(f"  evolution links dropped: {', '.join(relinked)}")
    if promoted:
        print(f"  now purchasable: {', '.join(promoted)}")
    print(f"  roster: {paint(str(len(remaining)), 'green')} species left")
    return 0


def cmd_clear(args):
    roster = read_existing_roster()
    folders = sorted({entry["folder"] for entry in roster.values()}
                     | {name for name, _ in each_moveset()})

    if not folders and not os.path.isfile(ROSTER_XML):
        print("  " + paint("nothing to clear", "yellow"))
        return 0

    if not args.yes:
        print("  " + paint(f"this deletes {len(folders)} sprite folder(s) "
                           f"and {os.path.relpath(ROSTER_XML, ROOT)}", "yellow"))
        if not ask_yes("Are you sure?", False):
            print("  cancelled")
            return 0

    remove_assets(folders)
    if os.path.isfile(ROSTER_XML):
        os.remove(ROSTER_XML)

    print("  " + paint(f"{len(folders)} folder(s) and the roster removed", "green"))
    return 0


def bar_for(count, widest, width=22):
    return BAR_FULL * max(1, round(count * width / max(1, widest)))


def cmd_stats(args):
    roster = read_existing_roster()
    if not roster:
        print("  " + paint("roster is empty, run an import first", "yellow"))
        return 0

    entries = sorted(roster.values(), key=lambda item: item["dex"])
    purchasable = [entry for entry in entries if entry["cost"] > 0]
    evolved = [entry for entry in entries if entry["cost"] == 0]
    legendary = [entry for entry in entries if entry["legendary"]]

    print("  " + paint("ROSTER", "bold"))
    print(f"    species         {len(entries)}")
    print(f"    purchasable     {len(purchasable)}")
    print(f"    evolved forms   {len(evolved)}")
    print(f"    legendary       {len(legendary)}")
    print(f"    dex range       #{entries[0]['dex']} .. #{entries[-1]['dex']}")

    if not purchasable:
        return 0

    by_type = {}
    for entry in purchasable:
        by_type[entry["type"]] = by_type.get(entry["type"], 0) + 1
    widest = max(by_type.values())
    print()
    print("  " + paint("TYPES (purchasable)", "bold"))
    for name, count in sorted(by_type.items(), key=lambda item: (-item[1], item[0])):
        print(f"    {name:9} {count:3}  {paint(bar_for(count, widest), 'cyan')}")

    by_cost = {}
    for entry in purchasable:
        by_cost[entry["cost"]] = by_cost.get(entry["cost"], 0) + 1
    widest = max(by_cost.values())
    print()
    print("  " + paint("COST", "bold"))
    for cost, count in sorted(by_cost.items()):
        print(f"    {cost:3} mana {count:3}  {paint(bar_for(count, widest), 'cyan')}")

    totals = [entry["base_total"] for entry in purchasable]
    strongest = max(purchasable, key=lambda entry: entry["base_total"])
    weakest = min(purchasable, key=lambda entry: entry["base_total"])
    fastest = max(purchasable, key=lambda entry: entry["speed"])
    toughest = max(purchasable, key=lambda entry: entry["hp"])
    print()
    print("  " + paint("POWER (purchasable)", "bold"))
    print(f"    base total      {min(totals)} .. {max(totals)}    avg {sum(totals) // len(totals)}")
    print(f"    strongest       {strongest['name']} ({strongest['base_total']})")
    print(f"    weakest         {weakest['name']} ({weakest['base_total']})")
    print(f"    fastest         {fastest['name']} ({fastest['speed']} speed)")
    print(f"    tankiest        {toughest['name']} ({toughest['hp']} hp)")

    links = {entry["name"]: entry["evolves_to"] for entry in entries if entry["evolves_to"]}
    longest_name, longest = "", 0
    for entry in purchasable:
        length, current = 1, entry["name"]
        while current in links:
            current = links[current]
            length += 1
        if length > longest:
            longest, longest_name = length, entry["name"]
    print()
    print("  " + paint("EVOLUTION", "bold"))
    print(f"    links           {len(links)}")
    print(f"    with evolution  {sum(1 for entry in purchasable if entry['evolves_to'])}"
          f" of {len(purchasable)} purchasable")
    if longest_name:
        print(f"    longest chain   {longest_name} ({longest} forms)")

    by_cell = {}
    for entry in entries:
        key = (entry["slice"], entry["scale"])
        by_cell[key] = by_cell.get(key, 0) + 1
    draws = [entry["slice"] * entry["scale"] for entry in entries]
    disk = sum(os.path.getsize(path) for _, path in each_moveset())
    print()
    print("  " + paint("SPRITES", "bold"))
    for (cell, scale), count in sorted(by_cell.items()):
        print(f"    cell {cell:3} x{scale}   -> {cell * scale:3} px   {count:3} species")
    print(f"    draw size       {min(draws)} .. {max(draws)} px")
    print(f"    on disk         {disk / 1024:.0f} KB")
    return 0


def banner():
    clear_screen()
    print()
    print("  " + paint(f"POKEMON TACTICS  {DOT}  ASSET IMPORTER", "bold"))
    print("  " + rule())


def status_block():
    roster = {}
    try:
        roster = read_existing_roster()
    except Exception as error:
        print("  " + paint(f"unreadable roster: {error}", "red"))

    playable = sum(1 for entry in roster.values() if entry["cost"] > 0)
    sprites = sum(1 for _ in each_moveset())

    print(f"  roster    {paint(str(len(roster)), 'green')} species, "
          f"{paint(str(playable), 'green')} purchasable")
    print(f"  sprites   {paint(str(sprites), 'green')} folders in Content/Pokemons")
    print(f"  sources   {paint('pokeapi.co', 'blue')} + {paint('sprites.pmdcollab.org', 'blue')}")
    print()


MENU = [
    ("1", "Import a Pokedex range"),
    ("2", "Roster statistics"),
    ("3", "Verify assets and roster"),
    ("4", "Verify and remove orphan folders"),
    ("5", "Import a local pack (.zip or folder)"),
    ("6", "Delete a Pokedex range"),
    ("7", "Clear everything"),
    ("0", "Quit"),
]


def ask(prompt, default=""):
    suffix = f" [{default}]" if default else ""
    try:
        answer = input(f"  {paint(prompt + suffix, 'bold')}: ").strip()
    except (EOFError, KeyboardInterrupt):
        print()
        return None
    return answer or default


def ask_yes(prompt, default=False):
    answer = ask(prompt + " (y/n)", "y" if default else "n")
    return bool(answer) and answer.lower().startswith("y")


def pause():
    try:
        input("\n  " + paint("Enter to go back to the menu", "grey"))
    except (EOFError, KeyboardInterrupt):
        pass


class Options:
    def __init__(self, **values):
        self.__dict__.update(values)


def run_interactive_fetch():
    raw = ask("Pokedex range (e.g. 1-151)", "1-151")
    if raw is None:
        return
    try:
        bounds = parse_range(raw)
    except argparse.ArgumentTypeError as error:
        print("  " + paint(str(error), "red"))
        return

    no_legendary = ask_yes("Drop legendaries?", False)
    skip_sprites = ask_yes("Skip sprite downloads?", False)
    refresh = ask_yes("Ignore the cache and pull everything again?", False)

    print()
    cmd_fetch(Options(range=bounds, workers=4, no_legendary=no_legendary,
                      skip_sprites=skip_sprites, keep_existing=False, refresh=refresh))


def run_interactive_delete():
    raw = ask("Pokedex range to delete (e.g. 152-251)")
    if not raw:
        return
    try:
        bounds = parse_range(raw)
    except argparse.ArgumentTypeError as error:
        print("  " + paint(str(error), "red"))
        return

    print()
    cmd_delete(Options(range=bounds))


def run_interactive_import():
    source = ask("Path to the .zip or folder")
    if not source:
        return
    source = source.strip('"').strip("'")
    if not os.path.exists(source):
        print("  " + paint("path not found", "red"))
        return

    name = ask("Pokemon name (folder under Content/Pokemons)")
    if not name:
        return

    portrait = ask("Normal.png portrait (optional, Enter skips)")
    print()
    try:
        cmd_import(Options(name=name, sprite=source, portrait=portrait or None))
    except SystemExit as error:
        print("  " + paint(str(error), "red"))


def interactive():
    while True:
        banner()
        status_block()
        for key, label in MENU:
            print(f"  {paint('[' + key + ']', 'cyan')} {label}")
        print()

        choice = ask("choice")
        if choice is None or choice == "0":
            print("\n  see you\n")
            return 0

        print()
        if choice == "1":
            run_interactive_fetch()
        elif choice == "2":
            cmd_stats(Options())
        elif choice == "3":
            cmd_report(Options(prune=False))
        elif choice == "4":
            cmd_report(Options(prune=True))
        elif choice == "5":
            run_interactive_import()
        elif choice == "6":
            run_interactive_delete()
        elif choice == "7":
            cmd_clear(Options(yes=False))
        else:
            print("  " + paint("invalid option", "yellow"))

        pause()


def main():
    parser = argparse.ArgumentParser(
        description=DESCRIPTION, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command")

    fetcher = sub.add_parser("fetch", help="download the pokedex and sprites, write Data/pokemons.xml")
    fetcher.add_argument("--range", required=True, type=parse_range, help="e.g. 1-151")
    fetcher.add_argument("--workers", type=int, default=4)
    fetcher.add_argument("--no-legendary", action="store_true")
    fetcher.add_argument("--skip-sprites", action="store_true")
    fetcher.add_argument("--keep-existing", action="store_true",
                         help="leave sprites already on disk alone")
    fetcher.add_argument("--refresh", action="store_true",
                         help="ignore the HTTP cache and pull everything again")
    fetcher.set_defaults(func=cmd_fetch)

    sprites = sub.add_parser("sprites", help="download the sprites of every pokemon already in the roster")
    sprites.add_argument("--workers", type=int, default=8)
    sprites.add_argument("--keep-existing", action="store_true",
                         help="leave sprites already on disk alone")
    sprites.add_argument("--refresh", action="store_true",
                         help="ignore the HTTP cache and pull everything again")
    sprites.set_defaults(func=cmd_sprites)

    importer = sub.add_parser("import", help="convert a manually downloaded pack")
    importer.add_argument("--name", required=True)
    importer.add_argument("--sprite", required=True)
    importer.add_argument("--portrait")
    importer.set_defaults(func=cmd_import)

    stats = sub.add_parser("stats", help="roster statistics and data")
    stats.set_defaults(func=cmd_stats)

    deleter = sub.add_parser("delete", help="remove a pokedex range from the roster and disk")
    deleter.add_argument("--range", required=True, type=parse_range, help="e.g. 152-251")
    deleter.set_defaults(func=cmd_delete)

    cleaner = sub.add_parser("clear", help="delete every sprite and the roster")
    cleaner.add_argument("--yes", action="store_true", help="skip the confirmation")
    cleaner.set_defaults(func=cmd_clear)

    reporter = sub.add_parser("report", help="verify sprites, roster and pipeline")
    reporter.add_argument("--prune", action="store_true",
                          help="delete sprite folders that are not in the roster")
    reporter.set_defaults(func=cmd_report)

    if len(sys.argv) == 1:
        sys.exit(interactive())

    args = parser.parse_args()
    sys.exit(args.func(args) or 0)


if __name__ == "__main__":
    main()

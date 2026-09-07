#!/usr/bin/env python3
"""Build deterministic Phase N4 contact sheets from the native parity inventory."""

from __future__ import annotations

import argparse
import hashlib
import html
import json
import math
from datetime import datetime
from pathlib import Path
from typing import Any, Iterable

from PIL import Image, ImageDraw, ImageFont


BACKGROUND = (14, 18, 29, 255)
PANEL = (27, 34, 51, 255)
PANEL_ALT = (34, 43, 63, 255)
TEXT = (235, 241, 250, 255)
MUTED = (161, 174, 194, 255)
NATIVE = (79, 193, 133, 255)
MISSING = (235, 176, 79, 255)
DISABLED = (151, 159, 174, 255)
PLACEHOLDER = (181, 126, 220, 255)
GRID_A = (46, 53, 69, 255)
GRID_B = (61, 69, 87, 255)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument("--inventory", type=Path, default=Path("docs/FULL_PORT_PARITY_INVENTORY.json"))
    parser.add_argument("--recovered-game-root", type=Path)
    parser.add_argument("--output", type=Path)
    return parser.parse_args()


def resolve(repo_root: Path, path: str | Path) -> Path:
    candidate = Path(path)
    return candidate if candidate.is_absolute() else repo_root / candidate


def load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8-sig") as stream:
        return json.load(stream)


def choose_font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        Path("C:/Windows/Fonts/msyhbd.ttc" if bold else "C:/Windows/Fonts/msyh.ttc"),
        Path("C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size=size)
    return ImageFont.load_default()


FONT_TITLE = choose_font(28, bold=True)
FONT_SECTION = choose_font(20, bold=True)
FONT_LABEL = choose_font(16, bold=True)
FONT_BODY = choose_font(14)
FONT_SMALL = choose_font(12)


def wrapped_lines(draw: ImageDraw.ImageDraw, text: str, font: ImageFont.ImageFont, width: int) -> list[str]:
    if not text:
        return []
    lines: list[str] = []
    for paragraph in text.splitlines() or [text]:
        current = ""
        for character in paragraph:
            proposed = current + character
            if current and draw.textlength(proposed, font=font) > width:
                lines.append(current)
                current = character
            else:
                current = proposed
        lines.append(current)
    return lines


def draw_wrapped(
    draw: ImageDraw.ImageDraw,
    xy: tuple[int, int],
    text: str,
    font: ImageFont.ImageFont,
    fill: tuple[int, int, int, int],
    width: int,
    max_lines: int,
) -> int:
    x, y = xy
    lines = wrapped_lines(draw, text, font, width)
    if len(lines) > max_lines:
        lines = lines[:max_lines]
        last = lines[-1]
        while last and draw.textlength(last + "…", font=font) > width:
            last = last[:-1]
        lines[-1] = last + "…"
    line_height = int(getattr(font, "size", 14) * 1.35)
    for line in lines:
        draw.text((x, y), line, font=font, fill=fill)
        y += line_height
    return y


def checkerboard(size: tuple[int, int], cell: int = 12) -> Image.Image:
    image = Image.new("RGBA", size, GRID_A)
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            if ((x // cell) + (y // cell)) % 2:
                draw.rectangle((x, y, min(x + cell - 1, size[0] - 1), min(y + cell - 1, size[1] - 1)), fill=GRID_B)
    return image


def open_rgba(path: Path) -> Image.Image:
    if not path.is_file():
        raise FileNotFoundError(f"Gallery source is missing: {path}")
    with Image.open(path) as source:
        return source.convert("RGBA")


def paste_contained(canvas: Image.Image, source: Image.Image, box: tuple[int, int, int, int]) -> None:
    x, y, width, height = box
    background = checkerboard((width, height))
    contained = source.copy()
    contained.thumbnail((width, height), Image.Resampling.LANCZOS)
    px = (width - contained.width) // 2
    py = (height - contained.height) // 2
    background.alpha_composite(contained, (px, py))
    canvas.alpha_composite(background, (x, y))


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def manifest_path(repo_root: Path, path: Path) -> str:
    try:
        return path.relative_to(repo_root).as_posix()
    except ValueError:
        return path.as_posix()


def model_color(record: dict[str, Any]) -> tuple[int, int, int, int]:
    art_status = str(record.get("art", {}).get("status", ""))
    if "placeholder" in art_status.lower():
        return PLACEHOLDER
    if not record.get("enabledInSts1", True):
        return DISABLED
    if record.get("nativeSource"):
        return NATIVE
    return MISSING


def status_text(record: dict[str, Any]) -> str:
    if not record.get("enabledInSts1", True):
        return "STS1 source-disabled"
    if record.get("nativeSource"):
        return "native behavior"
    return "behavior pending"


def title_for(table: dict[str, str], stable_id: str, fallback: str) -> str:
    key = f"{stable_id}.title"
    title = table.get(key)
    if not title:
        raise ValueError(f"Missing native localization title: {key}")
    return title or fallback


def new_sheet(title: str, subtitle: str, width: int, height: int) -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (width, height), BACKGROUND)
    draw = ImageDraw.Draw(image)
    draw.text((24, 18), title, font=FONT_TITLE, fill=TEXT)
    draw.text((24, 56), subtitle, font=FONT_BODY, fill=MUTED)
    return image, draw


def page_records(records: list[dict[str, Any]], page_size: int) -> Iterable[tuple[int, list[dict[str, Any]]]]:
    for index in range(0, len(records), page_size):
        yield index // page_size + 1, records[index:index + page_size]


def build_card_sheets(
    repo_root: Path,
    output: Path,
    cards: list[dict[str, Any]],
    eng: dict[str, str],
    zhs: dict[str, str],
    assets: list[dict[str, Any]],
) -> list[str]:
    files: list[str] = []
    ordered = sorted(cards, key=lambda record: record["sts2StableId"])
    for page, records in page_records(ordered, 12):
        sheet, draw = new_sheet(
            f"Togawa Sakiko card gallery {page}/{math.ceil(len(ordered) / 12)}",
            "Every 250x190 and 500x380 native portrait pair; border indicates behavior status.",
            1560,
            1660,
        )
        for slot, record in enumerate(records):
            column = slot % 2
            row = slot // 2
            x = 20 + column * 770
            y = 92 + row * 258
            color = model_color(record)
            draw.rounded_rectangle((x, y, x + 750, y + 238), radius=10, fill=PANEL if row % 2 == 0 else PANEL_ALT, outline=color, width=3)
            small_path = resolve(repo_root, record["art"]["sts2Small"])
            large_path = resolve(repo_root, record["art"]["sts2Large"])
            small = open_rgba(small_path)
            large = open_rgba(large_path)
            if small.size != (250, 190):
                raise ValueError(f"Wrong small card dimensions for {record['sts2StableId']}: {small.size}")
            if large.size != (500, 380):
                raise ValueError(f"Wrong large card dimensions for {record['sts2StableId']}: {large.size}")
            paste_contained(sheet, small, (x + 12, y + 38, 250, 190))
            paste_contained(sheet, large, (x + 274, y + 38, 250, 190))
            draw.text((x + 12, y + 12), "small", font=FONT_SMALL, fill=MUTED)
            draw.text((x + 274, y + 12), "large", font=FONT_SMALL, fill=MUTED)
            label_x = x + 538
            label_width = 200
            label_y = draw_wrapped(draw, (label_x, y + 14), title_for(eng, record["sts2StableId"], record["name"]), FONT_LABEL, TEXT, label_width, 2)
            label_y = draw_wrapped(draw, (label_x, label_y + 2), title_for(zhs, record["sts2StableId"], record["name"]), FONT_BODY, TEXT, label_width, 2)
            label_y = draw_wrapped(draw, (label_x, label_y + 6), record["sts2StableId"], FONT_SMALL, MUTED, label_width, 3)
            draw_wrapped(draw, (label_x, max(label_y + 6, y + 190)), status_text(record), FONT_SMALL, color, label_width, 2)
            for image_path, role in ((small_path, "small"), (large_path, "large")):
                assets.append({
                    "modelType": "card",
                    "stableId": record["sts2StableId"],
                    "role": role,
                    "path": image_path.relative_to(repo_root).as_posix(),
                    "dimensions": list(open_rgba(image_path).size),
                    "sha256": sha256(image_path),
                })
        filename = f"cards-{page:02d}.png"
        sheet.convert("RGB").save(output / filename, optimize=True)
        files.append(filename)
    return files


def build_power_sheets(
    repo_root: Path,
    output: Path,
    powers: list[dict[str, Any]],
    eng: dict[str, str],
    zhs: dict[str, str],
    inherited: dict[str, dict[str, Any]],
    assets: list[dict[str, Any]],
) -> list[str]:
    files: list[str] = []
    ordered = sorted(powers, key=lambda record: record["sts2StableId"])
    for page, records in page_records(ordered, 20):
        sheet, draw = new_sheet(
            f"Togawa Sakiko power gallery {page}/{math.ceil(len(ordered) / 20)}",
            "Custom 32x32 and 84x84 pairs plus explicit base-game presentation reuses.",
            1440,
            1160,
        )
        for slot, record in enumerate(records):
            column = slot % 4
            row = slot // 4
            x = 18 + column * 354
            y = 92 + row * 210
            color = model_color(record)
            draw.rounded_rectangle((x, y, x + 336, y + 192), radius=10, fill=PANEL if row % 2 == 0 else PANEL_ALT, outline=color, width=3)
            if record.get("inheritsBasePresentation"):
                base = inherited.get(record["name"])
                if base is None:
                    raise ValueError(f"Missing base-game presentation mapping for {record['name']}")
                base_path = Path(base["path"])
                source = open_rgba(base_path)
                if source.size != tuple(base["dimensions"]):
                    raise ValueError(f"Base-game presentation dimensions changed for {record['name']}: {source.size}")
                paste_contained(sheet, source, (x + 14, y + 40, 168, 112))
                draw.text((x + 196, y + 126), "BASE GAME REUSE", font=FONT_SMALL, fill=DISABLED)
                assets.append({
                    "modelType": "power",
                    "stableId": record["sts2StableId"],
                    "role": "base-game reuse",
                    "path": manifest_path(repo_root, base_path),
                    "dimensions": list(source.size),
                    "sha256": sha256(base_path),
                })
            else:
                small_path = resolve(repo_root, record["art"]["sts2Small"])
                large_path = resolve(repo_root, record["art"]["sts2Large"])
                small = open_rgba(small_path)
                large = open_rgba(large_path)
                if small.size != (32, 32) or large.size != (84, 84):
                    raise ValueError(f"Wrong power icon dimensions for {record['sts2StableId']}: {small.size}, {large.size}")
                paste_contained(sheet, small, (x + 14, y + 52, 80, 80))
                paste_contained(sheet, large, (x + 104, y + 36, 112, 112))
                for image_path, role in ((small_path, "small"), (large_path, "large")):
                    assets.append({
                        "modelType": "power",
                        "stableId": record["sts2StableId"],
                        "role": role,
                        "path": image_path.relative_to(repo_root).as_posix(),
                        "dimensions": list(open_rgba(image_path).size),
                        "sha256": sha256(image_path),
                    })
            english_title = inherited[record["name"]]["eng"] if record.get("inheritsBasePresentation") else title_for(eng, record["sts2StableId"], record["name"])
            chinese_title = inherited[record["name"]]["zhs"] if record.get("inheritsBasePresentation") else title_for(zhs, record["sts2StableId"], record["name"])
            draw_wrapped(draw, (x + 12, y + 12), english_title, FONT_LABEL, TEXT, 310, 1)
            draw_wrapped(draw, (x + 224, y + 47), chinese_title, FONT_BODY, TEXT, 100, 2)
            draw_wrapped(draw, (x + 12, y + 154), record["sts2StableId"], FONT_SMALL, MUTED, 312, 2)
        filename = f"powers-{page:02d}.png"
        sheet.convert("RGB").save(output / filename, optimize=True)
        files.append(filename)
    return files


def build_relic_sheet(
    repo_root: Path,
    output: Path,
    relics: list[dict[str, Any]],
    eng: dict[str, str],
    zhs: dict[str, str],
    assets: list[dict[str, Any]],
) -> list[str]:
    sheet, draw = new_sheet("Togawa Sakiko relic gallery", "Every 128x128 icon/outline and 256x256 large icon.", 1530, 1340)
    for slot, record in enumerate(sorted(relics, key=lambda item: item["sts2StableId"])):
        column = slot % 3
        row = slot // 3
        x = 18 + column * 504
        y = 92 + row * 304
        color = model_color(record)
        draw.rounded_rectangle((x, y, x + 486, y + 286), radius=10, fill=PANEL if row % 2 == 0 else PANEL_ALT, outline=color, width=3)
        roles = (("icon", "icon"), ("outline", "outline"), ("big", "large"))
        for index, (key, label) in enumerate(roles):
            image_path = resolve(repo_root, record["art"][key])
            source = open_rgba(image_path)
            expected = (256, 256) if key == "big" else (128, 128)
            if source.size != expected:
                raise ValueError(f"Wrong relic {label} dimensions for {record['sts2StableId']}: {source.size}")
            paste_contained(sheet, source, (x + 12 + index * 154, y + 70, 142, 142))
            draw.text((x + 12 + index * 154, y + 218), label, font=FONT_SMALL, fill=MUTED)
            assets.append({
                "modelType": "relic",
                "stableId": record["sts2StableId"],
                "role": label,
                "path": image_path.relative_to(repo_root).as_posix(),
                "dimensions": list(source.size),
                "sha256": sha256(image_path),
            })
        draw_wrapped(draw, (x + 12, y + 10), title_for(eng, record["sts2StableId"], record["name"]), FONT_LABEL, TEXT, 255, 1)
        draw_wrapped(draw, (x + 278, y + 12), title_for(zhs, record["sts2StableId"], record["name"]), FONT_BODY, TEXT, 190, 1)
        draw_wrapped(draw, (x + 12, y + 252), record["sts2StableId"], FONT_SMALL, MUTED, 462, 1)
    filename = "relics.png"
    sheet.convert("RGB").save(output / filename, optimize=True)
    return [filename]


def build_potion_sheet(
    repo_root: Path,
    output: Path,
    potions: list[dict[str, Any]],
    eng: dict[str, str],
    zhs: dict[str, str],
    assets: list[dict[str, Any]],
) -> list[str]:
    sheet, draw = new_sheet("Togawa Sakiko potion gallery", "Every 64x64 layer and its deterministic composite preview.", 1450, 760)
    for slot, record in enumerate(sorted(potions, key=lambda item: item["sts2StableId"])):
        column = slot % 3
        row = slot // 3
        x = 18 + column * 474
        y = 92 + row * 324
        color = model_color(record)
        draw.rounded_rectangle((x, y, x + 456, y + 306), radius=10, fill=PANEL if row % 2 == 0 else PANEL_ALT, outline=color, width=3)
        composite = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        for index, layer in enumerate(record["art"]["layers"]):
            layer_path = resolve(repo_root, layer["destination"])
            source = open_rgba(layer_path)
            if source.size != (64, 64):
                raise ValueError(f"Wrong potion layer dimensions for {record['sts2StableId']}: {source.size}")
            paste_contained(sheet, source, (x + 12 + index * 118, y + 78, 108, 108))
            draw_wrapped(draw, (x + 12 + index * 118, y + 190), layer["name"], FONT_SMALL, MUTED, 108, 1)
            composite.alpha_composite(source)
            assets.append({
                "modelType": "potion",
                "stableId": record["sts2StableId"],
                "role": layer["name"],
                "path": layer_path.relative_to(repo_root).as_posix(),
                "dimensions": list(source.size),
                "sha256": sha256(layer_path),
            })
        paste_contained(sheet, composite, (x + 250, y + 62, 150, 150))
        draw.text((x + 282, y + 216), "composite", font=FONT_SMALL, fill=MUTED)
        draw_wrapped(draw, (x + 12, y + 12), title_for(eng, record["sts2StableId"], record["name"]), FONT_LABEL, TEXT, 260, 1)
        draw_wrapped(draw, (x + 286, y + 14), title_for(zhs, record["sts2StableId"], record["name"]), FONT_BODY, TEXT, 150, 1)
        draw_wrapped(draw, (x + 12, y + 264), record["sts2StableId"], FONT_SMALL, MUTED, 430, 2)
    filename = "potions.png"
    sheet.convert("RGB").save(output / filename, optimize=True)
    return [filename]


def build_presentation_sheets(
    repo_root: Path,
    output: Path,
    records: list[dict[str, Any]],
    assets: list[dict[str, Any]],
) -> list[str]:
    files: list[str] = []
    ordered = sorted(records, key=lambda record: record["nativePath"])
    for page, page_items in page_records(ordered, 20):
        sheet, draw = new_sheet(
            f"Togawa Sakiko presentation assets {page}/{math.ceil(len(ordered) / 20)}",
            "Character, UI, frame, map, act, enemy, event, and VFX source assets; behavior remains gated separately.",
            1460,
            1160,
        )
        for slot, record in enumerate(page_items):
            column = slot % 5
            row = slot // 5
            x = 18 + column * 288
            y = 92 + row * 258
            draw.rounded_rectangle((x, y, x + 270, y + 240), radius=10, fill=PANEL if row % 2 == 0 else PANEL_ALT, outline=DISABLED, width=2)
            image_path = resolve(repo_root, "TogawaSakiko/TogawaSakiko/" + record["nativePath"])
            source = open_rgba(image_path)
            expected = tuple(int(value) for value in record["dimensions"].split("x"))
            if source.size != expected:
                raise ValueError(f"Wrong presentation dimensions for {record['nativePath']}: {source.size}, expected {expected}")
            paste_contained(sheet, source, (x + 12, y + 12, 246, 174))
            draw_wrapped(draw, (x + 12, y + 194), record["nativePath"], FONT_SMALL, TEXT, 246, 2)
            assets.append({
                "modelType": "presentation",
                "stableId": record["relativePath"],
                "role": "source",
                "path": image_path.relative_to(repo_root).as_posix(),
                "dimensions": list(source.size),
                "sha256": sha256(image_path),
            })
        filename = f"presentation-{page:02d}.png"
        sheet.convert("RGB").save(output / filename, optimize=True)
        files.append(filename)
    return files


def build_character_presentation_sheets(
    repo_root: Path,
    output: Path,
    manifest: dict[str, Any],
    assets: list[dict[str, Any]],
) -> list[str]:
    records = manifest.get("assets", [])
    if len(records) != 12:
        raise ValueError(f"Character presentation manifest must contain 12 assets, found {len(records)}")

    compact_records = [record for record in records if "gesture" not in record]
    hand_records = [record for record in records if "gesture" in record]
    if len(compact_records) != 8 or len(hand_records) != 4:
        raise ValueError("Character presentation manifest topology changed.")

    sheet, draw = new_sheet(
        "Togawa Sakiko native character surfaces",
        "Exact-size derived assets used by native character, map, card, transition, merchant, and rest-site presentation.",
        1600,
        1020,
    )
    for slot, record in enumerate(compact_records[:6]):
        x = 18 + slot * 260
        y = 92
        draw.rounded_rectangle((x, y, x + 242, y + 310), radius=10, fill=PANEL, outline=NATIVE, width=3)
        image_path = resolve(repo_root, record["output"])
        source = open_rgba(image_path)
        expected = tuple(record["dimensions"])
        if source.size != expected:
            raise ValueError(f"Wrong character presentation dimensions for {record['role']}: {source.size}, expected {expected}")
        paste_contained(sheet, source, (x + 12, y + 12, 218, 210))
        draw_wrapped(draw, (x + 12, y + 232), record["role"], FONT_LABEL, TEXT, 218, 2)
        draw_wrapped(draw, (x + 12, y + 278), f"{source.width}x{source.height}", FONT_SMALL, MUTED, 218, 1)
        assets.append({
            "modelType": "characterPresentation",
            "stableId": "TogawaSakiko",
            "role": record["role"],
            "path": image_path.relative_to(repo_root).as_posix(),
            "dimensions": list(source.size),
            "sha256": sha256(image_path),
        })

    for slot, record in enumerate(compact_records[6:]):
        x = 18 + slot * 782
        y = 424
        draw.rounded_rectangle((x, y, x + 764, y + 570), radius=10, fill=PANEL_ALT, outline=NATIVE, width=3)
        image_path = resolve(repo_root, record["output"])
        source = open_rgba(image_path)
        expected = tuple(record["dimensions"])
        if source.size != expected:
            raise ValueError(f"Wrong character presentation dimensions for {record['role']}: {source.size}, expected {expected}")
        paste_contained(sheet, source, (x + 12, y + 52, 740, 456))
        draw_wrapped(draw, (x + 12, y + 14), record["role"], FONT_LABEL, TEXT, 570, 1)
        draw.text((x + 630, y + 16), f"{source.width}x{source.height}", font=FONT_SMALL, fill=MUTED)
        draw_wrapped(draw, (x + 12, y + 518), record["status"], FONT_SMALL, MUTED, 740, 2)
        assets.append({
            "modelType": "characterPresentation",
            "stableId": "TogawaSakiko",
            "role": record["role"],
            "path": image_path.relative_to(repo_root).as_posix(),
            "dimensions": list(source.size),
            "sha256": sha256(image_path),
        })

    character_filename = "character-surfaces.png"
    sheet.convert("RGB").save(output / character_filename, optimize=True)

    hands, hand_draw = new_sheet(
        "Togawa Sakiko multiplayer hand compatibility set",
        "Four exact 422x1200 native topology placeholders; recolored Defect silhouettes carry the neutral replace-later badge.",
        1600,
        1260,
    )
    for slot, record in enumerate(hand_records):
        x = 18 + slot * 394
        y = 92
        hand_draw.rounded_rectangle((x, y, x + 376, y + 1144), radius=10, fill=PANEL if slot % 2 == 0 else PANEL_ALT, outline=PLACEHOLDER, width=3)
        image_path = resolve(repo_root, record["output"])
        source = open_rgba(image_path)
        expected = tuple(record["dimensions"])
        if source.size != expected:
            raise ValueError(f"Wrong multiplayer hand dimensions for {record['gesture']}: {source.size}, expected {expected}")
        paste_contained(hands, source, (x + 12, y + 52, 352, 1010))
        hand_draw.text((x + 12, y + 14), record["gesture"], font=FONT_LABEL, fill=TEXT)
        hand_draw.text((x + 266, y + 17), f"{source.width}x{source.height}", font=FONT_SMALL, fill=MUTED)
        draw_wrapped(hand_draw, (x + 12, y + 1074), record["status"], FONT_SMALL, PLACEHOLDER, 352, 2)
        assets.append({
            "modelType": "characterPresentation",
            "stableId": "TogawaSakiko",
            "role": f"multiplayer hand {record['gesture']}",
            "path": image_path.relative_to(repo_root).as_posix(),
            "dimensions": list(source.size),
            "sha256": sha256(image_path),
        })

    hands_filename = "character-hands.png"
    hands.convert("RGB").save(output / hands_filename, optimize=True)
    return [character_filename, hands_filename]


def write_index(output: Path, generated_files: list[str], counts: dict[str, int]) -> None:
    cards = "\n".join(
        f'<figure><a href="{html.escape(filename)}"><img src="{html.escape(filename)}" loading="lazy"></a><figcaption>{html.escape(filename)}</figcaption></figure>'
        for filename in generated_files
    )
    document = f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Togawa Sakiko Phase N4 presentation gallery</title>
<style>body{{background:#0e121d;color:#ebf1fa;font:16px Segoe UI,Microsoft YaHei,sans-serif;margin:24px}}p{{color:#a1aec2}}.summary{{display:flex;gap:12px;flex-wrap:wrap}}.summary span{{background:#1b2233;border:1px solid #3d4557;border-radius:8px;padding:8px 12px}}figure{{margin:28px 0}}img{{max-width:100%;height:auto;border:1px solid #3d4557;border-radius:8px}}figcaption{{color:#a1aec2;margin-top:6px}}</style></head>
<body><h1>Togawa Sakiko Phase N4 presentation gallery</h1>
<p>Generated from the native parity inventory. Gallery inclusion does not register unfinished content in gameplay pools.</p>
<div class="summary"><span>{counts['cards']} cards</span><span>{counts['powers']} powers</span><span>{counts['relics']} relics</span><span>{counts['potions']} potions</span><span>{counts['presentation']} presentation assets</span></div>
<p>{counts['characterPresentation']} derived native character surfaces are audited separately below.</p>
{cards}
</body></html>
"""
    (output / "index.html").write_text(document, encoding="utf-8", newline="\n")


def main() -> None:
    args = parse_args()
    repo_root = args.repo_root.resolve()
    recovered_game_root = (
        args.recovered_game_root.resolve()
        if args.recovered_game_root
        else repo_root.parent.parent / "Recovered" / "v0.111.0-41cef1ea"
    )
    inventory_path = resolve(repo_root, args.inventory)
    output = (args.output.resolve() if args.output else repo_root / "artifacts" / "n4-gallery" / f"gallery-{datetime.now():%Y%m%d-%H%M%S}")
    output.mkdir(parents=True, exist_ok=False)

    inventory = load_json(inventory_path)
    character_manifest_path = repo_root / "TogawaSakiko" / "TogawaSakiko" / "diagnostics" / "n4_character_presentation_manifest.json"
    character_manifest = load_json(character_manifest_path)
    localization_root = repo_root / "TogawaSakiko" / "TogawaSakiko" / "localization"
    tables = {
        kind: {
            language: load_json(localization_root / language / f"{kind}.json")
            for language in ("eng", "zhs")
        }
        for kind in ("cards", "powers", "relics", "potions")
    }
    base_power_loc = {
        language: load_json(recovered_game_root / "localization" / language / "powers.json")
        for language in ("eng", "zhs")
    }
    base_modifier_loc = {
        language: load_json(recovered_game_root / "localization" / language / "modifiers.json")
        for language in ("eng", "zhs")
    }
    inherited_power_presentations = {
        "MonsterVigorPower": {
            "path": recovered_game_root / "images" / "powers" / "vigor_power.png",
            "dimensions": (256, 256),
            "eng": base_power_loc["eng"]["VIGOR_POWER.title"],
            "zhs": base_power_loc["zhs"]["VIGOR_POWER.title"],
        },
        "PlayerFilightPower": {
            "path": recovered_game_root / "images" / "packed" / "modifiers" / "flight.png",
            "dimensions": (80, 80),
            "eng": base_modifier_loc["eng"]["FLIGHT.title"],
            "zhs": base_modifier_loc["zhs"]["FLIGHT.title"],
        },
    }

    counts = {
        "cards": len(inventory["cards"]),
        "powers": len(inventory["powers"]),
        "relics": len(inventory["relics"]),
        "potions": len(inventory["potions"]),
        "presentation": len(inventory["presentationFiles"]),
        "characterPresentation": len(character_manifest.get("assets", [])),
    }
    expected = {"cards": 95, "powers": 36, "relics": 11, "potions": 6, "presentation": 68, "characterPresentation": 12}
    if counts != expected:
        raise ValueError(f"Parity inventory count drift: expected {expected}, found {counts}")

    assets: list[dict[str, Any]] = []
    generated_files: list[str] = []
    generated_files.extend(build_card_sheets(repo_root, output, inventory["cards"], tables["cards"]["eng"], tables["cards"]["zhs"], assets))
    generated_files.extend(build_power_sheets(repo_root, output, inventory["powers"], tables["powers"]["eng"], tables["powers"]["zhs"], inherited_power_presentations, assets))
    generated_files.extend(build_relic_sheet(repo_root, output, inventory["relics"], tables["relics"]["eng"], tables["relics"]["zhs"], assets))
    generated_files.extend(build_potion_sheet(repo_root, output, inventory["potions"], tables["potions"]["eng"], tables["potions"]["zhs"], assets))
    generated_files.extend(build_presentation_sheets(repo_root, output, inventory["presentationFiles"], assets))
    generated_files.extend(build_character_presentation_sheets(repo_root, output, character_manifest, assets))
    write_index(output, generated_files, counts)

    manifest = {
        "schemaVersion": 1,
        "generatedFrom": inventory_path.relative_to(repo_root).as_posix(),
        "counts": counts,
        "renderedModelCount": counts["cards"] + counts["powers"] + counts["relics"] + counts["potions"],
        "renderedAssetCount": len(assets),
        "sheets": generated_files,
        "assets": assets,
    }
    (output / "gallery_manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

    print(f"Phase N4 gallery generated: {output}")
    print(f"Models: {manifest['renderedModelCount']}; source assets rendered: {len(assets)}; sheets: {len(generated_files)}")


if __name__ == "__main__":
    main()

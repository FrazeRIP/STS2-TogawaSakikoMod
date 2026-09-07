#!/usr/bin/env python3
"""Derive exact-size STS2 character UI assets from Sakiko source art and the placeholder master."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image, ImageEnhance, ImageFilter, ImageOps


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument("--recovered-game-root", type=Path)
    return parser.parse_args()


def contain(source: Image.Image, size: tuple[int, int], padding: int = 0) -> Image.Image:
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    fitted = source.convert("RGBA")
    fitted.thumbnail((size[0] - padding * 2, size[1] - padding * 2), Image.Resampling.LANCZOS)
    canvas.alpha_composite(fitted, ((size[0] - fitted.width) // 2, (size[1] - fitted.height) // 2))
    return canvas


def save(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)


def alpha_silhouette(source: Image.Image, color: tuple[int, int, int, int]) -> Image.Image:
    result = Image.new("RGBA", source.size, color)
    result.putalpha(source.convert("RGBA").getchannel("A"))
    return result


def build_locked_portrait(source: Image.Image) -> Image.Image:
    background = Image.new("RGB", source.size, (72, 75, 78))
    alpha = source.getchannel("A").filter(ImageFilter.MaxFilter(3))
    silhouette = Image.new("RGB", source.size, (3, 4, 6))
    background.paste(silhouette, mask=alpha)
    border = Image.new("RGB", source.size, (142, 145, 147))
    inner = Image.new("L", source.size, 0)
    for x in range(2, source.width - 2):
        for y in range(2, source.height - 2):
            inner.putpixel((x, y), 255)
    border.paste(background, mask=inner)
    return border


def recolor_hand(source: Image.Image) -> Image.Image:
    rgba = source.convert("RGBA")
    gray = ImageOps.grayscale(rgba)
    colored = ImageOps.colorize(gray, black="#06131d", mid="#315b72", white="#b7e2e8").convert("RGBA")
    colored.putalpha(rgba.getchannel("A"))
    return ImageEnhance.Contrast(colored).enhance(1.08)


def main() -> None:
    args = parse_args()
    repo_root = args.repo_root.resolve()
    recovered_root = (
        args.recovered_game_root.resolve()
        if args.recovered_game_root
        else repo_root.parent.parent / "Recovered" / "v0.111.0-41cef1ea"
    )
    resource_root = repo_root / "TogawaSakiko" / "TogawaSakiko"
    character_root = resource_root / "images" / "character"
    placeholder_path = resource_root / "images" / "placeholders" / "missing_content.png"

    button = Image.open(character_root / "select" / "button.png").convert("RGBA")
    alternate = Image.open(character_root / "image_alt.png").convert("RGBA")
    small_orb = Image.open(character_root / "cardback" / "small_orb.png").convert("RGBA")
    shoulder = Image.open(character_root / "shoulder2.png").convert("RGBA")
    placeholder = Image.open(placeholder_path).convert("RGBA")

    top_panel = contain(button, (88, 88), padding=5)
    top_panel_outline = alpha_silhouette(top_panel, (255, 255, 255, 255))
    save(top_panel, resource_root / "images" / "ui" / "top_panel" / "character_icon_togawa_sakiko.png")
    save(top_panel_outline, resource_root / "images" / "ui" / "top_panel" / "character_icon_togawa_sakiko_outline.png")

    select_portrait = contain(alternate, (132, 195), padding=2)
    save(select_portrait, resource_root / "images" / "packed" / "character_select" / "char_select_togawa_sakiko.png")
    save(build_locked_portrait(select_portrait), resource_root / "images" / "packed" / "character_select" / "char_select_togawa_sakiko_locked.png")

    map_marker = contain(button, (49, 64), padding=3)
    save(map_marker, resource_root / "images" / "charui" / "map_marker_char_name.png")

    energy_icon = contain(small_orb, (71, 72), padding=7)
    save(energy_icon, resource_root / "images" / "card_ui" / "energy_togawa_sakiko.png")
    rich_text_energy_icon = Image.open(resource_root / "images" / "charui" / "text_energy.png").convert("RGBA")
    if rich_text_energy_icon.size != (24, 24):
        raise ValueError(f"Sakiko rich-text energy icon must be 24x24, found {rich_text_energy_icon.size}.")
    save(
        rich_text_energy_icon,
        repo_root / "TogawaSakiko" / "images" / "packed" / "sprite_fonts" / "togawa_sakiko_energy_icon.png",
    )

    transition = Image.radial_gradient("L").resize((2560, 1200), Image.Resampling.BICUBIC)
    transition = ImageOps.autocontrast(transition)
    transition_rgba = Image.merge("RGBA", (transition, transition, transition, Image.new("L", transition.size, 255)))
    save(transition_rgba, resource_root / "images" / "ui" / "transitions" / "togawa_sakiko_transition.png")

    shoulder_box = shoulder.getchannel("A").getbbox()
    if shoulder_box is None:
        raise ValueError("Sakiko shoulder art has no visible pixels.")
    save(shoulder.crop(shoulder_box), character_root / "presentation" / "rest_site.png")

    hand_records: list[dict[str, object]] = []
    badge = contain(placeholder, (164, 164), padding=8)
    for gesture in ("point", "rock", "paper", "scissors"):
        source_path = recovered_root / "images" / "ui" / "hands" / f"multiplayer_hand_defect_{gesture}.png"
        hand = recolor_hand(Image.open(source_path))
        hand.alpha_composite(badge, ((hand.width - badge.width) // 2, 30))
        output_path = resource_root / "images" / "ui" / "hands" / f"multiplayer_hand_togawa_sakiko_{gesture}.png"
        save(hand, output_path)
        hand_records.append({
            "gesture": gesture,
            "sourceTopology": source_path.as_posix(),
            "output": output_path.relative_to(repo_root).as_posix(),
            "dimensions": [hand.width, hand.height],
            "status": "generated compatibility placeholder",
        })

    character_assets = [
        {
            "role": "top-panel icon",
            "output": "TogawaSakiko/TogawaSakiko/images/ui/top_panel/character_icon_togawa_sakiko.png",
            "dimensions": [88, 88],
            "status": "derived from STS1 character-select art",
        },
        {
            "role": "top-panel outline",
            "output": "TogawaSakiko/TogawaSakiko/images/ui/top_panel/character_icon_togawa_sakiko_outline.png",
            "dimensions": [88, 88],
            "status": "generated alpha silhouette",
        },
        {
            "role": "character-select icon",
            "output": "TogawaSakiko/TogawaSakiko/images/packed/character_select/char_select_togawa_sakiko.png",
            "dimensions": [132, 195],
            "status": "derived from STS1 alternate character art",
        },
        {
            "role": "locked character-select icon",
            "output": "TogawaSakiko/TogawaSakiko/images/packed/character_select/char_select_togawa_sakiko_locked.png",
            "dimensions": [132, 195],
            "status": "generated locked-state compatibility art",
        },
        {
            "role": "map marker",
            "output": "TogawaSakiko/TogawaSakiko/images/charui/map_marker_char_name.png",
            "dimensions": [49, 64],
            "status": "derived from STS1 character-select art",
        },
        {
            "role": "card energy icon",
            "output": "TogawaSakiko/TogawaSakiko/images/card_ui/energy_togawa_sakiko.png",
            "dimensions": [71, 72],
            "status": "derived from STS1 card-back orb art",
        },
        {
            "role": "transition mask",
            "output": "TogawaSakiko/TogawaSakiko/images/ui/transitions/togawa_sakiko_transition.png",
            "dimensions": [2560, 1200],
            "status": "generated native-size compatibility mask",
        },
        {
            "role": "rest-site portrait",
            "output": "TogawaSakiko/TogawaSakiko/images/character/presentation/rest_site.png",
            "dimensions": [shoulder_box[2] - shoulder_box[0], shoulder_box[3] - shoulder_box[1]],
            "status": "cropped from STS1 shoulder art",
        },
        *hand_records,
    ]

    manifest = {
        "schemaVersion": 1,
        "baseline": "Slay the Spire 2 v0.111.0 (41cef1ea)",
        "derivation": "STS1 Sakiko source art plus the generated neutral missing-content master",
        "exactNativeDimensions": {
            "topPanelIcon": [88, 88],
            "characterSelectIcon": [132, 195],
            "mapMarker": [49, 64],
            "cardEnergyIcon": [71, 72],
            "characterTransition": [2560, 1200],
            "multiplayerHand": [422, 1200],
        },
        "multiplayerHands": hand_records,
        "assets": character_assets,
    }
    manifest_path = resource_root / "diagnostics" / "n4_character_presentation_manifest.json"
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"Native character presentation assets generated: {manifest_path}")


if __name__ == "__main__":
    main()

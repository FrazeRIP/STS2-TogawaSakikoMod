#!/usr/bin/env python3
"""Derive exact-size STS2 character UI assets from Sakiko source art and the placeholder master."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from PIL import Image, ImageChops, ImageEnhance, ImageFilter, ImageOps


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument("--recovered-game-root", type=Path)
    parser.add_argument("--sts1-root", type=Path)
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


def visible_bounds(source: Image.Image) -> tuple[int, int, int, int]:
    bounds = source.convert("RGBA").getchannel("A").point(lambda alpha: 255 if alpha >= 8 else 0).getbbox()
    if bounds is None:
        raise ValueError("Presentation source has no visible pixels.")
    return bounds


def fit_visible(source: Image.Image, size: tuple[int, int], padding: int = 2) -> Image.Image:
    cropped = source.convert("RGBA").crop(visible_bounds(source))
    scale = min((size[0] - 2 * padding) / cropped.width, (size[1] - 2 * padding) / cropped.height)
    fitted = cropped.resize((round(cropped.width * scale), round(cropped.height * scale)), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", size)
    canvas.alpha_composite(fitted, ((size[0] - fitted.width) // 2, (size[1] - fitted.height) // 2))
    return canvas


def build_relic_icons(sts1_root: Path, resource_root: Path) -> list[dict[str, object]]:
    source_root = sts1_root / "src/main/resources/togawasakikomod/images/relics"
    records: list[dict[str, object]] = []
    for source_path in sorted(source_root.glob("*.png")):
        if source_path.stem.endswith("Outline"):
            continue
        icon = Image.open(source_path).convert("RGBA")
        outline = Image.open(source_path.with_stem(source_path.stem + "Outline")).convert("RGBA")
        # Use the same crop/scale for both layers so the outline stays registered to its relic.
        bounds = ImageChops.lighter(icon.getchannel("A"), outline.getchannel("A")).point(lambda alpha: 255 if alpha >= 8 else 0).getbbox()
        if bounds is None:
            raise ValueError(f"Relic source is empty: {source_path}")
        width, height = bounds[2] - bounds[0], bounds[3] - bounds[1]
        scale = min(124 / width, 124 / height)
        fitted_size = (round(width * scale), round(height * scale))
        offset = ((128 - fitted_size[0]) // 2, (128 - fitted_size[1]) // 2)
        for layer, suffix in ((icon, ""), (outline, "_outline")):
            canvas = Image.new("RGBA", (128, 128))
            canvas.alpha_composite(layer.crop(bounds).resize(fitted_size, Image.Resampling.LANCZOS), offset)
            save(canvas, resource_root / "images/relics" / (source_path.stem.lower() + suffix + ".png"))
        large = Image.open(source_root / "large" / source_path.name).convert("RGBA")
        save(fit_visible(large, (256, 256), padding=8), resource_root / "images/relics/big" / source_path.name.lower())
        records.append({"name": source_path.stem, "sourceBounds": list(visible_bounds(icon)), "outlineCrop": list(bounds)})
    if len(records) != 11:
        raise ValueError(f"Expected 11 original relics, found {len(records)}.")
    return records


def build_card_frames(resource_root: Path, recovered_root: Path) -> list[dict[str, object]]:
    records: list[dict[str, object]] = []
    for card_type in ("attack", "skill", "power"):
        source_path = resource_root / "images/character/cardback" / f"bg_{card_type}_p.png"
        original = Image.open(source_path).convert("RGBA")
        # STS1 puts its 600x844 card in a 1024px canvas. Ignore stray alpha outside the card body.
        cropped = original.crop((212, 90, 812, 934))
        native_path = recovered_root / "images/atlases/ui_atlas.sprites/card" / f"card_frame_{card_type}_s.tres"
        native_resource = native_path.read_text(encoding="utf-8")
        atlas_match = re.search(r'path="res://([^"]+)"', native_resource)
        region_match = re.search(r"region = Rect2\(([^)]+)\)", native_resource)
        if atlas_match is None or region_match is None:
            raise ValueError(f"Cannot parse native frame region: {native_path}")
        x, y, width, height = (int(float(value)) for value in region_match.group(1).split(","))
        native = Image.open(recovered_root / atlas_match.group(1)).convert("RGBA").crop((x, y, x + width, y + height))
        frame = cropped.resize((width, height), Image.Resampling.LANCZOS)
        # Preserve the original piano art, with the native portrait opening and outer silhouette.
        frame.putalpha(ImageChops.multiply(frame.getchannel("A"), native.getchannel("A")))
        output = resource_root / "images/card_ui" / f"frame_{card_type}.png"
        save(frame, output)
        records.append({"type": card_type, "source": source_path.relative_to(resource_root).as_posix(), "output": output.relative_to(resource_root).as_posix(), "dimensions": [width, height]})
    return records


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
    # Dedicated head art replaces the historical square character-select placeholder.
    head_source = repo_root / "TogawaSakiko/ArtSources/character_icon/sakiko_head.png"
    top_panel = fit_visible(Image.open(head_source), (88, 88), padding=5)
    top_panel_outline = alpha_silhouette(top_panel, (255, 255, 255, 255))
    save(top_panel, resource_root / "images" / "ui" / "top_panel" / "character_icon_togawa_sakiko.png")
    save(top_panel_outline, resource_root / "images" / "ui" / "top_panel" / "character_icon_togawa_sakiko_outline.png")

    select_portrait = contain(alternate, (132, 195), padding=2)
    save(select_portrait, resource_root / "images" / "packed" / "character_select" / "char_select_togawa_sakiko.png")
    save(build_locked_portrait(select_portrait), resource_root / "images" / "packed" / "character_select" / "char_select_togawa_sakiko_locked.png")

    # The approved face portrait replaces the full-body selection tile; retain the locked-state art.
    select_source = repo_root / "TogawaSakiko/ArtSources/character_select/sakiko_face_portrait_v1.png"
    select_portrait = ImageOps.fit(Image.open(select_source).convert("RGBA"), (132, 195), method=Image.Resampling.LANCZOS)
    save(select_portrait, resource_root / "images" / "packed" / "character_select" / "char_select_togawa_sakiko.png")

    map_marker = contain(button, (49, 64), padding=3)
    # The map pointer uses a dedicated inverted sixteenth note in Sakiko's hair color.
    note_source = repo_root / "TogawaSakiko/ArtSources/map_marker/sakiko_note.png"
    map_marker = fit_visible(Image.open(note_source), (49, 64), padding=3)
    save(map_marker, resource_root / "images" / "charui" / "map_marker_char_name.png")

    energy_icon = contain(small_orb, (71, 72), padding=7)
    # The 22px inline orb is too small here: thumbnail() never enlarges it. Use the original large card orb.
    energy_icon = fit_visible(Image.open(character_root / "cardback" / "energy_orb_p.png"), (71, 72))
    save(energy_icon, resource_root / "images" / "card_ui" / "energy_togawa_sakiko.png")
    sts1_root = args.sts1_root.resolve() if args.sts1_root else repo_root.parent / "STS1-TogawaSakikoMod"
    relic_icons = build_relic_icons(sts1_root, resource_root)
    card_frames = build_card_frames(resource_root, recovered_root)
    rich_text_energy_icon = Image.open(resource_root / "images" / "charui" / "text_energy.png").convert("RGBA")
    rich_text_energy_icon = fit_visible(small_orb, (24, 24), padding=1)
    save(rich_text_energy_icon, resource_root / "images" / "charui" / "text_energy.png")
    save(fit_visible(Image.open(character_root / "cardback" / "energy_orb_p.png"), (68, 70)),
         resource_root / "images" / "charui" / "big_energy.png")
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
    # Full-figure native-room art supersedes the historical shoulder crop above.
    feedback_root = repo_root / "TogawaSakiko" / "ArtSources" / "feedback"
    campfire = fit_visible(Image.open(feedback_root / "sakiko_campfire.png"), (680, 840), padding=4)
    merchant = fit_visible(Image.open(feedback_root / "sakiko_merchant.png"), (256, 384), padding=2)
    masked = fit_visible(Image.open(feedback_root / "sakiko_masked.png"), (168, 320), padding=0)
    save(campfire, character_root / "presentation" / "rest_site.png")
    save(merchant, character_root / "presentation" / "merchant.png")
    save(masked, character_root / "image_another_mask.png")
    mask = Image.open(feedback_root / "another_mask.png").convert("RGBA")
    mask_icon = fit_visible(mask, (128, 128), padding=2)
    save(mask_icon, resource_root / "images" / "relics" / "anothermask.png")
    mask_outline = alpha_silhouette(mask_icon, (255, 255, 255, 255))
    mask_outline.putalpha(mask_outline.getchannel("A").filter(ImageFilter.MaxFilter(3)))
    save(mask_outline, resource_root / "images" / "relics" / "anothermask_outline.png")
    save(fit_visible(mask, (256, 256), padding=8), resource_root / "images" / "relics" / "big" / "anothermask.png")

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
            "dimensions": [680, 840],
            "status": "generated full figure with elevated campfire perspective",
        },
        {"role": "merchant portrait", "output": "TogawaSakiko/TogawaSakiko/images/character/presentation/merchant.png", "dimensions": [256, 384], "status": "generated STS2-style standing figure"},
        {"role": "Another Mask combat portrait", "output": "TogawaSakiko/TogawaSakiko/images/character/image_another_mask.png", "dimensions": [168, 320], "status": "generated masked variation of original combat sprite"},
        *hand_records,
    ]

    character_assets[0]["status"] = "generated Sakiko head matching native top-panel icons"
    select_asset = next(asset for asset in character_assets if asset["role"] == "character-select icon")
    select_asset["status"] = "approved face-focused portrait matching native character-selection tiles"
    select_asset["source"] = select_source.relative_to(repo_root).as_posix()
    next(asset for asset in character_assets if asset["role"] == "map marker")["status"] = "generated pale-blue inverted sixteenth-note pointer"
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
        "relicVisibleBounds": relic_icons,
        "originalCardFrames": card_frames,
        "assets": character_assets,
    }
    manifest_path = resource_root / "diagnostics" / "n4_character_presentation_manifest.json"
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"Native character presentation assets generated: {manifest_path}")


if __name__ == "__main__":
    main()

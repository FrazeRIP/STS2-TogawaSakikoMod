#!/usr/bin/env python3
"""Retain generated solid-magenta masters and extract their pixel-aligned opacity."""

from pathlib import Path
import argparse
import importlib.util
import json
import shutil

import numpy as np
from PIL import Image


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--generated-root", type=Path, required=True)
    parser.add_argument("--transparency-script", type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parent.parent
    destination = repo / "TogawaSakiko/ArtSources/feedback"
    previews = repo / "artifacts/feedback-art"
    destination.mkdir(parents=True, exist_ok=True)
    previews.mkdir(parents=True, exist_ok=True)
    spec = importlib.util.spec_from_file_location("transparency", args.transparency_script)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    sources = {
        "another_mask": "exec-649706ab-b8e7-43d0-b8f1-6b3e036c1ff7.png",
        "sakiko_masked": "exec-4e7fa783-280c-44c5-8c50-c073726958b5.png",
        "sakiko_merchant": "exec-dbc61856-6c40-4ee0-b2c6-bce1ff772d4c.png",
        "sakiko_campfire": "exec-f0a3a3fe-ed64-4652-bfe6-4872740a435d.png",
    }
    records = []
    for name, filename in sources.items():
        source = destination / f"{name}_source.png"
        shutil.copy2(args.generated_root / filename, source)
        rgb = np.asarray(Image.open(source).convert("RGB"), dtype=np.float32)
        # The background alone has strong red AND blue with no green. All ivory,
        # skin, blue hair and dark clothing remain opaque; only keyed edges fade.
        excess = np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1]
        # Generated keys contain small compression/color variations, so clear the
        # magenta family rather than requiring one exact RGB triplet.
        alpha = np.clip((185 - excess) / 155, 0, 1)
        alpha[alpha < 0.015] = 0
        alpha[alpha > 0.985] = 1
        opacity = np.rint(alpha * 255).astype(np.uint8)
        solid_bounds = Image.fromarray(opacity).point(lambda value: 255 if value >= 32 else 0).getbbox()
        left, top, right, bottom = solid_bounds
        extent = np.zeros_like(opacity, dtype=bool)
        extent[max(0, top - 4):bottom + 4, max(0, left - 4):right + 4] = True
        opacity[~extent] = 0
        Image.fromarray(opacity).save(destination / f"{name}_alpha.png")
        background = tuple(np.median(rgb[excess > 185], axis=0).astype(int))
        foreground = module.dematte(rgb, opacity, background)
        foreground = module.dilate_hidden_rgb(foreground, opacity, 2)
        rgba = Image.fromarray(np.dstack((np.rint(foreground).astype(np.uint8), opacity)))
        rgba.save(destination / f"{name}.png", optimize=True)
        for suffix, background in (("light", (230, 230, 230)), ("dark", (18, 25, 31))):
            preview = Image.new("RGBA", rgba.size, (*background, 255))
            preview.alpha_composite(rgba)
            preview.thumbnail((700, 700), Image.Resampling.LANCZOS)
            preview.convert("RGB").save(previews / f"{name}_{suffix}.png")
        records.append({"name": name, "dimensions": list(rgba.size), "visibleBounds": rgba.getbbox(),
                        "transparentPixels": int(np.count_nonzero(opacity == 0)),
                        "opaquePixels": int(np.count_nonzero(opacity == 255))})
    (destination / "manifest.json").write_text(json.dumps({"background": "#FF00FF", "edgeCleanup": "pixel-aligned chroma opacity; dematte; hidden RGB dilation 2px", "assets": records}, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(records, indent=2))


if __name__ == "__main__":
    main()

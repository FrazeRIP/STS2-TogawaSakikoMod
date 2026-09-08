# Sakiko small head icon

Generated with the built-in image generation tool, using the recovered Silent and Ironclad 88x88 top-panel icons as style references and Sakiko campfire art for character identity.

Prompt: Create one head-only Togawa Sakiko UI icon matching the native simplified angular painted shapes and readable silhouette. Pale blue hair, gold eyes, black ribbons, composed expression, three-quarter view facing left. No shoulders, frame, text, glow or shadow. Centered with padding on solid magenta #FF00FF for extraction.

The source and corrected grayscale opacity map are retained here. The map was derived from the exact source; key-color edge residue was removed and the mask contracted by two source pixels. Transparency-map processing applied edge decontamination and two-pixel hidden-RGB dilation. Antialiased transparency is retained when downsampling.

The runtime PNG and matching white alpha silhouette are 88x88 under `TogawaSakiko/images/ui/top_panel`. The presentation builder fits `sakiko_head.png` to those dimensions with five-pixel padding. Source art remains outside the package allowlist.

Validation: Release PackNativeMod and Test-NativePackage passed. An isolated headless MegaDot check loaded both textures and the icon scene from the PCK, verified 88x88 dimensions, transparent corners, an opaque center and matching outline alpha. Live in-game appearance has not been checked.

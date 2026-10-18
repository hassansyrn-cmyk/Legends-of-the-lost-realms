# Interaction feedback and skin quality

## Requested implementation plan

1. Extend temporary Developer Mode to all chapters and verify ordinary progression is restored when disabled.
2. Display decreasing bars and remaining seconds using the existing interaction timers, including cancellation and faster-channel relics.
3. Improve Aether Sovereign texture and mesh detail while retaining a cheaper distance LOD.
4. Replace whole-object green restoration with selective jewel glow; preserve the banner red-to-green change. Verify rendered results and push to legendary-skin.

## Changes

Developer Mode password remains 3258. Its session copy unlocks all 25 chapters, 48 weapons and 7 outfits. Normal saved progression is untouched.

World-space anchored countdowns cover expansion objectives and keys, secret caches, choice points, shortcuts, exploration finds, trial shrines, dungeon entry/exit and foci, storm memories/rods/circuits/relays, astral records/seals/routes/foci, and teleport gates. The vault chest-opening bar also decreases with remaining seconds. Bars use the real gameplay durations and disappear on completion or cancellation; no new waiting time was added. Active hints and the HUD have reserved space above the timer labels.

Restored objectives keep their original base colour and textures. A texture-based mask emits light from cool-coloured jewel pixels, with a small child light and checkpoint-style activation effect. Non-jewel surfaces retain their textures. Banners keep their existing green texture and green emission without the new cyan light. Astral focus controllers remain compatible with the new shader.

Aether Sovereign now retains its original 2048px texture, with Android ASTC 4x4 compression. A 60,000-triangle close LOD replaces the old 24,000-triangle mesh; the distance LOD has 20,000 triangles. Only one renders at a time. Original rig, animation root normalization and locked movement tuning are preserved.

## Verification

Unity 6000.6.0f1, Direct3D 11. See results.txt and the PNG captures in this directory. The probe protects the player's save with -menuVisualProbe and checks real interactions, pause, cancellation, faster channels, restoration material isolation, banner appearance, astral controller compatibility, shader compilation, wardrobe texture resolution, and both skin LODs. Screenshots are checked for magenta rendering failures and inspected visually. Skin pose, foot alignment, animation motion, zero RootT curves, and the exhaustive 15,000-outcome loot check also pass. All six existing verification scripts pass.

Existing Unity Search startup indexing exceptions and imported gate missing-script warnings are unrelated to this work. An Android device performance check and a new release bundle remain outside this verification.

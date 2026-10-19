# Arabic / English localization

Implemented on the legendary-skin worktree from fc4a8b9. The original dirty astra-expanded checkout is preserved.

## Player behavior

- A native-name language button (العربية / English) is available at the top of the main menu and settings.
- The choice is saved independently as LostRealms.Language, so new journeys, changing chapters and toggling Developer Mode do not reset it. Existing players retain English until they choose Arabic.
- Switching updates the current UI and existing world labels without a chapter reload. Source data, resource paths, IDs, save keys, movement and combat values remain unchanged.

## Translation

943 authored catalog entries cover menus, HUD and control hints, all 25 chapter names and objectives, story records and ending, Lyra's quest, mastery/relics, crypt, trials/discoveries, bestiary and achievements, weapons/outfits and stat descriptions, chests/probability disclosures/rewards, combat notifications, developer controls and interaction countdowns. Modern Standard Arabic uses a consistent fantasy glossary. Keyboard letters, numerical values and third-party brand names remain recognizable. No authored spoken dialogue was found; music, combat voices and SFX are preserved.

The editable English/Arabic catalog is Assets/Resources/Localization/GameText.json. Every entry includes a stable content key, source, Arabic text, context and a character budget. Exact entries and parameterized templates take priority; source fragments support the existing composed messages. Only text at rendering sinks is localized, so internal model names and gameplay comparisons cannot change language.

## Arabic rendering

Bundled Noto Sans Arabic UI provides complete glyph coverage and compact UI line metrics. Its SIL OFL license and source URL accompany the font. MIT-licensed Arabic Support is vendored with its original license and provenance.

Logical paragraphs wrap before shaping; balanced bracket groups and bullet stat items stay together. Signed numbers, decimals, fractions, ranges, clocks, percentages and balanced brackets are protected during bidi processing. Unity 6000.6 IMGUI reorders text itself, so each prepared visual line uses an explicit LTR override to avoid reversing it twice. Legacy TextMesh needs the prepared visual string directly. Both paths have separate rendered comparison fixtures. Diacritic combination is explicitly disabled to avoid an upstream position bug while preserving individual marks.

Arabic text aligns to the right where appropriate. Menu icons and navigation arrows mirror; physical gameplay controls, compass and world navigation retain their directions. Fitted layouts are cached to avoid repeating Arabic wrapping and measurement every frame.

## Verification

Run with Unity 6000.6.0f1, -batchmode -force-d3d11 -menuVisualProbe -executeMethod LostRealms.ArabicLocalizationValidation.Run (no -quit).

The probe verifies English fallback, translated labels, every dynamic template, placeholder parity, complete font coverage, connected letters, numeric/sign/bracket integrity, current-world language switching, Developer Mode compatibility and save isolation. Captures cover main/settings, both atlas pages, wardrobe, arsenal, bestiary, achievements, mastery, relics, Lyra's quest, vault/probabilities, developer prompt, HUD, pause, completion and defeat. Rendering fixtures distinguish native IMGUI bidi from legacy TextMesh. Results and captured text are adjacent to this document. More than 300 UI and narrative entries were independently reviewed for wording and consistency.

All six existing regression scripts pass. Locked movement constants and generated Aster animation clips were not changed. Existing Unity Search startup indexing exceptions and imported gate missing-script warnings are unrelated. Android device typography and performance still need an on-device playtest; no new release bundle or Play Console upload is included.

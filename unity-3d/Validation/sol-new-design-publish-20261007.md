# Sol New Design

The finished menu redesign uses generated blue-enamel and gold-trim artwork, coordinated fantasy fonts, fitted labels and icons, consistent chapter cards, distinct Normal/Golden/Epic chests, and animated Rare/Epic item reveals. The Epic coffer faces the same direction as the other tiers.

## Cleanup

The old menu images were replaced at their existing Unity resource paths, preserving importer GUIDs. The unused `UI_Icon_CompletionBadge.png` and its importer metadata were removed after checking runtime names and serialized GUID references. All remaining 50 UI PNGs are used by the game; the three fonts are also present with licenses and metadata. There are no missing resource keys or duplicate UI GUIDs. HUD artwork and progress bars remain because the game uses them.

Generated screenshot previews and the original-design rollback backups remain locally outside the shipped asset set. Screenshot previews are excluded from Git through `Validation/MenuReview/`. Asset prompts, font licenses and review notes are included in this branch. The earlier asset manifest records generation-time measurements, including the subsequently removed badge.

## Verification

- All six `.github/scripts/verify-*.sh` checks passed: Aster motion, weapon system, startup safety, Phase 2 relics, Phase 3 graphics and Unity CI configuration.
- Unity 6000.6.0f1 compiled the final code and rendered three final 1280 × 720 screens: Atlas, locked Atlas and Vault. All were visually inspected. `MENU_VISUAL_REVIEW_CAPTURED 3`; no C# errors, missing fonts or text overflow diagnostics.
- The preceding menu review covered 22 states; reward review covered Rare/Epic, duplicate/common and settled rewards. Final changes after those reviews were chapter positioning, shared chest art areas and Epic orientation.
- Runtime resource audit: 53 resource keys, zero missing image/font/metadata files, zero unused UI PNGs and zero duplicate UI GUIDs.
- `git diff --check` passed. A physical Android device was not tested.

The branch starts from `d298274` on `chest-rewards`. Old committed artwork remains recoverable through that parent revision; ignored local backups also retain the original menus and intermediate chest designs.

# Minimal blue and gold in-game HUD

Generated raster panels and six action icons replace the previous green HUD. Generated art has no baked labels or values. Existing Cinzel action labels and Crimson Text information are rendered live with width fitting. HP, health lag, energy, currencies, chapter title, weapon, gate state, timer, notifications and boss phase continue using gameplay state. The next-isle distance/compass box was removed at the user's request.

Follow-up: action captions were raised slightly; the hold hints now occupy a centered 56%-width lower chord and end at 88% of button height, above the bottom jewel. Hints use smaller fitted text and follow the pressed drawing rectangle. Runtime compilation passed after this adjustment. A pre-adjustment script backup is in `Temp/opencode/hud-hint-spacing-20261007-backup`.

Joystick outer base and inner knob are two distinct PNG resources. The base remains fixed at virtual (150,612); the 58px knob moves by TouchRouter.Move * 50 and returns when movement input ends. Six action hit rectangles and all gameplay input handlers are unchanged. Pressed buttons retain their visual response. Pause and element selector remain clickable.

Original Aster portrait SHA-256, identical to the pre-change backup:
`2602509BCCFB3EE5C3DACE59632989476EBE624E8D0E8AA2115A8806ACC46FB0`

Validation: runtime and editor scripts compiled successfully with existing deprecation/unused-field warnings; all six source regression checks passed. Restore script parses successfully. Generated images were visually inspected. In-game screenshot verification is pending while the existing Unity editor holds this project open. `MenuVisualProbe.RunHUD` captures idle, frost, gale, low health, long chapter title and boss HUDs, and checks all six action routes, hold release, movement and return-to-center.

Rollback: `Temp/opencode/revert-minimal-hud-20261007.ps1`, backed by `Temp/opencode/hud-minimal-20261007-backup`. Restores only this HUD pass, preserving menu redesign and the preceding war axe / ladder changes. No Aster animations, character model, movement physics or gameplay handlers changed. Included in the `Sol-New-Design` branch as a separate HUD commit for straightforward rollback.

Image provenance: built-in imagegen; exact prompts in `hud-minimal-20261007-prompts.json`, generated asset dimensions and source paths in `hud-minimal-20261007-assets.json`.

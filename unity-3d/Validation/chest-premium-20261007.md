# Premium chest and reward reveal review — 2026-10-07

The Atlas treasury coin moved from x740 to x770, with a 22px icon and consistent spacing for gold, gems and stars inside the existing capsule.

Golden and Epic chests now use distinct generated transparent artwork. Golden has a sun-medallion lock and gold-dominant craftsmanship. Epic adds a crown, dragon finials, platinum trim and amethyst accents. Both fit their card art areas without obscuring text or buttons.

New Rare and Epic weapon/outfit rewards animate an expanding generated halo and outward star sprites. Rare uses gold for 2.1 seconds; Epic uses violet, more stars and a 2.8-second reveal. Animation uses unscaled time while the menu is paused. Duplicate and currency rewards do not trigger the item celebration. Existing reward sounds remain in place.

## Saved artwork

- `Assets/Resources/UI/Icons/UI_Chest_Golden.png` (768 × 512, transparent)
- `Assets/Resources/UI/Icons/UI_Chest_Epic.png` (768 × 512, transparent)
- `Assets/Resources/UI/Icons/UI_Reward_Halo.png` (512 × 512, transparent)

Generated using the built-in imagegen tool. Exact prompts are saved in `chest-premium-20261007-prompts.txt`. Integration only positions and animates the generated raster artwork.

## Verification

Unity 6000.6.0f1 rendered seven 1280 × 720 screens: Atlas, Vault, Rare item, Epic outfit, duplicate, common resources and settled Epic reward. All seven were visually inspected. The second pass strengthened the glow by rendering it above the item box and beneath the item itself.

`Temp/opencode/chest-premium-visual-review-final.log` contains `MENU_VISUAL_REVIEW_CAPTURED 7`, no C# compilation errors, no text overflow diagnostics and no reward-effect gate failures. The gate assertions verify that new Rare/Epic items qualify while duplicates and common resources do not. Existing unrelated missing-script/Android editor warnings remain. This review validates menu visuals, not Android device performance.

Screenshots: `Validation/MenuReview/{atlas,vault,vault-rare,vault-reward,vault-duplicate,vault-common,vault-settled}.png`. The probe uses synthetic progress and disables player-save persistence.

## Reversal

`Temp/opencode/revert-premium-chests-20261007.ps1` restores the menus exactly as they were before this chest/coin/reward pass, retaining the earlier blue-and-gold redesign. Its backup is `Temp/opencode/chest-premium-20261007-backup`.

`Temp/opencode/revert-menu-art-20261007.ps1` restores the original pre-redesign menu artwork and script. Both rollback scripts passed PowerShell syntax checks; neither has been executed. `git diff --check` passed. Character animation, locked movement, weapons, chest odds and costs were not edited by this pass.

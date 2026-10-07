# Chapter card and Normal chest alignment — 2026-10-07

Chapter labels moved from 16px to 31px below each unlocked card's top edge, with wider side padding. Chapter titles and stars were moved slightly lower to maintain separation. Locked cards retain their existing content placement.

The Normal chest now uses a generated bronze, iron and wooden coffer. Its previous small inner frame is removed. All three chest tiers use one shared 200 × 96 art area at the same offset and baseline, preserving their distinct designs.

Artwork: `Assets/Resources/UI/Icons/UI_Chest_Normal.png` (768 × 512 transparent PNG). Created with the built-in imagegen tool. Exact prompt: `Validation/chapter-normal-20261007-prompts.txt`.

Backup: `Temp/opencode/chapter-normal-20261007-backup`.
Revert only this pass: `Temp/opencode/revert-chapter-normal-20261007.ps1`. The earlier full-menu and premium-chest rollback scripts also remove the new Normal chest if those earlier revisions are reverted. Rollback syntax and `git diff --check` pass.

Final rendered review passed before publishing `Sol-New-Design`. `LostRealms.MenuVisualProbe.RunAlignment` captured Atlas, locked Atlas and Vault at 1280 × 720 using synthetic progress with player-save persistence disabled. All three screenshots were visually inspected: chapter labels clear their outlines, and all three chests use the same size, alignment and orientation. The log `Temp/opencode/sol-new-design-final-review.log` reports `MENU_VISUAL_REVIEW_CAPTURED 3`, with no C# errors, missing fonts or text-overflow warnings.

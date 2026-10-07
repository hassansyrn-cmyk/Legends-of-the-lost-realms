# Chapter placement and enemy physics review

7 October 2026 — local changes on `Sol-New-Design`.

All 15 chapters passed the final layout inspection. The scan covered 1,533 prop roots and found no unsupported or floating placements, missing solid colliders, invalid overlaps, obstructed central lanes, or props inside reserved trap sweep areas. Natural ground cover beneath tree foliage is allowed.

## Changes

- Scenery is placed on visible deck geometry, with support checks at the center, edges, and corners. Architecture receives space before smaller dressing. Unsupported or crowded individual placements are omitted.
- Solid scenery keeps clear traversal lanes, spawn plazas, checkpoints, gates, and trap movement areas. Collision boxes use mesh-local bounds so rotation cannot inflate them. Tree trunks retain appropriately scaled, upright capsule collision.
- Every enemy family now uses a collision controller, fixed-step movement, gravity, and a terminal fall speed. Movement stops against walls; knockback can push enemies over edges. Enemies retain collision while airborne and award a single defeat after falling into the void. Corpses killed in midair also fall.
- Moving platforms identify enemy riders before changing position. Enemy spawns are checked after world dressing and moved away from trap areas.
- All trap generations route environmental enemy damage through the guardian-immunity rule. The first guardian survives its arena traps before Aster arrives, while player attacks still damage it.
- Trial shrine placement searches additional supported, accessible positions. A stale inventory-test weapon identifier was corrected.

Aster’s movement settings, animation baker, animation clips, models, menus, portrait, and wardrobe preview were preserved.

## Validation

- Full regression suite: **2,218 passing runtime assertions**.
- After the final collider and placement refinements: **250 passing focused runtime assertions**, covering all 15 chapters, all 22 enemy families, walls, falls, moving platforms, pause, shrine access, airborne deaths, defeat credit, and guardian immunity.
- All six repository regression guards passed. Runtime and editor compilation passed. The focused playthrough emitted no runtime errors.
- Visually inspected 45 chapter renders: start, middle, and exit/guardian views for each chapter.

| Chapter | Props | Enemies | Layout issues |
|---|---:|---:|---:|
| 01 | 188 | 9 | 0 |
| 02 | 126 | 9 | 0 |
| 03 | 110 | 9 | 0 |
| 04 | 65 | 6 | 0 |
| 05 | 86 | 11 | 0 |
| 06 | 108 | 11 | 0 |
| 07 | 46 | 6 | 0 |
| 08 | 114 | 14 | 0 |
| 09 | 116 | 15 | 0 |
| 10 | 66 | 8 | 0 |
| 11 | 102 | 17 | 0 |
| 12 | 104 | 19 | 0 |
| 13 | 120 | 20 | 0 |
| 14 | 132 | 20 | 0 |
| 15 | 50 | 11 | 0 |

## Visual review

[Chapters 1–5](<ChapterPhysics/review-01.jpg>)
[Chapters 6–10](<ChapterPhysics/review-06.jpg>)
[Chapters 11–15](<ChapterPhysics/review-11.jpg>)

## Rollback

A scoped rollback is saved in `Temp/opencode/restore_chapter_physics.ps1`, with the original files in `Temp/opencode/chapter-physics-20261007-backup`. It restores this task’s changes and removes the new physics/probe scripts while preserving the previously approved character and UI work. The backup is local; the committed changes can also be reverted through Git.

# Campaign and gameplay expansion proposal

Prepared 8 October 2026 for the current Unity game on Sol-New-Design. This is a proposal; no gameplay has been changed.

## Goal and current baseline

The owner's current campaign completion time is under one hour. The implemented campaign has 15 chapters across four realms, with guardians at chapters 4, 7, 10 and 15. Regular chapters already grow from 12 to 19 islands. It also has optional shrine trials, elemental weaknesses, several weapon styles, defense mechanics, seven upgrade tracks, collections and treasure chests.

Increasing island counts again would add distance without necessarily adding decisions. Prioritize distinct objectives, encounter composition, exploration rewards and build choices.

Proposed targets, to validate through playtesting rather than promise:

- Main campaign: 25 chapters, six realms, approximately 3.5–5 hours on a first clear.
- Optional exploration and side content: another 2–3 hours.
- Broad completion: approximately 6–8 hours; repeatable challenges remain separate.
- Typical main chapter: 8–12 minutes, with checkpoints and resumable progress. Do not force every chapter to meet a minimum duration.
- Experienced players should still be able to finish faster through skill and route knowledge.

Duskblade and Soulreaper currently have temporary testing pickups in chapters 1 and 2. Remove these from the normal campaign only after weapon testing is complete, retaining a separate testing route. Compare fresh-save, normal-loadout and experienced-player runs; their effect on completion time is currently unmeasured.

## Phase 1 — measure pacing and prototype three richer chapters

Record chapter clear times, deaths, skipped encounters, optional participation and equipped weapons. Capture active play time separately from menus and retries. Start with the owner's run and 3–5 players unfamiliar with the routes, then expand testing before final balance decisions.

Prototype chapters 2, 6 and 9 using current art and controls:

| Chapter | Proposed main experience | Optional branch |
| --- | --- | --- |
| Whispering Falls | Restore a ruined crossing through two short encounters and a route-choice puzzle | Hidden waterfall shrine with a named elite and a guaranteed reward |
| Temple of Keys | Extend the existing key theme into a readable temple mechanism, ambush and return shortcut | Sealed burial chamber with an elemental challenge |
| Crystal Hollow | Redirect crystal energy to open a route, then cross a changing hazard sequence | Frozen cavern with a frost elite and a lore discovery |

Each prototype should have a recognizable landmark, one main objective with clear feedback, a safe checkpoint, an optional detour and a different encounter rhythm. Design generous solutions using existing jumping, movement and elemental powers. Avoid precision puzzles that depend on new movement physics.

Gate: unfamiliar players understand the objective without developer guidance; the detour is rewarding; no softlocks, unsupported props or blocked routes; players can explain how these chapters differ. Measure duration before committing to the full campaign expansion.

## Phase 2 — give the existing 15 chapters identities

Keep current chapter names and progression. Author each around a combination of exploration, traversal, puzzle and combat rather than repeating the same island sequence.

- Verdant Kingdom: ruined crossings, overgrown shrines, ambush clearings and an Elder Grove guardian finale.
- Desert realm: buried courtyards, temple mechanisms, exposed hazard routes and the lava golem finale.
- Frozen realm: crystal circuits, timed hazard windows and deliberate traversal choices, culminating in the winter guardian.
- Emberfall: siege paths, coordinated enemy squads, lava mechanisms and a Warden finale that concludes the first campaign act.

Across these chapters, add six reusable objective types: restore a mechanism, find keys through different routes, clear a deliberate ambush, solve an elemental interaction, stop a ritual and defeat a named elite. Use authored combinations and locations; do not randomly stack every type into every chapter.

Add six named elite encounters across the existing realms, each with one signature move and a first-clear reward. Reuse suitable enemy models with distinct animation, attack tells and equipment before commissioning more models.

Replace the main gate's generic collection-percentage requirement chapter by chapter with explicit authored objectives. Keep coins, gems, secrets and optional fights meaningful for rewards and stars. Do not require all pickups to advance. Preserve existing saves and earned stars through migration.

Retain and improve existing trials. Connect them to optional routes and specific encounters; adding another generic hunt counter is not enough. Stars should reward exploration and mastery without blocking the story.

## Phase 3 — deeper combat and progression

Use the current arsenal before adding many more weapons:

- Four mastery paths: blade, heavy weapon, polearm and unarmed. Each offers a choice between two playstyle perks, with affordable respecs. Existing weapon style attacks remain the foundation.
- Eight initial equippable relics, with two slots. Examples: a perfect-parry energy refund, charged-hit burn, a riposte bonus or a double-jump spell discount. Cap stacking and preserve readable effect feedback.
- Predictable milestone rewards for elites, quests and dungeons. Keep the current treasure chest system; do not make a specific random chest drop necessary for campaign progress.
- Add encounter roles and complementary groups: shield guard plus ranged attacker, healer plus bruiser, or a pressure enemy protecting a ritual. Limit simultaneous attackers and keep clear telegraphs.
- Give existing guardians different phase mechanics and arena objectives, building on the lava golem's distinct patterns. Increase challenge through decisions and patterns rather than inflated health.

No new mobile action buttons in this milestone. Unlocks change existing actions, and build selection happens in the Sanctuary. Leave Aster's locked locomotion and root-animation pipeline intact. New mechanics need combat interaction checks and real device playtests.

## Phase 4 — two new realms and ten chapters

| Realm | Chapters | Identity | Gameplay hook | Guardian |
| --- | --- | --- | --- | --- |
| Stormveil Expanse | 16–20 | Storm towers, weathered bridges and cloud ruins | Redirect lightning between conductors; cross clearly signaled wind windows | Storm guardian with grounded chase, charge lanes and lightning patterns |
| Astral Ruins | 21–25 | Broken celestial temples and suspended observatories | Shift clearly marked platform routes and reconnect astral mechanisms | New final guardian combining previously taught mechanics with distinct phase changes |

Each realm has four adventure chapters and one guardian chapter, two new enemy families and one new environmental hazard family. Four new standard families total; no new enemy may ship without textured meshes, usable idle/walk/attack/hit/death animation and physical ground/fall behavior.

Add story beats through a realm contact, short environmental discoveries and guardian consequences. The Warden's defeat reveals the remaining fracture, providing a reason to travel onward. Use brief dialogue rather than expensive cutscene production for the first release.

Do not simply duplicate existing routes or recolor bosses. Each realm needs distinct architecture, encounter layouts, audio and an exploration mechanic. Prototype the hook before producing its full asset set. Require asset previews and in-game scale checks before integration.

Expand chapter saves, Atlas layout, realm mapping, achievements and test coverage together. Preserve old inventory, upgrades and stars. Assign new enemy/weapon identifiers from the actual current catalogs; older expansion documents contain identifiers now occupied by the integrated enemies and Epic weapons.

## Phase 5 — optional adventures

- Six optional dungeons, one per realm, about 15–20 minutes each. Each contains a small route puzzle, two authored fights and a unique elite or guardian variant. Use a curated layout and a guaranteed first-clear relic or cosmetic.
- Twelve compact side quests, two per realm, with a named contact, a specific location and a payoff. Prefer recovering a missing explorer, cleansing a shrine or reopening a lost route over repeating kill/collect quotas.
- Discoveries track lore and secrets in the Atlas. Signal entrances through landmarks and environmental clues; avoid adding a navigation distance bar.
- Optional content can be revisited and resumed. It does not block the campaign or require repeated farming.

Build one complete dungeon and quest chain first. Confirm players enjoy them before multiplying the template across six realms.

## Phase 6 — replay after the campaign

Add Rift Expeditions: 10–15 minute runs with authored encounter rooms, a choice of three temporary boons between fights and a final elite. Start offline with personal bests and seeded challenges; online rankings are a separate project.

Add guardian rematches with different patterns and explicit reward limits. Consider New Game Plus only after the main expansion is balanced; it needs altered encounters and build opportunities, not only higher health. Repeated runs do not count toward the first-clear campaign duration target.

## Production order and release gates

1. Pacing measurements plus the three-chapter prototype.
2. Existing campaign objective and encounter pass.
3. Mastery/relic prototype, one dungeon and one quest chain.
4. Stormveil hook, then its five chapters.
5. Astral hook, then its five chapters and ending.
6. Remaining optional adventures and repeatable Rifts.

This order permits review and rollback at each milestone. Avoid simultaneously rebuilding combat, every chapter and the save system. Estimate schedules only after the first prototype establishes production speed and asset requirements.

At each gate: verify Aster animation/physics guards, enemy ground and edge-fall behavior, prop placement and traversal clearance, weapon grip/orientation, warning-to-damage timing, save migration, and touch UI. Compile, run relevant focused checks plus the full chapter regression, visually inspect actual gameplay, and test on target Android hardware. Automatic assertion counts are useful evidence but do not establish that a chapter is enjoyable or long enough.

Performance should remain within the current target device budget. Prefer streaming or activating nearby encounter areas rather than keeping an expanded chapter's entire population active. Profile before choosing implementation limits.

## First implementation milestone

Implement only the three richer chapter prototypes and pacing measurements first. Keep the new systems reversible and preserve the current campaign as the comparison baseline. Review actual playtime, clarity and enjoyment before approving the broader production scope.

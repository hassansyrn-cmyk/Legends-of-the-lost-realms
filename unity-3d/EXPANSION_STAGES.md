# Game expansion stages

Branch: `Game-expansion`. Baseline: `43acf98` on `Sol-New-Design`.

Stage 1 implementation is complete: all 2,343 full campaign assertions and the six repository verification scripts passed. Human pacing, enjoyment and physical Android review are the next inputs before Stage 2.

The campaign duration target remains 3.5–5 hours for the eventual 25-chapter campaign, with 6–8 hours including optional content. Stage 1 is a playable prototype and measurement foundation, not a claim that those targets have already been reached.

| Stage | Deliverable | Exit gate |
| --- | --- | --- |
| 1 | Three distinct chapter prototypes, optional elite clearings, and local pacing evidence | Automated regression and visual review; owner/new-player feedback determines whether to scale the approach |
| 2 | Objectives, encounter composition and secrets across the existing 15 chapters | Each chapter has a distinct purpose; playtests confirm clarity and pacing |
| 3 | Four mastery paths, eight relics, one optional dungeon and one quest chain | Build choices work without new mobile buttons; rewards and difficulty are balanced |
| 4 | Stormveil Expanse, chapters 16–20 | Unique exploration hook, four adventure chapters, two enemy families and a guardian; save migration verified |
| 5 | Astral Ruins, chapters 21–25, and the new campaign ending | Distinct realm mechanics and guardian; full 25-chapter campaign verified |
| 6 | Remaining optional dungeons/quests, Rift Expeditions and guardian rematches | Optional rewards, replay variety and device performance validated |

## Stage 1: implemented prototype scope

- **Chapter 2 — Whispering Falls:** defeat the guards and restore two crossing beacons. The second clearing offers an alternate beacon. The gate opens from these objectives instead of collection percentages.
- **Chapter 6 — Temple of Keys:** recover two guarded temple keys. Recovering both activates a two-way return passage between the clearings; stand still at a passage to travel. Living nearby enemies prevent travel.
- **Chapter 9 — Crystal Hollow:** attune three crystals in order, Ember → Frost → Gale. Use the existing power selector and stand still beside the matching crystal.
- **Each prototype:** a larger, stationary side clearing at the fifth route island, with a named elite. Clearing it grants `60 + chapter × 5` Gold and four Gems once per chapter run. Rewards bank at the normal chapter finish. The elite is optional and is not included in mandatory foe totals.
- Objective interactions freeze during pause. Checkpoint retries preserve progress; restarting the chapter resets progress and reward eligibility. Existing trials remain available.
- Objective progress uses the existing resource panel; contextual instructions use the existing notice panel. No new permanent HUD box or action button.
- Temporary first-chapter Epic pickups are excluded from normal progression. Weapon-test launches can opt in with `-epicPlaytest`; the Editor expansion menu also equips either Epic for testing.

## Pacing measurements

All chapters record local, bounded playtest history in `campaign-pacing.json` under Unity's persistent data folder. The last 100 attempts are retained. No network telemetry is sent, and automatic validation never writes player pacing history or saves.

Each attempt records chapter, active unscaled gameplay time, paused time, defeat/retry time, outcome, starting/ending weapon, starting upgrade ranks, deaths, kills, initial/living foes, trial completion, objective completion and optional-clearing participation. `livingEnemies` is a remaining-enemy observation, not a definitive count of deliberately skipped encounters; summons can affect it. Time spent outside gameplay is separate from active time. Returning to the chapter menu closes the attempt.

In Unity, **Lost Realms → Expansion → Export Pacing Summary** creates a local chapter summary CSV and an attempt CSV in `Validation/ExpansionStage1/`. Compare completed fresh-save runs separately from retries, upgraded saves and Epic-equipped runs.

## How to review Stage 1

1. Play chapters 2, 6 and 9 from the Realm Atlas, or use the Editor expansion menu while the game is running.
2. Try both the direct objective route and each optional clearing. Review enemy placement, clarity of hints, platform support and return routes.
3. Pause while restoring a node; retry from a checkpoint; restart the chapter; verify the intended progress rules.
4. In Temple of Keys, recover both keys and test the two-way return passage.
5. In Crystal Hollow, try the wrong element and an out-of-order crystal, then complete the sequence.
6. Run a fresh-save playthrough without test Epics, export pacing evidence, and note what felt repetitive or unclear. Ask 3–5 unfamiliar players to try the prototypes before deciding on Stage 2.

Automated checks establish functional behavior, not enjoyment or a proven duration increase. Physical Android playtests and human pacing feedback remain review work after this implementation milestone.

## Reversibility

Stage 1 adds no saved progression schema and does not rewrite Aster's animation assets or physics tuning. Its implementation is isolated in this branch. Reverting the Stage 1 implementation commit restores the previous chapter behavior; the separate local pacing history can remain without affecting gameplay.

The detailed design proposal remains in `CONTENT_EXPANSION_ROADMAP.md`. This staged execution sheet controls implementation order; no later stage is authorized by the Stage 1 request.

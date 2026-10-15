# Game expansion stages

Branch: `Game-expansion`. Baseline: `43acf98` on `Sol-New-Design`.

Stages 1–5 are implemented. The full 25-chapter campaign passed 3,619 runtime assertions with zero runtime errors. The final Stage 5 build also passed 458 focused checks, and all six repository verification scripts pass. Human pacing, enjoyment, combat balance and physical Android review remain outstanding.

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

## Stage 2: existing-campaign expansion

The owner authorized Stage 2 on October 9, 2026. This extends the objective foundation to all 15 existing chapters; later stages remain separate work.

| Chapter | Main progression | Optional discovery |
| --- | --- | --- |
| 1 — Mosslight Trail | One guarded trail shrine; gentle introduction | Explorer's Cache |
| 2 — Whispering Falls | Two guarded beacons; alternate second beacon | Waterfall Sentinel: three tracking strike zones |
| 3 — Rootbound Ruins | Two guarded ruin keys | Rootbound Duelist: twin flank strikes |
| 4 — The Elder Grove | Two approach seals and Heartwood guardian | Elder Grove Memory |
| 5 — Sunscorched Pass | Two outposts, each with a reinforcement wave | Caravan Cache |
| 6 — Temple of Keys | Two guarded keys and return shortcut | Burial Chamber Warden: heavy radial slam |
| 7 — Sandstone Colossus | Two approach seals and lava golem | Sunscar Memory |
| 8 — Frostwind Climb | Frost → Ember ward sequence | Frozen Explorer's Cache |
| 9 — Crystal Hollow | Ember → Frost → Gale crystal sequence | Crystal Sentinel: outer-ring nova with a safe center |
| 10 — Crown of Winter | Two approach seals and Whiteout guardian | Winter Memory |
| 11 — Ember Foothills | Two guarded ritual channels, in order | Ember Pilgrim's Cache |
| 12 — Brimstone Rampart | Three outposts with reinforcement waves | Rampart Hexer: center and diagonal strikes |
| 13 — Cindervein Gorge | Three guarded ritual channels, in order | Cindervein Cache |
| 14 — Obsidian Ascent | Gale → Ember → Frost sigil sequence | Obsidian Executioner: advancing three-zone cascade |
| 15 — Emberfall Summit | Two approach seals and Emberfall Warden | Last Warden Memory |

The main gate now follows explicit chapter objectives throughout the campaign. Existing collection percentages still determine higher stars; pickups remain optional for advancement. Guardian chapters require both their seals and guardian victory.

Normal restoration takes 0.85 seconds standing beside a clear focus. Rituals require 2.5 seconds and follow a sequence. Elemental sequences use the existing power selector. Outposts release complementary melee/ranged reinforcements after their initial defenders are cleared; the focus completes only after the reinforcement wave is defeated.

Named elites reuse existing textured, animated characters and physics. Their signature attacks have a 1.15-second warning, locked strike locations, an eight-second cooldown, and obey the existing attack-token limit. Stun, freeze, death and falling cancel an uncommitted signature. Objective clearings use authored melee, ranged and bruiser pairings; reinforcements are bonus kills and do not change the chapter collection denominator. Pause freezes both warnings and attacks.

Non-elite caches grant `25 + chapter × 3` Gold and two Gems. Named elite clearings grant `60 + chapter × 5` Gold and four Gems. Rewards pay once per run and bank at chapter finish; checkpoint retries preserve discovery and objective progress, while full restarts reset them. Each non-guardian chapter has a stationary optional clearing at its fifth route island; guardian memories are tucked onto an approach island.

These additions do not add chapters, mastery tracks, relic progression, dungeons or replay modes. Playtime and enjoyment must be measured with human playthroughs; automated completion tests do not establish a campaign-duration increase.


## How to review Stage 2

Play the existing chapters from the Realm Atlas. The resource panel shows the current objective count, and the notice panel explains nearby interactions. Clear guards before standing beside a focus. Use the existing power selector for elemental sequences.

For a short review, try chapter 3 (keys and Duelist), 5 (outpost ambushes), 8 (Frost/Ember sequence), 11 (ritual channels), 12 (three ambushes and Hexer), and 14 (sigils and Executioner). Named elites are on optional clearings near the fifth route island in chapters 2, 3, 6, 9, 12 and 14. Guardian memories are on the first approach island in chapters 4, 7, 10 and 15.

Pause while channeling and during a warned elite strike. Retry a checkpoint and verify progress remains; restart the chapter to begin a fresh objective run. Finish a chapter to bank discovery rewards. Use the existing pacing export to compare fresh and upgraded runs.

Evidence: `Validation/expansion-stage2.md`, with 36 rendered review images in `Validation/ExpansionStage2/`. These changes are local on `Game-expansion`; Stage 2 has not been committed or pushed.

## Stage 3: mastery, relics and the first optional adventure

The owner authorized Stage 3 on October 9, 2026. This adds build choices to existing actions, eight relics, Oathkeeper's Crypt and Lyra's Wayfinder quest. Stormveil and the later expansion stages remain separate milestones.

**Sanctuary → Mastery & Relics** offers Blade, Heavy, Polearm and Unarmed paths. Finish chapter 2 to unlock mastery; existing completion counts. Each path has two perks. Its first choice is free and changing the learned perk costs 25 Gold. Only the current weapon's path contributes. No new action buttons or Aster animation clips are introduced.

| Path | Choice A | Choice B |
| --- | --- | --- |
| Blade | Duelist: ripostes deal +25% damage | Flow: third-combo swings restore 8 Energy |
| Heavy | Rupture: charged melee staggers non-guardians for 0.45 seconds | Resolve: perfect defense heals 1 HP, eight-second cooldown |
| Polearm | Precision: outer-reach melee deals +20% damage | Conduit: spells cost three less Energy |
| Unarmed | Pressure: third-combo strikes deal +25% damage | Meditation: perfect defense restores 12 extra Energy |

Two relic slots accept distinct owned relics; collecting one does not auto-equip it. Guaranteed unlocks are saved immediately, while the existing per-run discovery Gold/Gems still bank at chapter finish.

| Relic | Effect | Guaranteed source |
| --- | --- | --- |
| Dawn Sigil | Perfect parry restores eight extra Energy | Finish chapter 1; old completed saves credited |
| Ember Seal | Charged melee applies a three-second burn | Chapter 2 optional elite discovery |
| Duelist Knot | Ripostes deal +15% damage | Chapter 3 optional elite discovery |
| Skyglass | Airborne spells after a double jump cost five less Energy | Chapter 6 optional elite discovery |
| Wardstone | Perfect dodge heals 1 HP, eight-second cooldown | Chapter 9 optional elite discovery |
| Siphon Shard | Defeating a foe restores six Energy | Chapter 12 optional elite discovery |
| Frost Emblem | Matching-weakness melee deals +10% damage | First crypt clear |
| Wayfinder Heart | Restore/ritual channels finish 25% faster | Lyra's quest reward |

Bonuses apply conditionally: melee burn/stagger do not affect ranged weapon holds, Precision uses the existing polearm tip region, and Skyglass needs the second jump. Damage bonuses remain bounded; spells cost at least seven Energy. Wardstone and Resolve cannot heal twice from a single defense, and each has its own cooldown. Energy and healing obey existing caps.

**Sanctuary → Lyra / Wayfinder Quest** accepts a commission, then tracks: chapter 2 Waterfall Sentinel discovery → both chapter 6 temple keys → Oathkeeper's Crypt → return to Lyra. Claiming pays the Wayfinder Heart and 200 Gold once. Clearing the crypt before the temple step is recognized when that step is reached. Players who already passed the early quest locations can revisit them.

**Oathkeeper's Crypt** is on chapter 3's optional clearing beside the Rootbound Duelist. Defeat him, then stand at the blue entrance crystal. The five-room route has an oath memory, shield guard/caster fight, Ember → Frost → Gale cipher, bruiser/ember archer fight, and a telegraphed Oathkeeper duel. Existing textured character models, root animation, double jump, collision and fall physics are reused. Carved stone arches mark the crypt rooms. Golden crystals restore a room after its encounter or puzzle is complete; blue return crystals leave the dungeon.

Completed rooms persist between visits and game restarts. An unfinished room's enemies and puzzle reset on re-entry. Main-chapter objects are suspended while inside, then restored to their actual prior active state. Dungeon navigation/checkpoints follow its route. Main-campaign completion cannot trigger from the crypt. First clear banks the Frost Emblem and 150 Gold; replay grants no duplicate first-clear reward.

### How to review Stage 3

1. Open Sanctuary, choose mastery perks, equip two relics, and accept Lyra's commission. Try a free first choice, a paid switch, and switching with insufficient Gold.
2. Compare each weapon family and its two choices with the same enemies. Test parry, dodge, third-combo finishers, charged melee, polearm tip hits and spells after a double jump.
3. Recover the map in chapter 2 and both keys in chapter 6. Enter the crypt from chapter 3, clear its encounters and ordered cipher, then return to Lyra.
4. Pause during restoration, leave during an unfinished encounter, restart the game and return. Check that completed rooms survive and unfinished fights reset.
5. Replay a cleared crypt and verify no repeat first-clear payout. Play on the target Android device to assess readability, touch input and combat balance.

Stage 3 is local on `Game-expansion`. The Stage 2 baseline files were backed up under `Temp/opencode/stage3-baseline/` before implementation, so this milestone can be removed without discarding Stage 2. New progression fields can remain in a save without affecting the older implementation. Automated checks establish behavior; human playtests are still needed to establish enjoyment and actual playtime.

Stage 3 evidence: `Validation/expansion-stage3.md`, five crypt room renders and seven GUI captures in `Validation/ExpansionStage3/`. The full campaign and final focused runs emitted zero runtime errors. No Stage 3 commit or push has been made.

## Stage 4: Stormveil Expanse

The owner authorized Stage 4 on October 9, 2026. This adds chapters 16–20. Astral Ruins, chapters 21–25 and later replay modes remain separate stages.

| Chapter | Route and encounter identity | Main progression |
| --- | --- | --- |
| 16 — Cloudbreak Approach | Gradually rising switchback terraces; first wind crossing | Two circuits with two relays each |
| 17 — Thunderwatch Ruins | Paired watchtowers and wide alternating courts | Three guarded two-relay circuits |
| 18 — Galeweaver Causeway | Alternating bridge lanes and signaled wind windows | Three guarded three-relay circuits |
| 19 — Eye of the Tempest | A winding ascent around the storm's eye | Four guarded three-relay circuits |
| 20 — Crown of Stormveil | Two approach circuits and a broad guardian court | Both three-relay circuits and the Tempest Regent |

**Conductors:** clear the nearby guards, then stand on a small side pad for 0.65 seconds to turn the relay's arm. Step off before turning it again. Three pad positions aim at three different directions. Aim the lightning at the next blue focus and complete the entire chain. Blue links are aligned; amber links miss their receiver. A fully linked circuit locks after 0.9 seconds. This uses movement alone on keyboard, controller and touch, without adding an action button or reusing the elemental-sequence control.

**Wind windows:** bridge markings cycle through green calm, amber warning and red gust. Gusts push bodies only over the exposed gap, leaving the broad puzzle terraces safe. Pause freezes the cycle. The masonry and bridges use imported meshes, measured collision footprints and clear central lanes; weathered joints are smoothed within the visible deck's footprint.

**New foes:** Storm Stalkers are grounded, textured reptilian melee pursuers. Storm Scribes are articulated armored conductor keepers that retreat when crowded and warn a locked-position lightning strike. Both use real rigged idle, walk, attack, hit and death clips and the existing enemy collision, knockback, gravity and abyss deaths.

**Tempest Regent:** a separate armored gryphon construct with grounded pursuit and obstacle avoidance. It uses a close claw strike, locked lightning volley, warned charge lane and a storm cross with a safe center. Its second and third phases accelerate pursuit and expand the volley. Stand beside one of four arena conductors for 1.1 seconds to ground its storm, interrupt the pending attack and expose a three-second recovery. Conductors share a ten-second recharge. It remains immune to environmental trap damage.

Each chapter has an optional storm record on a reachable side court, granting 80 Gold and three Gems once per run. The records connect Lyra's earlier commission to the fracture revealed by the Emberfall Warden, and point toward the later Astral stage. The final record requires Regent victory. Rewards bank with chapter completion. Circuits and records survive checkpoint retries; a full chapter restart resets the run.

The Realm Atlas has a second Stormveil page with the existing gold/blue cards and typography. Old saves expand from 15 to 20 chapter slots without losing earlier stars, times, currencies, upgrades, inventory, relics or achievement claims. Previously completed chapter-15 saves unlock chapter 16 automatically. Existing achievement IDs are retained; three Stormveil achievements and three bestiary entries are appended. Campaign progress now shows 20 chapters and 60 stars. Chapter 20 does not unlock an unimplemented chapter 21.

### How to review Stage 4

1. Open **Realm Atlas → New Realms → Chapter 16**. Completed old campaigns start at chapter 16; editor review keeps all chapters accessible as before.
2. In Cloudbreak Approach, clear a circuit's guards, try an incorrect pad and observe the amber beam. Turn every relay toward its next focus to restore the circuit.
3. Compare the Storm Stalker's pursuit with the Storm Scribe's retreat and warned lightning. Dodge after the target warning locks.
4. In Galeweaver Causeway, watch the bridge markings and cross during green calm. Test pause during the wind warning and while turning a relay.
5. In Crown of Stormveil, finish the approach circuits, let the Regent pursue, dodge its charge lane and ground an arena conductor. Confirm the recharge and longer recovery.
6. Retry a checkpoint, restart a chapter, revisit a collected record, and finish chapter 20. Check saved stars, banked rewards and the return to the Atlas.

Stage 4 is local on `Game-expansion`. Its pre-change Stage 3 baseline is backed up under `Temp/opencode/stage4-baseline/`. Validation and visual review are complete; see `Validation/expansion-stage4.md`. A guarded rollback is prepared in `Temp/opencode/restore_stage4.ps1`; no Stage 4 commit or push has been made. Human pacing, combat balance and physical Android review remain necessary before claiming a duration or performance target.

## Stage 5: Astral Ruins and the campaign ending

The owner authorized Stage 5 on October 9, 2026. This completes the main campaign at 25 chapters and 75 stars. Stage 6 optional adventures and replay modes remain a separate milestone.

| Chapter | Route identity | Required star links |
| --- | --- | --- |
| 21 — Starlit Threshold | Twelve celestial terraces; introduction to branching courts | Two Sun/Moon mechanisms, four focuses |
| 22 — The Broken Orrery | Fourteen terraces with staggered elevations and fracture pulses | Three mechanisms, six focuses |
| 23 — Moonshard Galleries | Fifteen terraces ascending through alternating galleries | Three mechanisms, six focuses |
| 24 — Sanctum of Echoes | Sixteen terraces following an undulating switchback | Four mechanisms, eight focuses |
| 25 — Heart of the Fracture | Ten approach terraces and a broad final observatory | Two mechanisms, four focuses, then the Fracture Keeper |

**Shifting star routes:** each hub has a Moon court on its left and a Sun court on its right. Stand on the matching labeled pad for 0.65 seconds, wait for the 0.9-second warning, then cross the selected side bridge. Clear that court's guards and stand beside its orrery for one second to connect its focus. Return to the hub and connect the other court. Every required focus must be connected before the chapter gate opens. A bridge cannot shift while Aster or a living enemy occupies it, including during its pending warning. Pause freezes warnings and channels. Checkpoint retries preserve links; a full restart resets them.

**Astral fractures:** from chapter 22, marked pulses warn with a floor ring for 1.5 seconds before erupting. They deal one damage to Aster and push ordinary enemies. They cannot damage guardians. The pale ivory/gold temples, amethyst star circuits, hollow arches and circular decks sit in a violet celestial sky. Deck and bridge collision follows the actual imported meshes rather than an invisible rectangular floor over the void.

**New enemies:** Astral Sentinels are grounded lance-and-buckler melee defenders. Star Oracles retreat when crowded, lock a warned starfall at Aster's position, and heal nearby living Sentinels by 1.5 HP, at most once every seven seconds. Both have textured generic rigs with real in-place idle, walk, attack, cast, hit and death motion, and retain the existing collision body, gravity and fall deaths.

**Fracture Keeper:** a separate six-armed, rigged final guardian with grounded pursuit, a warned charge lane, a locked starfall volley and a spreading sweep. Its three phases use Ember, Frost and Gale wards in that order. Match the existing elemental power selector and stand beside the corresponding arena focus for one second to interrupt its attack and expose it for ten seconds. Each new phase restores the ward. Its current ward or exposed state appears on the existing guardian HUD. Environmental traps cannot kill it, and its attacks do not use a jumping root that could sink through the arena.

Each Astral chapter has an optional Lyra/observatory record worth 100 Gold and four Gems once per run, banked at chapter completion. The last record requires guardian victory. The records explain the fracture and tie Lyra's earlier quest to the reconstructed crossings. Finishing chapter 25 offers **Read Epilogue**, with interactive Realm Atlas and Sanctuary destinations.

Old saves expand to 25 score slots without losing earlier times, stars, currencies, inventory, relics or achievement claims. Existing chapter-20 completion unlocks chapter 21. The Atlas's **New Realms** page presents chapters 16–25; three Astral bestiary entries and three achievements are appended without changing existing IDs. Aster's portrait, generated clips, locked movement and two-part analog remain unchanged.

### How to review Stage 5

1. Open **Realm Atlas → New Realms → Chapter 21**. Clear a court, reconnect its focus and return over the bridge before selecting the other pad. Try shifting while occupying the bridge.
2. In chapters 22–24, follow the floor warning, dodge a fracture pulse, and compare Sentinel pursuit with Oracle retreat and starfall. Try pause during a bridge warning or focus channel.
3. In chapter 25, complete both approach mechanisms. Let the Keeper chase, avoid its locked attack warnings, match its current ward at an arena focus and attack while exposed. Repeat for all three phases.
4. Collect a record twice, retry a checkpoint, restart a chapter, and finish it. Confirm reward banking and the different progress rules.
5. Finish chapter 25 and open **Read Epilogue**. Test both destination buttons and returning to earlier chapters. Check the new bestiary and achievement pages.

The pre-Stage-5 baseline is saved under `Temp/opencode/stage5-baseline/`, preserving Stages 2–4. The milestone remains local on `Game-expansion`; no Stage 5 commit or push is authorized. Validation evidence is recorded in `Validation/expansion-stage5.md`. Human pacing, difficulty and target-device playtests remain necessary to assess the expanded game.

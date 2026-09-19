# Legends of the Lost Realms — Professional-Level Upgrade Plan

## Executive assessment

The active product is the **Unity 3D edition** in `unity-3d/`. It is already a substantial playable vertical slice rather than a prototype. The current build contains fifteen chapters across four realms, four guardian encounters, forty-one weapon definitions plus fists, elemental powers, enemy families with state-based attacks, affixes, checkpoints, trials, save progression, audio, post-processing, mobile controls, and a sizeable automated validation suite.

The next quality leap should not be a random feature expansion. It should make the existing systems feel like one intentional action-adventure game. The priority is to establish a repeatable player loop:

> **Explore → read a threat → make a movement/combat decision → earn a meaningful reward → improve the build → face a more expressive challenge.**

At present, the project has many good ingredients, but several are still implemented as bespoke runtime logic. Level structure is largely generated from route arrays, the economy is mostly a chapter-end bank with a weapon drop, the upgrade tracks are broad stat ranks, and character identity is concentrated in the hero and guardian labels rather than in authored relationships and quest-driven context. Professional quality will come from authored variation, clear rules, controlled progression, and physical-device proof.

## Current strengths

| Area | Current evidence | Assessment |
|---|---|---|
| Campaign scope | 15 named chapters, four realms, four guardian fights | Strong foundation for a compact premium campaign |
| Core movement | Fixed-step movement, coyote time, jump buffering, double jump, dash, air dash, grapple, platform carry | Good feel foundation; preserve the locked constants |
| Combat | Three-hit chain, charged attack, perfect dodge, parry, counter, elemental weaknesses, spell and weapon variants | Strong feature set; needs stronger encounter grammar and balance |
| Enemy design | Patrol, notice, windup, attack, recovery, ranged spacing, affixes, summons, elite combo | Good readable state model; needs authored compositions and more distinct counterplay |
| Rewards | Coins, gems, stars, weapons, upgrade ranks, trials, checkpoints | Present but not yet a compelling buildcraft economy |
| Presentation | Realm lighting, fog, post FX, VFX, ambience, music tracks, impact feedback | Good polish direction; needs consistent art hierarchy and profiling |
| Reliability | Quality checks, source guards, fixed physics safeguards, runtime validation | Unusually strong baseline; extend it to balance, progression, and device QA |

## Critical constraints to preserve

The following systems are already guarded by repository instructions and should be treated as contracts during every improvement pass:

1. **Aster animation baking.** Root translation flattening and the prefab baseline-Y compensation must remain intact. Regenerate generated animation assets through the prescribed Unity editor menu after source animation changes.
2. **Locomotion physics.** Keep movement in `FixedUpdate`, use one `CharacterController.Move` per step, preserve the 60 Hz fixed timestep, and do not reintroduce the old speed, root-motion, or double-jump regressions.
3. **Touch controls.** `TouchRouter.ActionRect` remains the single source of truth for both hit testing and drawn action buttons. Every new action must fit the current six-button mobile cluster or replace an action deliberately through a usability test.
4. **Combat feedback.** Keep telegraphs, damage numbers, hit sparks, enemy auras, hit-stop, parry, perfect dodge, counter state, and enemy collision bodies unless a replacement is proven better.
5. **Asset budgets.** Keep weapon files within the existing size guard and profile Android memory, thermal behavior, draw calls, and frame time on physical devices.
6. **Pause and save behavior.** All timers, audio, trials, moving platforms, and combat warnings must pause consistently. Save migrations must remain additive and backward-compatible.

## Target product definition

The recommended target is a **premium-feeling, replayable 3D action-adventure for Android and Windows**, with a focused campaign and meaningful optional mastery rather than an endless content treadmill.

The target experience should have these properties:

- A new player understands movement, defense, powers, and rewards within the first chapter without reading a manual.
- Every realm introduces a mechanical identity, not only a palette and background.
- Every enemy has a recognizable threat, a readable tell, and at least one reliable counter.
- Every weapon changes decisions rather than only changing damage numbers.
- Every chapter contains a safe route, a risk route, a secret or mastery route, and a memorable set-piece.
- Bosses test the lessons of their realm and change the player's decision priorities between phases.
- Currency has clear sinks, predictable earn rates, and no dominant upgrade path.
- The game can be completed with skill, while mastery systems reward replay without requiring grinding.
- The campaign remains stable at a locked target frame rate on the lowest supported Android tier.

# Roadmap

## Phase 0 — Establish a production baseline

**Goal:** Make future changes safe and measurable before adding more content.

Create a small, versioned game-balance layer instead of leaving tuning values scattered across runtime scripts. Move player, enemy, weapon, power, reward, and chapter parameters into serializable definitions or JSON assets. Runtime code should consume these definitions without changing the current behavior during the migration.

Add a debug-only balance overlay that shows chapter ID, encounter ID, enemy state, damage source, current elemental affinity, active modifiers, currency earned, and frame time. Add deterministic seeds for level generation and enemy affixes so a reported combat or economy issue can be reproduced exactly.

Extend the validation suite with the following checks:

- All chapters load with a valid spawn, checkpoint, route, gate, and completion condition.
- All weapons and enemy kinds have valid names, models, damage ranges, animation fallbacks, and elemental metadata.
- Every upgrade track has a defined cost curve and maximum rank.
- No reward can be granted twice from a single event, including trial completion, enemy death, boss death, crate destruction, and chapter completion.
- Save migration works from every existing schema version.
- A full simulated campaign cannot create negative currencies, overflow upgrade ranks, or unlock chapters out of order.

**Exit gate:** The current runtime suite, source guards, and the new data validation suite pass. No gameplay tuning changes are included in this phase.

## Phase 1 — Rebuild the player loop and onboarding

**Goal:** Give the first thirty minutes a clear curriculum and a satisfying rhythm.

Author a chapter-by-chapter curriculum. The first four chapters should teach one movement or combat idea at a time. Chapters five through ten should combine ideas. Chapters eleven through fifteen should test mastery with more demanding mixtures and fewer recovery opportunities.

Recommended curriculum:

| Chapters | Teaching objective | Required content |
|---|---|---|
| 1–2 | Move, jump, attack, collect, checkpoint | Safe route, one enemy family, one optional elevated pickup route |
| 3–4 | Dash, perfect dodge, parry, boss recovery | First real encounter mix and the first guardian exam |
| 5–7 | Elemental weaknesses and weapon identity | Realm hazards, ranged pressure, weapon comparison, second guardian |
| 8–10 | Grapple, moving platforms, trials, enemy affixes | Vertical route choices and third guardian |
| 11–12 | Spell timing, summons, elite enemies | Multi-threat arenas with deliberate counterplay |
| 13–15 | Build mastery and final realm identity | High-risk optional routes, final guardian chain, ending payoff |

Replace generic start-of-level instructions with short contextual prompts. A prompt should appear only when the player first reaches the relevant situation, should demonstrate the action visually, and should disappear after the player succeeds. Add a practice arena in the Sanctuary where the player can test movement, each weapon style, each power, parry timing, and spell charging without losing resources.

**Exit gate:** A first-time player can reach the first guardian without external instructions. Internal playtests can explain what they learned, what rewards they earned, and why the next chapter is unlocked.

## Phase 2 — Make combat a system of decisions

**Goal:** Replace feature accumulation with readable counterplay.

Create a shared combat vocabulary. Every attack should belong to one of a small set of threat families: line, cone, circle, projectile, pursuit, grab, area denial, or summon. Each threat should have a telegraph, an active window, a recovery window, and a counter that is valid for at least two player approaches.

Standardize attack data around:

- Telegraph duration.
- Active duration.
- Recovery duration.
- Damage and stagger value.
- Range and tracking strength.
- Which response is rewarded: jump, dash, parry, spacing, elemental weakness, or target priority.
- Whether the attack can be interrupted.
- Which player states it can punish.

Give each enemy family a stronger battlefield role:

| Role | Example behavior | Player decision |
|---|---|---|
| Duelist | Pressures close range with a punishable string | Parry, perfect dodge, or maintain spacing |
| Controller | Creates a hazard or narrow safe lane | Reposition before committing to damage |
| Artillery | Fires readable projectiles and retreats | Close distance safely or use cover/line of sight |
| Support | Buffs or summons another threat | Prioritize target or exploit a brief cast window |
| Bulwark | Blocks or reduces frontal damage | Flank, use matching element, or bait a heavy attack |
| Ambusher | Appears from a hidden or vertical route | Observe environment and move proactively |

Introduce enemy groups through authored encounter compositions rather than relying primarily on kind selection from route index arithmetic. A chapter should have a deliberate sequence such as **introduction → pressure → rest → combination → mastery test**. Store encounter compositions as data so designers can tune them without editing `RealmWorld.Build` logic.

Improve hit reactions with a small, consistent priority model: light hit, heavy hit, elemental reaction, guard break, stun, launch, and death. Prevent hit-stop and camera shake from stacking into visual noise by using capped budgets per event window.

**Exit gate:** Each enemy family has a documented counter. Every guardian phase teaches or tests a counter already introduced in its realm. A combat test player can identify why damage was taken after watching a recording.

## Phase 3 — Give weapons and characters identity

**Goal:** Make the arsenal and cast memorable instead of mostly statistical.

Refactor weapons into a small number of highly differentiated families. Forty-one definitions are useful only if the player can perceive meaningful differences. Recommended families are:

- **Blade:** balanced reach, reliable combo, moderate stagger.
- **Axe or hammer:** slow, high stagger, guard breaking, weak recovery.
- **Spear or staff:** long reach, line control, lower close-range burst.
- **Chakram or returning weapon:** ranged spacing, path planning, lower single-hit damage.
- **Unarmed or knuckle weapon:** fast strings, counter bonuses, short reach.
- **Dagger:** mobility, back-hit bonus, low stagger.

Each weapon should have one signature rule, one visible VFX tell, one audio identity, and one reason to use it in a particular encounter. Avoid making every weapon a better version of the previous weapon. Use sidegrades with a clear tradeoff.

Give Aster a small character arc expressed through gameplay. The Sanctuary should contain named allies or realm spirits who explain why each power and weapon exists. Each guardian victory should change one Sanctuary element, unlock one short conversation, and reveal one piece of the Heart of Realms story. Keep dialogue skippable and concise; the objective is context, not a visual novel.

Add three build archetypes without creating an unmanageable class system:

- **Vanguard:** health, stagger resistance, close-range commitment.
- **Wayfarer:** movement, dash economy, grapple, traversal rewards.
- **Channeler:** elemental weakness, spell economy, reaction damage.

The player may equip a limited number of passive relics. Relics should modify behavior or create a situational advantage rather than simply adding more percentage damage.

**Exit gate:** In a blind test, players can describe the difference between at least five weapons and choose different weapons for different enemy situations. At least three viable builds can clear the same chapter with different strengths and weaknesses.

## Phase 4 — Replace the flat economy with a controlled progression model

**Goal:** Make rewards exciting, understandable, and resistant to runaway inflation.

Separate currencies by purpose:

| Currency | Earned from | Spent on | Rule |
|---|---|---|---|
| Gold | Enemies, crates, chapter performance, trials | Weapon tuning, consumables, Sanctuary services | Frequent, low-friction currency |
| Gems | Optional routes, secrets, guardian milestones | Permanent unlocks, relic slots, cosmetics | Scarce, exploration-focused currency |
| Aether shards | Perfect-defense play, mastery objectives, guardian challenges | Power and spell specialization | Skill-focused currency |
| Realm sigils | Guardian victory and chapter mastery | Realm-specific unlocks and story rewards | Milestone currency, not farmable |

The current upgrade tracks should be regrouped into a smaller set of player-facing categories:

1. **Vitality:** maximum health and recovery quality.
2. **Momentum:** dash, jump, grapple, and movement economy.
3. **Arsenal:** weapon handling and stagger, not universal damage inflation.
4. **Attunement:** elemental and spell interaction.
5. **Fortune:** reward quality and secret discovery, capped carefully.

Use a visible cost curve and show the next two ranks before purchase. Do not let universal weapon damage, maximum health, and energy all scale at the same rate. A good early curve should provide a meaningful first purchase within one or two chapters, a meaningful decision by chapter five, and a complete core build near the campaign end.

Add reward bands to every chapter:

- **Completion reward:** always granted.
- **Exploration reward:** gems or a secret for optional route completion.
- **Mastery reward:** no-damage, time, trial, or guardian challenge.
- **Build reward:** weapon, relic, or upgrade material.
- **Story reward:** Sanctuary or realm progression.

Make chapter rewards previewable from the map. Show exactly what has been collected and what remains. Prevent repeated farming from becoming the optimal strategy by reducing repeat payouts while preserving a small daily or per-run bonus only if a future replay mode requires it.

**Exit gate:** A ten-run economy simulation shows that progression remains positive but not fully unlocked too early. A first-time player sees at least one meaningful purchase after the first chapter and still has decisions at the final chapter.

## Phase 5 — Author richer chapters and replay content

**Goal:** Turn generated route scaffolding into authored adventure spaces.

Retain `RealmWorld` as the runtime builder, but move chapter design into data-driven chapter assets. Each chapter definition should contain:

- Route nodes and alternate route nodes.
- Encounter IDs and spawn rules.
- Rest beats and checkpoint placement.
- Hazard sets and realm modifiers.
- Secret cache rules.
- Trial location and objective.
- Weapon or relic reward.
- Set-piece trigger.
- Guardian preparation and arena rules.

Each non-boss chapter should contain at least one memorable set-piece. Examples include a collapsing bridge, a storm crossing, a moving caravan, a vertical grapple ascent, a shrine defense, a chase sequence, or a short escape after a guardian event. Keep set-pieces deterministic enough for testing and short enough for mobile sessions.

Add a post-campaign mastery layer instead of immediately adding dozens of chapters:

- Chapter challenge cards with three rotating modifiers.
- Time trial routes with fixed enemy seeds.
- No-hit guardian rematches.
- Shrine trials with escalating tiers.
- Weapon-specific mastery objectives.
- Realm completion boards stored locally first, with online features deferred.

**Exit gate:** Every chapter has a distinct visual, mechanical, and encounter identity. A reviewer can identify the realm and the chapter from gameplay footage without reading the HUD.

## Phase 6 — Physics, camera, and mobile feel pass

**Goal:** Make interaction remain reliable under real frame-rate and touch conditions.

Keep the locked hero physics contract. Improve around it rather than replacing it:

- Add a small configurable landing forgiveness window without changing the fixed-step movement model.
- Add explicit ground material response for stone, sand, ice, and moving platforms.
- Give moving platforms a stable rider attachment strategy and test lateral and vertical motion separately.
- Add sweep-based validation for grapple targets and prevent pulls through solid geometry.
- Add a low-cost camera collision solver that preserves boss readability and never clips through terrain.
- Add camera state profiles for traversal, combat, guardian intro, and defeat.
- Cap camera shake, FOV kick, hit-stop, and directional kick independently so a large event remains readable.

For mobile, test with one-handed and two-handed grips. Add adjustable button scale, opacity, and edge padding. Preserve the existing six-button cluster unless a physical-device test proves a replacement is better. Add a touch calibration screen with a short interactive movement and attack check.

**Exit gate:** The game is comfortable at the target screen sizes and touch latency. No critical control requires a precise edge tap. Character movement, platform riding, grapple arrival, and camera collision remain stable under frame-time spikes.

## Phase 7 — Audio, presentation, and accessibility

**Goal:** Make the game communicate state clearly without visual overload.

Use a consistent audio hierarchy. Guardian warnings, perfect defense, guard break, elemental reaction, checkpoint activation, and reward confirmation should always win over low-priority ambience. Keep the existing bounded voice bank and add per-category concurrency rules.

Give each realm a compact musical identity with exploration, combat, and guardian variations. Use transitions based on gameplay state rather than repeatedly calling track selection every frame. Preserve music settings and pause behavior.

Improve presentation in the following order:

1. Silhouette and threat readability.
2. Lighting hierarchy around the player and active objective.
3. Reward readability and pickup confirmation.
4. Impact timing and camera response.
5. Atmosphere, post FX, and decorative particles.

Add accessibility options that are realistic for the current scope:

- High-contrast telegraphs.
- Reduced camera shake.
- Reduced flash intensity.
- Larger HUD and touch controls.
- Hold or toggle options for charged actions where safe.
- Separate music and effects volume sliders.
- Color-independent elemental symbols in addition to color.

**Exit gate:** A player can identify danger, reward, checkpoint, and elemental weakness without relying on color alone. Reduced-effects mode removes discomfort without removing gameplay information.

## Phase 8 — Production hardening and release readiness

**Goal:** Prove the game on devices and protect it against future regressions.

Create a supported-device matrix with at least one low-end Android device, one mid-range Android device, one high-end Android device, and the target Windows configuration. Record frame time, memory, loading time, thermal behavior, audio behavior, input latency, and battery impact for a thirty-minute session.

Add automated build checks for:

- Missing or duplicated resources.
- Invalid animation clips and avatar assignments.
- Oversized textures and meshes.
- Shader stripping failures.
- Duplicate audio listeners and cameras.
- Null references during boot, level load, checkpoint respawn, pause, resume, and app focus changes.
- Save schema upgrade and corrupted-save recovery.
- Full campaign progression from a clean save.

Run structured human QA passes for:

- New player onboarding.
- Every chapter on normal progression.
- Every guardian phase and failure state.
- Every weapon family.
- Every elemental reaction.
- Every trial kind.
- Touch controls in landscape orientation.
- Resume after interruption and low-memory backgrounding.
- Audio mute, post FX, camera shake, and accessibility options.

**Release gate:** A release candidate has no known progression blockers, no high-severity combat readability issues, no reproducible save corruption, and documented physical-device results. Editor validation is not presented as proof of Android release quality.

# Recommended implementation order for the next development cycle

The next implementation cycle should be deliberately narrow:

1. Build the data-driven chapter and encounter schema without changing the current visuals.
2. Author chapters 1–4 as the tutorial and first guardian curriculum.
3. Add the Sanctuary practice arena and weapon comparison screen.
4. Convert the first five enemy families to explicit counter documents and test cases.
5. Rebalance the five player-facing upgrade categories and produce an economy simulation.
6. Add one signature set-piece to each realm.
7. Playtest on physical Android devices before expanding the roster or adding more VFX.
8. Only then author the remaining chapters and mastery layer.

This order reduces risk because it validates the game loop before multiplying content. It also protects the existing physics and animation work from being buried under additional feature code.

# Definition of professional quality for this project

The game should be considered ready for a serious release candidate only when the following statements are true:

- **Gameplay:** Players make meaningful movement, defense, weapon, and elemental decisions rather than repeating one dominant attack.
- **Content:** Every chapter has a distinct purpose, encounter rhythm, optional reward path, and memorable event.
- **Physics:** Locomotion, platform riding, grapple travel, collisions, and camera behavior remain reliable across supported frame rates.
- **Characters:** Aster, allies, enemies, and guardians communicate identity through mechanics, animation, audio, and short contextual story beats.
- **Economy:** Rewards are predictable, valuable, and balanced. There is no mandatory grind and no dominant universal upgrade path.
- **Progression:** The player always knows what was earned, what it unlocks, and what the next meaningful goal is.
- **Presentation:** Threats and rewards remain readable under the strongest visual effects and on small screens.
- **Quality assurance:** Automated tests, deterministic reproductions, physical-device profiling, and human playthroughs agree about the game's behavior.

## Repository review references

The assessment is based on the active Unity edition and the repository's current implementation notes. The Git configuration contains an `Antigravity2` remote-tracking branch definition, but the GitHub connector was disabled in this session, so this review uses the mounted local repository rather than claiming a fresh remote checkout.

[1]: ./unity-3d/README.md "Legends of the Lost Realms Unity 3D overview"
[2]: ./unity-3d/QUALITY_UPGRADE_PLAN.md "Current quality upgrade plan and validation status"
[3]: ./unity-3d/Assets/Scripts/RealmGame.cs "Main game coordinator and progression state"
[4]: ./unity-3d/Assets/Scripts/Hero.cs "Hero movement, physics, defense, and combat"
[5]: ./unity-3d/Assets/Scripts/Enemy.cs "Enemy state machine, attacks, affixes, and boss behavior"
[6]: ./unity-3d/Assets/Scripts/RealmWorld.cs "Realm route construction and encounter scaffolding"
[7]: ./unity-3d/Assets/Scripts/RealmTrials.cs "Optional trial objectives and rewards"
[8]: ./unity-3d/Assets/Scripts/QualityUpgradeChecks.cs "Automated quality and regression checks"
[9]: ./AGENTS.md "Repository guardrails and locked systems"

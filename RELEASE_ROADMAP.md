# Legends of the Lost Realms — Pro-Level Release Roadmap

## Current state (honest assessment)

**What's already strong**
- Unity 3D edition: 15 chapters, 4 realms, 4 guardians, 41 weapons
- Quality Upgrade v1 done: 3,267 runtime assertions + 72 source checks passed (Sept 2026)
- Data-driven level JSONs, automated QA suite, Android + Windows builds
- Visual Rebirth assets, touch controls, save progression, trials
- Locked physics contract, animation baking safeguards, additive save migrations

**What's still missing for release**
The existing `PROFESSIONAL_GAME_UPGRADE_PLAN.md` covers Phases 0–8 in detail. This roadmap translates that into **prioritized action**, not a second plan.

---

## Priority gaps (work on these first)

1. **Onboarding clarity** — A new player must understand movement, defense, powers, and rewards within chapter 1–2 without reading a manual.
2. **Combat readability** — Every enemy needs a documented threat family, telegraph, recovery window, and at least one reliable counter.
3. **Economy balance** — Four currencies with clear sinks, predictable earn rates, no dominant upgrade path.
4. **Physical device QA** — Editor validation is not Android release quality. Need frame time, memory, thermal, touch, and audio on real devices.
5. **Chapter identity** — Every realm and chapter needs a distinct mechanical identity, not only palette and background.
6. **Reward integrity** — No duplicate grants, previewable rewards, visible cost curves on upgrades.
7. **Accessibility** — High-contrast telegraphs, reduced shake/flash, larger HUD, color-independent elemental symbols.
8. **Set pieces** — One memorable deterministic encounter per realm (collapse, chase, shrine defense, grapple ascent, etc.).
9. **Audio hierarchy** — Guardian warnings and perfect-defense always win; concurrency rules; transitions based on gameplay state.
10. **Mastery layer** — Challenge cards, time trials, no-hit rematches, weapon mastery objectives.

---

## Recommended roadmap (6 sprints)

### Sprint 1 — Onboarding + combat vocabulary (weeks 1–2)
- Contextual prompts that appear only on first encounter, demonstrate visually, disappear on success.
- Sanctuary practice arena: test movement, weapons, powers, parry timing, spell charging without risk.
- Document threat families (line, cone, circle, projectile, pursuit, grab, area denial, summon) for all enemy archetypes.
- Give each enemy a readable telegraph, active window, recovery window, and counter.
- Exit gate: first-time player reaches first guardian without external instructions; combat test player can explain why damage was taken after watching a recording.

### Sprint 2 — Economy + weapon identity (weeks 3–4)
- Separate currencies: gold (frequent), gems (exploration), aether shards (skill), realm sigils (milestone).
- Visible upgrade cost curve; show next two ranks before purchase.
- Regroup 41 weapons into 6 families with signature rules, VFX tells, audio identities.
- Sidegrades with tradeoffs, not stat upgrades.
- Exit gate: economy simulation shows positive but not fully unlocked progression; blind test player can describe 5+ weapon differences.

### Sprint 3 — Chapter identity + set pieces (weeks 5–6)
- Data-driven chapter assets: routes, encounters, rest beats, hazards, secrets, trials, set-piece triggers.
- One signature set-piece per realm: collapsing bridge, storm crossing, moving caravan, vertical grapple, shrine defense, chase, escape.
- Realm gameplay contracts: Dunes = sand platforms + wind corridors + heat; Frozen = ice traction + opposing wind + icicles.
- Exit gate: reviewer identifies realm and chapter from gameplay footage without HUD.

### Sprint 4 — Physics/camera/mobile feel (weeks 7–8)
- Landing forgiveness, ground material response, stable platform rider, grapple sweep validation.
- Camera collision solver + state profiles (traversal, combat, guardian intro, defeat).
- One-handed + two-handed touch tests; adjustable button scale/opacity/padding; touch calibration screen.
- Exit gate: comfortable on target screen sizes; no critical precise edge tap; stable under frame-time spikes.

### Sprint 5 — Audio + accessibility (weeks 9–10)
- Audio hierarchy: guardian warnings, perfect defense, guard break, elemental reaction, checkpoint, reward always win.
- Realm musical identity with exploration/combat/guardian variations; state-based transitions.
- Accessibility: high-contrast telegraphs, reduced shake/flash, larger HUD/touch, hold-or-toggle charged actions, color-independent elemental symbols.
- Exit gate: player identifies danger/reward/checkpoint/element without color alone; reduced-effects mode removes discomfort without losing info.

### Sprint 6 — Device QA + release candidate (weeks 11–12)
- Supported-device matrix: low-end Android, mid-range Android, high-end Android, Windows.
- Profile frame time, memory, loading, thermal, audio, input latency, battery (30-min session).
- Automated build checks: missing/duplicate resources, invalid clips, oversized assets, shader stripping, duplicate listeners, null refs on boot/load/checkpoint/pause/resume/focus, save migration + corrupted-save recovery, full campaign from clean save.
- Structured human QA: onboarding, every chapter, every guardian phase, every weapon family, every elemental reaction, every trial, touch landscape, resume after interruption, mute/options.
- Exit gate: no progression blockers, no high-severity readability issues, no reproducible save corruption, documented physical-device results.

---

## Do this week (immediate actions)

1. Pick your **single highest-priority gap** from the list above.
2. Open `PROFESSIONAL_GAME_UPGRADE_PLAN.md` and tell me which phase you want to start with.
3. Run `tools/test_pure_java_controllers.sh` and `tools/run_phase6_checks.sh` to confirm the current test baseline still passes.
4. Build the Windows executable and test the first 3 chapters yourself as a new player — note where you felt confused or overpowered.
5. Take 5 screenshots of moments where you thought "this needs clarity" — bring them to our next session.

---

## What to avoid (scope traps)

- Don't add more weapons before the 41 feel distinct.
- Don't add more chapters before the existing 15 each have identity and set pieces.
- Don't add online features before local save and progression are rock solid.
- Don't add more VFX before threat readability is proven on a low-end Android screen.
- Don't treat editor validation as Android release proof.
- Don't rework physics — the locked contract exists for a reason; improve around it.

---

## Release readiness gates (final checklist)

- [ ] Gameplay: meaningful movement/defense/weapon/elemental decisions, no single dominant attack.
- [ ] Content: every chapter has purpose, rhythm, optional path, memorable event.
- [ ] Physics: locomotion, platforms, grapple, collisions, camera stable across frame rates.
- [ ] Characters: Aster, allies, enemies, guardians communicate identity through mechanics + audio + story.
- [ ] Economy: predictable rewards, no mandatory grind, no dominant upgrade path.
- [ ] Progression: player always knows what was earned, what it unlocks, next goal.
- [ ] Presentation: threats/rewards readable under strongest FX and on small screens.
- [ ] QA: automated tests + deterministic reproductions + physical-device profiling + human playthroughs agree.

---

_This roadmap complements the existing `PROFESSIONAL_GAME_UPGRADE_PLAN.md` and `unity-3d/QUALITY_UPGRADE_PLAN.md`. Those documents remain the source of truth for implementation details._

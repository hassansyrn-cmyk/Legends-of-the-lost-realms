# Production Rebuild

## Visual direction

The new visual language is hand-painted stylized fantasy with crisp silhouettes,
restrained detail at gameplay scale, and world-specific accent colors. Characters
use modular cutout rigs rather than large pose sheets: head, torso, limbs, weapon,
and optional secondary-motion pieces are transformed around authored joints.

Every character must remain readable at 96–220 virtual pixels, face its target,
anticipate attacks visibly, recover visibly, and keep weapon/hazard silhouettes
separate from the body. Runtime images require genuine alpha and may not contain
checkerboards, guide rails, baked backgrounds, or external glow.

## Animation standard

- Locomotion is driven by velocity, grounded state, and stride phase.
- Arms, legs, head, torso, weapons, capes, and accessories animate independently.
- Attacks have anticipation, active, follow-through, and recovery poses.
- Hurt reactions preserve collision state and never hide an enemy telegraph.
- Bosses blend locomotion with attack poses instead of swapping full-body frames.
- Secondary motion is procedural and capped to avoid detached joints.

## AI standard

Enemies use perception, intent, commitment, recovery, and repositioning states.
Boss decisions consider distance, elevation, approach velocity, dodging, recent
attacks, and phase. New encounters should combine complementary roles rather
than only increasing damage or health.

## Delivery phases

1. Production safety: fixed-step simulation, debug-only tools, lifecycle cleanup,
   complete CI verification, and release artifact hygiene.
2. Aster: integrate the modular hero rig, authored joints, locomotion blending,
   attack arcs, landing compression, and cloak secondary motion.
3. Enemies: replace all eight visuals with modular rigs and add coordinated
   awareness, predictive attacks, edge avoidance, and role-based spacing.
4. Bosses: rebuild three guardians with layered rigs, arena-aware navigation,
   phase transitions, interrupts, and deterministic-but-adaptive move selection.
5. Worlds: replace backgrounds, platforms, traps, effects, HUD, and map using
   one palette system and validate readability on small phones and tablets.
6. Content: expand each realm with optional challenge rooms, new traversal
   combinations, secrets, enemy pairings, and post-boss mastery stages.
7. Ship: signed AAB, device performance matrix, accessibility pass, store assets,
   privacy policy, crash reporting decision, and closed playtest balancing.

## Current source asset

`art/production/aster_modular_rig_v1.png` is the first style anchor. Run:

```bash
python3 tools/extract_modular_rig.py \
  art/production/aster_modular_rig_v1.png \
  app/src/main/res/drawable-nodpi/aster_rig
```

The generated board is retained as source art. Extracted runtime parts must be
visually reviewed before renderer integration.

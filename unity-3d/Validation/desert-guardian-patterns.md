# Desert guardian combat and Soulreaper edge — October 2026

Soulreaper retains its lower-shaft grip `(-0.008, -0.20, -0.145)`. The mesh rolls 180 degrees around the shaft so the cutting edge leads the swing without reversing which end contains the blade or changing the handle anchor. Its combo and charged poses were visually inspected.

The chapter 7 lava golem has a dedicated controller, separate from the other realm guardians. It closes toward Aster at 2.85 m/s (3.4 in phase 3), with an explicit chase interval between ranged patterns. Terrain clearance uses supported footprint samples for its large capsule; real props/buildings still block it, and its controller retains gravity and edge support checks. Charges choose and telegraph a supported lane, lock that direction, and cap each fixed movement step before contacting Aster so the golem cannot climb the hero capsule.

Five patterns are available from phase 1:

1. Overhead slam followed by a separately warned opposite-fist strike.
2. Broad torso-driven sweeping blow.
3. Stomp with a delayed outer ring of lava bursts.
4. Locked-target magma fissures spreading to both sides (three bursts, five in later phases).
5. Telegraphing brace followed by a physical charging rush.

Phase transitions have a dedicated roar. All warnings lock their aim before damage; delayed bursts pause with gameplay. Existing guardian trap immunity, phase escalation and heralds are preserved.

`GuardianPoseSetup.Bake` creates humanoid muscle curves on a constant standing base for ten golem states: two fist attacks, slam, sweep, stomp, eruption, roar, charge brace, and the legacy cast/victory aliases. Upper-body poses, clenched fists and a single raised stomp leg distinguish the actions. Root curves stay constant and the opposite foot remains planted. No Aster animation or baker is written.

Complete focused validation passed **153 assertions**, including ten full grounded pose cycles, supplied enemy rigs/textures and animations, hand placement, distinct Epic charged effects, real arena approach paths, all five patterns, a charge travelling over 3 metres, no climbing Aster, and no runtime errors. Log: `Temp/opencode/guardian-patterns-complete.log` (`NEW_EPIC_TESTS_PASSED 153`). The live controller also passed its isolated 22-assertion run. All six repository verification scripts passed.

Gameplay screenshots are in `Validation/NewEpic/Guardian-gameplay-*.png`; isolated full-cycle poses are `DesertGuardian-cycle-*.png`.

Final regression across all 15 chapters passed **2233 assertions**, including guardian phase behavior, physics, trap immunity and no runtime exceptions or errors. Log: `Temp/opencode/guardian-patterns-full.log` (`REALM_RUNTIME_TESTS_PASSED 2233`).

The preceding controller, grip and pose generator are saved in `Temp/opencode/guardian-patterns-backup`. The complete pre-integration rollback checkpoint remains `Temp/opencode/new-epic-20261008-backup`.

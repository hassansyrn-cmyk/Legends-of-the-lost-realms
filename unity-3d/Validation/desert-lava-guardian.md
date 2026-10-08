# Desert guardian and temporary Epic weapon tests

The Burning Dunes guardian in chapter 7 now uses the supplied textured, rigged lava golem. Its model and collision capsule are 4.5 metres tall. The original Sunscar model now appears as the 2.1-metre Sunscar Brute (enemy kind 25) on the sixth route island in chapters 5–7. Existing guardian progression remains kind 9, preserving boss records and gate logic; the Bestiary has 26 entries.

The guardian uses humanoid melee and slam animations, a casting gesture for eruptions, a phase-transition gesture, hit stagger, and accelerated running for its charge. Only new StoneBrute clips were generated; Aster's animation assets and baker are unchanged.

- Lava eruption: a locked ground warning lasts 1.25 seconds before a burst at Aster's marked position. From phase 2 it adds two flanking bursts. Cooldown is 8/7/6 seconds in phases 1/2/3.
- Ground slam and melee combos retain the established golem fight controller and recovery windows.
- Phase-3 charge adds a 0.75-second warning, then charges in the locked direction. It retains the existing arena and deck-edge checks.
- Guardian immunity to traps, death handling, summons, pause, gravity and collision remain active.

Temporary playtest pickups are on the **second route island**: Duskblade in **chapter 1**, Soulreaper in **chapter 2**. The ordinary chapter weapon drops and Epic chest rewards remain available. Remove the marked `stage<=2 && i==1` placement in RealmWorld after the user's weapon approval; do not remove the catalog or chest entries.

Focused validation: **101 assertions passed**, including actual mesh deformation in all four special poses, guardian model/capsule size, regular Sunscar placement, both temporary pickups, an executed eruption, trap immunity and Bestiary migration. Boss pose renders are in `Validation/NewEpic/DesertGuardian-*.png`.

The immediately preceding integration is backed up in `Temp/opencode/desert-guardian-backup`. The complete original-file rollback remains in `Temp/opencode/new-epic-20261008-backup`; its hash-checked restore script must be run without `-Apply` first.

Full gameplay regression: **2,233 assertions passed**, with zero runtime errors. All 15 chapters and 26 enemy families, existing guardian phases, moving decks, falls, pause and trap immunity passed. Runtime and Editor compilation and all six repository verification scripts passed.

# Chapter turret placement fix — 2026-09-28

The imported Stone, Lion and Dragon models were present and spawned normally, but ChapterLayout.GroundTraps deleted them because their authored flank positions failed the visible-deck mounting test. T-key spawns bypass this chapter cleanup.

ChapterLayout.PlaceTurretsOnDeck now checks turrets after terrain setup and before scenery scatter. Unsupported turrets search nearby half-metre positions for the same level-surface test used by final cleanup. Candidates preserve the centre lane and other trap reservations; a moved turret updates its reservation. Unsupported placements still go through the existing final rejection check.

Validation:
- Before: ChapterTurretProbe built all 15 chapters; zero surviving turrets in every chapter, 15 failed early-turret checks.
- After: all 15 chapters retain exactly one turret on route island 1 (the first island after spawn); all surviving turrets have imported models and pass mounting support. FAILURES=0 in chapter-turrets.txt.
- QUALITY_SOURCE_GUARDS_PASSED 72.
- Unity compiled the changed runtime scripts and ran the chapter probe. This is a headless placement test, not a rendered visual or combat playtest.
- The old compile_all.ps1 helper fails with duplicate Unity assembly references (CS0433); Unity's own compiler is the authoritative check used here.

First-route-island variants retain the existing mapping: chapters 1–4 Stone, 5–7 Lion, 8–10 Dragon, 11–15 Lion on odd chapters / Dragon on even chapters. No standalone player or Android build was produced; restart Play mode to use the updated source.

Reproduce in an isolated Unity editor process: -batchmode -nographics -executeMethod LostRealms.ChapterTurretProbe.Run (the probe exits with success/failure itself).

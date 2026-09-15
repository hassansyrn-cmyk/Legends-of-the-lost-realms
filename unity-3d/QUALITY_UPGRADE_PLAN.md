# Quality upgrade — first playable milestone

The aim is a cohesive, readable fantasy action game. This milestone improves the
existing fifteen-chapter campaign without replacing validated character assets.
“Professional quality” is a production target, not a claim that one pass proves
release readiness.

## Build now

1. **Art direction:** balance warm key lighting with cool ambient fill, ease
   Emberfall's red cast, lift crushed shadows, and replace sharp bloom taps with
   a small separable blur. Preserve realm identities and mobile cost limits.
2. **Content and gameplay:** optional shrine trials across exploration chapters:
   defeat enemies, recover three echoes, or land elemental weakness hits. Show
   progress, explicit rewards, and a single completion payout per chapter run.
3. **Combat clarity:** shared mesh ground warnings with a visible countdown;
   cancel warnings and queued follow-ups on stun. Preserve existing damage,
   defense windows, enemy families, and attack animation routes.
4. **Audio:** cached clips, two-source music crossfades, restrained pitch/volume
   variation, capped concurrent voices, and priority for important feedback.
5. **Physics and VFX:** carry platforms inside the hero's single fixed-step move;
   stabilize pushable props, stop duplicate crate rewards, preserve independent
   particle axes when repairing curve modes, and clean up temporary resources.
6. **UX:** trial panel, completion feedback, corrected four-realm copy and jump
   instructions. Preserve the six-button touch cluster and shared hit rectangles.

## Validation gates

- Compile runtime and editor code in the installed Unity version.
- Run the existing play-mode suite plus new behavioral regression checks.
- Render all four realms with the actual graphics backend; check shader errors.
- Verify locked locomotion constants, root flattening, weapon budgets and touch
  rectangles. Do not regenerate Aster clips for this pass.
- Record exact results and distinguish editor evidence from physical-device QA.

## Completed validation — 15 September 2026

All six implementation areas above are in place. Visual inspection also exposed
the Warden's missing humanoid avatar; its importer now creates a valid avatar,
and a behavioral check confirms its attack moves the arm. Aster's generated
clips and baker were not changed.

- Runtime suite: **3,267 assertions passed**, with no runtime errors.
- Source guard suite: **72 checks passed**, including locked movement and assets.
- Runtime compilation and four-realm D3D11 rendering passed.
- Latest captures: `Validation/QualityUpgrade-20260915-112214/`.
- Android packaging status is recorded in `Validation/quality-build.txt`.

These results establish editor behavior, not physical Android performance or a
complete human campaign playthrough.

## Following milestones

After playtesting this build: author more distinct encounter layouts and enemy
attack tells, replace remaining placeholder enemy art with verified animated
assets, refine onboarding and difficulty, then profile Android frame time,
memory, thermal behavior, touch comfort and audio on real devices. A release
candidate needs full campaign playthroughs, save-upgrade checks and device QA.

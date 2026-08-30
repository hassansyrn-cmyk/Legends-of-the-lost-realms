# Legends of the Lost Realms

Landscape Android 2D fantasy action-platformer with ten levels across three realms, local progression, touch combat, elemental powers, and multi-phase guardian encounters.

## Run & Operate

- `./tools/run_phase6_checks.sh` — run level, save, asset, unit, lint, and debug APK checks
- `gradle :app:testDebugUnitTest` — run JVM unit tests when Android SDK 35 is configured
- `gradle :app:assembleDebug` — build the debug APK when Android SDK 35 is configured
- `pnpm --filter @workspace/api-server run dev` — run the API server (port 5000)
- `pnpm run typecheck` — full typecheck across all packages

## Stack

- Android Canvas game written in Java 17
- Android Gradle Plugin 8.7.3, compile/target SDK 35, minimum SDK 24
- JSON-authored level content and SharedPreferences save data
- Python/Pillow asset and level verification tools
- pnpm workspaces, Node.js 24, TypeScript 5.9
- Build: esbuild (CJS bundle)

## Where things live

- Android app: `app/`
- Game code: `app/src/main/java/com/manus/lostrealms/`
- Level definitions: `app/src/main/assets/levels/`
- Packaged art: `app/src/main/res/drawable-nodpi/`
- Source/reference art: `art/`
- Verification and asset tools: `tools/`

## Architecture decisions

- Preserve the native Android Canvas engine and improve it incrementally rather than replacing it.
- Keep level content data-driven and preserve all established save keys.
- Move deterministic rules into pure Java controllers so behavior can be tested without Android.
- Keep transient scene geometry, rendering, and audio coordination in the active game scene until each subsystem has a tested extraction path.

## Product

- Ten-level action-platforming campaign spanning Verdant Kingdom, Burning Dunes, and Frozen Peaks.
- Responsive movement, four-hit combo, charged and aerial attacks, perfect dodge, and Ember/Frost/Gale powers.
- Tactical enemy archetypes, checkpoints, secrets, upgrades, realm stars, and three multi-phase bosses.

## User preferences

- Target premium mobile indie quality; weak art and systems may be rebuilt rather than preserved.
- Preserve the ten-level campaign, three realms, local progression, existing movement/combat foundation, and save compatibility.

## Gotchas

- Android builds require SDK Platform 35 and Build Tools 35.0.0; the current Replit runtime provides Java and Gradle but not the Android SDK.
- The Gradle wrapper script is present, but its `gradle/wrapper/gradle-wrapper.jar` was absent from the supplied archive; use installed Gradle or restore the wrapper files.
- Run the Python checks before Android compilation so content and sprite regressions fail quickly.

## Pointers

- See the `pnpm-workspace` skill for workspace structure, TypeScript setup, and package details

# Combat Device Validation

Date: August 30, 2026

## Scope

This validation covers the three guardian encounters and the representative enemy
archetypes used across the ten campaign levels:

- Thornwold, Corrupted Guardian — level 4, world 1
- Akaros, Stone Warden — level 7, world 2
- Vyrn, the Icebound Maw — level 10, world 3
- Enemy archetypes 0 through 7, including ground, flying, shielded, heavy,
  and ranged enemies

## Previously completed checks

The following checks were completed during the earlier Android-enabled
verification run documented for this release:

The local Android SDK was configured with:

- Android SDK Platform 35
- Android SDK Build Tools 35.0.0
- Android Emulator 37.1.11
- Android Platform Tools 37.0.1

The full project verification command passes:

```text
./tools/run_phase6_checks.sh
```

This includes level and asset checks, pure Java combat controller checks,
`:app:testDebugUnitTest`, `:app:assembleDebug`, and `:app:lintDebug`.

The generated debug APK was also inspected with `aapt` and confirms:

- package: `com.manus.lostrealms`
- compile SDK: 35
- target SDK: 35
- minimum SDK: 24
- landscape activity orientation

Lint completes successfully with two non-blocking existing warnings: the
intentional landscape `screenOrientation` API usage and one unused resource.

### Reproducible remote APK build

Because a persistent Android SDK is not part of the current workspace, the same
committed Android source was also built by the repository's Android Debug APK
workflow:

- Workflow run: https://github.com/hassansyrn-cmyk/Legends-of-the-lost-realms/actions/runs/33311517920
- Result: **Success**
- Source commit: `a25575e16cf9187ff95f72aa88f6e7be9e433567`
- The workflow installed Android SDK 35 and Build Tools 35.0.0, then passed
  `gradle :app:assembleDebug`.
- The workflow now derives the APK and artifact names from `app/build.gradle`,
  uploads an `APK_BUILD_METADATA.txt` sidecar containing the package, version
  code/name, full source revision, and workflow run URL, and can publish both
  files as a public GitHub prerelease for direct device download.
- The current checkout's `app/build.gradle`, `GameView.java`,
  `InputHandler.java`, `BossController.java`, and `EnemyController.java`
  match the source used by that successful run.

The successful run's existing artifact remains attached to that workflow run,
but it uses the old hard-coded v4.11.4 label and the connected GitHub API
returned HTTP 403 when this workspace attempted to download it. A manually
dispatched run of the updated workflow is required to publish the corrected
v5.0.0 artifact and its public prerelease download URL. The remote result is
build evidence only and not device gameplay evidence until the APK is installed
on a physical landscape Android device.

### Current checkout debug APK — August 30, 2026

The current checkout was built locally after temporarily provisioning Android
SDK Platform 35 and Build Tools 35.0.0 outside the project directory:

- Source revision: `ecc720ea2cfca8bf54addec33198345e1987ed66`
- Verification: **Success** — `gradle :app:testDebugUnitTest :app:assembleDebug
  :app:lintDebug`
- Downloadable APK: `Legends_of_the_Lost_Realms_v5.0.0_ecc720e-debug.apk`
- Package: `com.manus.lostrealms`
- Version name/code: `5.0.0` / `60`
- Compile/target/minimum SDK: `35` / `35` / `24`
- APK SHA-256:
  `e53a0e67b7a0a64f216459b32f0388d7717716cbd44661e338b1a486c27fc82c`
- Metadata sidecar: `APK_BUILD_METADATA.txt`

The APK manifest was inspected with Build Tools `aapt` and confirms the
`com.manus.lostrealms` package, version `5.0.0`, version code `60`, target SDK
35, and the landscape launch activity. The APK and metadata sidecar are
available as downloadable Replit artifacts for the physical-device tester.

### Public release publication status — August 30, 2026

The verified APK and metadata sidecar are retained together in the workspace
handoff outputs:

- APK: `.agents/outputs/Legends_of_the_Lost_Realms_v5.0.0_ecc720e-debug.apk`
- Metadata: `.agents/outputs/APK_BUILD_METADATA.txt`
- APK SHA-256:
  `e53a0e67b7a0a64f216459b32f0388d7717716cbd44661e338b1a486c27fc82c`

The configured GitHub connection was able to inspect the public repository and
create a prerelease record, but GitHub's separate `uploads.github.com` asset
host rejected both verified-file uploads with:
`The connectors proxy cannot reach https://uploads.github.com`. The
connection's initialized release client produced the same rejection. The
repository's public workflow is also an older artifact-only workflow
(hard-coded v4.11.4 with `contents: read`); attempts to update it through the
repository-content route were blocked by the upstream proxy, and the
Git-data route rejected adding a workflow file with the current connection's
permissions. No public release or direct download URL was retained: the
temporary prerelease and tag were deleted rather than leaving testers a
misleading page. Therefore, **no public release or direct download URL is
available yet**; the two workspace outputs above are the verified handoff files
until a GitHub account or upload path with release-asset access completes the
publication.

### Task 23 publication recheck — August 30, 2026

A guarded retry was made with the active GitHub connection using the clearly
labeled prerelease tag `debug-v5.0.0-ecc720e`. GitHub accepted the prerelease
record, but the initialized release client again failed when it routed the APK
and metadata uploads to the separate `uploads.github.com` host:

> The connectors proxy cannot reach https://uploads.github.com; this endpoint is
> not available through getClient() without credential settings

The cleanup path completed successfully: the temporary release and tag were
deleted, and a fresh public API read confirms that the repository has no
release, tag, or downloadable asset left behind. The remote Android workflow
also cannot be used as a fallback through this connection: its workflow-file
read is rejected with HTTP 403, and the visible successful runs do not expose a
release-publishing path. No final release or asset URL is recorded until a
GitHub connection or workflow-capable path can upload through
`uploads.github.com`. The verified source revision and APK SHA-256 above are
unchanged.

## Fairness audit completed without a device

The portable combat audit found no unfair timing or collision regression:

- Boss attack windups range from 0.42 to 0.76 seconds, with active and recovery
  phases represented separately.
- Every boss windup displays a pulsing arena telegraph and the attack name.
- Boss hazards remain visibly telegraphed before becoming active.
- Enemy windups range from 0.34 to 0.72 seconds and render a distinct warning
  indicator before commitment.
- Heavy enemy contact damage is limited to its committed attack state.
- Player damage applies a 0.90-second invulnerability window.
- Touch coordinates are transformed through the letterboxed virtual 1280x720
  landscape canvas, and movement, jump, power, and attack controls occupy
  separate regions.

No gameplay code change was justified by these portable checks. The validation
script was made executable so the documented `./tools/run_phase6_checks.sh`
command works directly.

## Device execution blocker

### Current workspace recheck — August 30, 2026

The required hardware playthroughs were not performed because this workspace
still has no attached physical Android device:

| Required exercise | Status |
| --- | --- |
| Install and launch the debug APK on a landscape Android device | **Pending tester** — APK is now available as a downloadable artifact; no device is attached here |
| Level 4 through all three Thornwold phases | **Not run** |
| Level 7 through all three Akaros phases | **Not run** |
| Level 10 through all three Vyrn phases | **Not run** |
| Representative enemies from levels 1–3, 5–6, and 8–9 | **Not run** |
| Physical touch comfort, telegraph readability, and frame pacing | **Not run** |

### Latest workspace recheck — August 30, 2026

The portable portion of `./tools/run_phase6_checks.sh` passes, including level,
save, art, layout, sprite-sheet, and pure Java combat checks. The same command
then stops before Android unit tests, packaging, and lint because the temporary
Android SDK used for the APK build is no longer present:

```text
Could not determine the dependencies of task ':app:testDebugUnitTest'.
SDK location not found. Define a valid SDK location with an ANDROID_HOME
environment variable or by setting the sdk.dir path in local.properties.
```

This fresh host recheck does not replace the successful Android verification
recorded above for source revision `ecc720ea2cfca8bf54addec33198345e1987ed66`.
The downloadable APK remains the verified artifact for the required
playthrough, but it has not been installed here.

The release-gate recheck for this task produced the following additional
evidence:

- APK SHA-256 recomputation: **Match** —
  `e53a0e67b7a0a64f216459b32f0388d7717716cbd44661e338b1a486c27fc82c`
- `adb`, `emulator`, `sdkmanager`, and `aapt`: **Not available on PATH**
- `adb devices -l`: **Could not run** (`adb: command not found`)
- `/dev/kvm`: **Absent**
- No physical device or emulator could be named, installed to, launched, or
  observed during this task.

The portable checks and build are not a substitute for the required hardware
observations. No gameplay issue was confirmed and no combat code was changed
as part of this build. Device model, install result, gameplay observations,
and fixes remain pending the physical tester.

The current host evidence is:

- `adb devices -l`: unavailable because `adb` is not installed; no attached
  device is present in the workspace.
- `/dev/kvm`: absent, so an x86_64 Android emulator cannot provide hardware
  accelerated gameplay here.
- `sdkmanager` and `aapt`: provisioned temporarily for the local build and
  manifest inspection; they are not part of the project checkout.
- `./tools/run_phase6_checks.sh`: portable checks pass, but the current host
  recheck stops at Android tasks because the temporary SDK is absent. The APK
  and metadata sidecar were already produced by the successful build recorded
  above.

No physical Android device can be enumerated: `adb` is not installed, so
`adb devices -l` cannot run in this workspace. No physical device model,
install result, touch observations, frame-pacing observations, or gameplay
fixes are available to record yet.
An API 35 x86_64 AVD was created, but the host does not expose `/dev/kvm`; the
Android emulator exits with:

```text
x86_64 emulation currently requires hardware acceleration!
CPU acceleration status: /dev/kvm is not found
```

An API 35 ARM64 AVD was also attempted, but ARM64 images cannot run on this
x86_64 host. Therefore, real-device touch ergonomics, frame pacing, and visual
telegraph readability remain release-blocking checks for a physical landscape
Android device or a host with hardware-accelerated Android emulation.

## Task 20 named-device execution record

The required named-device run could not be started from this workspace. The final
host probe on August 30, 2026 found no Android execution path:

- `adb`, `emulator`, `sdkmanager`, and `aapt` are unavailable on `PATH`.
- `/dev/bus/usb` and `/dev/usb` are absent; no USB device bus is exposed.
- `/dev/kvm` is absent, so a hardware-accelerated x86_64 emulator cannot boot.
- No Android SDK environment variables are configured.

| Check | Recorded result |
| --- | --- |
| Named landscape device | **None available** — no physical device, USB device bus, or emulator was exposed to the workspace |
| APK used | `.agents/outputs/Legends_of_the_Lost_Realms_v5.0.0_ecc720e-debug.apk`; no rebuild was performed for this attempt |
| Install | **Not run** — `adb` is not installed and no device target exists |
| Launch | **Not observed** |
| Thornwold level 4, phases 1–3 | **Not run** |
| Akaros level 7, phases 1–3 | **Not run** |
| Vyrn level 10, phases 1–3 | **Not run** |
| Enemy archetypes 0–7 | **Not run on hardware** |
| Touch comfort | **Not observed** |
| Telegraph readability | **Not observed** |
| Frame pacing | **Not observed** |
| Fixes from device evidence | **None** — no device runtime evidence was available |

The APK handoff file remains integrity-verified against the metadata sidecar
(SHA-256 `e53a0e67b7a0a64f216459b32f0388d7717716cbd44661e338b1a486c27fc82c`).
The public GitHub release asset is still unavailable as documented above; that
publication is a separate follow-up task. This record therefore does not claim
that the APK was installed or that gameplay passed on hardware.

## Final release-gate disposition

The final workspace recheck on August 30, 2026 reproduced the portable results
and recomputed the APK checksum successfully. It did not produce physical-device
evidence: there is still no named Android device, installation result, launch
observation, touch-comfort result, telegraph-readability result, frame-pacing
result, or three-phase boss playthrough to sign off.

**Final gate disposition: FAILED — environmental blocker; no physical-device
execution was possible in this workspace.**

This is not a gameplay failure: no device run occurred, so no hardware result
or gameplay defect is being claimed. The release remains blocked until a tester
installs and exercises the APK on a named landscape Android device.

**Release status: physical Android combat gate remains pending tester execution.**
The APK and metadata above are ready for installation on a named landscape
Android device. No gameplay fix is claimed from this host-only recheck.

## Task 22 execution attempt — August 30, 2026

The assigned final combat sign-off was rechecked from the current workspace after
the task was assigned. No Android execution path was exposed:

- `adb`, `fastboot`, `emulator`, `sdkmanager`, `aapt`, `lsusb`, and
  `usb-devices` are unavailable on `PATH`.
- No candidate Android tools were found under the common SDK locations
  `/opt/android-sdk`, `/root/Android/Sdk`, or `/home/runner/Android/Sdk`.
- `/dev/bus/usb`, `/dev/usb`, and `/dev/kvm` are absent.
- No `adb`, emulator, or QEMU process is running, and no ADB TCP listener is
  available.

The required APK was not rebuilt or modified. Its SHA-256 was recomputed as
`e53a0e67b7a0a64f216459b32f0388d7717716cbd44661e338b1a486c27fc82c`, matching
the metadata sidecar.

| Task 22 check | Result |
| --- | --- |
| Named landscape Android device | **Unavailable** — no device, USB bus, emulator, or ADB endpoint exposed |
| Install of the verified APK | **Not run** — no `adb` or device target |
| Launch result | **Not observed** |
| Thornwold level 4, phases 1–3 | **Not run** |
| Akaros level 7, phases 1–3 | **Not run** |
| Vyrn level 10, phases 1–3 | **Not run** |
| Enemy archetypes 0–7 | **Not run on hardware** |
| Touch comfort, telegraph readability, frame pacing | **Not observed** |
| Device-derived fixes | **None** — no device runtime evidence available |

**Task 22 disposition: BLOCKED — the assigned physical-device sign-off could not
be executed in this workspace.** This is an environmental blocker, not a
gameplay failure. The physical Android combat gate remains pending a tester with
a named landscape Android device.

## Task 24 execution attempt — August 30, 2026

The physical sign-off was attempted again from the current workspace. A final
environment sweep found no Android execution path:

- `adb`, `fastboot`, `emulator`, `sdkmanager`, `aapt`, `lsusb`, and
  `usb-devices` are unavailable on `PATH`.
- No candidate Android SDK was present under the common workspace locations.
- `/dev/bus/usb`, `/dev/usb`, and `/dev/kvm` are absent.
- No Android, emulator, QEMU, or ADB-server process is running, and no ADB
  listener is available.

The required APK was not rebuilt or modified. Its checksum was recomputed and
still matches the metadata sidecar:

`e53a0e67b7a0a64f216459b32f0388d7717716cbd44661e338b1a486c27fc82c`

| Task 24 check | Result |
| --- | --- |
| Named landscape Android device | **Unavailable** — no physical device, USB bus, emulator, or ADB endpoint exposed |
| Install of the verified APK | **Not run** — no `adb` or device target |
| Launch result | **Not observed** |
| Thornwold level 4, phases 1–3 | **Not run** |
| Akaros level 7, phases 1–3 | **Not run** |
| Vyrn level 10, phases 1–3 | **Not run** |
| Enemy archetypes 0–7 | **Not run on hardware** |
| Touch comfort | **Not observed** |
| Telegraph readability | **Not observed** |
| Frame pacing | **Not observed** |
| Device-derived fixes | **None** — no device runtime evidence available |

**Task 24 disposition: BLOCKED — environmental blocker.** The named-device
install, launch, three guardian playthroughs, enemy-archetype observation, and
physical usability checks could not be performed. This is not a gameplay
failure: no hardware run occurred, so no hardware result or gameplay defect is
being claimed. The physical Android combat gate remains pending tester
execution on a named landscape Android device.

## Task 25 release publication attempt — August 30, 2026

The local Android Debug APK workflow was hardened for future tester releases:

- `permissions: contents: write` is required for release publication.
- APK and `APK_BUILD_METADATA.txt` are both required and must be non-empty.
- A pre-existing release tag is rejected instead of being overwritten.
- After `gh release create`, the workflow requires exactly two assets and
  verifies both expected filenames.
- An exit trap deletes any partial release and its tag if creation or
  post-upload verification fails, so an upload failure cannot leave an empty
  tester-facing release.

The configured GitHub connection could inspect the public repository and create
Git Data blobs, but it could not install this workflow on the remote `main`
branch:

- Repository-content update was rejected by the connector's Cloudflare route
  with HTTP 403 (`uploads.github.com`/GitHub write routes are unavailable
  through that proxy).
- GitHub GraphQL `CreateCommitOnBranch` was rejected with `FORBIDDEN` because
  the connected account does not have the workflow-commit permission exposed
  through this connection.
- The GitHub Git Data tree endpoint was unavailable through the connector.

The remote branch remains unchanged at source revision
`a25575e16cf9187ff95f72aa88f6e7be9e433567`; its release list was rechecked and
is empty. No placeholder release or tag was left behind. The guarded workflow
change is preserved in this checkout for the next workflow-capable GitHub
account or upload path.

The verified v5.0.0 handoff files remain intact and unchanged:

- Source revision: `ecc720ea2cfca8bf54addec33198345e1987ed66`
- APK: `.agents/outputs/Legends_of_the_Lost_Realms_v5.0.0_ecc720e-debug.apk`
- APK SHA-256:
  `e53a0e67b7a0a64f216459b32f0388d7717716cbd44661e338b1a486c27fc82c`
- Metadata: `.agents/outputs/APK_BUILD_METADATA.txt`

**Task 25 disposition: BLOCKED — publication-capable GitHub authorization is
not available in this workspace.** No final prerelease page or direct asset
URLs are recorded because none were successfully published. The local
workflow path is ready, but the verified APK cannot be claimed as publicly
downloadable until a workflow-capable account or upload path completes and
verifies the release.

## Task 26 physical combat-gate attempt — August 30, 2026

The assigned named-device validation was rechecked from the current workspace.
No Android execution path is exposed:

- `adb`, `emulator`, `sdkmanager`, and `aapt` are unavailable on `PATH`.
- No candidate Android tools were found under `/opt`, `/usr/local`, or
  `/home/runner`.
- `/dev/bus/usb`, `/dev/usb`, and `/dev/kvm` are absent.
- No Android, emulator, QEMU, or ADB-server process is running, and no local
  listener is available for an ADB-over-network target.

The handoff APK was not rebuilt or modified. Its SHA-256 was recomputed and
matches `.agents/outputs/APK_BUILD_METADATA.txt`:

`e53a0e67b7a0a64f216459b32f0388d7717716cbd44661e338b1a486c27fc82c`

| Task 26 check | Result |
| --- | --- |
| Named landscape Android device/model | **Unavailable** — no physical device or emulator target was exposed |
| Install of the verified APK | **Not run** — no `adb` executable or device target |
| Launch result | **Not observed** |
| Thornwold level 4, phases 1–3 | **Not run** |
| Akaros level 7, phases 1–3 | **Not run** |
| Vyrn level 10, phases 1–3 | **Not run** |
| Enemy archetypes 0–7 | **Not run on hardware** |
| Touch comfort | **Not observed** |
| Telegraph readability | **Not observed** |
| Frame pacing | **Not observed** |
| Device-derived fixes | **None** — no device runtime evidence was available |

**Task 26 disposition: BLOCKED — environmental blocker.** The named-device
install, launch, three guardian playthroughs, enemy-archetype observation, and
physical usability checks could not be performed in this workspace. This is
not a gameplay failure: no hardware run occurred, so no hardware result or
gameplay defect is being claimed. The physical Android combat gate remains
pending tester execution on a named landscape Android device.

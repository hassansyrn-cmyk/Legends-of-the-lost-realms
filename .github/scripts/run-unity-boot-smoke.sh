#!/usr/bin/env bash
# Runs the compiled Unity player and asserts that startup creates Aster, the
# realm, and a valid above-ground camera without runtime exceptions.
set -Eeuo pipefail

repo_root="$(pwd)"
player="$repo_root/unity-3d/Builds/LinuxSmoke/LostRealms3D.x86_64"
log="$repo_root/unity-3d/Validation/boot-player.log"
report="$repo_root/unity-3d/Validation/boot-smoke.txt"
persistent_report="$HOME/.config/unity3d/Lost Realms Studio/Legends of the Lost Realms 3D/boot-smoke.txt"

[[ -x "$player" ]] || { echo "Missing executable smoke player: $player" >&2; exit 1; }
mkdir -p "$(dirname "$log")"
rm -f "$log" "$report"

set +e
timeout 45s xvfb-run -a -- "$player" -batchmode -nographics -realmBootSmoke -logFile "$log"
status=$?
set -e
if [[ -f "$persistent_report" ]]; then cp "$persistent_report" "$report"; fi

if [[ $status -eq 124 ]]; then
  echo "Unity boot smoke timed out." >&2
  tail -n 200 "$log" >&2 || true
  exit 1
fi
if [[ $status -ne 0 ]]; then
  echo "Unity boot smoke player exited with $status." >&2
  tail -n 260 "$log" >&2 || true
  exit "$status"
fi
[[ -f "$report" ]] || { echo "Unity boot smoke report was not created." >&2; tail -n 200 "$log" >&2 || true; exit 1; }
grep -Fqx 'BOOT_SMOKE_PASSED' "$report" || { cat "$report" >&2; exit 1; }
if grep -Eq 'NullReferenceException|BOOT_SMOKE_FAILED|Exception:' "$log"; then
  echo "Unity boot smoke logged a runtime exception." >&2
  grep -EnC 6 'NullReferenceException|BOOT_SMOKE_FAILED|Exception:' "$log" >&2 || true
  exit 1
fi
printf 'Compiled Unity boot smoke passed.\n'

#!/usr/bin/env bash
# Guards against the cache and Humanoid-rig regressions that can OOM-kill the Unity builder.
set -Eeuo pipefail

workflow=".github/workflows/android-debug.yml"
model_meta="unity-3d/Assets/Art/Models/Aster.fbx.meta"

fail() {
  printf 'CI configuration check failed: %s\n' "$1" >&2
  exit 1
}

[[ -f "$workflow" ]] || fail "missing Android workflow"
[[ -f "$model_meta" ]] || fail "missing Aster model importer metadata"

# Unity Library contents are derived artifacts. A cache from a different asset hash
# must not be restored because it can leave the AssetDatabase inconsistent.
if grep -Eq '^[[:space:]]*restore-keys:' "$workflow"; then
  fail "Unity Library cache must restore exact keys only"
fi

grep -Eq '^[[:space:]]*dockerCpuLimit:[[:space:]]*2[[:space:]]*$' "$workflow" \
  || fail "Unity builder must use two CPUs to bound peak build memory"

# A Humanoid model needs serialized HumanDescription data. Empty mappings force
# Unity to re-infer a rig and are not a reproducible CI import configuration.
grep -Eq '^  animationType: 3$' "$model_meta" \
  || fail "Aster must remain a Humanoid model"
grep -Eq '^    human:$' "$model_meta" \
  || fail "Aster Humanoid mapping must not be empty"
grep -Eq '^    skeleton:$' "$model_meta" \
  || fail "Aster skeleton mapping must not be empty"

printf 'Unity CI configuration check passed.\n'

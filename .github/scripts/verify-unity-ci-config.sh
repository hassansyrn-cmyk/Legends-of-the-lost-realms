#!/usr/bin/env bash
# Guards against Unity cache and Aster model-binding regressions.
set -Eeuo pipefail

workflow=".github/workflows/android-debug.yml"
model_meta="unity-3d/Assets/Art/Models/Aster.fbx.meta"
material="unity-3d/Assets/Art/Materials/Aster.mat"
prefab="unity-3d/Assets/Resources/Characters/Aster.prefab"
albedo_meta="unity-3d/Assets/Art/Textures/Aster_1.jpg.meta"
material_meta="unity-3d/Assets/Art/Materials/Aster.mat.meta"

fail() {
  printf 'CI configuration check failed: %s\n' "$1" >&2
  exit 1
}

[[ -f "$workflow" ]] || fail "missing Android workflow"
[[ -f "$model_meta" ]] || fail "missing Aster model importer metadata"
[[ -f "$material" ]] || fail "missing Aster material"
[[ -f "$prefab" ]] || fail "missing Aster character prefab"
[[ -f "$albedo_meta" ]] || fail "missing Aster albedo metadata"
[[ -f "$material_meta" ]] || fail "missing Aster material metadata"

# Unity Library contents are derived artifacts. A cache from a different asset hash
# must not be restored because it can leave the AssetDatabase inconsistent.
if grep -Eq '^[[:space:]]*restore-keys:' "$workflow"; then
  fail "Unity Library cache must restore exact keys only"
fi

grep -Eq '^[[:space:]]*dockerCpuLimit:[[:space:]]*2[[:space:]]*$' "$workflow" \
  || fail "Unity builder must use two CPUs to bound peak build memory"

# This particular replacement is a Mixamo model. Its original importer metadata
# deliberately uses Unity's mapping rather than the previous model's serialized
# bone map. Applying that old map changes generated sub-assets and loses the
# prefab's material override in a player build.
grep -Eq '^  animationType: 3$' "$model_meta" \
  || fail "Aster must remain a Humanoid model"
grep -Fxq '    human: []' "$model_meta" \
  || fail "Aster must use the new Mixamo Humanoid mapping"
grep -Fxq '    skeleton: []' "$model_meta" \
  || fail "Aster must use the new Mixamo skeleton mapping"
grep -Fxq '    globalScale: 1' "$model_meta" \
  || fail "Aster Mixamo rig must retain its unit scale"
grep -Fxq '    skeletonHasParents: 1' "$model_meta" \
  || fail "Aster Mixamo skeleton must retain parent relationships"

albedo_guid="$(sed -n 's/^guid: //p' "$albedo_meta")"
material_guid="$(sed -n 's/^guid: //p' "$material_meta")"
[[ -n "$albedo_guid" ]] || fail "Aster albedo GUID is missing"
[[ -n "$material_guid" ]] || fail "Aster material GUID is missing"

main_texture_binding="$(awk '/- _MainTex:/{in_main_texture=1; next} in_main_texture && /guid:/{print; exit}' "$material")"
printf '%s\n' "$main_texture_binding" | grep -Fq "guid: $albedo_guid" \
  || fail "Aster material is not bound to Aster_1.jpg"
grep -Fq "objectReference: {fileID: 2100000, guid: $material_guid, type: 2}" "$prefab" \
  || fail "Aster prefab is not bound to the textured Aster material"

printf 'Unity CI configuration check passed.\n'

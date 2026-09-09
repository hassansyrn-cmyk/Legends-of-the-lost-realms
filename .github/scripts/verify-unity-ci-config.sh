#!/usr/bin/env bash
# Guards the Unity Android build and the source-driven Aster Phase 1 integration.
set -Eeuo pipefail

workflow=".github/workflows/android-debug.yml"
model="unity-3d/Assets/Art/Models/Aster.fbx"
animation_root="unity-3d/Assets/Art/Animations/AsterMixamo"
material="unity-3d/Assets/Resources/Materials/Aster.mat"
albedo_meta="unity-3d/Assets/Art/Textures/Aster_1.jpg.meta"
hero_script="unity-3d/Assets/Scripts/Hero.cs"
phase1_script="unity-3d/Assets/Editor/AsterPhase1.cs"
build_script="unity-3d/Assets/Editor/RealmBuild.cs"

fail() {
  printf 'CI configuration check failed: %s\n' "$1" >&2
  exit 1
}

[[ -f "$workflow" ]] || fail "missing Android workflow"
[[ -f "$material" ]] || fail "missing Aster runtime material"
[[ -f "$albedo_meta" ]] || fail "missing Aster albedo metadata"
[[ -f "$hero_script" ]] || fail "missing Aster runtime visual code"
[[ -f "$phase1_script" ]] || fail "missing source-driven Aster Phase 1 importer"
[[ -f "$build_script" ]] || fail "missing Unity build script"

# Unity Library contents are derived artifacts. A cache from a different source
# revision must not be restored because it can leave the AssetDatabase inconsistent.
if grep -Eq '^[[:space:]]*restore-keys:' "$workflow"; then
  fail "Unity Library cache must restore exact keys only"
fi
grep -Fq "'unity-3d/Assets/**'" "$workflow" \
  || fail "Unity cache key must include the supplied Aster assets"
grep -Eq '^[[:space:]]*dockerCpuLimit:[[:space:]]*2[[:space:]]*$' "$workflow" \
  || fail "Unity builder must use two CPUs to bound peak build memory"

# Phase 1 is reproducibly generated from the user's curated FBX sources.
[[ -s "$model" ]] || fail "missing supplied Mixamo Aster base FBX"
for clip in walk run attack_1 attack_2 attack_3 charged jump dodge hit death; do
  [[ -s "$animation_root/Aster_${clip}.fbx" ]] \
    || fail "missing supplied Aster_${clip}.fbx"
done
grep -Fq 'AsterPhase1.Prepare();' "$build_script" \
  || fail "Android build must prepare Aster before building"
grep -Fq 'MODEL_INTEGRATION_PASSED' "$phase1_script" \
  || fail "Aster preparation must emit its completion marker"

# The material must retain the replacement model's supplied albedo texture.
albedo_guid="$(sed -n 's/^guid: //p' "$albedo_meta")"
[[ -n "$albedo_guid" ]] || fail "Aster albedo GUID is missing"
main_texture_binding="$(awk '/- _MainTex:/{in_main_texture=1; next} in_main_texture && /guid:/{print; exit}' "$material")"
printf '%s\n' "$main_texture_binding" | grep -Fq "guid: $albedo_guid" \
  || fail "Aster material is not bound to Aster_1.jpg"

# Runtime assignment avoids FBX renderer sub-asset IDs that change on cold import.
grep -Fq 'Resources.Load<Material>("Materials/Aster")' "$hero_script" \
  || fail "Aster runtime material is not loaded from Resources"
grep -Fq 'renderer.sharedMaterial=asterMaterial' "$hero_script" \
  || fail "Aster runtime material is not assigned to every renderer"
for state in idle walk run attack_1 attack_2 attack_3 charged jump dodge hit death; do
  grep -Fq "\"$state\"" "$hero_script" || fail "runtime state $state is not integrated"
done

printf 'Unity CI configuration check passed.\n'

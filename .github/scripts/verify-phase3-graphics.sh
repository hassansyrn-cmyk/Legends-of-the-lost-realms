#!/usr/bin/env bash
# Guards Phase 3's realm readability and mobile visual budget.
set -Eeuo pipefail

scenery="unity-3d/Assets/Scripts/RealmScenery.cs"
atmosphere="unity-3d/Assets/Scripts/RealmAtmosphere.cs"
shader="unity-3d/Assets/Resources/Shaders/Weathered.shader"
probe="unity-3d/Assets/Scripts/RuntimeProbe.cs"

fail() {
  printf 'Phase 3 graphics check failed: %s\n' "$1" >&2
  exit 1
}

for texture in verdant_moss_tile ember_sand_tile frost_ice_tile; do
  [[ -s "unity-3d/Assets/Resources/Art/${texture}.jpg" ]] || fail "missing generated ${texture} texture"
  [[ -f "unity-3d/Assets/Resources/Art/${texture}.jpg.meta" ]] || fail "missing metadata for ${texture} texture"
done
[[ -f "$scenery" ]] || fail "missing scenery builder"
[[ -f "$atmosphere" ]] || fail "missing atmosphere controller"
[[ -f "$shader" ]] || fail "missing weathered terrain shader"

grep -Fq 'Resources.Load<Texture2D>(textures[realm])' "$scenery" || fail "realm terrain does not load generated texture"
grep -Fq 'RealmAtmosphere.Apply(world,realm)' "$scenery" || fail "realm atmosphere is not attached to worlds"
grep -Fq '_MainTex("Realm Terrain",2D)' "$shader" || fail "terrain shader has no terrain texture property"
grep -Fq 'tex2D(_MainTex,IN.worldPos.xz*_TileScale)' "$shader" || fail "terrain shader does not world-tile terrain art"
grep -Fq 'class RealmAtmosphere' "$atmosphere" || fail "missing atmosphere controller"
grep -Fq 'CreateRouteLandmarks()' "$atmosphere" || fail "missing route landmark construction"
grep -Fq 'CreateAmbientMotes()' "$atmosphere" || fail "missing ambient motion construction"
grep -Fq 'QualitySettings.shadowDistance=28' "$atmosphere" || fail "missing mobile shadow-distance budget"
if grep -Fq 'root.localPosition=' "$atmosphere"; then fail "route landmark root must use GameObject.transform.localPosition"; fi
if grep -Fq 'foreach(Transform piece in ring)' "$atmosphere"; then fail "rune ring enumeration must use ring.transform"; fi
grep -Fq 'Realm uses generated terrain texture' "$probe" || fail "runtime probe does not verify terrain texture"
grep -Fq 'Realm atmosphere adds landmarks and ambient motes' "$probe" || fail "runtime probe does not verify atmosphere content"

printf 'Phase 3 graphics regression check passed.\n'

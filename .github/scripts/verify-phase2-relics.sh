#!/usr/bin/env bash
# Guards Phase 2 against placeholder cube collectibles and inert checkpoints returning.
set -Eeuo pipefail

world="unity-3d/Assets/Scripts/RealmWorld.cs"
relics="unity-3d/Assets/Scripts/RealmRelics.cs"
probe="unity-3d/Assets/Scripts/RuntimeProbe.cs"

fail() {
  printf 'Phase 2 relic check failed: %s\n' "$1" >&2
  exit 1
}

[[ -f "$world" ]] || fail "missing world builder"
[[ -f "$relics" ]] || fail "missing relic visual system"
[[ -f "$probe" ]] || fail "missing runtime probe"

grep -Fq 'RelicArt.Gem(go.transform,accent)' "$world" \
  || fail "gems are not using the Phase 2 faceted relic visual"
grep -Fq 'RelicArt.Checkpoint(go.transform,accent,stone)' "$world" \
  || fail "checkpoints are not using the Phase 2 sanctuary visual"
grep -Fq 'if(visual)visual.Activate()' "$world" \
  || fail "checkpoint activation does not drive its visual feedback"
grep -Fq 'class GemVisual' "$relics" \
  || fail "missing gem animation controller"
grep -Fq 'class CheckpointVisual' "$relics" \
  || fail "missing checkpoint animation controller"
grep -Fq 'Faceted gem core' "$relics" \
  || fail "gem mesh is not faceted"
grep -Fq 'Sanctuary octagonal dais' "$relics" \
  || fail "checkpoint silhouette is not a sanctuary dais"
grep -Fq 'Aura=light' "$relics" \
  || fail "checkpoint is missing its activation aura"
grep -Fq 'Gem pickup has animated faceted relic visual' "$probe" \
  || fail "runtime probe does not verify the gem visual"
grep -Fq 'Checkpoint has animated sanctuary visual' "$probe" \
  || fail "runtime probe does not verify the checkpoint visual"

printf 'Phase 2 relic regression check passed.\n'

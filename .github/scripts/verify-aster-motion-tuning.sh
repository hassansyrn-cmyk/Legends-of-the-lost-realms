#!/usr/bin/env bash
# Prevents the excessively fast Phase 1 Aster playback from returning and
# guards the double-jump flip removal (the flip clip still ships, unplayed).
set -Eeuo pipefail

hero="unity-3d/Assets/Scripts/Hero.cs"
probe="unity-3d/Assets/Scripts/RuntimeProbe.cs"
source_clip="unity-3d/Assets/Art/Animations/AsterMixamo/Aster_double_jump.fbx"
importer="unity-3d/Assets/Editor/AsterPhase1.cs"

fail() {
  printf 'Aster motion check failed: %s\n' "$1" >&2
  exit 1
}

[[ -f "$hero" ]] || fail "missing Hero movement implementation"
[[ -f "$probe" ]] || fail "missing runtime motion probe"
[[ -s "$source_clip" ]] || fail "missing supplied double-jump flip FBX"
[[ -f "$importer" ]] || fail "missing Aster importer"

# The prior source used a 6.1 world-unit target velocity and ran a 0.7 second
# run clip up to 1.45x. Together they made Aster slide and cycle unnaturally fast.
if grep -Fq 'wish*6.1f' "$hero"; then
  fail "legacy 6.1 movement target remains"
fi
if grep -Fq 'speedRatio*.82f' "$hero"; then
  fail "legacy run playback multiplier remains"
fi
if grep -Fq 'SetSpeed(3.1f)' "$hero"; then
  fail "legacy overly-fast dodge playback remains"
fi

# The locomotion cap, controlled deceleration, and flip state must be explicit.
grep -Fq 'const float MaxMoveSpeed=4.8f' "$hero" \
  || fail "missing controlled max locomotion speed"
grep -Fq 'Vector3.MoveTowards(velocity,desiredVelocity,response*dt)' "$hero" \
  || fail "missing deterministic acceleration and deceleration"
if grep -Fq 'Visual.Restart("double_jump")' "$hero"; then
  fail "double-jump flip was removed by design"
fi
grep -Fq '"double_jump"' "$hero" \
  || fail "double-jump state is not in the runtime animation map"
grep -Fq 'Aster_double_jump.fbx' "$importer" \
  || fail "double-jump FBX is not imported"
grep -Fq '"double_jump"' "$probe" \
  || fail "runtime probe does not verify the double-jump state"
grep -Fq 'Second jump reuses jump motion' "$probe" \
  || fail "runtime probe does not exercise the double-jump transition"

printf 'Aster motion regression check passed.\n'

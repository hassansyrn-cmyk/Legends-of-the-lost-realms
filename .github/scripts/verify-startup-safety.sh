#!/usr/bin/env bash
# Covers the exact startup condition reported on Android: world objects updating
# while RealmGame has not yet finished assigning the player and camera target.
set -Eeuo pipefail

world="unity-3d/Assets/Scripts/RealmWorld.cs"
hero="unity-3d/Assets/Scripts/Hero.cs"
game="unity-3d/Assets/Scripts/RealmGame.cs"
atmosphere="unity-3d/Assets/Scripts/RealmAtmosphere.cs"

fail() {
  printf 'Startup safety check failed: %s\n' "$1" >&2
  exit 1
}

for source in "$world" "$hero" "$game" "$atmosphere"; do
  [[ -f "$source" ]] || fail "missing startup-sensitive source: $source"
done

# Every Phase 2 world object can awaken before its owner has completed LoadLevel.
# Do not let a transient null Player turn into an endless Android Update exception.
grep -Fq 'if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;' "$world" \
  || fail "world updates are not guarded against startup without a player"
grep -Fq 'if(!g||!g.CameraRig||!Visual||!Controller)return;' "$hero" \
  || fail "hero update is not guarded against incomplete startup"
grep -Fq 'if(!I||!Player)return;' "$game" \
  || fail "game input is not guarded against incomplete startup"
grep -Fq 'if(!RealmGame.I||RealmGame.I.Screen!=GameScreen.Playing||!RealmGame.I.Player)return;' "$atmosphere" \
  || fail "atmosphere update is not guarded against startup without a player"
grep -Fq 'CameraRig.Target=Player.transform;' "$game" \
  || fail "camera target is not explicitly restored after player creation"
grep -Fq 'CameraRig.Snap();' "$game" \
  || fail "camera reset is missing after player creation"

printf 'Startup safety regression check passed.\n'

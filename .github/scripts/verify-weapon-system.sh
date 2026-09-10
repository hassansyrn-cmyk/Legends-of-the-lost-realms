#!/usr/bin/env bash
# Guards the imported mobile weapon sources and their drop/equipment gameplay seam.
set -Eeuo pipefail

weapons="unity-3d/Assets/Scripts/WeaponSystem.cs"
world="unity-3d/Assets/Scripts/RealmWorld.cs"
hero="unity-3d/Assets/Scripts/Hero.cs"
game="unity-3d/Assets/Scripts/RealmGame.cs"
probe="unity-3d/Assets/Scripts/RuntimeProbe.cs"

fail() {
  printf 'Weapon system check failed: %s\n' "$1" >&2
  exit 1
}

for asset in Aster_Axe Aster_LongSword Aster_CurvedSword; do
  fbx="unity-3d/Assets/Resources/Weapons/${asset}.fbx"
  [[ -s "$fbx" ]] || fail "missing supplied ${asset} model"
  [[ -f "${fbx}.meta" ]] || fail "missing Unity metadata for ${asset}"
  # A 30MB, million-triangle source model must never accidentally be restored.
  [[ $(stat -c '%s' "$fbx") -lt 2000000 ]] || fail "${asset} exceeds mobile asset budget"
done

for source in "$weapons" "$world" "$hero" "$game" "$probe"; do
  [[ -f "$source" ]] || fail "missing weapon integration source: $source"
done

grep -Fq 'enum WeaponId' "$weapons" || fail "missing weapon definitions"
grep -Fq 'Weapons/Aster_Axe' "$weapons" || fail "axe model is not loadable from Resources"
grep -Fq 'Weapons/Aster_LongSword' "$weapons" || fail "longsword model is not loadable from Resources"
grep -Fq 'Weapons/Aster_CurvedSword' "$weapons" || fail "curved sword model is not loadable from Resources"
grep -Fq 'public sealed class WeaponDrop' "$weapons" || fail "missing floating weapon drop behavior"
grep -Fq 'public sealed class EquippedWeapon' "$weapons" || fail "missing equipped weapon visual behavior"
grep -Fq 'WeaponDrop(p+' "$world" || fail "realm route does not create a random weapon drop"
grep -Fq 'weaponIsland=random.Next(2,count-2)' "$world" || fail "weapon drop placement is not randomized"
grep -Fq 'var weapon=g.CurrentWeapon;' "$hero" || fail "hero attacks ignore equipped weapon stats"
grep -Fq 'float damage=(charged?3.5f:combo==3?2.5f:1.5f)*weapon.Damage;' "$hero" || fail "weapon damage modifier is missing"
grep -Fq 'float reach=(charged?3.4f:2.7f)*weapon.Reach;' "$hero" || fail "weapon reach modifier is missing"
grep -Fq 'public void EquipWeapon(WeaponId id)' "$game" || fail "game cannot equip collected weapons"
grep -Fq 'equippedWeapon' "$game" || fail "equipped weapon is not persisted"
grep -Fq 'Weapon drop can equip' "$probe" || fail "runtime probe does not cover weapon drops"
grep -Fq 'Equipping axe changes equipped stats' "$probe" || fail "runtime probe does not cover weapon stat changes"

printf 'Weapon system regression check passed.\n'

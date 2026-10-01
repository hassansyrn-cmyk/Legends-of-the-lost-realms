# Staff charged attacks — September 30, 2026

## October 1 follow-up — release timing and Brass Fangs attachment

- All five ranged charged attacks now release at 85% of their attack animation, before its recovery tail, using the existing playback duration and weapon tempo. Position and aim are sampled at release. Pending shots cancel on weapon replacement or attack interruption and wait through pause.
- Brass Fangs uses its measured thin axis and centered geometry to rest across Aster's back, following his facing continuously instead of flipping between world directions.
- Focused Unity rerun: STAFF_FOCUSED_TESTS_PASSED 49 (Validation/charge-timing-runtime.log). Includes no projectile at startup/mid-swing, release before recovery finishes, pause, impacts, weapon-switch cancellation, and consistent Brass Fangs resting pose at 0/90/180/270 degrees. 72 quality guards and 95 PowerShell CI-equivalent checks passed. Phone animation feel still needs playtesting; no Android build or push for this follow-up.

- Ember Torch: hold BLADE/attack to launch a fireball. Direct hits burn through the existing fire damage path; nearby visible foes within 1.6 m receive half damage.
- Sage Staff: hold BLADE/attack to launch a faster blue thunder bolt. On impact it can arc to one visible foe within 3.5 m for 55% damage. Uses the existing gale elemental damage/knockback channel; no new saved element is added.
- Warden Pike: storm lance with imported electric projectile particles, a jagged luminous core, muzzle flash, lightning impact and a chain to one nearby foe.
- Reaper's Scythe: one broad violet spectral crescent with imported slash particles. Travels up to 12 m and damages the first enemy it reaches.
- Brass Fangs: three gold claw-shaped slashes, slightly spread, each carrying 45% of charged damage. Travels up to 12 m; close aim can land all three.
- Short presses retain melee attacks. Existing charge thresholds, cooldowns, weapon damage and Arsenal scaling remain. Charged shots replace melee damage for these five weapons, cost no extra spell energy; fire and thunder travel up to 16 m.
- Shared swept collision prevents missed targets between frames; solid scenery blocks direct and secondary hits. Gameplay movement pauses with the game. Projectiles belong to the chapter world.
- Arsenal descriptions advertise each charged attack. Other physical polearms and Moon Chakram keep their existing behavior.

Validation: standalone C# compile passed; 72 quality guards and 95 PowerShell CI-equivalent checks passed. Expanded Unity play-mode tests cover the Hero charged-attack dispatch, correct elemental mapping, no simultaneous melee hit, energy, pause, direct/secondary damage, wall blocking, expiration, short-press melee dispatch, scythe/claw projectile counts and target damage. Final result: STAFF_FOCUSED_TESTS_PASSED 33, zero runtime errors. Imported demo scripts are disabled and missing components skipped. Final visual previews were inspected: charge-WardenPike.png, charge-PureScythe.png and charge-BrassFangs.png. Actual touch gestures, presentation and balance still need user playtesting. No Android build was produced; no commit or push was made.


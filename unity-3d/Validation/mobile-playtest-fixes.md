# Phone playtest fixes — September 30, 2026

- Desert houses, towers, ruins, tents and walls use enabled solid box colliders instead of relying on imported building triangles. Gate_01 retains its mesh-shaped arch collision, without a box filling the opening.
- Frozen realms no longer instantiate the five untextured green aurora panels responsible for the large translucent rectangles. Snow and rain remain.
- Moon Chakram sheath orientation uses the mesh's thinnest axis as its plane normal. Its center accounts for hierarchy scale and attaches at visible torso height, avoiding the imported chest attachment offset. Final isolated render: `chakram-back-fit.png`.
- Charged throws use the textured Moon Chakram mesh. The carried model hides during flight, returns after the catch, and cannot spawn duplicate throws while already flying. Switching equipment removes the old projectile.

Validation: Unity compiled and completed the focused play-mode checks without runtime errors (`MOBILE_FOCUSED_TESTS_PASSED 666`). Coverage includes chapters 5–7 building collider rays and arch collision type, absent aurora panels, chakram orientation and torso position, actual projectile mesh, outward travel, catch restoration, and equipment-switch cleanup. The count includes per-object checks and varies with generated decoration. Final back placement was visually inspected in the isolated render. Standalone C# compilation, 72 quality source guards and 95 PowerShell CI-equivalent checks also passed earlier in this pass.

No Android device run or APK rebuild was performed. Mobile appearance and actual walking through arch openings still need the user's phone playtest with a fresh build. No commits or pushes were made for this pass.

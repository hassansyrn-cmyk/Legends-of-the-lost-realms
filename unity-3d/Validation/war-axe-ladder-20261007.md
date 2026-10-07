# War Axe grip and ladder removal

The original axe mesh has a longitudinal Z handle axis and a cutting edge opposite its small counterweight. Inspection of the imported FBX in Blender confirmed this geometry; the weapon asset itself was not edited or re-exported.

The War Axe's existing -82-degree handle alignment is retained, with a 180-degree roll around its local Z axis to reverse which side of the axe leads. The War Axe now uses its existing measured Z=0.18 handle grip anchor, placing the handle through the palm rather than using the model's pommel origin. Damage, reach, tempo and chop animation selection are unchanged.

The small ladder was removed from the village dressing pool. Both runtime and source FBX assets, the source prefab and all three importer metadata files were deleted. A name/GUID scan found no remaining references in Assets. Shared village materials and textures remain.

All six source regression checks passed through `verify-unity-ci-config.sh`, and `git diff --check` passed. Aster model/animation assets, baker and movement code were not changed. Unity was open during this pass, so an in-game swing render was not performed; visual confirmation of the swing orientation remains a playtest check after stopping and restarting Play mode. The ladder is generated at level creation, so restarting Play mode also removes any existing instance.

Local rollback backup: `Temp/opencode/war-axe-ladder-20261007-backup`. Restore script: `Temp/opencode/revert-war-axe-ladder-20261007.ps1`. Included in the `Sol-New-Design` branch.

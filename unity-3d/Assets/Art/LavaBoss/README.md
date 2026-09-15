# Emberfall boss source assets

Supplied by the project owner in Downloads on 15 September 2026. These are the
new golem and its Mixamo takes, imported independently of Aster.

`Lost Realms > Lava Boss > Prepare supplied Mixamo boss` generates the humanoid
prefab, materials, and `Resources/Animations/LavaBoss` clips. The importer uses
the same idle root height and orientation across takes so walking cannot drag
the visible model away from the enemy's gameplay position. Never run the Aster
baker for this character.

The fourth-realm boss (kind 21) uses the new prefab. DogKnight remains available
for the ordinary enemy family. The boss carries the existing Juggernaut
greatsword, sized for its 3.6-metre body.

Combat uses Great Sword Slash, Standing Melee Attack Downward / Great Sword
Attack, and Jump Attack / Great Sword Jump Attack. Mutant breathing, walking,
running, roaring, flexing, and dying supply locomotion, phase transitions and
death. Extra creature clips remain available for future encounter variations.

Damage is driven by the combat state machine, with a ground warning before each
strike. Root motion never moves the gameplay actor. The jumping attack follows
a bounded route to its marked landing point, and stun cancels the attack.

Run `Lost Realms > Lava Boss > Validate rig and render` to check the humanoid
avatar, stable root curves, arm motion and weapon attachment, and capture poses.

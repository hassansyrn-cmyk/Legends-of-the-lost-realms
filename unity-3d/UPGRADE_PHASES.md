# Legends of the Lost Realms 3D Upgrade Plan

**Author:** Manus AI
**Target branch:** `chatgpt-astra/unity-3d`

## Objective

The upgrade will turn the current prototype into a more coherent mobile third-person action-adventure without expanding all systems at once. Each phase must produce an installable Android build that can be evaluated independently. The implementation will use original project assets, the Aster FBX package supplied by the project owner, and permissively licensed external assets only when their license and attribution requirements can be verified.

## Phased Scope

| Phase | Scope | Review gate |
|---|---|---|
| **1. Aster character and movement** | Replace the legacy Aster mesh with the supplied 52-bone Mixamo character. Integrate idle, walk, run, three melee variations, charged attack, jump, double-jump flip, roll-dodge, hit reaction, and death. Add reproducible import automation and Android artifact verification. | Aster is textured, correctly scaled, animated in all gameplay states, and packaged in the APK. |
| **2. Checkpoints and gems** | Replace cube placeholders with readable fantasy checkpoint and collectible designs. Add activation, idle, collection, and feedback effects. Use permissively licensed external assets only if they outperform original in-project models. | Checkpoints and gems have distinctive silhouettes, materials, animation, sound, and clear gameplay feedback. |
| **3. Graphics and presentation** | Improve lighting, atmosphere, environment materials, landmarks, particles, camera composition, color grading, HUD hierarchy, and visual readability. | Each realm has a distinct visual identity and remains readable and performant on a phone. |
| **4. Gameplay polish and optimization** | Tune combat windows, enemy telegraphs, target assist, traversal, difficulty, rewards, haptics, audio feedback, accessibility, and mobile performance. | The full ten-level loop is responsive, understandable, balanced, and stable on representative Android hardware. |

## Phase 1 Implementation

The supplied ZIP contains 25 FBX files. Every file contains the same textured fantasy-warrior character mesh, a 52-bone Mixamo humanoid rig, one animation, and two embedded textures. Phase 1 uses one FBX as the canonical model and ten additional FBXs as curated animation sources. The remaining files are intentionally excluded from the player build to avoid redundant meshes and unnecessary APK growth.

| Gameplay state | Supplied FBX selected | Integration behavior |
|---|---|---|
| Idle | `Unarmed Idle.fbx` | Canonical character model, rig, textures, and looping idle. |
| Walk | `Walking.fbx` | Code-driven locomotion with animation speed matching. |
| Run | `Sword And Shield Run.fbx` | Faster looping locomotion with a distinct silhouette. |
| Combo strike 1 | `Standing Melee Attack Downward.fbx` | First light attack with forward facing assist. |
| Combo strike 2 | `One Hand Club Combo.fbx` | Second combo variation, accelerated to the gameplay window. |
| Combo strike 3 | `Standing Melee Attack Kick Ver. 1.fbx` | Finisher variation with stronger damage. |
| Charged attack | `Great Sword Jump Attack.fbx` | Heavier attack with a longer commitment window. |
| Jump | `Standing Jump.fbx` | Takeoff and airborne pose for the existing double-jump system. |
| Double-jump flip | `Inverted Double Kick To Kip Up.fbx` | Full airborne inversion triggered only by the second jump. |
| Dodge | `Falling To Roll.fbx` | Fast roll visual synchronized with the existing invulnerable dash. |
| Hit reaction | `Standing React Large From Left.fbx` | Short impact response without removing player control for too long. |
| Death | `Standing Death Backward 01.fbx` | Defeat animation with no locomotion loop. |

Phase 1 keeps movement displacement controlled by gameplay code. The Mixamo clips are imported as Humanoid animations and have root translation locked during import. This prevents animation root motion from fighting the `CharacterController`, collision system, ledges, or touch input. Short crossfades and explicit state priority make dodge and hit feedback immediate while preserving readable attack anticipation and recovery.

## Reference Principles

The implementation uses high-level movement principles observed in three comparable games. *Oceanhorn 2* supports camera-relative locomotion, restrained target assistance, short melee chains, and a rapid evasive roll.[1] *Kena: Bridge of Spirits* demonstrates readable attack anticipation, linked combo follow-through, and dodge priority without eliminating attack commitment.[2] *The Pathless* demonstrates strong speed silhouettes, momentum continuity, and visual feedback that remains readable during traversal.[3] No models, animations, textures, audio, user-interface assets, or code from these games will be copied.

## Phase 1 Acceptance Criteria

The Android build must compile from a clean Unity Library. The generated Aster prefab must use the supplied Mixamo mesh, a valid Humanoid avatar, and the supplied albedo and normal textures. The compiled APK must contain the `Aster` material, `Aster_0`, `Aster_1`, and all twelve named clips. Runtime validation must confirm that Aster has a skinned renderer, uses the `Aster` material, remains within the intended character scale, remains at or below the normal movement-speed cap, and exposes every required animation state including the second-jump flip.

## Phase 2 Implementation

Phase 2 replaces the previous cube-like gem and checkpoint props with original low-poly relics generated in the project. The game does not import a third-party asset pack for this phase. This avoids attribution, licensing, visual-cohesion, and APK-size concerns while producing silhouettes designed specifically for the existing floating-island camera.

| Object | Visual design | Feedback and mobile-performance approach |
|---|---|---|
| **Gem** | A six-sided faceted core, inner light, four orbiting shards, and a tilted rune halo. | The root moves slowly while the individual facets communicate the collectible state. The familiar attraction range pulls the gem toward Aster and collection emits an enhanced accent-colored burst. |
| **Checkpoint** | An octagonal two-tier sanctuary dais, tall seven-sided heart crystal, four sentinel shards, and two floating rune halos. | It glows softly while dormant. Activating it increases rune rotation, crystal motion, emission intensity, point-light intensity, scale pulse, collection burst count, and interaction range. |

The relic meshes are shared procedural meshes with low side counts: six or seven sides for crystals, eight sides for the sanctuary, and a 16-by-4 segment rune halo. Gems use seven mesh-renderer objects, and checkpoints use ten mesh-renderer objects plus one non-shadow-casting point light. This small budget is appropriate for the current world population and keeps every important object readable at phone scale.

## Phase 2 Acceptance Criteria

The Android build must compile from a clean Unity Library. Every gem must instantiate the `GemVisual` controller with a faceted core, four shards, and a rune halo. Every checkpoint must instantiate the `CheckpointVisual` controller with its sanctuary heart, four shards, two halos, and aura light. Triggering a checkpoint must invoke the visual activation state. The runtime probe must exercise the actual checkpoint approach, confirm activation, and continue passing the existing movement, combat, and ten-level route checks.

## References

[1]: https://www.youtube.com/watch?v=YRp5_TvU3oI "Oceanhorn 2: Knights of the Lost Realm First Footage"
[2]: https://www.youtube.com/watch?v=TtNkY1rbmZM "Kena: Bridge of Spirits Gameplay Trailer"
[3]: https://www.youtube.com/watch?v=WJvodqvjP-Y "The Pathless State of Play Gameplay Overview"

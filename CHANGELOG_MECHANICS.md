# Krash Kart Mechanics and Code Changelog

## 1. 3-Lap Race Winning System

* **What Changed**: Connected `LapObject.cs`, `ObjectiveCompleteLaps.cs`, `MultiplayerRaceManager.cs`, `KartAgent.cs`, and `GameFlowManager.cs` to establish a 3-lap winning condition across Single Player and Multiplayer modes.
* **Why**: The prototype lacked an active race loop, win condition, and victory scene transition.
* **How**: 
  - `LapObject.cs` detects kart triggers using `GetComponentInParent<ArcadeKart>()`.
  - `ObjectiveCompleteLaps.cs` tracks lap progress (`currentLap`), updating from 1 to 3.
  - When `currentLap >= lapsToComplete` (3), `ObjectiveCompleteLaps` invokes `GameFlowManager.SendMessage("EndGame", true)`, playing victory audio/messages and loading `WinScene.unity`.
* **Why Not Alternatives**: 
  - *Alternative 1 (Time-based race finish)*: Rejected because lap-based completion provides clear competitive milestones for both human players and AI agents.
  - *Alternative 2 (Hardcoding scene load directly inside LapObject)*: Rejected because decoupling scene transitions through `GameFlowManager` preserves standard microgame event flow and prevents breaking UI toast notifications.

## 2. Idle Steering Power Scaling

* **What Changed**: Updated `ArcadeKart.cs` to scale steering power dynamically based on kart forward velocity (`m_SpeedRatio`).
* **Why**: The kart body turned on the spot while completely stationary (idle), breaking realistic kart physics.
* **How**: Steering torque applied to the Rigidbody in `ArcadeKart.cs` is multiplied by the kart's normalized forward velocity ratio, reducing steering torque to zero when stationary.
* **Why Not Alternatives**:
  - *Alternative 1 (Locking steering input keys when speed is zero)*: Rejected because it prevents wheel visual turning animation while waiting at the starting grid.

## 3. Wheel Geometry Mirroring Adapter

* **What Changed**: Added an X-axis scale mirroring toggle (`flipX`) in `KartAnimation.cs` and `KartAnimationNetworked.cs`.
* **Why**: Right-side wheel meshes from custom asset packs (e.g. Stylized Car) used single-sided geometry. Y-180 degree rotation exposed backfaces, causing right-side wheels to render pitch black.
* **How**: `UpdateWheelFromCollider` checks if `flipX` is enabled and sets `wheelTransform.localScale.x = -Mathf.Abs(scale.x)`, cleanly mirroring mesh normals outwards.
* **Why Not Alternatives**:
  - *Alternative 2 (Editing mesh normals in Blender or external 3D software)*: Rejected to avoid modifying source asset files and maintain a pure code-based solution within Unity.

## 4. Cross-Assembly Agent Race Finish Delegate

* **What Changed**: Added static event `GameModeManager.OnAgentFinishedRace` in `KartGame.GameFlow` namespace.
* **Why**: Calling `MultiplayerRaceManager` directly from `KartAgent.cs` caused C# compilation errors (`CS0012` / `CS0234`) due to assembly definition (`asmdef`) boundary restrictions between `KartGame.AI` and Netcode runtime.
* **How**: `KartAgent.cs` triggers `GameModeManager.OnAgentFinishedRace?.Invoke(this)` upon completing 3 laps. `MultiplayerRaceManager` subscribes to this delegate during `OnEnable`.
* **Why Not Alternatives**:
  - *Alternative 1 (Adding Unity.Netcode.Runtime reference to KartGame.AI.asmdef)*: Rejected because AI agent code should remain decoupled from multiplayer networking assemblies for single-player training efficiency.

## 5. Particle VFX Auto-Destruction

* **What Changed**: Added `Destroy(vfx, 2f)` in `SpellProjectile.cs` and `TrapMine.cs`.
* **Why**: Spell impact and mine explosion particle prefabs remained in the active scene hierarchy indefinitely, creating memory clutter and performance degradation over long sessions.
* **How**: Invoked Unity `Destroy` with a 2-second delay immediately upon instantiating impact particle effects.
* **Why Not Alternatives**:
  - *Alternative 1 (Leaving particles unmanaged)*: Rejected due to hierarchy bloat.
  - *Alternative 2 (Complex Object Pooling)*: Deferred until full release optimization phase to keep prototype particle lifecycle simple.

## 6. Unity Gaming Services (UGS) Core Initialization Refactor

* **What Changed**: Refactored `RelayManager.cs` to add an asynchronous `InitializeServicesAsync()` initialization routine.
* **Why**: `RelayService` and `AuthenticationService` calls threw runtime exceptions because `UnityServices.InitializeAsync(options)` was never invoked prior to requesting Relay allocations or anonymous sign-in.
* **How**: `RelayManager.cs` now checks `UnityServices.State`, calls `await UnityServices.InitializeAsync(options)`, signs in anonymously via `AuthenticationService`, and guards both `CreateRelay()` and `JoinRelay()` methods.
* **Why Not Alternatives**:
  - *Alternative 1 (Calling InitializeAsync inline inside CreateRelay only)*: Rejected because `Start()` would still throw when checking authentication state on scene start.
  - *Alternative 2 (Manual Editor setup script)*: Rejected because automated runtime initialization ensures seamless gameplay in standalone builds and ParrelSync clones.

## 7. Modular Kart Attribute Data Schema (KartDataSO)

* **What Changed**: Created `KartDataSO.cs` asset definition and integrated it into `ArcadeKart.cs`. Added `Weight` stat to `ArcadeKart.Stats`. Removed hardcoded player speed scaling in `ArcadeKart.Start()`.
* **Why**: Support diverse kart archetypes (Light/Agile, Medium/Balanced, Heavy/Brawler) with modular ScriptableObject assets instead of hardcoded inspector fields.
* **How**: `ArcadeKart.Awake()` checks if `kartData` is assigned and applies base stats, drift grip, additional steering, and Rigidbody mass from the ScriptableObject.
* **Why Not Alternatives**:
  - *Alternative 1 (Hardcoding stats inside distinct MonoBehaviour subclasses)*: Rejected due to prefab bloat and code duplication. ScriptableObjects allow data-driven tuning without recompiling.

## 8. Environmental Weather Modifier Engine

* **What Changed**: Created `WeatherManager.cs` supporting 6 weather conditions (`DryNormal`, `DryHeat`, `RainLightDrizzle`, `RainHeavyPour`, `SnowVisibleTrack`, `SnowFullSnow`).
* **Why**: Modulate gameplay physics (Acceleration, Braking, Steering, Grip, Top Speed) dynamically depending on track environmental conditions.
* **How**: `ArcadeKart.TickPowerups()` evaluates active `WeatherModifiers` and scales calculated final stats per physics frame.
* **Why Not Alternatives**:
  - *Alternative 1 (Physics Material swap on track mesh)*: Rejected because WheelColliders in this project use raycast-driven custom arcade physics where friction is computed in C#, not Unity PhysX materials.

## 9. Spell Catalog and Draft Pipeline (PowerupDraftManager)

* **What Changed**: Created `PowerupDraftManager.cs` and added `description` / `icon` metadata fields to `BaseSpell.cs`.
* **Why**: Enable draft screen game flow, power-up catalog queries, and dynamic equipping of spells to kart slots at runtime.
* **How**: Implemented `GetAllSpells()`, `GenerateDraftOptions(count)`, and `EquipSpellToKart(kart, spellPrefab, slotIndex)` supporting runtime spell instantiation and slot replacement.
* **Why Not Alternatives**:
  - *Alternative 1 (Hardcoding 4 spells per kart prefab)*: Rejected because draft mechanics require flexible runtime spell assignment.

## 10. Defensive Energy Shield Spell and Procedural Barrier Architecture

* **What Changed**: Created `ShieldSpell.cs`, `KartShield.cs`, `ShieldBubble.cs`, and `ShieldBubble.shader`. Updated `SpellProjectile.cs`, `TrapMine.cs`, and `HexNetworkHelper.cs` to integrate single-hit defensive absorption.
* **Why**: Players lacked counterplay against offensive attacks (hostile projectiles, deployed trap mines, and hex spells). A defensive reaction mechanic was required to reward timing and strategic spell use.
* **How**:
  - `ShieldSpell.cs` inherits from `BaseSpell` and matches GUID `795e6819871cefe41a26adf2d63d837d` for instant zero-configuration slot integration into `KartClassic_Player.prefab` (Slot 3, key K).
  - `KartShield.cs` operates as an authoritative state manager with `NetworkVariable<bool> IsActive` for Netcode multiplayer and local boolean fallback for offline play.
  - `ShieldBubble.cs` generates a procedural 20x28 UV sphere mesh at runtime, dynamically fitted to kart bounding dimensions, with ease-in scaling, breathing pulsation, and break/dissolve animations.
  - `ShieldBubble.shader` implements double-sided Fresnel rim glow rendering across both Universal Render Pipeline (URP) and Built-in pipelines without requiring external 3D assets.
  - `SpellProjectile.cs`, `TrapMine.cs`, and `HexNetworkHelper.cs` check `shield.TryBlockHit()` before applying launch forces, slow debuffs, or hex state transitions. When absorbed, the offensive effect is cancelled, the shield is consumed, and dissolution feedback triggers across all clients.
* **Why Not Alternatives**:
  - *Alternative 1 (Static 3D bubble mesh imported from external software)*: Rejected to eliminate external asset dependencies and ensure pure runtime procedural scalability.
## 11. Prefab Integration and Lifecycle Guarding for KartShield

* **What Changed**:
  - `Assets/Karting/Prefabs/KartClassic_Player.prefab`: Attached `KartShield` component (`fileID: 6284917392817401923`) to root GameObject `4355458286913933503` and registered it in `m_AddedComponents`.
  - `Assets/Karting/Scripts/Spells/KartShield.cs`: Added `IsSpawned` validation in `HasShield`, `ActivateShield`, and `TryBlockHit`. Overrode `NetworkBehaviour.OnDestroy()`.
  - `Assets/Karting/Scripts/Spells/ShieldSpell.cs`: Added parent traversal lookup `kart.GetComponentInParent<KartShield>()` before fallback instantiation.
* **Why**: Eliminate `KeyNotFoundException` caused by Netcode for GameObjects (NGO) runtime lookup failure when invoking ServerRpc on dynamically added `NetworkBehaviour` components.
* **How**:
  - By embedding `KartShield` in `KartClassic_Player.prefab` ahead of spawn time, NGO's `NetworkObject` assigns a valid `NetworkBehaviourId` and registers all ServerRpc/ClientRpc signatures in `NetworkBehaviour.__rpc_name_table` during `InitializeVariables()`.
  - Netcode operations (`ActivateShieldServerRpc`, `BreakShieldClientRpc`, `IsActive.Value`) are strictly guarded with `if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsSpawned)`.
  - If invoked offline or in an unspawned test state, execution seamlessly routes to local routines (`ApplyLocalShieldActivation`, `BreakShieldLocal`) preventing any unhandled Netcode runtime exceptions.
* **Why Not Alternatives**:
  - *Alternative 1 (Pure non-networked MonoBehaviour shield)*: Rejected because multiplayer races require server-authoritative shield status replication and synchronized visual bubble break/burst events across all connected clients.
## 12. Single-Hit Defensive Absorption and Offline Fallback for Spell Projectiles

* **What Changed**:
  - `Assets/Karting/Scripts/Spells/SpellProjectile.cs`: Generalized `Update()`, `DestroyAfterLifetime()`, and `OnTriggerEnter()` to operate in both Netcode server-authoritative multiplayer and single-player/offline modes.
  - `Assets/Karting/Scripts/Spells/ProjectileSpell.cs`: Added single-player fallback instantiation when not running in an active Netcode session.
* **Why**: Ensure projectiles properly trigger `KartShield.TryBlockHit()` to be absorbed, play hit VFX, and despawn upon first impact across both multiplayer and single-player test sessions, while immediately consuming and breaking the shield bubble.
* **How**:
  - Replaced strict `!IsServer` check with `if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer) return;` so offline projectiles execute movement and contact triggers.
  - In `OnTriggerEnter()`, when `shield.TryBlockHit()` returns true, `hasHit` is set to true, hit VFX is spawned (via RPC or local instantiation), the incoming projectile is despawned/destroyed, and the function returns early to block all launch forces.
  - `KartShield.TryBlockHit()` clears `IsActive` (multiplayer) and `isLocalActive` (single-player), stops the expiration coroutine, and triggers `BreakShieldClientRpc` / `BreakShieldLocal`, destroying the visual bubble on impact.
* **Why Not Alternatives**:
  - *Alternative 1 (Multi-hit shield durability counter)*: Rejected to preserve high-stakes spell counterplay where one defensive cast deflects exactly one offensive threat.

## 13. Blink Boost Utility Spell and Procedural Warp Visual Pipeline

* **What Changed**:
  - Created `Assets/Karting/Shaders/BlinkWarp.shader`: Dual-pass URP and Built-in procedural Fresnel and sine-band electric distortion shader.
  - Created `Assets/Karting/Scripts/Spells/BlinkBoostVisuals.cs`: Procedural departure flash, afterimage kart ghost renderer, tapered 3D ribbon warp streak, and expanding arrival shockwave ring.
  - Created `Assets/Karting/Scripts/Spells/BlinkBoostSpell.cs`: Cast handler featuring safe forward sphere-cast collision sweeps, track elevation raycasting, momentum preservation, and post-blink speed boost.
  - Configured `Assets/Karting/Prefabs/KartClassic_Player.prefab`: Bound `BlinkBoostSpell` (`fileID: 7382910482910482019`) to Slot 4 (Hotkey: `L`).
* **Why**: Implement the 5th core launch prototype spell from the game design specification, providing players with high-skill positional warping and corner-skipping capabilities.
* **How**:
  - `ComputeSafeDestination()` performs a `Physics.SphereCast` with a 0.9m radius and 0.5m skin margin against track geometry, clamping the destination if an obstacle or track barrier is detected to prevent wall-glitching or falling out of bounds.
  - Follow-up downward ground raycast snaps the warp destination safely to the track surface elevation.
  - Teleportation updates `Rigidbody.position` directly to preserve physics stability.
  - `ApplyPostBlinkMomentum()` preserves forward velocity, zeroes out unhelpful lateral drift, and adds a transient `BlinkBoostSurge` stat powerup via `kart.AddPowerup()`.
  - Visuals execute dynamically without external textures or 3D models: procedural UV sphere flash, afterimage mesh copy, mathematical ribbon streak, and planar shockwave ring.
* **Why Not Alternatives**:
  - *Alternative 1 (Unchecked transform.position += forward * distance)*: Rejected because karts would phase through track walls and guardrails, breaking game boundaries.
## 14. Phase Shift Defensive Spell and Procedural Hologram Wireframe Pipeline

* **What Changed**:
  - Created `Assets/Karting/Shaders/PhaseShiftWireframe.shader`: Dual-pass procedural wireframe hologram shader rendering triplanar grid lines, silhouette Fresnel glow, and moving vertical scanlines without external textures.
  - Created `Assets/Karting/Scripts/Spells/KartPhaseShift.cs`: Authoritative phased state tracker managing procedural material swaps to the wireframe shader and clean restoration of original materials.
  - Created `Assets/Karting/Scripts/Spells/PhaseShiftSpell.cs`: Cast action triggering a 1.5s intangibility window.
  - Integrated intangible phase evasion checks in `SpellProjectile.cs`, `TrapMine.cs`, and `HexNetworkHelper.cs`.
  - Added `KartPhaseShift` (`fileID: 5183920194810294819`) and `PhaseShiftSpell` (`fileID: 4918204819204810294`) to `KartClassic_Player.prefab`.
  - Updated `SpellSystem.cs` to add a dedicated `spellSlotSpace` triggered by `KeyCode.Space`.
* **Why**: Provide players with a high-skill defensive timing reaction spell that makes the kart temporarily intangible, allowing projectiles, mines, and hexes to pass through harmlessly.
* **How**:
  - When active, `KartPhaseShift` snapshots all renderer material arrays, swaps them to an additive glowing wireframe instance, and sets `IsPhased = true`.
  - In `SpellProjectile.cs`, `TrapMine.cs`, and `HexNetworkHelper.cs`, incoming attacks query `kart.GetComponent<KartPhaseShift>()?.IsPhased`; if true, the impact is bypassed, letting projectiles continue past and mines remain untriggered.
  - Upon timer expiration or despawn, `RestoreOriginalMaterials()` restores the cached material arrays with zero memory or asset leaks.
* **Why Not Alternatives**:
  - *Alternative 1 (Disabling all kart colliders globally)*: Rejected because disabling all colliders indiscriminately causes WheelColliders to lose raycast surface contact, causing the kart to fall through the track.

## 15. Phase Shift Kart-to-Kart Collider Phasing

* **What Changed**:
  - Updated `Assets/Karting/Scripts/Spells/KartPhaseShift.cs` to add `SetKartPhasingCollisions(bool ignore)`.
* **Why**: While Phase Shift rendered the kart intangible to spells, physical kart-to-kart collision was still blocking movement through other karts.
* **How**:
  - Uses `Physics.IgnoreCollision(myCollider, otherCollider, true)` specifically between the caster's colliders and all detected opposing karts in the scene.
  - Preserves wheel and chassis interaction with the track, environment, and ground layers while completely eliminating chassis collision with opponent vehicles.
  - On phase completion or despawn, `SetKartPhasingCollisions(false)` restores normal physical contact across all tracked colliders.
* **Why Not Alternatives**:
  - *Alternative 1 (Changing LayerMask to a non-colliding layer)*: Rejected because altering GameObject layers disrupts camera raycasts, minimap filters, and network tag lookups. `Physics.IgnoreCollision` operates specifically on kart-to-kart pairs.

## 16. Gravity Pull Spell and Procedural Inward Neon Vector VFX

* **What Changed**:
  - Created `Assets/Karting/Shaders/GravityPullAdditive.shader`: URP-native additive unlit shader (`UniversalPipeline`, `UniversalForward`) for clean, transparent neon rendering without pipeline errors.
  - Created `Assets/Karting/Scripts/Spells/GravityPullVisuals.cs`: Procedural VFX generator rendering 4 contracting concentric neon rings, 6 converging radial vector rays, and an energetic singularity pulse core at the kart base.
  - Created `Assets/Karting/Scripts/Spells/GravityPullSpell.cs`: Offensive area-of-effect crowd control spell pulling enemy karts within a 16m radius inward over 2.5s with distance falloff and immunity checks.
  - Updated `Assets/Karting/Scripts/Spells/SpellSystem.cs`: Added dedicated `spellSlotTab` triggered via `KeyCode.Tab` with runtime cooldown fill support.
  - Updated `Assets/Karting/Prefabs/KartClassic_Player.prefab`: Added `GravityPullSpell` component (`fileID: 3829104829104820192`) and wired it to `spellSlotTab`.
  - Removed obsolete `GravityPullDisk.shader` which previously caused a solid magenta quad error in URP.
* **Why**: Replace the flat occluding distortion quad with clean, readable arcade visuals designed via `nvidia/nemotron-3.5-super-vl:free` while eliminating URP pink shader failures.
* **How**:
  - `GravityPullVisuals.cs` instantiates ground-aligned `LineRenderer` loops cycling from 16m down to 0.6m in staggered inward waves, producing continuous visual suction toward the caster.
  - 6 rotating radial streamer lines point toward the center, visually communicating gravitational pull direction to all players on track.
  - Compact singularity core pulses smoothly under the kart chassis.
  - `GravityPullSpell.cs` iterates via `Physics.OverlapSphereNonAlloc` over 2.5 seconds, calculating inward direction vectors toward the caster with vertical damping (`inwardDirection.y *= verticalDamping`) to keep target karts grounded.
  - Pull force uses `ForceMode.Acceleration` scaled by smooth sine pulse and distance falloff (`1 - dist / pullRadius`).
  - Immunities enforced: targets with active `KartPhaseShift.IsPhased` or `KartShield.HasShield` are unaffected.
  - Visual resources and instantiated game objects are tracked and destroyed automatically upon completion.
* **Why Not Alternatives**:
  - *Alternative 1 (2D Space Distortion Mesh Quad)*: Rejected because full-screen textured distortion quads frequently cause depth sorting conflicts, track road occlusion, and pipeline shader mismatches in URP.
  - *Alternative 2 (Instantaneous inward teleport)*: Rejected because physics-based gradual inward acceleration allows affected players to counter-steer and maintain dynamic control.

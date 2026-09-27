# RESEARCH_ML Mechanics and Code Changelog

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

## 10. Offline Testing Scene Pipeline and Multi-System Defensive Guards

* **What Changed**:
  - `QuickDriveSetup.cs`: Automated generator for `Assets/Karting/Scenes/QuickDrive.unity` using `OvalTrack_Training`, `KartClassic_Player`, and `KartClassic_MLAgent 1`. Automates stripping of all netcode/multiplayer components in strict dependency order, sets dynamic `Rigidbody` physics, binds Cinemachine camera follow/lookAt targets, and links track checkpoint colliders to `KartAgent.Colliders`.
  - `AudioUtility.cs`: Added null guards for `AudioManager` and `audioMixer` in `CreateSFX`, `GetAudioGroup`, `SetMasterVolume`, and `GetMasterVolume`.
  - `KartAgent.cs`: Added auto-detection of `Agent Checkpoints` in `Awake()`, guarded `CollectObservations()` to maintain a consistent 12-vector observation space, and guarded `OnActionReceived()` against unassigned checkpoint colliders.
  - `ArcadeKart.cs`: Added `!Rigidbody.isKinematic` checks in `GroundAirbourne()` and `MoveVehicle()` orientation logic.
  - `ManualPlayerPerformanceLogger.cs`: Moved `SetupLoggingDirectory()` call to `Awake()` and added fallback directory creation guards in `SaveResults()`.
  - `HexSpell.cs`: Added an offline local target fallback when `HexNetworkHelper` is absent.
* **Why**: Provide instant offline testing of the player kart and ML Agent without multiplayer networking overhead, while eliminating console errors and warnings.
* **How**:
  - `QuickDriveSetup.cs` uses `SerializedObject` to configure Cinemachine and ML-Agent properties across assembly boundaries.
  - Netcode components are stripped in reverse dependency order (`Custom NetworkBehaviours` -> `NetworkRigidbody` -> `NetworkTransform` -> `NetworkObject`), ensuring clean destruction without engine exceptions.
  - Checkpoint colliders are automatically assigned and tagged `LaneDivider` to support performance logging.
* **Why Not Alternatives**:
  - *Alternative 1 (Running a local Netcode host in test scenes)*: Rejected because local host networking introduces latency, requires NetworkManager overhead, and complicates fast iteration.
  - *Alternative 2 (Maintaining static pre-baked test scenes)*: Rejected because static test scenes break when prefab hierarchies or component dependencies change. Auto-generation guarantees an up-to-date test environment.

## 11. True Arcade Drifting Physics Overhaul

* **What Changed**: Completely rewrote the drifting logic in `ArcadeKart.cs` to remove artificial inward pull and complex velocity lerping, replacing them with a simplified angular rotation and reduced grip model.
* **Why**: The previous implementation fought the player's steering inputs and felt clunky. We needed a satisfying, Mario Kart-style true drift that relies on natural momentum side-slip.
* **How**: 
  - Drifting now instantly reduces `m_CurrentGrip` to `DriftGrip`, causing a natural lateral slide based on preserved momentum.
  - Added a snappy entry hop (`Rigidbody.AddForce(Vector3.up * 2.0f)`) when drift initiates.
  - Removed artificial apex pulling; turning power is now cleanly defined by `turnInput * (baseStats.Steer + DriftAdditionalSteer)`.
* **Why Not Alternatives**:
  - *Alternative 1 (Velocity lerping and artificial inward forces)*: Previously attempted but rejected because it made cornering feel "on-rails" and unpredictable.

## 12. Decoupled Modular Drifting Physics Engine (KartDrift.cs)

* **What Changed**: Separated all drifting handling, physics, VFX management, and mini-turbo boost logic out of `ArcadeKart.cs` into a dedicated component `KartDrift.cs`. `ArcadeKart.cs` now holds a `KartDrift DriftController` reference and delegates `IsDrifting`, `WantsToDrift`, `EffectiveGrip()`, `SteeringPower()`, and `ApplyBoost()` to `KartDrift`.
* **Why**: Enforces single-responsibility architecture, eliminates monolithic bloat in `ArcadeKart.cs`, and delivers satisfying arcade drift physics inspired by Mario Kart / CTR (entry hop impulse, smooth counter-steering yaw, 3-tier mini-turbo boost system, and color-coded particle sparks / tire trails).
* **How**:
  - Created `KartDrift.cs` with configurable entry speed/steer thresholds, single-frame entry hop impulse (`HopVelocityChange`), asymmetric steer-into-turn vs counter-steer dampening, and 3-stage mini-turbo charge timer (Tier 1 blue, Tier 2 yellow, Tier 3 magenta).
  - Wired `KartDrift.Tick()` directly into `ArcadeKart.FixedUpdate()` after ground contact calculation.
  - Exposed `DriftSteering`, `DriftDirection`, and `SetVFXActive()` on `KartDrift` for visual body lean and remote network replication compatibility.
* **Why Not Alternatives**:
  - *Alternative 1 (Keeping drift code inside ArcadeKart.cs)*: Rejected because `ArcadeKart.cs` was exceeding 780 lines with entangled responsibilities (wheels, powerups, hex spells, weather, drifting, audio, network synchronization).
  - *Alternative 2 (Physics Material friction curve manipulation alone)*: Rejected because Unity WheelCollider sideways friction curves do not produce arcade-style snappy counter-steering or controlled hop drift transitions.

## 13. Project Versioning and Safe Recovery Tooling (kart-git.ps1 Refactor)

* **What Changed**: Overhauled `kart-git.ps1` with PowerShell parameter binding (`param([string]$Command, [string]$Arg1)`), uncommitted change collision detection (`Safe-CheckoutRestoreBranch`), and explicit hard reset command (`Safe-DiscardAllUncommittedChanges` / `reset-hard`).
* **Why**: Developers attempting to revert to previous snapshots (`prototype-v1.0`, baseline tags, or prior commit saves) encountered silent failures when local uncommitted changes conflicted with historical commits, leaving the workspace in an inconsistent, broken state with false-positive success reporting.
* **How**:
  - Bound CLI parameters to bypass the interactive menu when arguments are passed (e.g. `.\kart-git.ps1 revert-to <tag>`, `.\kart-git.ps1 status`, `.\kart-git.ps1 save "message"`).
  - Inspects `git status --porcelain` before initiating checkouts. Prompts user to stash (`[S]`), discard (`[D]`), or abort (`[C]`).
  - Added dedicated hard-reset option to discard local experimental modifications and restore clean HEAD.
* **Why Not Alternatives**:
  - *Alternative 1 (Relying solely on external GUI clients like GitHub Desktop or GitKraken)*: Rejected because a self-contained, project-specific PowerShell automation script provides rapid, one-command operations tailored to Unity's `.meta` and lock-file requirements without external client dependencies.

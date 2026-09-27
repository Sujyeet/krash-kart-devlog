# RESEARCH_ML Root Cause & Resolution Log

## 1. Single-Player Lap Trigger Ignition Failure

* **Issue Description**: Crossing `StartFinishLine` in single-player or local editor test mode did not register laps or print trigger logs.
* **Root Cause Analysis**: `LapObject.cs` contained the check:
  `if (netObj != null && !netObj.IsOwner) return;`
  In single-player or unspawned editor test mode, `netObj.IsSpawned` is `false`, causing `netObj.IsOwner` to evaluate to `false`. The method returned early on the first line, blocking all lap processing.
* **Resolution Strategy**: Updated the ownership filter in `LapObject.cs`:
  `if (netObj != null && netObj.IsSpawned && !netObj.IsOwner) return;`
* **Verification Method**: Confirmed that unspawned local karts trigger `StartFinishLine` logs and advance lap counters during single-player testing.

## 2. Premature Lap Trigger at Race Countdown

* **Issue Description**: Laps were incrementing to 1 immediately at 0.0 seconds into the race before the player drove forward.
* **Root Cause Analysis**: Karts were instantiated at scene origin directly inside the `StartFinishLine` trigger collider volume before `NetworkedArcadeKart` relocated them to starting grid positions 3 meters behind the line.
* **Resolution Strategy**: Added a level load timer guard in `LapObject.cs`:
  `if (Time.timeSinceLevelLoad < 1.5f) return;`
* **Verification Method**: Verified that initial kart spawn placement during countdown is ignored, and lap 1 only starts when the player accelerates forward across the finish line.

## 3. UI NullReferenceException on Lap Completion

* **Issue Description**: Completing laps threw a `NullReferenceException` in `Objective.cs` and `ObjectiveCompleteLaps.cs`, crashing the script before `WinScene` could load.
* **Root Cause Analysis**: 
  - `Objective.cs` directly called `TimeDisplay.OnUpdateLap()` without checking if `TimeDisplay` was subscribed to by active UI elements.
  - `Objective.CompleteObjective` called `m_ObjectiveHUDManger.UnregisterObjective(this)` without verifying if `m_ObjectiveHUDManger` existed in the scene.
* **Resolution Strategy**:
  - Replaced direct action calls with null-conditional invocations: `TimeDisplay.OnUpdateLap?.Invoke()`.
  - Added null guards in `Objective.cs` for HUD manager references: `if (m_ObjectiveHUDManger != null)`.
  - Reordered `ObjectiveCompleteLaps.cs` to trigger `GameFlowManager.SendMessage("EndGame", true)` before HUD cleanup.
* **Verification Method**: Tested 3-lap completions in scene testing; verified zero console exceptions and clean automatic transition to `WinScene.unity`.

## 4. Inactive GameObject Coroutine Exception

* **Issue Description**: `DisplayMessage.cs` threw `Coroutine couldn't be started because the game object 'WinGameMessage' is inactive!`.
* **Root Cause Analysis**: `Display()` attempted to launch a timing coroutine on a disabled GameObject instance (`WinGameMessage`).
* **Resolution Strategy**: Updated `DisplayMessage.cs` to check `gameObject.activeInHierarchy` and call `gameObject.SetActive(true)` prior to invoking `StartCoroutine`.
* **Verification Method**: Confirmed victory notification messages display on screen without coroutine initialization errors.

## 5. Relay Re-Authentication Exception on Scene Reload

* **Issue Description**: Reloading the scene or re-hosting threw `[Relay] Failed to initialize: Invalid state for this operation. The player is already signed in.`.
* **Root Cause Analysis**: `RelayManager.cs` called `AuthenticationService.Instance.SignInAnonymouslyAsync()` unconditionally on `Start()`.
* **Resolution Strategy**: Wrapped authentication with `if (!AuthenticationService.Instance.IsSignedIn)` check.
* **Verification Method**: Verified seamless hosting and scene reloads without authentication state exceptions.

## 6. InputStruct Null Check Compiler Error

* **Issue Description**: `KartAnimation.cs` failed compilation with `error CS0019: Operator '==' cannot be applied to operands of type 'InputData' and '<null>'`.
* **Root Cause Analysis**: `InputData` is a value-type C# struct on `ArcadeKart`, making `kartController.Input == null` invalid syntax.
* **Resolution Strategy**: Updated line 56 in `KartAnimation.cs` to check `if (kartController == null) return;`.
* **Verification Method**: Verified error-free compilation in Unity Editor.

## 7. Relay Core Registry Uninitialized Failure on Multiplayer Host

* **Issue Description**: Clicking "Host Multiplayer" threw runtime exceptions: `[Relay] Failed to initialize: Singleton is not initialized. Please call UnityServices.InitializeAsync() to initialize.` followed by `[Relay] Failed to create: Attempting to call Relay Services requires initializing Core Registry. Call 'UnityServices.InitializeAsync' first!`.
* **Root Cause Analysis**: `RelayManager.cs` initialized `InitializationOptions` but omitted the mandatory `await UnityServices.InitializeAsync(options)` call before attempting `AuthenticationService.Instance.SignInAnonymouslyAsync()` and `RelayService.Instance.CreateAllocationAsync(4)`.
* **Resolution Strategy**: 
  - Restructured service initialization in `RelayManager.cs` into an async `InitializeServicesAsync()` method that explicitly calls `await UnityServices.InitializeAsync(options)`.
  - Added re-entrancy and state checks (`UnityServices.State == ServicesInitializationState.Initialized`).
  - Guarded `CreateRelay()` and `JoinRelay()` with `await InitializeServicesAsync()` before executing allocation network calls.
* **Verification Method**: Verified code flow so `UnityServices` initializes prior to authentication and Relay allocation requests.

## 8. Hot-Loop Component Query Overhead in ArcadeKart

* **Issue Description**: `GetComponent<NetworkObject>()` and `GetComponent<NetworkedKartAnimState>()` were invoked every frame in `Update()` and every tick in `FixedUpdate()`, producing unnecessary CPU overhead.
* **Root Cause Analysis**: Missing cached member variables for network components in `ArcadeKart.cs`.
* **Resolution Strategy**: Cached `m_CachedNetObj` and `m_CachedAnimState` during `Awake()`, eliminating all hot-loop component queries.
* **Verification Method**: Verified zero per-frame `GetComponent` invocations during gameplay profile checks.

## 9. Jump Landing VFX Lifetime Leak

* **Issue Description**: Every kart landing instantiated `JumpVFX` without lifetime destruction, accumulating orphan GameObjects in the scene hierarchy.
* **Root Cause Analysis**: `Instantiate(JumpVFX, transform.position, Quaternion.identity)` had no accompanying `Destroy` call.
* **Resolution Strategy**: Added `Destroy(jumpInstance, 3f)` immediately following instantiation.
* **Verification Method**: Confirmed that jump landing particle instances are automatically cleaned up 3 seconds after spawning.

## 10. Static Delegate Memory Leak Across Domain Reloads

* **Issue Description**: `GameModeManager.OnAgentFinishedRace` static action delegate accumulated stale references across scene and domain reloads.
* **Root Cause Analysis**: Static event delegates in Unity survive scene transitions and domain reloads unless explicitly reset.
* **Resolution Strategy**: Added `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` in `GameModeManager.cs` to clear delegates automatically.
* **Verification Method**: Verified delegate is null on fresh run initialization.

## 11. Race Countdown Timing Bypass

* **Issue Description**: Karts were able to move immediately upon level load while the race countdown animation was still playing.
* **Root Cause Analysis**: `GameFlowManager.CountdownThenStartRaceRoutine` had `yield return new WaitForSeconds(0f)`.
* **Resolution Strategy**: Synchronized race start routine to wait for `raceCountdownTrigger.duration` before invoking `StartRace()`.
* **Verification Method**: Confirmed karts remain locked with `SetCanMove(false)` until countdown concludes.

## 12. ML-Agents Vector Observation Truncation Warning Flood

* **Issue Description**: Unity Console flooded with hundreds of warnings per second: `More observations (13) made than vector observation size (12). The observations will be truncated.`
* **Root Cause Analysis**: `KartAgent.CollectObservations` was pushing 13 floats (including an extra `IsOnStraightSegment` observation) into a VectorSensor expecting 12 floats matching the trained neural network model.
* **Resolution Strategy**: Removed the unmodeled 13th observation to align with the 12-dimension trained neural network input schema.
* **Verification Method**: Confirmed console warning flood ceased completely.

## 13. Suspension Bottoming Out and Speedbreaker High-Centering

* **Issue Description**: Karts felt excessively fast yet got high-centered and stuck in the middle of track speedbumps and curbs.
* **Root Cause Analysis**: 
  - `Rigidbody.mass` was set to 1000kg in `ArcadeKart.Awake()`, overpowering the 30,000 N/m suspension springs and 12x gravity multiplier (`1000 * 9.81 * 12 = 117,720 N`).
  - `GroundAirbourne()` triggered downward airborne gravity at `AirPercent >= 0.25f` (slamming the chassis down whenever a single wheel lifted over a speedbump).
* **Resolution Strategy**:
  - Calibrated default `Weight` to 250kg matching the prefab's suspension spring rating.
  - Adjusted `GroundAirbourne` threshold to `AirPercent >= 0.75f` (true jumps only).
  - Balanced base `TopSpeed` to 13.5 and `Acceleration` to 4.5.
* **Verification Method**: Verified karts smoothly drive over speedbumps and curbs without beaching or bottoming out.

## 14. Premature Win Triggered by AI Bots Crossing Lap Triggers

* **Issue Description**: The game ended prematurely in under a minute with "You Won" even when the human player had not completed the required laps or sat at the starting line.
* **Root Cause Analysis**: `LapObject.cs` triggered `Objective.OnUnregisterPickup` whenever ANY kart crossed the trigger collider. AI bots (`KartAgent`) racing around the track were continuously triggering the human player's shared lap objective, accumulating lap counts on behalf of the player.
* **Resolution Strategy**:
  - Added an explicit AI agent filter in `LapObject.cs`: `if (kart.GetComponent("KartAgent") != null) return;`.
  - Added `lapsToComplete = Mathf.Max(1, lapsToComplete)` guard and synchronized `MultiplayerRaceManager.lapsToComplete` in `ObjectiveCompleteLaps.cs`.
* **Verification Method**: Verified that AI bot line crossings are ignored by the human player's lap objective, and the race only finishes when the human player actually completes the configured laps.

## 15. Cross-Assembly Compilation Error in LapObject

* **Issue Description**: C# compilation failed with `error CS0234: The type or namespace name 'AI' does not exist in the namespace 'KartGame'`.
* **Root Cause Analysis**: `LapObject.cs` resides in `KartGame.asmdef`, which does not have a compile-time assembly reference to `KartGame.AI.asmdef`.
* **Resolution Strategy**: Replaced direct generic type query with decoupled string-based component lookup `kart.GetComponent("KartAgent") != null`.
* **Verification Method**: Verified zero compiler errors via Unity MCP and clean assembly build.

## 16. Console Exception Flooding in Offline Quick Drive Testing Scene

* **Issue Description**: Entering Play Mode in the generated `QuickDrive.unity` testing scene flooded the console with multiple recurring exceptions and warnings:
  1. `NullReferenceException` in `AudioUtility.SetMasterVolume` (blocking `GameFlowManager.Start()` race loop).
  2. Continuous `UnassignedReferenceException` in `KartAgent.OnActionReceived` (`Colliders` unassigned).
  3. ML-Agents observation padding warning: `Fewer observations (1) made than vector observation size (12)`.
  4. PhysX error: `Setting angular velocity of a kinematic body is not supported` in `ArcadeKart.MoveVehicle` / `GroundAirbourne`.
  5. `ManualPlayerPerformanceLogger`: `No GameObjects found with 'LaneDivider' tag`.
  6. `ArgumentNullException: Value cannot be null (Parameter: path1)` in `ManualPlayerPerformanceLogger.SaveResults`.
  7. `HexSpell: Missing HexNetworkHelper on this kart`.
* **Root Cause Analysis**:
  1. `AudioUtility.SetMasterVolume` unconditionally accessed `m_AudioManager.audioMixer` without null checking when no `AudioManager` was present in the scene.
  2. The starter track prefab (`OvalTrack.prefab`) lacked checkpoint colliders, leaving `KartAgent.Colliders` null. In `CollectObservations()`, missing colliders triggered an early exit after only 1 observation instead of 12, causing observation padding warnings. In `OnActionReceived()`, indexing `Colliders` threw exceptions every physics frame.
  3. Player and Agent kart prefabs carried multiplayer network components (`NetworkObject`, `NetworkTransform`, `NetworkRigidbody`, `NetworkedArcadeKart`) that defaulted or forced `rb.isKinematic = true` when Netcode was inactive, triggering PhysX errors when `ArcadeKart` applied velocities.
  4. No track colliders or lane dividers were tagged `LaneDivider` in the generated scene.
  5. `ManualPlayerPerformanceLogger` initialized `actualLogPath` in `Start()`, but `OnDisable()` called `SetFinished()` during scene unload/stop before or without a valid path, passing null to `Path.Combine`.
  6. `HexSpell.OnCast` lacked an offline single-player fallback for when `HexNetworkHelper` was stripped.
* **Resolution Strategy**:
  1. **Audio Guarding**: Added null checks for `m_AudioManager` and `m_AudioManager.audioMixer` across all methods in `AudioUtility.cs`, and ensured an `AudioManager` object is spawned by `QuickDriveSetup.cs`.
  2. **Track & Checkpoints**: Updated `QuickDriveSetup.cs` to use `OvalTrack_Training.prefab`, automatically extract its 10 `Agent Checkpoints` colliders, tag them `LaneDivider`, and assign them to `KartAgent.Colliders`.
  3. **Agent Robustness**: In `KartAgent.cs`, added auto-detection of `Agent Checkpoints` in `Awake()`, guarded `CollectObservations()` to always emit all 12 vector observations even if checkpoints are absent, and guarded `OnActionReceived()`.
  4. **Kinematic & Netcode Stripping**: Updated `PrepareKartForOffline()` in `QuickDriveSetup.cs` to strip Netcode components in reverse dependency order (`Custom NetworkBehaviours` -> `NetworkRigidbody` -> `NetworkTransform` -> `NetworkObject`), and explicitly set `rb.isKinematic = false` and `useGravity = true`.
  5. **Kinematic Velocity Guards**: Guarded `ArcadeKart.GroundAirbourne` and `ArcadeKart.MoveVehicle` angular orientation with `!Rigidbody.isKinematic`.
  6. **Logger Path Initialization**: Moved `SetupLoggingDirectory()` invocation to `Awake()` in `ManualPlayerPerformanceLogger.cs` and added a null fallback check in `SaveResults()`.
  7. **Spell Offline Fallback**: Added a local target query fallback in `HexSpell.cs` for offline play when `HexNetworkHelper` is null.
* **Verification Method**: Generated a fresh `QuickDrive.unity` scene via `Krash Kart/Quick Drive Setup`, executed Play Mode in Unity Editor, allowed the race loop to start and run, and verified through Unity MCP console logs that zero errors and zero unassigned reference warnings are produced.

## 17. Drift Mechanic "turnInputAbs" Compilation Error

* **Issue Description**: Injecting the original true drift mechanics removed the `float turnInputAbs = Mathf.Abs(turnInput);` declaration in `ArcadeKart.cs`, causing `error CS0103: The name 'turnInputAbs' does not exist in the current context`.
* **Root Cause Analysis**: The `multi_replace_file_content` edit inadvertently swallowed the variable declaration during a chunk replacement that updated the `m_DriftTurningPower` logic below it.
* **Resolution Strategy**: Restored the `float turnInputAbs = Mathf.Abs(turnInput);` declaration exactly where it was removed.
* **Verification Method**: Verified error-free compilation and successful entry into Unity Play Mode.

## 18. Read-Only Property Assignment and Duplicate Compilation in KartDrift Separation

* **Issue Description**: C# compilation failed with `error CS0200: Property or indexer 'ArcadeKart.WantsToDrift' cannot be assigned to -- it is read only` at lines 333 and 361 in `ArcadeKart.cs`, alongside warning `CSC : warning CS2002: Source file 'KartDrift.cs' specified multiple times`.
* **Root Cause Analysis**:
  1. `WantsToDrift` in `ArcadeKart.cs` was refactored into a read-only computed property delegating to `DriftController.WantsToDrift`, but legacy code in `GatherInputs()` was still attempting to write `WantsToDrift = false;` and `WantsToDrift = Input.Brake && ...`.
  2. `KartDrift.cs` was registered twice because Unity automatically discovered the new file and inserted it into `KartGame.csproj` while an explicit `<Compile>` line had also been manually appended.
* **Resolution Strategy**:
  1. Removed obsolete assignments to `WantsToDrift` in `ArcadeKart.GatherInputs()`, allowing `KartDrift` to autonomously determine drift eligibility based on velocity and steering thresholds.
  2. Removed the duplicate `<Compile Include="Assets\Karting\Scripts\KartSystems\KartDrift.cs" />` entry from `KartGame.csproj`.
* **Verification Method**: Executed `dotnet build "ML TEST.sln"`. Verified 0 errors and a clean build.

## 19. "kart-git.ps1" Silent Revert Failure and Working Tree Desynchronization

* **Issue Description**: Running `.\kart-git.ps1 revert-to <tag>` or using the interactive menu failed to revert the project to previous snapshots or saves. The script reported successful restoration, but the project remained on the modified branch and missing prefab references (`KartClassic_Player.prefab`) left the project in a broken state.
* **Root Cause Analysis**:
  1. `kart-git.ps1` lacked a PowerShell `param(...)` block, causing all CLI argument invocations (`save`, `revert-to`, `status`) to be silently ignored and always loading the interactive menu.
  2. Inside `Menu-Revert` and `Menu-History`, Git commands piped errors to null: `git checkout -b $restoreBranch $target 2>$null | Out-Null`. Because the working tree contained unstaged/dirty files that conflicted with the target commit, Git aborted the checkout. Because stderr was silenced, the script falsely displayed a green "Restored" message while leaving the working copy untouched.
  3. `Assets/Karting/Prefabs/KartClassic/KartClassic_Player.prefab` and its `.meta` file were inadvertently deleted in the working directory during experimental file movement, breaking prefab links in test scenes.
* **Resolution Strategy**:
  1. Restored `KartClassic_Player.prefab` and `KartClassic_Player.prefab.meta` directly from Git HEAD (`git checkout HEAD -- Assets/Karting/Prefabs/KartClassic/KartClassic_Player.prefab*`).
  2. Refactored `kart-git.ps1` with robust parameter handling (`param([string]$Command, [string]$Arg1)`), allowing both direct CLI usage and interactive menu execution.
  3. Implemented `Safe-CheckoutRestoreBranch`: detects dirty working trees before attempting any branch creation or checkout. Provides interactive choices to stash (`[S]`), cleanly discard (`[D]`), or cancel (`[C]`). Removed error suppression so Git failure messages are surfaced to the developer.
  4. Added `Safe-DiscardAllUncommittedChanges` command (`.\kart-git.ps1 reset-hard` or option `[4]` in the menu) with an explicit confirmation safeguard.
* **Verification Method**:
  1. Restored `KartClassic_Player.prefab` and verified scene references in Unity.
  2. Verified `.\kart-git.ps1 status` CLI functionality.
  3. Ran Unity MCP `read_console` and confirmed 0 compilation errors and 0 runtime exceptions.

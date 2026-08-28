# Krash Kart Root Cause & Resolution Log

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

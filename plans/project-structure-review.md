# Project Structure Review — ML TEST (Unity 2022.3.62f2 Kart + ML-Agents)

Review of folder structure, version control setup, packages, and asset organization.
Severity legend: 🔴 Critical · 🟠 High · 🟡 Medium · 🟢 Minor / OK

---

## 🔴 Critical Red Flags

### 1. Two version control systems running in parallel (Git + Plastic SCM)
Evidence: `.plastic/` folder, `ignore.conf`, `com.unity.collab-proxy` package, plus [.gitignore](../.gitignore) and [kart-git.ps1](../kart-git.ps1).
- Risk: both tools fight over file tracking; meta-file churn, double merge conflicts, corrupted history.
- Fix: pick ONE. If Git: remove Plastic workspace metadata (`.plastic/`, `ignore.conf`) and the collab-proxy package. If Plastic: drop the Git repo.

### 2. Duplicate ML model in two differently-named folders
- [`Assets/ML AGENT/ArcadeDriver.onnx`](../Assets/ML%20AGENT/ArcadeDriver.onnx)
- [`Assets/ML-Agents/ArcadeDriver.onnx`](../Assets/ML-Agents/ArcadeDriver.onnx)

Same-named ONNX brain in two places. Risks: scenes/prefabs silently referencing the stale copy, doubled build size, confusion during training iterations.
- Fix: keep one canonical location (e.g. `Assets/ML-Agents/Models/`), delete the other, and re-link any `BehaviorParameters` references.

### 3. All custom code buried inside the template's folder
Your own code (`Multiplayer/`, `Spells/`, `AI/`, `GameFlow/`) lives inside [`Assets/Karting/Scripts/`](../Assets/Karting/Scripts) — the Unity Karting Microgame's directory, mixed with template sample code.
Meanwhile [`Assets/Scripts/`](../Assets/Scripts) and [`Assets/Multiplayer Scripts/`](../Assets/Multiplayer%20Scripts) are **empty folders with orphaned .meta files** — evidence of an abandoned reorganization.
- Risk: impossible to tell your code from sample code; template cleanup/upgrades become destructive; empty folders confuse collaborators.
- Fix: delete the empty folders (+ their .meta), then migrate custom code to a clean root like `Assets/_Project/Scripts/{Runtime,Editor}`.

### 4. No assembly definitions (.asmdef) anywhere
Zero asmdefs found. Everything compiles into Assembly-CSharp, including editor scripts spread across three Editor folders (`Assets/Editor`, `Karting/Scripts/Editor`, `Karting/Scripts/AI/Editor`).
- Risk: full-recompile on every script change (slow iteration with ML training loops), no compile isolation, editor code can accidentally reference runtime types and vice versa.
- Fix: add at minimum `Game.Runtime.asmdef` and `Game.Editor.asmdef`; later split AI/Multiplayer.

---

## 🟠 High

### 5. Training artifacts committed under Assets
[`Assets/ML-Agents/Timers/*.json`](../Assets/ML-Agents/Timers) are auto-regenerated every training run → constant repo churn/noise.
- Fix: delete from tracking, add `Assets/ML-Agents/Timers/` to .gitignore.

### 6. Spaces in asset folder names
`ML AGENT`, `Multiplayer Scripts`, `Road props for games`, `Stylized Car`.
- Risk: breaks/quadruples-escapes CLI workflows you already use (ML-Agents python commands, PowerShell scripts, CI, batch builds).
- Fix: rename to PascalCase-no-spaces (`RoadPropsForGames`). Do renames inside Unity so .meta files stay attached.

### 7. AI-tool artifact folders not gitignored
`.agents/`, `.codex/`, `.uploads/` exist at root but are absent from [.gitignore](../.gitignore).
- Fix: add them (or move tool state outside the repo).

### 8. Loose files at Assets root
[`TrackWidthMeasurer.cs`](../Assets/TrackWidthMeasurer.cs), `nerf_dartbullet.glb`, `Screenshot.png`, `link.xml`, `DefaultNetworkPrefabs.asset`.
- Fix: move into organized homes (`_Project/Scripts/Track/`, `_Project/Art/`, etc.). Keep `link.xml` where it is or document why it exists (IL2CPP stripping protection).

### 9. ML config with wrong extension + spaces
[`config/ppo/simple oval yaml.txt`](../config/ppo/simple%20oval%20yaml.txt) — valid YAML content but `.txt` extension and spaces in name; `mlagents-learn` won't accept it as-is.
- Fix: rename to `SimpleOval.yaml`.

### 10. Five kart controller implementations coexisting
`ArcadeKart`, `ArcadeKartP`, `NetworkedArcadeKart`, `NetworkedKartController`, `OwnerAuthoritativeKart` all live side-by-side.
- Risk: editing the wrong one; divergent physics behavior between single-player and networked karts.
- Fix: decide the canonical controller(s), archive/delete the rest into an `_Attic` folder.

---

## 🟡 Medium

11. **Package bloat** ([Packages/manifest.json](../Packages/manifest.json)): four IDE plugins (vscode, visualstudio, rider, cursor), Visual Scripting, test-framework installed but no `Tests/` folder exists. Barracuda 3.0.0 is legacy (required by ML-Agents 2.0.1 — fine, but plan for Sentis migration eventually).
12. **Documentation sprawl**: 7 markdown docs at project root (`IDEA.md`, `GAME_DEVELOPMENT_BLUEPRINT.md`, `CHANGELOG_MECHANICS.md`, `GIT_GUIDE.md`, `PROTOTYPE_DOCUMENTATION.md`, `ROOT_CAUSE_RESOLUTIONS.md`, `README.md`) plus `docs/concepts/`. Consolidate into `docs/`.
13. **CSV run logs at root**: `AI_Manual_Player_Run_*.csv` — already gitignored via `*.csv`, but cluttering root. Note: global `*.csv` ignore could accidentally exclude legit data assets later; consider scoping it.
14. **Unused template weight**: [`Assets/Karting/Tutorials/`](../Assets/Karting/Tutorials) tutorial framework ships with the microgame; strip if unused.
15. **Build scene 0 is ResearchLauncher** ([EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset)) — confirm this should ship in player builds vs. being editor-only tooling.

## 🟢 Fine as-is

- [.gitignore](../.gitignore) correctly covers Library/Temp/Obj/Builds/Logs/UserSettings, csproj/sln.
- [`Assets/Resources/`](../Assets/Resources) nearly empty (good — Resources abuse avoided).
- Scenes well organized under `Karting/Scenes/{MLTraining,GameplayGyms}`.
- Unity version pinned (2022.3.62f2 LTS); URP 14 matches.
- `TemplateEditorDetection.cs` properly wrapped in `#if UNITY_EDITOR`.

---

## Recommended action order

1. Commit current work; resolve Git-vs-Plastic decision (Critical #1).
2. Delete empty `Assets/Scripts/` + `Assets/Multiplayer Scripts/` folders and duplicate ONNX (#2, #3).
3. Update .gitignore: Timers/, `.agents/`, `.codex/`, `.uploads/` (#5, #7).
4. Rename space-containing folders and the `.txt` YAML inside Unity (#6, #9).
5. Create `_Project/` structure and migrate custom code out of `Karting/` (#3, #8).
6. Add Runtime/Editor asmdefs (#4).
7. Consolidate kart controllers and prune unused packages/docs (#10–12).

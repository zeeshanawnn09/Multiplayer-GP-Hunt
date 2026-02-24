# AGENTS.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

Multiplayer-GP-Hunt is a Unity 6 (6000.0.59f2) multiplayer game using **Photon PUN 2** for real-time networking and **PlayFab** for backend authentication (login, registration, password reset). The render pipeline is **HDRP** (High Definition Render Pipeline 17.0.4).

## Build & Run

- Open the project in **Unity 6** (editor version `6000.0.59f2`).
- The solution file is `Multiplayer-GP-Hunt.sln` (auto-generated, gitignored).
- There is no custom build script; use Unity's standard **File > Build Settings** workflow.
- Build scenes are configured in `ProjectSettings/EditorBuildSettings.asset`:
  - Scene 0: `Assets/Scenes/LobbyScene.unity` (lobby / room browser)
  - Scene 1: `Assets/Scenes/Test.unity` (gameplay level loaded after joining a room)
  - `Assets/Scenes/Login&Register.unity` is the login/registration UI scene.
- No automated test suite or linting setup exists in this repo.

## Architecture

### Scene Flow
`Login&Register` → (PlayFab auth) → `LobbyScene` → (Photon join/create room) → `Test` (gameplay)

### Networking (Photon PUN 2)
- Photon is installed as a local plugin under `Assets/Plugins/Photon/`.
- Config lives in `Assets/Plugins/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset` (AppId is committed).
- All networked scripts inherit from `MonoBehaviourPunCallbacks`.
- `LobbyMenu` (Assets/UI/) connects to Photon on Start, joins the default lobby, manages room creation/listing, and calls `PhotonNetwork.LoadLevel(1)` when the master client joins a room. `AutomaticallySyncScene` is enabled so non-master clients follow.
- `RoomController` (Assets/Multiplayer/) runs on the gameplay scene. The master client spawns all players via `PhotonNetwork.Instantiate` and transfers ownership to joining players.
- Networked prefab `PlayerCapsule` must live in `Assets/Resources/` (required by `PhotonNetwork.Instantiate`).
- `SetCharacterMat` is a `[PunRPC]` on `PlayerControls` — it's registered in the Photon RPC list in PhotonServerSettings.

### Player Controls (Assets/Gameplay/PlayerControls.cs)
- Uses legacy `Input.GetAxis` (not the new Input System, despite `InputSystem_Actions.inputactions` being present).
- Movement and mouse-look are processed only when `photonView.IsMine`.
- Camera is attached to the local player's camObject child after a short `Invoke` delay (`FinishInvoke`) to account for ownership transfer timing.
- `localPlayerInstance` is a static reference to the local player's GameObject; it gates duplicate spawning and persists via `DontDestroyOnLoad`.

### Authentication (Assets/Scripts/PlayFab/PlayFabManager.cs)
- Uses PlayFab Client SDK (`PlayFab.ClientModels`).
- PlayFab Title ID: `65710`, configured in `Assets/PlayFabSDK/Shared/Public/Resources/PlayFabSharedSettings.asset`.
- Provides `UserRegister()`, `UserLogin()`, and `ResetPassword()` methods wired to UI input fields (email, password, username via TMP_InputField).

### UI Scripts (Assets/UI/)
- `LobbyMenu` — room browser and creation UI in the lobby scene.
- `RoomPanel` — clickable room entry; stores a room name and calls `PhotonNetwork.JoinRoom`.
- `TestConnectionText` — debug HUD showing connection status, `photonView.IsMine`, and ownership info. Uses a static `TestUI` reference.

## Key Conventions

- All custom game scripts are C# and live outside `Assets/Plugins/` and `Assets/PlayFabSDK/`. The project's own code is in `Assets/Gameplay/`, `Assets/Multiplayer/`, `Assets/UI/`, and `Assets/Scripts/`.
- Photon RPCs must be registered in `PhotonServerSettings.asset` under `RpcList`. When adding a new `[PunRPC]`, update that list.
- Any prefab instantiated via `PhotonNetwork.Instantiate` must be placed in an `Assets/Resources/` folder.
- The project uses TextMeshPro (`TMPro`) for all UI text.

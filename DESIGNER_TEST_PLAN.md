# Multiplayer GP-Hunt Designer Test Plan

## Goal
This plan is for manual playtesting by design and QA. It focuses on player experience, multiplayer flow, and high-risk regressions in auth, lobby, spawning, combat, and respawn systems.

## Scope
- Scene flow: Login and Register -> Lobby -> Level_1
- Multiplayer room flow (Photon PUN)
- Role assignment and player ownership
- Combat and projectile behavior
- Death, ash pot ritual, and respawn flow
- HUD and connection feedback

## Out Of Scope
- Performance profiling and optimization
- Automated tests
- Platform certification checks

## Test Setup
- Unity version: 6000.0.59f2
- Required services:
  - PlayFab Title ID: 65710
  - Photon app configured and reachable
- Build scenes enabled:
  - Assets/Scenes/Login&Register.unity
  - Assets/Scenes/LobbyScene.unity
  - Assets/Scenes/Level_1.unity
- Clients required:
  - Minimum: 2 clients
  - Full validation: 4 clients

## Roles In Match
- 1 Priest
- 3 Ghosts

## Pass/Fail Rules
- Pass: Result matches Expected exactly.
- Fail: Any mismatch, error, freeze, soft-lock, incorrect UI, or incorrect network sync.
- Blocked: Test cannot continue due to environment or service issue.

## Severity Guide
- Critical: Blocks core game loop or causes hard desync/crash.
- High: Major gameplay feature fails or becomes unfair.
- Medium: Feature works partially but with visible defects.
- Low: Cosmetic or minor UX issue.

## Execution Order
1. Authentication smoke tests
2. Lobby and room flow
3. 4-player role assignment and spawn validation
4. Gameplay loop (combat + death + respawn)
5. Disconnect and rejoin edge cases
6. Stress and latency checks

## Test Cases

### A. Authentication (PlayFab)

| ID | Title | Preconditions | Steps | Expected Result | Priority |
|---|---|---|---|---|---|
| AUTH-001 | Register with valid credentials | On Login&Register scene | Enter valid email, username, password (8+ chars). Press Register. | Account created and scene transitions to LobbyScene. | High |
| AUTH-002 | Register with short password | On Login&Register scene | Enter password shorter than 8 chars. Press Register. | Registration blocked. User remains on login scene. Error shown in logs/UI. | High |
| AUTH-003 | Login with valid credentials | Existing account | Enter correct email and password. Press Login. | Login succeeds and transitions to LobbyScene. | High |
| AUTH-004 | Login with invalid password | Existing account | Enter wrong password. Press Login. | Login fails. User remains on login scene with error. | High |
| AUTH-005 | Password reset request | Valid registered email | Enter email. Press Reset Password. | Reset request accepted. Success feedback/log appears. | Medium |

### B. Lobby and Room Flow (Photon)

| ID | Title | Preconditions | Steps | Expected Result | Priority |
|---|---|---|---|---|---|
| LOBBY-001 | Connect to lobby | Entered LobbyScene | Wait for auto-connect. | State reaches Connected.InLobby. Region is eu. | High |
| LOBBY-002 | Create room with valid name | Connected to lobby | Enter room name and create room. | Room appears in list. Creator is master client. | High |
| LOBBY-003 | Create room with empty name | Connected to lobby | Leave name empty and attempt create. | Creation blocked. No invalid room created. | Medium |
| LOBBY-004 | Join existing room | Room exists with free slot | On second client, join room via list item. | Client joins same room successfully. | High |
| LOBBY-005 | Start match gating at 4 players | 3 players already in room | Join 4th player. Check Start/Connect control on master and non-master. | Start control enabled only for master when count is 4. | High |
| LOBBY-006 | Master loads gameplay scene | 4 players in room | Master presses Connect/Start. | All clients load Level_1 (synced scene transition). | Critical |

### C. Spawn, Ownership, and Role Assignment

| ID | Title | Preconditions | Steps | Expected Result | Priority |
|---|---|---|---|---|---|
| SPAWN-001 | Local player ownership | Client just entered Level_1 | Observe local character and controls. | Local character is controllable only on owning client. | Critical |
| SPAWN-002 | 4-player spawn placement | 4 players in Level_1 | Observe all spawn locations at start. | All players spawn on valid ground, not overlapping severely, no falling through world. | High |
| SPAWN-003 | Role assignment complete | 4 players loaded | Wait up to 10 seconds after all loaded. | Exactly 1 Priest and 3 Ghosts are assigned and reflected in visuals/HUD. | Critical |
| SPAWN-004 | No duplicate local spawn on re-entry | Player leaves and rejoins room | Rejoin room and load gameplay again. | Exactly one local player instance exists after rejoin. | High |

### D. Controls and Interaction

| ID | Title | Preconditions | Steps | Expected Result | Priority |
|---|---|---|---|---|---|
| CTRL-001 | Movement and look for local player | In Level_1 | Move with WASD and look with mouse. | Smooth movement/look for local player. | High |
| CTRL-002 | Remote client cannot control other players | 2+ clients in same room | Try controlling remote observed character from local inputs. | Local input only controls owned character. | Critical |
| CTRL-003 | Interact key near interactable | Near valid interactable (flower/soul/ash pot path) | Press interact key. | Correct interaction triggers only when in range and role allows it. | High |
| CTRL-004 | Local camera attachment | Just spawned | Observe camera after spawn and during first second. | Camera attaches to local player and remains stable. | Medium |

### E. Combat and Projectile Behavior

| ID | Title | Preconditions | Steps | Expected Result | Priority |
|---|---|---|---|---|---|
| COMBAT-001 | Priest damages Ghost | Priest and Ghost in line of sight | Priest fires at Ghost in range. | Ghost loses expected health (10 per hit default). | Critical |
| COMBAT-002 | Ghost damages Priest | Ghost and Priest in line of sight | Ghost fires at Priest in range. | Priest loses expected health (10 per hit default). | Critical |
| COMBAT-003 | Friendly fire blocked | Two Ghosts or role-invalid target | Fire repeatedly at invalid role target. | No damage applied where role rules disallow it. | High |
| COMBAT-004 | Priest projectile range cap | Priest aiming beyond 12m | Fire at distant target outside range. | No hit registered beyond Priest max range. | Medium |
| COMBAT-005 | Ghost projectile range cap | Ghost aiming beyond 28m | Fire at distant target outside range. | No hit registered beyond Ghost max range. | Medium |
| COMBAT-006 | Priest burst cooldown | Priest role active | Fire 5 quick shots, attempt 6th immediately. | 6th shot blocked until burst cooldown completes. | High |
| COMBAT-007 | Ghost burst cooldown | Ghost role active | Fire 10 quick shots, attempt 11th immediately. | 11th shot blocked until burst cooldown completes. | High |

### F. Death, Ash Pot, and Respawn

| ID | Title | Preconditions | Steps | Expected Result | Priority |
|---|---|---|---|---|---|
| RESPAWN-001 | Ghost death and auto-respawn | Ghost at low health | Kill ghost. Wait 3 seconds. | Ghost dies, hides, then respawns with restored health. | High |
| RESPAWN-002 | Priest death spawns ash pot | Priest at low health | Kill priest. | Priest dies and ash pot appears at death location. | Critical |
| RESPAWN-003 | Ghost picks up ash pot | Ash pot available | Ghost interacts to pick ash pot. | Ghost enters carrying state; pot no longer on ground. | High |
| RESPAWN-004 | Ritual respawn at respawn point | Ghost carrying ash pot, at PriestRespawnPoint | Stand in trigger and press ritual key. | Correct dead priest respawns at respawn point and carrying state clears. | Critical |
| RESPAWN-005 | Priest disconnected before ritual | Priest dies then leaves room | Try ritual with carried ash pot. | Ritual should fail safely with no crash/soft-lock. | High |

### G. HUD, Feedback, and UX

| ID | Title | Preconditions | Steps | Expected Result | Priority |
|---|---|---|---|---|---|
| UI-001 | Connection state updates | In Lobby and then in room | Observe connection status text across transitions. | Status updates correctly (Connected.InLobby, InRoom, etc.). | Medium |
| UI-002 | Role and health debug values update | In Level_1 with combat | Cause role assignment and damage events. | HUD reflects role and current health correctly. | High |
| UI-003 | Pickup counters update | Collect flowers/souls where applicable | Observe count updates after pickup actions. | Counts increment and remain in sync with gameplay events. | Medium |
| UI-004 | Crosshair local-only visibility | 2+ clients in same room | Compare local vs remote view of crosshair. | Crosshair shown only for local controlled player. | Medium |

### H. Network Edge Cases

| ID | Title | Preconditions | Steps | Expected Result | Priority |
|---|---|---|---|---|---|
| NET-001 | Master disconnect during room/gameplay | 3+ players in room | Close master client unexpectedly. | New master elected; room remains usable; no hard lock. | Critical |
| NET-002 | Late join after gameplay started | Gameplay already running | Join room with a new client. | Late joiner syncs scene/state without broken role/ownership. | High |
| NET-003 | Temporary network drop and recover | Active room/gameplay | Briefly disconnect one client and reconnect. | Client either recovers cleanly or fails gracefully with clear UI state. | High |
| NET-004 | Region consistency | Fresh launch, cleared PlayerPrefs recommended | Connect to lobby and observe region. | Region remains eu, does not drift to cached alternative. | High |

## Focused Regression Suite (Run Every Significant Change)
- AUTH-001, AUTH-003
- LOBBY-001, LOBBY-004, LOBBY-006
- SPAWN-001, SPAWN-003
- COMBAT-001, COMBAT-002, COMBAT-006
- RESPAWN-002, RESPAWN-004
- NET-001, NET-004

## Session Report Template
Use this per test session:

| Date | Build/Commit | Tester | Client Count | Network Notes | Overall Result |
|---|---|---|---|---|---|
| YYYY-MM-DD | value | name | 1/2/3/4 | latency/packet loss if known | Pass/Fail/Blocked |

Use this per failed test:

| Test ID | Severity | Repro Rate | Actual Result | Expected Result | Evidence | Notes |
|---|---|---|---|---|---|---|
| value | Critical/High/Medium/Low | x/y | what happened | what should happen | screenshot/video/log | optional |

## Designer Playtest Notes
- Validate fun and fairness while running the technical cases, especially:
  - Priest vs Ghost combat pacing
  - Burst cooldown readability and feel
  - Respawn ritual clarity and discoverability
  - Time spent waiting after death
- Record both bugs and balance friction in the same session report.

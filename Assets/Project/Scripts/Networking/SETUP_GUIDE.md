# Networking System - Setup Guide

## Overview

This guide explains how to set up the networking system for your mobile multiplayer game using Photon Fusion 2.x. The architecture exactly matches the **OtherGame reference implementation** (FusionHelper pattern):

- **GameManager**: Singleton singleton that tracks connected players
- **Launcher Prefab**: Contains both FusionNetworkService and FusionLauncher (created once on connection)
  - **FusionNetworkService** (FusionHelper equivalent): INetworkRunnerCallbacks, spawns player data
  - **FusionLauncher**: Creates and configures NetworkRunner
- **GameLauncher**: Entry point for initiating connection (finds/creates prefab)
- **PlayerSessionData**: Networked player data object (nickname syncing)
- **LobbyCanvas**: Main UI controller with event listeners
- **Event System**: 4 FusionEvent ScriptableObjects for loose coupling

## Quick Setup (3 Steps)

### Step 1: Create Event Assets

Create these FusionEvent ScriptableObjects in `Assets/Resources/Events/`:
- ✅ `OnPlayerJoinedEvent` 
- ✅ `OnPlayerLeftEvent` 
- ✅ `OnPlayerDataSpawnedEvent` 
- ✅ `OnShutdownEvent`

**How to create:**
1. Right-click in Project → Create → Networking → Fusion Event
2. Name each event
3. Save all 4 in Assets/Resources/Events/

### Step 2: Create GameManager & Launcher Prefab

**2a) Create GameManager Singleton (in scene):**
- Create empty GameObject called `GameManager`
- Add `GameManager.cs` component
- In Inspector, assign events:
  - `OnPlayerLeftEvent` → OnPlayerLeftEvent asset
  - `OnRunnerShutDownEvent` → OnShutdownEvent asset
- *(GameManager marks itself DontDestroyOnLoad automatically)*

**2b) Create Launcher Prefab (for connection):**
- Create empty GameObject called `Launcher`
- Add **both** components:
  1. `FusionNetworkService.cs` 
  2. `FusionLauncher.cs`
- In `FusionNetworkService` Inspector, assign:
  - `PlayerDataNO` → PlayerSessionData prefab
  - `OnPlayerJoinedEvent` → OnPlayerJoinedEvent asset
  - `OnPlayerLeftEvent` → OnPlayerLeftEvent asset
  - `OnShutdownEvent` → OnShutdownEvent asset
  - `OnDisconnectEvent` → (optional)
- *(FusionLauncher will automatically find FusionNetworkService via GetComponent)*
- Save as prefab: `Assets/Resources/Prefabs/Launcher.prefab`

**2c) Create GameLauncher (in scene):**
- Create empty GameObject called `GameLauncher`
- Add `GameLauncher.cs` component
- In Inspector, assign:
  - `LauncherPrefab` → Launcher prefab

### Step 3: Create PlayerSessionData Prefab

1. Create empty GameObject called `PlayerSessionData`
2. Add `PlayerSessionData.cs` component
3. In Inspector, assign:
   - `OnPlayerDataSpawnedEvent` → OnPlayerDataSpawnedEvent asset
4. Save as prefab: `Assets/Resources/Prefabs/PlayerSessionData.prefab`
5. Register in Fusion → Project Settings → Registered Prefabs

### Step 4: Create LobbyCanvas UI

Create this panel hierarchy in your main Canvas:

```
Canvas
└── LobbyCanvas (add LobbyCanvas.cs component here)
    ├── InitPanel (Panel - starts ACTIVE)
    │   ├── ModeButtonsPanel
    │   │   ├── HostButton (Button)
    │   │   └── JoinButton (Button)
    │   └── InputPanel (Panel - starts INACTIVE)
    │       ├── NicknameInput (TMP_InputField)
    │       ├── RoomInput (TMP_InputField)
    │       └── CreateJoinButton (Button)
    │
    └── LobbyPanel (Panel - starts INACTIVE)
        ├── LobbyName (TMP_Text)
        ├── LobbyPlayers (TMP_Text)
        ├── StartButton (Button)
        ├── Back (Button)
        └── Exit (Button)
```

**Assign in LobbyCanvas.cs Inspector:**
- `_initPanel` → InitPanel GameObject
- `_lobbyPanel` → LobbyPanel GameObject  
- `_modeButtons` → ModeButtonsPanel GameObject
- `_nickname` → NicknameInput component
- `_room` → RoomInput component
- `_lobbyPlayerText` → LobbyPlayers component
- `_lobbyRoomName` → LobbyName component
- `_startButton` → StartButton component
- `OnPlayerJoinedEvent` → OnPlayerJoinedEvent asset
- `OnPlayerLeftEvent` → OnPlayerLeftEvent asset
- `OnShutdownEvent` → OnShutdownEvent asset
- `OnPlayerDataSpawnedEvent` → OnPlayerDataSpawnedEvent asset

**Wire Button Callbacks:**
- **HostButton** → LobbyCanvas.SetGameMode(0)
- **JoinButton** → LobbyCanvas.SetGameMode(1)
- **CreateJoinButton** → LobbyCanvas.StartLauncher()
- **StartButton** → LobbyCanvas.StartGame()
- **Back Button** → LobbyCanvas.SetGameMode(-1)
- **Exit Button** → LobbyCanvas.ExitGame()

## Data Flow

### Connection Sequence

```
1. Scene starts → LobbyCanvas shows InitPanel with Host/Join buttons

2. Click Host or Join → SetGameMode(0/1) → shows InputPanel

3. Enter nickname + room → Click CreateJoin → StartLauncher()
   └─ Saves "Nick" to PlayerPrefs
   └─ Calls GameLauncher.Launch(gameMode, roomName)

4. GameLauncher.Launch()
   └─ Finds/creates Launcher prefab
   └─ Calls FusionLauncher.Launch()

5. FusionLauncher.Launch()
   └─ Creates NetworkRunner
   └─ Finds FusionNetworkService (sibling component)
   └─ Registers: runner.AddCallbacks(fusionService)
   └─ Starts game: runner.StartGame(StartGameArgs)

6. Connection established → FusionNetworkService.OnPlayerJoined() fires
   └─ If server: Spawns PlayerSessionData with player's inputAuthority
   └─ If local player: Caches LocalRunner
   └─ Raises OnPlayerJoinedEvent

7. OnPlayerJoinedEvent fires → LobbyCanvas.ShowLobbyCanvas()
   └─ Hides InitPanel
   └─ Shows LobbyPanel

8. PlayerSessionData.Spawned() fires
   └─ Reads "Nick" from PlayerPrefs
   └─ Calls RPC_SetNick(nick) → syncs to all clients
   └─ Raises OnPlayerDataSpawnedEvent

9. OnPlayerDataSpawnedEvent fires → LobbyCanvas.UpdateLobbyList()
   └─ Queries GameManager.GetPlayerData() for each runner.ActivePlayer
   └─ Reads PlayerSessionData.Nick from each player
   └─ Displays names with "(You)" tag for local player

✅ All players see correct nicknames in lobby
```

### Prefab Structure

**Launcher Prefab** (instantiated on connection):
```
Launcher (prefab in Assets/Resources/Prefabs/)
├── FusionNetworkService.cs     ← INetworkRunnerCallbacks (like FusionHelper in OtherGame)
│   └── Assigns: PlayerDataNO, Event listeners
└── FusionLauncher.cs           ← Creates NetworkRunner, finds FusionNetworkService
    └── AutoFind: var fusionService = GetComponent<FusionNetworkService>();
```

When instantiated, FusionLauncher automatically finds FusionNetworkService and registers it with the runner.

## Key Script Responsibilities

### GameManager (Singleton)
- Tracks connected players in dictionary `Dictionary<PlayerRef, PlayerSessionData>`
- Provides `GetPlayerData(player, runner)` for UI to query nicknames
- Manages game state enum (Lobby, Playing, Loading)
- Cleans up disconnected players via OnPlayerLeftEvent listener
- Persists across scenes (DontDestroyOnLoad)

### Launcher Prefab (Instantiated on Connection)

**FusionNetworkService** (Equivalent to FusionHelper in OtherGame)
- Implements `INetworkRunnerCallbacks`
- Spawns `PlayerSessionData` prefab on `OnPlayerJoined()` (server-side)
- Caches `LocalRunner` static reference for input authority
- Raises 4 critical FusionEvents:
  - `OnPlayerJoinedEvent` → triggers lobby show
  - `OnPlayerLeftEvent` → triggers disconnect cleanup
  - `OnShutdownEvent` → triggers session end
  - `OnDisconnectEvent` → handles server disconnect
- **Automatically found by FusionLauncher** via `GetComponent<FusionNetworkService>()`

**FusionLauncher**
- Creates or gets existing NetworkRunner component
- Finds FusionNetworkService via `GetComponent<FusionNetworkService>()`
- **Critical:** Registers callbacks with `runner.AddCallbacks(fusionService)` BEFORE StartGame()
- Calls `runner.StartGame(StartGameArgs)` with mode, session name, player count
- Marks GameObject as DontDestroyOnLoad

### GameLauncher (Scene Entry Point)
- Simple factory: finds or creates Launcher prefab
- Public method `Launch(GameMode, string room)` called by LobbyCanvas
- Delegates to `FusionLauncher.Launch()`

### PlayerSessionData (Networked Prefab)
- Networked property: `[Networked] NetworkString<_16> Nick`
- Networked property: `[Networked] NetworkObject Instance`
- **Spawned():** 
  - Input authority reads "Nick" from PlayerPrefs
  - Calls `RPC_SetNick(nick)` to sync across all clients
  - Registers with GameManager via `SetPlayerDataObject()`
  - Raises `OnPlayerDataSpawnedEvent`
- **Render():** Detects Nick changes, re-raises event for UI updates
- **RPC_SetNick(string):** Called by input authority, syncs nickname to state authority (server)

### LobbyCanvas (UI Hub)
- **Event Listeners:** Responds to 4 FusionEvents
  - `OnPlayerJoinedEvent` → `ShowLobbyCanvas()`
  - `OnPlayerLeftEvent` → `UpdateLobbyList()` (refresh display)
  - `OnPlayerDataSpawnedEvent` → `UpdateLobbyList()` (updates with fresh nicks)
  - `OnShutdownEvent` → `ResetCanvas()` (back to initial state)
- **Methods:**
  - `SetGameMode(int)` - Shows nickname/room inputs
  - `StartLauncher()` - Saves nick to PlayerPrefs, calls GameLauncher.Launch()
  - `UpdateLobbyList()` - Reads from GameManager.GetPlayerData(), displays nicks
  - `ShowLobbyCanvas()` - Shows lobby panel, hides init panel
  - `ResetCanvas()` - Returns to mode selection
  - `StartGame()` - Sets room to closed (IsOpen = false), disables matchmaking
  - `LeaveLobby()` - Disconnects from session

## Event Wiring Summary

| Event | Source | Listeners |
|-------|--------|-----------|
| OnPlayerJoinedEvent | FusionNetworkService.OnPlayerJoined() | LobbyCanvas.ShowLobbyCanvas() |
| OnPlayerLeftEvent | FusionNetworkService.OnPlayerLeft() | GameManager.PlayerDisconnected(), LobbyCanvas.UpdateLobbyList() |
| OnPlayerDataSpawnedEvent | PlayerSessionData.Spawned() & .Render() | LobbyCanvas.UpdateLobbyList() |
| OnShutdownEvent | FusionNetworkService.OnShutdown() | GameManager.DisconnectedFromSession(), LobbyCanvas.ResetCanvas() |

## Verification Checklist

Before testing, verify these are set up correctly:

✅ **Scene Setup:**
- [ ] GameManager GameObject in scene with GameManager component
- [ ] GameLauncher GameObject in scene with GameLauncher component
- [ ] LobbyCanvas in scene with LobbyCanvas component
- [ ] Canvas with InputField components (Nick, Room)

✅ **Prefabs Created:**
- [ ] `Assets/Resources/Prefabs/Launcher.prefab` with:
  - [ ] FusionNetworkService component (events assigned)
  - [ ] FusionLauncher component
- [ ] `Assets/Resources/Prefabs/PlayerSessionData.prefab` with:
  - [ ] PlayerSessionData component
  - [ ] OnPlayerDataSpawnedEvent assigned

✅ **Event Assets Created:**
- [ ] `Assets/Resources/Events/OnPlayerJoinedEvent.asset`
- [ ] `Assets/Resources/Events/OnPlayerLeftEvent.asset`
- [ ] `Assets/Resources/Events/OnPlayerDataSpawnedEvent.asset`
- [ ] `Assets/Resources/Events/OnShutdownEvent.asset`

✅ **Inspector Assignments:**
- [ ] GameManager: OnPlayerLeftEvent & OnRunnerShutDownEvent assigned
- [ ] GameLauncher: LauncherPrefab assigned
- [ ] FusionNetworkService (in Launcher prefab): All 4 events + PlayerDataNO assigned
- [ ] LobbyCanvas: All 4 events assigned
- [ ] LobbyCanvas: All 8 UI fields assigned (_initPanel, _lobbyPanel, _nickname, _room, _lobbyPlayerText, _lobbyRoomName, _startButton, _modeButtons)
- [ ] Button callbacks wired to LobbyCanvas methods

✅ **Fusion Settings:**
- [ ] PlayerSessionData registered in Project Settings → Fusion → Network Prefabs

## Testing

### Single Player Test (Host)
1. Play scene
2. Click Host button
3. Enter nickname (e.g., "TestPlayer")
4. Enter room name (e.g., "TestRoom")
5. Click Create button
6. Verify:
   - Lobby panel appears
   - Your nickname displays with "(You)" tag
   - Start button is visible

### Two Player Test (Recommended)
1. **Player 1 (Host):**
   - Click Host
   - Enter nickname "HostPlayer"
   - Enter room "MultiTest"
   - Click Create

2. **Player 2 (Client in same/second instance):**
   - Click Join
   - Enter nickname "ClientPlayer"
   - Enter room "MultiTest"
   - Click Join

3. **Verify Both Clients See:**
   - Lobby panel shows
   - Displays: "HostPlayer (You)" on host device, "ClientPlayer (You)" on client device
   - Both see each other's names in player list
   - Start button only visible to host
   - Room name displays correctly

## Common Issues & Solutions

### "Launcher Prefab is null"
- Verify Launcher prefab exists at `Assets/Resources/Prefabs/Launcher.prefab`
- Check GameLauncher component has LauncherPrefab assigned
- Ensure prefab path matches exactly in GameLauncher.cs

### "FusionNetworkService is null"
- Verify Launcher prefab has FusionNetworkService component attached
- Check FusionLauncher.cs uses `GetComponent<FusionNetworkService>()`
- Ensure component is not disabled

### "OnPlayerJoinedEvent not firing"
- Verify FusionNetworkService is assigned to Launcher prefab
- Check that all 4 events are assigned in FusionNetworkService Inspector
- Ensure runner.AddCallbacks() is called BEFORE runner.StartGame()

### "Nickname shows as empty/unknown"
- Verify `PlayerLocalPrefs.SetString("Nick", nickname)` is called in StartLauncher()
- Check PlayerSessionData.Spawned() reads from PlayerPrefs correctly
- Ensure RPC_SetNick() is being invoked

### "Lobby doesn't show"
- Check OnPlayerJoinedEvent is assigned to LobbyCanvas
- Verify LobbyCanvas.ShowLobbyCanvas() is in the event listener
- Make sure InitPanel/LobbyPanel GameObjects are correct

### "Callbacks not being called"
- **Most common issue:** runner.AddCallbacks() must be called BEFORE runner.StartGame()
- Verify FusionLauncher.Launch() has:
  ```
  var fusionService = GetComponent<FusionNetworkService>();
  _runner.AddCallbacks(fusionService);  // ← BEFORE StartGame()
  _runner.StartGame(args);
  ```

## Project Structure Reference

```
Assets/
├── Resources/
│   └── Prefabs/
│       ├── Launcher.prefab                    ← Both FusionNetworkService + FusionLauncher
│       └── PlayerSessionData.prefab
│   └── Events/
│       ├── OnPlayerJoinedEvent.asset
│       ├── OnPlayerLeftEvent.asset
│       ├── OnPlayerDataSpawnedEvent.asset
│       └── OnShutdownEvent.asset
└── Project/Scripts/Networking/
    ├── Managers/
    │   ├── GameManager.cs
    │   └── GameLauncher.cs
    ├── Services/
    │   ├── FusionNetworkService.cs         ← (Inside Launcher prefab)
    │   └── FusionLauncher.cs               ← (Inside Launcher prefab)
    ├── Models/
    │   └── PlayerSessionData.cs
    └── UI/
        └── LobbyCanvas.cs
```

## Architecture Summary

This implementation **exactly matches the OtherGame reference** (FusionHelper pattern):

### Key Design Principles
✅ **Single Launcher Prefab** - Contains both FusionNetworkService + FusionLauncher (no separate GameObject)
✅ **Automatic Discovery** - FusionLauncher finds FusionNetworkService via GetComponent
✅ **Event-Driven** - Loose coupling via 4 FusionEvent ScriptableObjects
✅ **Singleton GameManager** - Centralized player tracking, DontDestroyOnLoad
✅ **Networked Data Sync** - PlayerSessionData RPC syncs nicknames across all clients
✅ **Real-Time UI** - LobbyCanvas listens to events, reads fresh data on each update
✅ **Clean Separation** - Connection logic (FusionLauncher), player data (GameManager), UI (LobbyCanvas)
✅ **Host/Client Symmetry** - Same UI flow for both host and client

### Critical Registration Points
1. **GameManager** registers to OnPlayerLeftEvent & OnRunnerShutDownEvent
2. **FusionLauncher** registers FusionNetworkService **before** StartGame()
3. **PlayerSessionData** registers with GameManager on Spawned()
4. **LobbyCanvas** registers to all 4 FusionEvents in OnEnable()

This pattern ensures:
- No missed callbacks
- Automatic cleanup on disconnect
- Real-time player list updates
- Seamless multiplayer experience


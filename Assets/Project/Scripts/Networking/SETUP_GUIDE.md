<<<<<<< HEAD
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

=======
# Networking System - Complete Setup Guide

## Overview

This guide walks you through setting up the complete multiplayer networking system for your mobile game using **Photon Fusion 2.x**. It covers:
- Networking foundation (events, player data, connections)
- Lobby system (player list, room management)
- Character selection (30s timer, multi-player sync, auto-assign)
- Game lobby (ready state with selected characters)

**Total time: ~45-60 minutes**

---

# PART 1: FOUNDATION (Events, Managers, Prefabs)

## Step 1: Create Folder Structure

Create these folders in your Project:

```
Assets/Project/Resources/
├── Characters/                    (for character ScriptableObjects)
├── Events/                        (for FusionEvent assets)
└── Prefabs/                       (for network prefabs)
    ├── Launcher.prefab
    ├── PlayerSessionData.prefab
    ├── CharacterSelectionManager.prefab
    └── CharacterSelectionSlot.prefab
```

**In Unity:**
1. In Project panel, navigate to `Assets/Project/`
2. Right-click → Create Folder → Name it `Resources`
3. Inside Resources: Create folders `Characters`, `Events`, `Prefabs`

---

## Step 2: Create Event ScriptableObjects (6 events total)

**Location:** `Assets/Project/Resources/Events/`

### Create OnPlayerJoinedEvent
1. Right-click in `Events` folder
2. Select **Create → Networking → Fusion Event**
3. Name it: `OnPlayerJoinedEvent`
4. Leave default, save

### Create OnPlayerLeftEvent
1. Right-click → **Create → Networking → Fusion Event**
2. Name it: `OnPlayerLeftEvent`
3. Save

### Create OnPlayerDataSpawnedEvent
1. Right-click → **Create → Networking → Fusion Event**
2. Name it: `OnPlayerDataSpawnedEvent`
3. Save

### Create OnShutdownEvent
1. Right-click → **Create → Networking → Fusion Event**
2. Name it: `OnShutdownEvent`
3. Save

### Create OnCharacterSelectionCompleteEvent
1. Right-click → **Create → Networking → Fusion Event**
2. Name it: `OnCharacterSelectionCompleteEvent`
3. Save

### Create OnSelectionTimeRemainingEvent
1. Right-click → **Create → Networking → Fusion Event**
2. Name it: `OnSelectionTimeRemainingEvent`
3. Save

**Verify:** You should have 6 event assets in `Assets/Project/Resources/Events/`

---

## Step 3: Create GameManager (Scene Singleton)

### 3a) Create GameObject
1. In your scene, right-click in Hierarchy → **Create Empty**
2. Rename to `GameManager`
3. Add component → Search `GameManager.cs` → Add it

### 3b) Assign Events in Inspector
1. Select GameManager in Hierarchy
2. In Inspector, under GameManager component:
   - **OnPlayerLeftEvent** → Drag `OnPlayerLeftEvent.asset` here
   - **OnRunnerShutDownEvent** → Drag `OnShutdownEvent.asset` here
3. Save scene

**Verify:** GameManager has both events assigned

---

## Step 4: Create Launcher Prefab (Networking Core)

The Launcher prefab contains the critical networking components.

### 4a) Create Launcher GameObject
1. In Hierarchy, right-click → **Create Empty**
2. Rename to `Launcher`
3. Add component → Search `FusionNetworkService` → Add it
4. Add component → Search `FusionLauncher` → Add it

### 4b) Assign PlayerSessionData to FusionNetworkService
1. Select Launcher in Hierarchy
2. In Inspector, find **FusionNetworkService** component
3. **PlayerDataNO** field:
   - You don't have PlayerSessionData prefab yet (Step 6)
   - Leave empty for now, we'll assign it in Step 6
4. Assign the 4 events:
   - **OnPlayerJoinedEvent** → Drag `OnPlayerJoinedEvent.asset`
   - **OnPlayerLeftEvent** → Drag `OnPlayerLeftEvent.asset`
   - **OnShutdownEvent** → Drag `OnShutdownEvent.asset`
   - **OnDisconnectEvent** → (optional, can leave empty)

### 4c) Register as Prefab
1. Drag the `Launcher` GameObject into `Assets/Project/Resources/Prefabs/`
2. Name it `Launcher.prefab`
3. Delete the instance from scene (or unparent it)

**Verify:** `Assets/Project/Resources/Prefabs/Launcher.prefab` exists with FusionNetworkService and FusionLauncher components

---

## Step 5: Create GameLauncher (Scene Connection Manager)

### 5a) Create GameObject
1. In Hierarchy, right-click → **Create Empty**
2. Rename to `GameLauncher`
3. Add component → Search `GameLauncher.cs` → Add it

### 5b) Assign Launcher Prefab
1. Select GameLauncher in Hierarchy
2. In Inspector, under GameLauncher component:
   - **LauncherPrefab** field → Drag `Launcher.prefab` here
3. Save scene

**Verify:** GameLauncher has LauncherPrefab assigned

---

## Step 6: Create PlayerSessionData Prefab

### 6a) Create GameObject
1. In Hierarchy, right-click → **Create Empty**
2. Rename to `PlayerSessionData`
3. Add component → Search `PlayerSessionData` → Add it

### 6b) Assign Events
1. Select PlayerSessionData in Hierarchy
2. In Inspector, under PlayerSessionData component:
   - **OnPlayerDataSpawnedEvent** → Drag `OnPlayerDataSpawnedEvent.asset` (fires on both player spawn and character selection)
3. Save

### 6c) Register as Prefab
1. Drag `PlayerSessionData` into `Assets/Project/Resources/Prefabs/`
2. Name it `PlayerSessionData.prefab`
3. Delete from scene

### 6d) Assign to Launcher Prefab
1. Open `Assets/Project/Resources/Prefabs/Launcher.prefab` (double-click to edit)
2. Select the Launcher object
3. In FusionNetworkService component:
   - **PlayerDataNO** → Drag `PlayerSessionData.prefab` here
4. Save and close prefab

### 6e) Register in Fusion
1. Go to **Project Settings → Fusion → Network Prefabs**
2. Add `PlayerSessionData.prefab` to the registered prefabs list
3. Save

**Verify:** 
- `PlayerSessionData.prefab` exists with both events assigned
- It's in Launcher prefab's PlayerDataNO field
- It's registered in Fusion settings

---

# PART 2: CHARACTER SYSTEM (Database, Configs)

## Step 7: Create Character Configs (6 ScriptableObjects)

**Location:** `Assets/Project/Resources/Characters/`

### Create Character 1
1. Right-click in `Characters` folder
2. Select **Create → Networking → Character Config**
3. Name it: `Character_1`
4. Select it and fill in Inspector:
   - **Character Id**: `1`
   - **Character Name**: `Warrior`
   - **Description**: `Strong melee fighter with high health`
   - **Character Sprite**: Assign a sprite (create a solid color sprite if you don't have one)
   - **Character Prefab**: Assign any networked player prefab (or leave blank for now)
   - **Stats**: Health=150, Attack=20, Defense=10, AttackSpeed=1.0, MoveSpeed=5.0

### Create Characters 2-6
Repeat for each character (change only the Character Id, Name, and Description):

| ID | Name | Description |
|----|------|-------------|
| 2 | Mage | Fast spellcaster with high attack |
| 3 | Archer | Ranged attacker with balanced stats |
| 4 | Rogue | Quick assassin with low health |
| 5 | Paladin | Tanky healer with defense focus |
| 6 | Shaman | Support class with utility skills |

Customize health/attack/defense as desired.

**Verify:** You have 6 character assets in `Assets/Project/Resources/Characters/` with IDs 1-6

---

## Step 8: Create CharacterDatabase (Scene Manager)

### 8a) Create GameObject
1. In Hierarchy, right-click → **Create Empty**
2. Rename to `CharacterDatabase`
3. Add component → Search `CharacterDatabase` → Add it

### 8b) Assign All 6 Characters
1. Select CharacterDatabase in Hierarchy
2. In Inspector, under CharacterDatabase component:
   - **All Characters** array size: Set to `6`
   - Slot [0] → Drag `Character_1.asset`
   - Slot [1] → Drag `Character_2.asset`
   - ... (continue through Character_6)
3. Save scene

**Verify:** All 6 characters are assigned in order (0-5), matching IDs 1-6

---

# PART 3: LOBBY UI (All panels in one scene)

## Step 9: Create LobbyCanvas UI Structure

### 9a) Setup Canvas (if not exist)
1. Right-click in Hierarchy → **UI (TextMesh Pro) → Panel**
2. Rename to `Canvas`
3. Add component → Search `LobbyCanvas` → Add it

### 9b) Create InitPanel (Mode selection)
Inside Canvas:
1. Right-click on Canvas → **Create Empty Child**
2. Rename to `InitPanel`
3. Right-click on InitPanel → **UI (TextMesh Pro) → Panel**
4. Rename to `ModeButtonsPanel`
5. Add two buttons as children:
   - Right-click on ModeButtonsPanel → **UI (TextMesh Pro) → Button**
   - Rename to `HostButton`, add text child "Host"
   - Right-click on ModeButtonsPanel → **UI (TextMesh Pro) → Button**
   - Rename to `JoinButton`, add text child "Join"
6. Right-click on InitPanel → **UI (TextMesh Pro) → Panel**
7. Rename to `InputPanel`
8. Add children:
   - **UI (TextMesh Pro) → Input Field (TMP)**
   - Rename to `NicknameInput`, add label "Nickname: "
   - **UI (TextMesh Pro) → Input Field (TMP)**
   - Rename to `RoomInput`, add label "Room: "
   - **UI (TextMesh Pro) → Button**
   - Rename to `CreateJoinButton`, add text "Create/Join"
9. Set InputPanel to inactive (InitPanel child, starts visible)

### 9c) Create LobbyPanel (Player list + Start button)
Inside Canvas (sibling to InitPanel):
1. Right-click on Canvas → **Create Empty Child**
2. Rename to `LobbyPanel` (set to INACTIVE)
3. Add children:
   - **UI (TextMesh Pro) → Text (TMP)**
   - Rename to `LobbyName`, text: "Room: TestRoom"
   - **UI (TextMesh Pro) → Text (TMP)**
   - Rename to `LobbyPlayers`, text: "Players:\n..."
   - **UI (TextMesh Pro) → Button**
   - Rename to `StartButton`, text: "Start Game", set to INACTIVE initially
   - **UI (TextMesh Pro) → Button**
   - Rename to `LeaveButton`, text: "Leave"

### 9d) Create CharacterSelectionPanel
Inside Canvas (sibling to LobbyPanel):
1. Right-click on Canvas → **Create Empty Child**
2. Rename to `CharacterSelectionPanel` (set to INACTIVE)
3. Add children:
   - **UI (TextMesh Pro) → Text (TMP)**
   - Rename to `Title`, text: "Select Your Character"
   - **UI (TextMesh Pro) → Text (TMP)**
   - Rename to `Timer`, text: "30.0s"
   - **UI (TextMesh Pro) → Text (TMP)**
   - Rename to `SelectionStatus`, text: "Selected: 0/6"
   - **Create Empty** child
   - Rename to `CharactersGrid`
   - Add component: **Grid Layout Group** (set layout as needed)
4. Add component `CanvasGroup` to CharacterSelectionPanel itself

**Note:** Character slots are instantiated at runtime from `CharacterSelectionSlot.prefab`, so no template needed in the scene.

### 9e) Create GameLobbyPanel
Inside Canvas (sibling to CharacterSelectionPanel):
1. Right-click on Canvas → **Create Empty Child**
2. Rename to `GameLobbyPanel` (set to INACTIVE)
3. Add children:
   - **UI (TextMesh Pro) → Text (TMP)**
   - Rename to `Title`, text: "Game Ready!"
   - **UI (TextMesh Pro) → Text (TMP)**
   - Rename to `RoomInfo`, text: "Room: TestRoom"
   - **UI (TextMesh Pro) → Text (TMP)**
   - Rename to `PlayerList`, text: "Players:\nPlayer1 - Warrior\n..."
   - **UI (TextMesh Pro) → Button**
   - Rename to `LoadGameButton`, text: "Load Game"
   - **UI (TextMesh Pro) → Button**
   - Rename to `LeaveButton`, text: "Leave"

**Verify:** Canvas has 4 main panels: InitPanel, LobbyPanel, CharacterSelectionPanel, GameLobbyPanel

---

## Step 10: Assign LobbyCanvas Inspector Fields

### Select Canvas object (with LobbyCanvas component)

**Basic Panels:**
- **_initPanel** → InitPanel GameObject
- **_lobbyPanel** → LobbyPanel GameObject
- **_modeButtons** → ModeButtonsPanel GameObject

**Input Fields:**
- **_nickname** → NicknameInput component
- **_room** → RoomInput component

**Lobby Panel Fields:**
- **_lobbyPlayerText** → LobbyPlayers TMP_Text component
- **_lobbyRoomName** → LobbyName TMP_Text component
- **_startButton** → StartButton Button component

**Events:**
- **OnPlayerJoinedEvent** → `OnPlayerJoinedEvent.asset`
- **OnPlayerLeftEvent** → `OnPlayerLeftEvent.asset`
- **OnPlayerDataSpawnedEvent** → `OnPlayerDataSpawnedEvent.asset`
- **OnShutdownEvent** → `OnShutdownEvent.asset`

**Character Selection (NEW):**
- **_characterSelectionPanel** → CharacterSelectionPanel GameObject
- **_characterSelectionManagerPrefab** → (leave empty for now, will assign after Step 11c)

**Game Lobby (NEW):**
- **_gameLobbyPanel** → GameLobbyPanel GameObject
- **_gameLobbPlayerText** → PlayerList TMP_Text component (in GameLobbyPanel)
- **_gameLobbyRoomName** → RoomInfo TMP_Text component (in GameLobbyPanel)
- **_loadGameButton** → LoadGameButton Button component

**Verify:** All fields are assigned (green checkmarks)

---

## Step 11: Wire Button Callbacks

### InitPanel Buttons
1. Select **HostButton**
   - In Inspector, Button component → On Click()
   - Click **+ →** Select Canvas → LobbyCanvas → SetGameMode(0)
2. Select **JoinButton**
   - Button → On Click() → + → Select Canvas → LobbyCanvas → SetGameMode(1)
3. Select **CreateJoinButton** (in InputPanel)
   - Button → On Click() → + → Select Canvas → LobbyCanvas → StartLauncher()

### LobbyPanel Buttons
1. Select **StartButton**
   - Button → On Click() → + → Select Canvas → LobbyCanvas → StartGame()
2. Select **LeaveButton** (in LobbyPanel)
   - Button → On Click() → + → Select Canvas → LobbyCanvas → LeaveLobby()
3. Add another button **BackButton** or use existing, wire to LobbyCanvas → SetGameMode(-1)
4. Add **ExitButton** → Wire to LobbyCanvas → ExitGame()

### GameLobbyPanel Buttons
1. Select **LoadGameButton**
   - Button → On Click() → + → Select Canvas → LobbyCanvas → LoadGame()
2. Select **LeaveButton** (in GameLobbyPanel)
   - Button → On Click() → + → Select Canvas → LobbyCanvas → LeaveLobby()

**Verify:** All buttons have green "√" icons indicating callbacks are wired

---

# PART 4: CHARACTER SELECTION SYSTEM

## Step 12: Create CharacterSelectionManager Prefab

### 12a) Create GameObject
1. In Hierarchy, right-click → **Create Empty**
2. Rename to `CharacterSelectionManager`
3. Add component → Search `CharacterSelectionManager` → Add it

### 12b) Assign Events
1. In Inspector, under CharacterSelectionManager:
   - **Selection Timeout Seconds**: `30`
   - **OnCharacterSelectionCompleteEvent** → Drag `OnCharacterSelectionCompleteEvent.asset`
   - **OnSelectionTimeRemainingEvent** → Drag `OnSelectionTimeRemainingEvent.asset`
2. Save

### 12c) Register as Prefab & Update Launcher
1. Drag `CharacterSelectionManager` into `Assets/Project/Resources/Prefabs/`
2. Name it `CharacterSelectionManager.prefab`
3. Delete from scene
4. Now go back to Canvas, select LobbyCanvas:
   - **_characterSelectionManagerPrefab** field → Drag `CharacterSelectionManager.prefab`
5. Register in Fusion:
   - Go to **Project Settings → Fusion → Network Prefabs**
   - Add `CharacterSelectionManager.prefab` to the list

**Verify:** CharacterSelectionManager.prefab exists and is assigned to LobbyCanvas

---

## Step 13: Create CharacterSelectionSlot Prefab

**This step creates a Button prefab that will be instantiated multiple times (once per character).**

### 13a) Create Slot as Button Prefab
1. In Hierarchy, right-click → **UI (TextMesh Pro) → Button**
2. Rename to `CharacterSlot`
3. The Button component auto-creates:
   - **Image** child - displays the button background (we'll fill this with character sprite)
   - **Text (TMP)** child - displays the character name
4. Optional enhancement - add additional child for selection feedback:
   - Right-click on `CharacterSlot` → **Create Empty Child**
   - Rename to `SelectionIndicator`
   - Add component → **Image**
   - This will show visual feedback when character is selected (optional)
5. Configure Button component (optional for aesthetics):
   - Set **Transition** to "Color Tint" or "Sprite Swap"
   - Adjust **Colors** section for Normal/Highlighted/Pressed states

**Key benefit:** Button component eliminates manual setup - Image and Text children are already there, ready to be populated with character data.

### 13b) Add CharacterSelectionSlot Script
1. **Select the `CharacterSlot` GameObject in Hierarchy**
2. Add component → Search `CharacterSelectionSlot` → Add it
3. The script auto-discovers Button's built-in Image and Text children
4. At runtime, it pulls character data from **CharacterDatabase** by ID:
   - Character sprite → Image component
   - Character name → Text component
5. SelectionIndicator child is optional - script handles it if present
6. Save

**How it works:** The script uses Button's existing Image + Text structure. No custom child names needed. All character data comes from CharacterDatabase.

### 13c) Register as Prefab
1. Drag `CharacterSlot` into `Assets/Project/Resources/Prefabs/`
2. Name it `CharacterSelectionSlot.prefab`
3. Delete from scene

**Verify:** CharacterSelectionSlot.prefab exists with CharacterSelectionSlot component

---

## Step 14: Setup CharacterSelectionPanel

**This step adds the CharacterSelectionPanel script to the CharacterSelectionPanel UI GameObject created in Step 9d.**

### 14a) Add Component - **Add Script to CharacterSelectionPanel GameObject**
1. **Select the `CharacterSelectionPanel` GameObject in Hierarchy** (the one you created in Step 9d with the Title, Timer, SelectionStatus, and CharactersGrid children)
2. Add component → Search `CharacterSelectionPanel` → Add it

### 14b) Assign Inspector Fields
1. In Inspector, under CharacterSelectionPanel:
   - **Slots Container** → Drag CharactersGrid (the Grid Layout Group child)
   - **Timer Text** → Drag Timer TMP_Text component
   - **Selection Status Text** → Drag SelectionStatus TMP_Text component
   - **Slot Prefab** → Drag `CharacterSelectionSlot.prefab`
   - **Panel Canvas Group** → Drag CanvasGroup component (on CharacterSelectionPanel itself)
   - **On Selection Complete Event** → Create new FusionEvent or drag an event

**Verify:** All fields are assigned

---

# PART 5: FINAL WIRING & TESTING

## Step 15: Verify All Assignments

### Create Checklist
Go through and verify each of these are assigned:

**GameManager:**
- [ ] OnPlayerLeftEvent assigned
- [ ] OnRunnerShutDownEvent assigned

**CharacterDatabase:**
- [ ] All 6 characters in array (Character_1 through Character_6)

**GameLauncher:**
- [ ] LauncherPrefab assigned

**Launcher Prefab:**
- [ ] FusionNetworkService has all 4 events assigned
- [ ] FusionNetworkService has PlayerSessionData.prefab assigned
- [ ] FusionLauncher component exists

**PlayerSessionData.prefab:**
- [ ] OnPlayerDataSpawnedEvent assigned

**LobbyCanvas:**
- [ ] All 4 panels (InitPanel, LobbyPanel, CharacterSelectionPanel, GameLobbyPanel)
- [ ] All 16+ UI element fields assigned
- [ ] All 4 events assigned
- [ ] CharacterSelectionManagerPrefab assigned

**CharacterSelectionPanel:**
- [ ] All 5 inspector fields assigned
- [ ] CanvasGroup component exists

**CharacterSelectionSlot.prefab:**
- [ ] Button component exists (auto-created from UI → Button)
- [ ] CharacterSelectionSlot component added
- [ ] Image and Text children exist (auto-created by Button)
- [ ] (Optional) SelectionIndicator child for visual feedback

**All Buttons:**
- [ ] HostButton → SetGameMode(0)
- [ ] JoinButton → SetGameMode(1)
- [ ] CreateJoinButton → StartLauncher()
- [ ] StartButton → StartGame()
- [ ] LoadGameButton → LoadGame()
- [ ] LeaveButton(s) → LeaveLobby()
- [ ] BackButton → SetGameMode(-1)
- [ ] ExitButton → ExitGame()

**Fusion Settings:**
- [ ] PlayerSessionData.prefab registered
- [ ] CharacterSelectionManager.prefab registered

---

## Step 16: Test Setup

### Solo Test (Host)
1. Play scene
2. Click **Host**
3. Enter nickname "Player1"
4. Enter room "TestRoom"
5. Click **Create**
6. **Verify:**
   - Lobby panel appears
   - Shows "Player1 (You)"
   - Start button visible
7. Click **Start Game**
8. **Verify:**
   - Character panel appears
   - Timer shows ~30s
   - 6 character slots visible
   - Selection status shows "Selected: 0/1"
9. Click any character
10. **Verify:**
    - Character highlights (green)
    - Status shows "Selected: 1/1"
    - Panel auto-hides
    - Game Lobby appears showing "Player1 (You) - [Character Name]"
    - Load Game button visible

### Multi-Player Test
1. **Player 1 (Host):** Follow solo test above, but stop at Character Selection
2. **Player 2 (Client in another instance):**
   - Click **Join**
   - Enter nickname "Player2"
   - Enter room "TestRoom"
   - Click **Join**
3. **Both players in Lobby:**
   - Both see each other's names
   - Player 1 sees Start button
4. **Player 1 clicks Start Game**
5. **Both see Character Selection panel with 30s timer**
6. **Player 1 selects Warrior**
   - Both see Warrior greyed out
   - Status shows "Selected: 1/2"
7. **Player 2 selects Mage**
   - Mage greyed out for both
   - Status shows "Selected: 2/2"
   - **Panel auto-completes and hides (30s not needed)**
8. **Game Lobby appears for both:**
   - Player 1 sees: "Player1 (You) - Warrior" + "Player2 - Mage"
   - Player 2 sees: "Player1 - Warrior" + "Player2 (You) - Mage"
   - Only Player 1 sees Load Game button

---

## Step 17: (Optional) Implement LoadGame()

Edit LobbyCanvas.cs, find `LoadGame()` method, replace TODO:

```csharp
public void LoadGame()
{
    var runner = Networking.Services.FusionNetworkService.LocalRunner;

    if (!runner.IsServer)
    {
        Debug.LogWarning("[LobbyCanvas] Only the host can load the game.");
        return;
    }

    Debug.Log("[LobbyCanvas] Host loading game. All players ready with selected characters.");
    
    // TODO: Implement your game scene loading logic
    // Example options:
    // 1. SceneManager.LoadScene("GameScene");
    // 2. Use a LoadingManager to load async
    // 3. Spawn player instances based on selected character IDs from PlayerSessionData
}
```

---

# REFERENCE: Complete Setup Checklist

## Folder Structure
```
✅ Assets/Project/Resources/
   ✅ Characters/ (6 CharacterConfig assets)
   ✅ Events/ (4 FusionEvent assets)
   ✅ Prefabs/ (4 network prefabs)
```

## Scene GameObjects
```
✅ GameManager (with component + events assigned)
✅ CharacterDatabase (with 6 characters assigned)
✅ GameLauncher (with LauncherPrefab assigned)
✅ Canvas
   ✅ LobbyCanvas (component + all fields assigned)
   ✅ InitPanel
   ✅ LobbyPanel
   ✅ CharacterSelectionPanel (component + fields assigned)
   ✅ GameLobbyPanel
```

## Prefabs Created
```
✅ Assets/Project/Resources/Prefabs/Launcher.prefab
✅ Assets/Project/Resources/Prefabs/PlayerSessionData.prefab
✅ Assets/Project/Resources/Prefabs/CharacterSelectionManager.prefab
✅ Assets/Project/Resources/Prefabs/CharacterSelectionSlot.prefab
```

## Assets Created
```
✅ 6 × CharacterConfig in Assets/Project/Resources/Characters/
✅ 6 × FusionEvent in Assets/Project/Resources/Events/
```

## Inspector Assignments Completed
```
✅ GameManager: 2 events
✅ CharacterDatabase: 6 characters
✅ GameLauncher: 1 prefab
✅ Launcher.prefab: Events + PlayerSessionData
✅ PlayerSessionData.prefab: 2 events
✅ LobbyCanvas: 17 fields + 4 events
✅ CharacterSelectionPanel: 5 fields + canvas group
✅ CharacterSelectionSlot.prefab: Auto-discovers Button children (no inspector setup needed)
```

## UI Buttons Wired
```
✅ 8 total buttons wired to LobbyCanvas methods
```

## Fusion Registry
```
✅ PlayerSessionData.prefab registered
✅ CharacterSelectionManager.prefab registered
```

---

**You're all set!** Your multiplayer networking system with character selection is ready to test. 🚀
>>>>>>> projects-logic

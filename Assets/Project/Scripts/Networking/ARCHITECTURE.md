# Networking System - Architecture Documentation

## Design Principles

This system follows **SOLID Principles** and **Clean Architecture** patterns to ensure maintainability, testability, and scalability.

### SOLID Application

#### 1. **Single Responsibility Principle**
Each class has one reason to change:

- **FusionNetworkService**: Translate Fusion callbacks → events only
- **SessionManager**: Manage game state transitions only
- **PlayerSessionCache**: Store/retrieve cached sessions only
- **LobbyUIController**: Handle input routing only
- **LobbyUIPresenter**: Update UI display only

#### 2. **Open/Closed Principle**
System is open for extension, closed for modification:

- New services can be added without modifying existing ones
- Event-based communication allows new listeners without touching producers
- `ICacheProvider` interface (stub) enables cache implementation swapping
- `INetworkRunnerCallbacks` already extensible in Fusion

#### 3. **Liskov Substitution Principle**
All implementations are interchangeable:

- `PlayerSessionCache` can be replaced with `DatabaseCache` (future)
- `FusionNetworkService` can be replaced with `SteamNetworking` service
- `SessionManager` state machine is contract-based

#### 4. **Interface Segregation Principle**
Clients depend on specific interfaces, not monolithic contracts:

- Services expose only relevant methods (e.g., `GetPlayerProfile()` not entire cache)
- UI components depend on event subscriptions, not service internals
- `INetworkRunnerCallbacks` breaks into specific callback methods

#### 5. **Dependency Inversion Principle**
High-level modules depend on abstractions:

- Services use events, not direct references to listeners
- `SessionCacheProvider` abstracts cache creation/access
- `NetworkEventDefinitions` decouples event reference management

---

## Architectural Patterns

### 1. **Event-Driven Architecture**
Central to system operation:

**Pattern**: Observer pattern via ScriptableObject events.

**Benefits**:
- Loose coupling: producers don't know about consumers
- Centralized event management: all events in `NetworkEventDefinitions`
- Inspector-friendly: wire events in UI without code
- Testable: mock events easily
- Extensible: add listeners without modifying producers

**Flow**:
```
FusionNetworkService.OnPlayerJoined()
  ↓
_onPlayerJoinedEvent.Raise(player, runner)
  ↓
All registered listeners called
  ├─ PlayerSessionService.RegisterPlayerSession()
  ├─ LobbyUIController.OnPlayerJoined()
  └─ (Any other registered handler)
```

### 2. **Service Layer Pattern**
Encapsulates business logic:

**Services**:
- `FusionNetworkService`: Network callback translation
- `FusionConnectionManager`: Connection lifecycle
- `SessionManager`: Game state machine
- `PlayerSessionService`: Player session management
- `RoomManagementService`: Room operations

**Benefits**:
- Single point of logic for each concern
- Easy to mock for testing
- Reusable across UI/gameplay code
- Clear API surface

### 3. **Cache With TTL (Time-To-Live)**
Offline recovery without persistence:

**Pattern**: In-memory cache with automatic eviction.

**Implementation**:
```
PlayerSessionCache
  ├─ Dictionary<PlayerRef, CacheEntry>
  └─ CacheEntry
      ├─ PlayerRef
      ├─ PlayerProfile
      ├─ SessionInfo
      ├─ IsOffline flag
      └─ CacheEntryLifetime (handles TTL)
```

**Benefits**:
- Fast in-memory lookup
- No database dependency
- Automatic cleanup (no memory leaks)
- Configurable TTL per entry

**Typical Lifecycle**:
```
Player joins → RegisterPlayerSession() → Cached with fresh TTL
    ↓
Player plays → Data available instantly
    ↓
Player disconnects → MarkPlayerOffline() → TTL timer starts
    ↓
Player reconnects (within TTL) → GetPlayerSession() → Recovered
    ↓
TTL expires → Auto-evicted on next query or cleanup call
```

### 4. **State Machine for Game States**
Tracks gameplay phases:

**States**:
- `Disconnected`: Not in any session
- `Lobby`: In session, waiting for game start
- `Loading`: Scene/level loading
- `Playing`: Active gameplay

**State Transitions**:
```
Disconnected
    ↓ [Connect]
Lobby
    ↓ [StartGame]
Loading
    ↓ [SceneLoaded]
Playing
    ↓ [Disconnect]
Disconnected
```

**Implementation**:
```csharp
public void SetGameState(GameState newState)
{
    if (_currentState == newState) return;
    _currentState = newState;
    // Raise events for each state
    switch (newState)
    {
        case GameState.Loading:
            _onSceneLoadStartEvent?.Raise();
            break;
        // ...
    }
}
```

### 5. **Presenter Pattern for UI**
Separates input from display logic:

**Components**:
- **LobbyUIController**: Input handler (buttons, fields)
  - No business logic
  - Calls into services/presenter
  - Responds to user actions

- **LobbyUIPresenter**: Display logic (update UI)
  - No input handling
  - Listens to events
  - Updates text, button states, etc.

**Benefits**:
- Testability: mock servicess independently
- Reusability: same presenter with different controller
- Clarity: clear data flow (input → service → display)

### 6. **Singleton Pattern for Managers**
Per your specification, managers use singleton pattern:

**Managers**:
- `GameManager.Instance`
- `SessionManager.Instance`
- `PlayerSessionService.Instance`
- `RoomManagementService.Instance`

**Advantages**:
- Easy global access (familiar pattern)
- Single instance guaranteed
- DontDestroyOnLoad automatic

**Alternatives** (for future):
- Dependency injection container (Zenject)
- Service locator pattern
- Constructor injection

---

## Data Flow Diagrams

### Connection Flow
```
┌─────────────────┐
│   LobbyUI       │
│  (Controller)   │
└────────┬────────┘
         │ (OnStartGameClicked)
         ↓
┌─────────────────────────────┐
│ FusionConnectionManager      │
│ Connect(mode, roomName, etc) │
└────────┬────────────────────┘
         │ (StartGame)
         ↓
    ┌─────────────┐
    │ NetworkRunner│
    └────┬────────┘
         │ (OnPlayerJoined callback)
         ↓
┌──────────────────────────────┐
│ FusionNetworkService         │
│ OnPlayerJoined()             │
└────────┬─────────────────────┘
         │ (.Raise())
         ↓
┌─────────────────────────────────────┐
│ OnPlayerJoinedEvent (FusionEvent)   │
└────────┬────────────────────────────┘
         │ (All listeners called)
         ├──→ PlayerSessionService.RegisterPlayerSession()
         ├──→ LobbyUIController.OnPlayerJoined()
         └──→ SessionManager (potential listener)
```

### Offline Recovery Flow
```
Player in-game
    ↓ (Network drops)
NetworkRunner.OnPlayerLeft()
    ↓
FusionNetworkService.OnPlayerLeft() → Raise event
    ↓
GameManager.OnPlayerDisconnected()
    ↓
PlayerSessionService.MarkPlayerOffline()
    ↓
PlayerSessionCache marks IsOffline=true, resets TTL
    ↓ (Player can reconnect within TTL)
┌──────────────────────────────┐
│ Option A: Within TTL         │
│ PlayerSessionService         │
│ .GetPlayerProfile() → Found  │
│ (Data recovered)             │
└──────────────────────────────┘
    or
┌──────────────────────────────┐
│ Option B: TTL Expired        │
│ Cache auto-evicts entry      │
│ (Data lost, new session)     │
└──────────────────────────────┘
```

---

## Dependency Graph

```
FusionNetworkService
    ├─ Fusion (INetworkRunnerCallbacks)
    ├─ FusionEvent (OnPlayerJoined, OnPlayerLeft, etc.)
    └─ NetworkRunner (static: LocalRunner)

FusionConnectionManager
    ├─ FusionNetworkService (implicitly via events)
    ├─ FusionEvent (OnConnectionStatusChanged)
    └─ NetworkRunner (creates and manages)

SessionManager
    ├─ FusionEvent (OnGameStateChanged, etc.)
    └─ (No service dependencies)

PlayerSessionService
    ├─ SessionCacheProvider (provides PlayerSessionCache)
    ├─ Networking.Models (PlayerProfile, SessionInfo)
    ├─ FusionEvent (OnPlayerSessionCached, OnPlayerOffline)
    └─ Fusion (PlayerRef)

RoomManagementService
    ├─ NetworkRunner (via FusionNetworkService ref)
    ├─ FusionEvent (OnRoomPropertiesChanged, OnPlayerKicked)
    └─ Fusion (GameMode, PlayerRef)

GameManager
    ├─ SessionManager (dependency inject)
    ├─ PlayerSessionService (dependency inject)
    ├─ FusionConnectionManager (dependency inject)
    ├─ FusionEvent (OnPlayerLeft, OnRunnerShutdown)
    └─ (Legacy: PlayerBehaviour, CameraManager)

LobbyUIController
    ├─ LobbyUIPresenter (references)
    ├─ FusionConnectionManager (find)
    ├─ FusionEvent (OnShutdown, OnPlayerJoined)
    └─ (Legacy: GameManager, LevelManager)

LobbyUIPresenter
    ├─ PlayerSessionService (find)
    ├─ FusionEvent (OnPlayerJoined, OnPlayerLeft, etc.)
    └─ UI elements (TextMeshProUGUI, Button)

ConnectionStatusView
    ├─ FusionConnectionManager (find)
    ├─ FusionEvent (OnConnectionStatusChanged)
    └─ UI element (TextMeshProUGUI)

NetworkSceneManager
    ├─ NetworkSceneManagerDefault (extends)
    ├─ GameManager (find)
    ├─ SessionManager (find)
    ├─ FusionLauncher (legacy ref)
    ├─ FusionEvent (OnSceneLoadStart, OnSceneLoadComplete)
    └─ LoadingManager (legacy ref)
```

**Dependency Direction**: Arrows point downward (high-level depends on low-level only via abstraction)

---

## Extensibility Points

### 1. Add Custom Events
```csharp
// In NetworkEventDefinitions.cs
[Header("Custom Events")]
public FusionEvent OnGameStartedEvent;
public FusionEvent OnPlayerScoredEvent;

// Raise from any service
_onGameStartedEvent?.Raise();
_onPlayerScoredEvent?.Raise(playerRef, runner);
```

### 2. Implement Custom Cache
```csharp
// Create ICacheProvider interface (future)
public interface ICacheProvider
{
    void RegisterSession(...);
    PlayerProfile GetPlayerProfile(...);
}

// Implement with database
public class DatabaseCache : ICacheProvider { ... }

// Inject via SessionCacheProvider
```

### 3. Add Persistence to Cache
```csharp
// Wrap cache with serialization
public class PersistentSessionCache : PlayerSessionCache
{
    public override void RegisterPlayerSession(...)
    {
        base.RegisterPlayerSession(...);
        SaveToPlayerPrefs(...); // or disk
    }
}
```

### 4. Listen to Specific Events
```csharp
// Any script can subscribe
var eventDefs = NetworkEventDefinitions.Instance;
eventDefs.OnPlayerJoinedEvent.RegisterResponse((player, runner) =>
{
    Debug.Log($"Player {player} joined");
});
```

### 5. Extend with Matchmaking
```csharp
// Add methods to RoomManagementService
public List<SessionInfo> GetAvailableRooms(string filter)
{
    return _runner.SessionList.Where(s => s.IsOpen).ToList();
}

public void JoinRandomRoom() { ... }
```

---

## Performance Considerations

### Cache Performance
- **Insert**: O(1) - dictionary lookup
- **Query**: O(1) - dictionary lookup + cleanup on-demand
- **Eviction**: O(n) - linear scan of cache on cleanup (deferred)

### Event Performance
- **Raise**: O(n) - iterates all listeners sequentially
- **Register**: O(1) - list append
- **Unregister**: O(n) - list search and remove

### Memory
- **Cache Entry**: ~200 bytes per player (profile + metadata)
- **Default**: 4 players × 200 bytes = ~800 bytes
- **Max**: 100 players × 300s TTL auto-evicts old entries

### Network
- **PlayerSessionData RPC**: Only on nick/avatar change (minimal bandwidth)
- **Events**: Local only (no network transmission)
- **Scene Manager**: Coordinated via Fusion's scene system

---

## Testing Strategy

### Unit Tests
- `FusionNetworkServiceTests`: Mock runners, events
- `PlayerSessionCacheTests`: Cache operations, TTL
- `SessionManagerTests`: State transitions
- (See `NetworkingTests.cs` for scaffolding)

### Integration Tests
- Connection flow: FusionConnectionManager → FusionNetworkService → event listeners
- Offline recovery: Register → Disconnect → Cache check → TTL expiry
- State transitions: Multiple state changes and event propagation

### Manual Tests
1. **Connection Flow**: Connect → lobby → scene load → gameplay
2. **Offline Recovery**: Disconnect mid-game → reconnect → verify data
3. **Multiple Players**: 2-4 players with various disconnect/reconnect scenarios
4. **UI Responsiveness**: Button clicks, real-time player list updates
5. **Memory**: Play for 10+ minutes, check for leaks

---

## Potential Issues & Mitigations

### Issue: Static LocalRunner Reference
**Problem**: Tight coupling, hard to test
**Mitigation**: Eventual refactor to event-based pattern; keep for backward compat now

### Issue: FindObjectOfType Calls
**Problem**: Slow, creates hidden dependencies
**Mitigation**: Dependency inject in Awake; use for fallback only

### Issue: Cache Network Out of Sync
**Problem**: If player reconnects with modified data, cache serves old data
**Mitigation**: Validate cache on rejoin; optional: query server for latest

### Issue: TTL Not Exact
**Problem**: Time.realtimeSinceStartup variance in editor vs build
**Mitigation**: TTL is approximate; grace period with fallback logic acceptable

### Issue: Event Memory Leaks
**Problem**: Listeners don't unsubscribe
**Mitigation**: Always unsubscribe in OnDisable(); use event validation tool

---

## Migration from OtherGame Reference

The refactoring extracted and reorganized these reference scripts:

| Reference File | New Location | Changes |
|---|---|---|
| `GameManager.cs` | `Networking/Managers/GameManager.cs` | Decoupled from PlayerData storage → use PlayerSessionService |
| `FusionLauncher.cs` | `Networking/Services/FusionConnectionManager.cs` | Extracted connection logic; improved error handling |
| `FusionHelper.cs` | `Networking/Services/FusionNetworkService.cs` | Renamed; removed static globals; pure callback handler |
| `LobbyCanvas.cs` | `Networking/UI/LobbyUIController + Presenter` | Split input/display logic |
| `PlayerData.cs` | `Networking/Models/PlayerSessionData.cs` | Enhanced with offline recovery fields |
| `LevelManager.cs` | `Networking/SceneManagement/NetworkSceneManager.cs` | Renamed; cleaner integration with SessionManager |
| `FusionEvent.cs` | `Networking/Events/FusionEvent.cs` | Moved as-is; added error handling |

**Old code structure** (tightly coupled):
```
OtherGame/
├── Managers/
│   ├── GameManager         (holds PlayerData dict)
│   ├── FusionLauncher      (static LocalRunner)
│   ├── LevelManager
│   └── LoadingManager
├── Utils/
│   └── FusionHelper        (static + callbacks)
├── Lobby/
│   └── LobbyCanvas         (UI + logic)
└── Player/
    ├── PlayerData          (network data)
    └── PlayerBehaviour
```

**New code structure** (loosely coupled, layered):
```
Networking/
├── Managers/
│   └── GameManager         (orchestration only)
├── Services/
│   ├── FusionNetworkService    (callbacks)
│   ├── FusionConnectionManager  (connection)
│   ├── SessionManager          (state)
│   ├── PlayerSessionService    (cache)
│   └── RoomManagementService   (rooms)
├── Models/
│   ├── PlayerProfile
│   ├── SessionInfo
│   └── PlayerSessionData   (network data only)
├── Cache/
│   ├── PlayerSessionCache
│   ├── CacheEntryLifetime
│   └── SessionCacheProvider
├── Events/
│   ├── FusionEvent         (event bus)
│   └── NetworkEventDefinitions  (registry)
└── UI/
    ├── LobbyUIController   (input)
    ├── LobbyUIPresenter    (display)
    └── ConnectionStatusView
```

---

## Future Enhancements

### Phase 2: Input & Movement
- Implement `INetworkInput` in FusionNetworkService
- Create InputController with device input polling
- Add PlayerMovementController for physics

### Phase 3: Gameplay Systems
- Extend SessionManager with game round state (teams, scoring)
- Add RoomManagementService methods for ready system
- Implement spectator mode transitions

### Phase 4: Matchmaking & Discovery
- Implement SessionListUpdated callback in FusionNetworkService
- Create RoomBrowserService for session browsing
- Add skill-based matchmaking logic

### Phase 5: Persistence & Analytics
- Wrap PlayerSessionCache with database layer (ICacheProvider)
- Add event logging/telemetry service
- Implement player progression storage

### Phase 6: Advanced Networking
- Host migration (callback skeleton exists)
- Reconnection with resimulation
- lag compensation logic

---

## References

- **Event Pattern**: Observer pattern via ScriptableObjects (Unity standard)
- **State Machine**: Enum-based state management (simple, suitable for game states)
- **Cache**: Wikipedia: Time-to-live, Cache replacement policy
- **Architecture**: Clean Architecture (Robert C. Martin), Hexagonal Architecture
- **SOLID**: Robert C. Martin's SOLID principles
- **Patterns**: Gang of Four design patterns adapted for game development


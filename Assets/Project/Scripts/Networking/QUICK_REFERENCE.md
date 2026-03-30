# Networking System - Quick Reference

## Most Common Tasks

### 1. Connect to a Session
```csharp
var connectionManager = FindObjectOfType<FusionConnectionManager>();
var levelManager = FindObjectOfType<LevelManager>();

connectionManager.Connect(
    GameMode.Host,  // or GameMode.Client, GameMode.Shared
    "MyRoomName",
    levelManager     // INetworkSceneManager
);
```

### 2. Get Connected Player Count
```csharp
var runner = FusionNetworkService.LocalRunner;
if (runner != null)
{
    int playerCount = runner.ActivePlayers.Count;
    Debug.Log($"Players online: {playerCount}");
}
```

### 3. Get a Specific Player's Profile
```csharp
var sessionService = FindObjectOfType<PlayerSessionService>();
var profile = sessionService.GetPlayerProfile(playerRef);

if (profile != null)
{
    Debug.Log($"Player: {profile.Nickname}, ID: {profile.UniqueId}");
}
```

### 4. Check If Player Is Offline (In Cache)
```csharp
var sessionService = FindObjectOfType<PlayerSessionService>();
var (profile, _, isOffline) = sessionService._cache.GetPlayerSession(playerRef);

if (isOffline)
{
    Debug.Log("Player is marked offline but cached");
}
```

### 5. Close the Room (Server Only)
```csharp
var roomService = FindObjectOfType<RoomManagementService>();
roomService.CloseRoom();  // Prevents new joiners while game is playing
```

### 6. Kick a Player
```csharp
var roomService = FindObjectOfType<RoomManagementService>();
roomService.KickPlayer(playerRef);
```

### 7. Get Current Game State
```csharp
var gameManager = GameManager.Instance;
var state = gameManager.GetGameState();

if (state == GameManager.GameState.Playing)
{
    Debug.Log("Gameplay active");
}
```

### 8. Listen to Player Joins
```csharp
public void OnEnable()
{
    var events = NetworkEventDefinitions.Instance;
    events.OnPlayerJoinedEvent.RegisterResponse(OnAnyPlayerJoined);
}

private void OnAnyPlayerJoined(PlayerRef player, NetworkRunner runner)
{
    Debug.Log($"Player {player} joined!");
}
```

### 9. Save Player Nickname for Next Session
```csharp
// Automatically saved in PlayerPrefs by PlayerSessionData.Spawned()
// Already logged: PlayerPrefs.SetString("Nick", nickname);

// To retrieve:
string savedNick = PlayerPrefs.GetString("Nick", "Player");
```

### 10. Disconnect from Session
```csharp
var connectionManager = FindObjectOfType<FusionConnectionManager>();
connectionManager.Disconnect();

// Or via GameManager:
GameManager.Instance.LeaveRoom();
```

---

## Event Subscription Pattern

### Subscribe (OnEnable)
```csharp
private void OnEnable()
{
    var events = NetworkEventDefinitions.Instance;
    events.OnPlayerJoinedEvent.RegisterResponse(HandlePlayerJoined);
    events.OnShutdownEvent.RegisterResponse(HandleShutdown);
}
```

### Unsubscribe (OnDisable)
```csharp
private void OnDisable()
{
    var events = NetworkEventDefinitions.Instance;
    events.OnPlayerJoinedEvent.RemoveResponse(HandlePlayerJoined);
    events.OnShutdownEvent.RemoveResponse(HandleShutdown);
}
```

### Handle Event
```csharp
private void HandlePlayerJoined(PlayerRef player, NetworkRunner runner)
{
    Debug.Log($"Player {player} joined. Total: {runner.ActivePlayers.Count}");
}
```

---

## Cache Operations

### Register Player
```csharp
var cache = SessionCacheProvider.GetCache();
var profile = new PlayerProfile("John", "unique123");
cache.RegisterPlayerSession(playerRef, profile);
```

### Mark Offline (on disconnect)
```csharp
var cache = SessionCacheProvider.GetCache();
cache.MarkPlayerOffline(playerRef);
// Player remains cached for TTL duration
```

### Recover Offline Player
```csharp
var cache = SessionCacheProvider.GetCache();
var (profile, _, isOffline) = cache.GetPlayerSession(playerRef);

if (profile != null && isOffline)
{
    // Player can be recovered
    RespawnPlayer(profile);
}
```

### Manual Cleanup
```csharp
var cache = SessionCacheProvider.GetCache();
cache.CleanupExpired();  // Remove all TTL-expired entries
```

---

## Network Runner Access

### Get Current Runner
```csharp
// Via static reference (backward compat):
var runner = FusionNetworkService.LocalRunner;

// Via connection manager:
var connectionMgr = FindObjectOfType<FusionConnectionManager>();
var runner = connectionMgr.GetRunner();

// Via callback parameter:
public void SomeCallback(PlayerRef player, NetworkRunner runner)
{
    // Use runner directly
}
```

### Check If Server
```csharp
if (FusionNetworkService.LocalRunner.IsServer)
{
    Debug.Log("This is the server");
}
```

### Check If Local Player
```csharp
var runner = FusionNetworkService.LocalRunner;
if (playerRef == runner.LocalPlayer)
{
    Debug.Log("This is you");
}
```

---

## Service Singletons

All services are available as singletons:

```csharp
GameManager gameManager = GameManager.Instance;
SessionManager sessionManager = SessionManager.Instance;
PlayerSessionService sessionService = PlayerSessionService.Instance;
RoomManagementService roomService = RoomManagementService.Instance;
```

Or find them:
```csharp
var gameManager = FindObjectOfType<GameManager>();
var sessionManager = FindObjectOfType<SessionManager>();
// etc.
```

---

## Debug Commands

Add to your console/developer panel:

```csharp
public static class NetworkDebugCommands
{
    [ConsoleMethod]
    public static void PrintActivePlayers()
    {
        var runner = FusionNetworkService.LocalRunner;
        if (runner == null) { Debug.Log("Not connected"); return; }
        
        foreach (var player in runner.ActivePlayers)
        {
            var profile = FindObjectOfType<PlayerSessionService>()
                .GetPlayerProfile(player);
            Debug.Log($"{player}: {profile?.Nickname}");
        }
    }
    
    [ConsoleMethod]
    public static void PrintCacheSize()
    {
        var cache = SessionCacheProvider.GetCache();
        Debug.Log($"Cached players: {cache.GetCacheSize()}");
    }
    
    [ConsoleMethod]
    public static void PrintGameState()
    {
        var state = GameManager.Instance.GetGameState();
        Debug.Log($"Game state: {state}");
    }
    
    [ConsoleMethod]
    public static void PrintConnectionStatus()
    {
        var manager = FindObjectOfType<FusionConnectionManager>();
        Debug.Log($"Connection: {manager.GetConnectionStatus()}");
    }
}
```

---

## Common Patterns

### Pattern 1: React to Player Disconnect
```csharp
// In any MonoBehaviour:
private void OnEnable()
{
    var events = NetworkEventDefinitions.Instance;
    events.OnPlayerLeftEvent.RegisterResponse(OnPlayerLeft);
}

private void OnPlayerLeft(PlayerRef player, NetworkRunner runner)
{
    // Player left - mark offline for recovery
    var sessionService = FindObjectOfType<PlayerSessionService>();
    sessionService.MarkPlayerOffline(player);
    
    // Update UI
    Debug.Log($"Player {player} left, but cached for recovery");
}
```

### Pattern 2: Wait for Game to Start
```csharp
// In game logic:
private void OnEnable()
{
    var events = NetworkEventDefinitions.Instance;
    events.OnGameStateChangedEvent.RegisterResponse(OnStateChanged);
}

private void OnStateChanged(PlayerRef player, NetworkRunner runner)
{
    var gameManager = GameManager.Instance;
    if (gameManager.GetGameState() == GameManager.GameState.Playing)
    {
        // Game started, enable gameplay
        StartGameplay();
    }
}
```

### Pattern 3: List All Players (Online + Offline)
```csharp
var sessionService = FindObjectOfType<PlayerSessionService>();
var profiles = new List<(PlayerRef, PlayerProfile, bool)>();
sessionService.GetAllCachedProfiles(profiles);

foreach (var (playerRef, profile, isOffline) in profiles)
{
    string status = isOffline ? "(offline)" : "(online)";
    Debug.Log($"{profile.Nickname} {status}");
}
```

---

## Performance Checklist

- [ ] Cache TTL is reasonable (300s default, adjust if needed)
- [ ] CleanupExpired() called periodically (optional, evicts on query)
- [ ] Event listeners unsubscribe in OnDisable()
- [ ] No FindObjectOfType in Update() or hot loops
- [ ] Use LocalRunner static ref for frequent access
- [ ] Minimize RPC calls for nick/avatar changes (set once)

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| "LocalRunner is null" | Check FusionNetworkService attached to Runner prefab |
| Events not firing | Verify NetworkEventDefinitions asset created and assigned |
| Cache returning null | Check TTL hasn't expired; call GetPlayerSession() to trigger eviction |
| UI not updating | Ensure Presenter is listening to correct events |
| Players not visible in lobby | Verify OnPlayerJoinedEvent is raising; check PlayerSessionData prefab assigned |
| Offline recovery not working | Confirm MarkPlayerOffline() is called on disconnect |

---

## Key Enumerations

### GameState (SessionManager)
```csharp
enum GameState
{
    Disconnected,  // Not in session
    Lobby,         // Waiting for gamestart
    Loading,       // Scene loading
    Playing        // Active gameplay
}
```

### GameState (GameManager)
```csharp
enum GameState
{
    Lobby,        // In lobby
    Playing,      // Playing
    Loading,      // Loading
    Disconnected  // Disconnected
}
```

### ConnectionStatus (FusionConnectionManager)
```csharp
enum ConnectionStatus
{
    Disconnected,  // Not connected
    Connecting,    // Attempting connection
    Failed,        // Connection failed
    Connected,     // Connected to session
    Loading,       // Scene loading
    Loaded         // Scene loaded, ready for play
}
```

---

## File Locations

Event Assets (create these in Resources):
```
Assets/Resources/Events/
├── OnPlayerJoinedEvent.asset
├── OnPlayerLeftEvent.asset
├── OnConnectionStatusChangedEvent.asset
├── ... (etc for all events)
```

Event Definitions:
```
Assets/Resources/
└── NetworkEventDefinitions.asset
```

Scripts:
```
Assets/Project/Scripts/Networking/
├── Managers/
├── Services/
├── Models/
├── Cache/
├── Events/
├── UI/
├── SceneManagement/
└── Tests/
```

---

## Next Steps

1. Create NetworkEventDefinitions asset (see SETUP_GUIDE.md)
2. Wire all event assets in Inspector
3. Test connection flow end-to-end
4. Test offline recovery scenario
5. Monitor cache performance (CacheSize)
6. Extend with custom logic as needed


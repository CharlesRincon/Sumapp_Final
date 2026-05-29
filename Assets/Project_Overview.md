# Project Overview: Sumapp Final (Unity 6 AR Multiplayer)

## 1. Project Description
**Sumapp Final** is an augmented reality (AR) multiplayer board game developed for Android. It combines local AR interactions (using Vuforia) with real-time network synchronization (via Photon Fusion). The experience is centered around environmental management, where players navigate a board representing Colombian zones, managing water and money resources while collectively maintaining the health of a "Basin." The project aims to educate or simulate resource management challenges through a competitive and collaborative social loop.

**Core Pillars:**
- **AR-Driven Interaction:** Using physical cards and markers to trigger game events and project builds.
- **Resource Management:** Balancing Water and Money at individual levels and Basin Health at a global level.
- **Hybrid Multiplayer:** Real-time synchronization of state between players using a Host-Client architecture.
- **Social Minigames:** Transitioning from strategic board-game play to fast-paced clicker minigames.

## 2. Gameplay Flow / User Loop
1.  **Boot & Lobby:** Players start in the `LobbyScene`, enter a nickname, and join or host a session.
2.  **Character Selection:** Players select unique characters from a database, which are synced across all clients.
3.  **Turn-Order Initialization:** Everyone rolls a dice once to determine the sequence of play.
4.  **Main Game Loop (The Round):**
    - **Active Turn:** The current player rolls a dice and moves their piece.
    - **Tile Resolution:** Landing on a tile triggers effects (Hydric, Catastrophic, Trivia, Draw Card, or Project).
    - **AR Scanning:** If on a "Card" or "Project" tile, the player must scan a physical marker using the camera.
    - **Decision Making:** Players may face individual or collective decisions that alter the game state.
5.  **Minigame Phase:** After all players complete their turns, the game transitions to a `Minigame` scene for a competitive clicker challenge to earn extra rewards.
6.  **End of Match:** The game repeats for a set number of rounds or until the Basin Health reaches zero (Defeat) or the target round is completed (Victory).

## 3. Architecture
The project utilizes a **State-Authority (Host-Client)** model provided by **Photon Fusion**. 
- **Singleton Managers:** Centralized control via `GameManager` and `MinigameManager`.
- **Service-Oriented Logic:** Decoupled logic for specific domains like `BasinService`, `TileService`, and `FusionNetworkService`.
- **Event-Driven UI:** Uses a custom `FusionEvent` system (based on ScriptableObjects) to decouple networking logic from UI updates.
- **Data-Driven Design:** Game balance, card effects, and project costs are stored in ScriptableObject databases (`CardDatabase`, `ProjectDatabase`, `TriviaDatabase`).

`Location: Assets/Project/Scripts/Networking/`

## 4. Game Systems & Domain Concepts

### Game State Management
The `GameManager` is the brain of the project, managing a complex state machine (`Lobby`, `Setup`, `CharacterSelection`, `PlayerTurn`, `TileResolve`, etc.). It handles turn advancement, round transitions, and global modifiers (Weather, Droughts, etc.).
- `GameManager`: Core state authority and resource coordinator.
- `PlayerSessionData`: A `NetworkBehaviour` attached to each player that holds all synced variables (Water, Money, Position, Shield).
`Location: Assets/Project/Scripts/Networking/Managers/`

### AR Interaction System
Leverages **Vuforia Engine** to bridge physical assets with digital logic.
- `VuforiaCardScanner`: Listens for Vuforia target tracking and triggers `RPC_RequestCardScan` on the `GameManager`.
- `VuforiaProjectScanner`: Specifically handles scanning for project-related markers.
`Location: Assets/Project/Scripts/Networking/UI/`

### Resource & Basin System
A dual-layer resource system where players manage private wealth and public health.
- `BasinService`: Manages the shared Basin Health value.
- `TileService`: Determines the outcome of landing on specific board positions based on a `BoardTileConfig`.
- `Passive Effects`: Projects owned by players generate income at the end of each round based on the "Zone" they are in (Andean, Caribbean, etc.).
`Location: Assets/Project/Scripts/Networking/Services/`

### Minigame System
A separate competitive phase where players compete in a timed clicker game.
- `MinigameManager`: Tracks `MinigameClickCount` across all players using Fusion's `[Networked]` properties.
- `RPC_IncrementMinigameClickCount`: Validates clicks on the host to prevent cheating.
`Location: Assets/Project/Scripts/Networking/Managers/`

## 5. Scene Overview
1.  **LobbyScene:** The entry point. Handles networking initialization, character selection, and the main board game logic. It contains the 3D board representation and AR camera setup.
2.  **Minigame:** A lightweight scene focused on the clicker challenge. It uses the same `PlayerSessionData` objects but a different `MinigameManager`.
- **Scene Flow:** `LobbyScene` -> `Minigame` -> `LobbyScene` (Repeated until Victory/Defeat).

## 6. UI System
The project uses **uGUI** combined with a custom event-based binding logic.
- **LobbyCanvas:** Manages the main menu and networking UI.
- **TurnOrderPanel:** Displays current player status, dice results, and weather effects.
- **ProjectFlowUIController / TriviaUIController:** Context-sensitive UI that appears when a player needs to make a decision or answer a question.
- **AnimationsLogic:** Uses `LeanTween` for smooth transitions of UI elements and resource popups.
`Location: Assets/Project/Scripts/Networking/UI/`

## 7. Asset & Data Model
- **ScriptableObjects:** 
    - `CardDefinition`: Defines water/money/basin deltas, weather effects, and decision logic.
    - `ProjectDefinition`: Defines costs and income per zone.
    - `CharacterConfig`: Visual and metadata for playable characters.
    - `NetworkEventDefinitions`: Central registry for `FusionEvent` assets.
- **Prefabs:**
    - `PlayerSessionData`: Spawned automatically by Fusion when a player joins.
    - `Dice`: Physical dice object with physics-based rolling logic.

## 8. Notes, Caveats & Gotchas
- **State Authority:** Only the Host (State Authority) can modify `[Networked]` variables. Clients must use `RPC` calls to request changes.
- **AR Target Persistence:** The `VuforiaCardScanner` includes a cooldown (`_scanCooldownSeconds`) to prevent accidental double-scanning of cards.
- **Scene Persistence:** `GameManager` and `PlayerSessionData` are marked with `DontDestroyOnLoad` to maintain state between the Lobby and Minigame scenes.
- **Weather Overlays:** Active weather effects can modify nearly all resource gains. If a new weather card is scanned, it clears the previous weather regardless of duration remaining duration.
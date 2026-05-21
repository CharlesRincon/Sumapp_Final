# Project Overview
- Game Title: Sumak
- High-Level Concept: Multiplayer AR board game about protecting water sources.
- Players: Multiplayer (Photon Fusion).
- Render Pipeline: PC_RPAsset (likely URP based on context).
- Target Platform: Android.

# Game Mechanics
## Core Gameplay Loop
Players move on a board, land on tiles (Hydric, Catastrophic, Project, Card, Trivia), and make decisions to manage their resources (Water, Money) and the shared Basin health.
## Turn Order and Weather
At the start of the game, players roll for turn order. Certain cards (like Card_11: Arcoíris de Sía) trigger a "Weather Roll" at the start of a player's turn, which provides rewards or penalties based on a D6 roll.

# UI
- **TurnSubPanel (TurnNotificationPanel)**: Shows "¡Es tu turno!" when a player's turn begins.
- **TurnOrderPanel**: Used for initial turn order and also for displaying Weather Roll results.

# Key Asset & Context
- `Assets/Project/Scripts/Networking/UI/LobbyCanvas.cs`: Manages UI states and turn notifications.
- `Assets/Project/Scripts/Networking/UI/TurnOrderPanel.cs`: Displays the roll results.
- `Card_11`: ScriptableObject that triggers the weather roll effect.

# Implementation Steps
## 1. Update LobbyCanvas.cs
Add a public property `IsWaitingForTurnNotification` to allow other components to know if a turn start notification is pending for the local player.
- **File**: `Assets/Project/Scripts/Networking/UI/LobbyCanvas.cs`
- **Change**: Add `public bool IsWaitingForTurnNotification => IsLocalPlayerTurn && !_shownTurnNotificationThisTurn;` (and a helper property `IsLocalPlayerTurn`).

## 2. Update TurnOrderPanel.cs
Modify `ShowWeatherRollResult` to wait if a turn notification is active OR pending.
- **File**: `Assets/Project/Scripts/Networking/UI/TurnOrderPanel.cs`
- **Change**:
    - Update `ShowWeatherRollResult` to check `lobbyCanvas.IsWaitingForTurnNotification`.
    - Update `DelayWeatherRollUntilNotificationHidden` to also wait while `IsWaitingForTurnNotification` is true.

## 3. Verification & Testing
- **Test Case 1**: Start a game. Ensure the initial TurnOrderPanel shows correctly (this shouldn't change).
- **Test Case 2**: Scan or trigger Card_11 (Arcoíris de Sía). Wait for the next turn.
- **Test Case 3**: When the turn starts, the "¡Es tu turno!" notification should appear first.
- **Test Case 4**: After the notification disappears, the TurnOrderPanel should appear with the weather roll result.

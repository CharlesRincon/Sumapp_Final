# Project Overview
The goal is to ensure that minigame scores (stored in `MinigameClickCount` on `PlayerSessionData`) are local to each minigame session. The user reported that specifically after `WeatherMinigame`, scores start accumulating across different minigames.

# Key Asset & Context
- `MinigameManager.cs`: Base class for the clicker minigame.
- `PipeMinigameManager.cs`: Manager for the pipe repair minigame.
- `RainMinigameManager.cs`: Manager for the rain drop catching minigame.
- `WeatherMinigameManager.cs`: Manager for the weather trivia minigame.
- `PlayerSessionData.cs`: Holds the networked `MinigameClickCount` property.

# Implementation Steps

## 1. Standardize Score Resetting (Enforce Independent Sessions)
We will remove all "relative score" logic (baselines/offsets) and ensure every minigame manager resets the global `MinigameClickCount` to 0 upon spawning on the host.

### Update `WeatherMinigameManager.cs`
- Verify that `ResetAllPlayerScores()` is called in `Spawned()`.
- **Note:** It currently is, but we must ensure it's effective for all players in the runner.

### Update `PipeMinigameManager.cs`
- **Change:** Remove the `_resetScoreOnSpawn` inspector toggle and the `_startingClickCounts` baseline logic.
- **Change:** Always call `ResetAllPlayerClickCounts()` in `Spawned()` on the host.
- **Change:** Simplify `GetPlayerRepairCount` to return `playerData.MinigameClickCount` directly.
- **Change:** Update `GetLeaderboard` to sort by `data.MinigameClickCount`.

### Update `RainMinigameManager.cs`
- **Change:** Remove the `_resetScoreOnSpawn` toggle and `_startingClickCounts`.
- **Change:** Always call `ResetAllPlayerScores()` in `Spawned()` on the host.
- **Change:** Simplify `GetAllPoints` and `GetLeaderboard` to use `MinigameClickCount` directly.

### Update `MinigameManager.cs` (Clicker)
- Ensure it continues to reset scores in `Spawned()`.

## 2. Global Reset Strategy in GameManager
To prevent any carry-over from previous scenes, we will also clear the minigame score in the `GameManager` when preparing for a new minigame phase.

### Update `GameManager.cs`
- Find the logic that transitions players to the minigame (e.g., `PrepareMinigame`).
- Add a loop to reset `MinigameClickCount = 0` for all players before scene loading.

# Verification & Testing
1. **Weather to Pipe Transition:**
   - Play Weather minigame. Scores should be e.g. 50.
   - Return to lobby.
   - Enter Pipe minigame. Verify `MinigameClickCount` is 0 at the start of the scene.
2. **Cumulative Check:**
   - Ensure `MinigameClickCount` in `PlayerSessionData` is not used for anything other than the *active* minigame.

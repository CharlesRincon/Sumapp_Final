# Project Overview
The `RainMinigame` has three main issues:
1. Raindrops spawn outside the visible screen.
2. Raindrops only fall correctly for the host (because they are networked but not properly synced).
3. The minigame is shared: if one player clicks a drop, it disappears for everyone.

The user wants the minigame to be **individual**: each player should have their own raindrops that don't affect others.

# Implementation Steps

## 1. De-network the Raindrops
We will convert `RainDrop` from a `NetworkBehaviour` to a local `MonoBehaviour`. This solves the "shared" problem and the "host-only falling" problem.

### Modify RainDrop.cs
- Change class inheritance to `MonoBehaviour`.
- Change `Spawned()` to `Start()` (or `Awake()`).
- Change `FixedUpdateNetwork()` to `Update()`.
- Replace `Runner.Despawn(Object)` and `RPC_RequestDespawn` with `Destroy(gameObject)`.
- Keep the call to `data.RPC_AddMinigamePoints(points)` to sync the score.

## 2. Local Spawning in RainMinigameManager.cs
We will modify the manager to spawn drops locally on every client instead of only on the host.

### Modify RainMinigameManager.cs
- Remove `if (!Object.HasStateAuthority)` from `FixedUpdateNetwork()`'s spawn logic, but **keep it** for the timer/game state management.
- The host will manage the `RemainingTime` and `GameActive` state (which are `[Networked]`).
- Clients will check these `[Networked]` properties in their own `Update()` or `FixedUpdateNetwork()` to decide when to spawn drops locally.
- Use `Instantiate` instead of `Runner.Spawn`.
- Parent the instantiated drops to the `DropsContainer`.

## 3. Dynamic Spawn Range
We will calculate the spawn range based on the actual UI container size.

### Modify RainMinigameManager.cs
- Add a method to find the `DropsContainer` and calculate `_spawnXRange` and `_spawnY` dynamically.
- Use `container.rect.width` to ensure drops always spawn within the visible screen area.

# Verification & Testing
1. **Individual Play (Multiplayer)**:
    - Run Host and Client.
    - Verify that Host and Client see different raindrops.
    - Verify that clicking a drop on Host does NOT remove any drop on Client.
2. **Score Sync**:
    - Verify that points earned on Client are correctly reflected in the leaderboard (synced via `RPC_AddMinigamePoints`).
3. **Layout Check**:
    - Verify raindrops spawn within the horizontal bounds of the screen on different resolutions.

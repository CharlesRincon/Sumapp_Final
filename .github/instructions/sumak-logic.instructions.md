---
applyTo: "**/*.cs"
description: "Sumak game logic — state machine, systems, data models, AR integration"
---

# SUMAK — Game Logic Documentation

> **Proyecto de grado:** "Sumak: Juego de mesa en realidad aumentada para fomentar la protección de fuentes hídricas"
> Universidad San Buenaventura de Bogotá — Ingeniería Multimedia
> Stack: Unity 6000.3.11f1 · Photon Fusion 2.0 · C# 9.0 · .NET Standard 2.1 · Android 8.0+

---

## Table of Contents

1. [System Architecture Overview](#1-system-architecture-overview)
2. [State Machine — Game Lifecycle](#2-state-machine--game-lifecycle)
3. [Data Models](#3-data-models)
4. [Core Systems](#4-core-systems)
   - 4.1 [Session & Networking](#41-session--networking)
   - 4.2 [Turn System](#42-turn-system)
   - 4.3 [Board & Tile System](#43-board--tile-system)
   - 4.4 [Shared Basin (Cuenca Compartida)](#44-shared-basin-cuenca-compartida)
   - 4.5 [Resource System](#45-resource-system)
   - 4.6 [Card System](#46-card-system)
   - 4.7 [Project System](#47-project-system)
   - 4.8 [Decision System](#48-decision-system)
   - 4.9 [Minigame System](#49-minigame-system)
   - 4.10 [Scoring & Win/Loss Conditions](#410-scoring--winloss-conditions)
5. [Event Definitions](#5-event-definitions)
6. [Augmented Reality Integration](#6-augmented-reality-integration)
7. [Scene & Flow Diagram](#7-scene--flow-diagram)
8. [Host Authority Rules](#8-host-authority-rules)
9. [Edge Cases & Pitfalls](#9-edge-cases--pitfalls)

---

## 1. System Architecture Overview

Sumak follows the **event-driven service layer** pattern established in the project conventions:

```
┌─────────────────────────────────────────────────────────────┐
│                        MANAGERS LAYER                       │
│  GameManager · TurnManager · BasinManager · ScoreManager   │
│  CardManager · ProjectManager · MinigameManager            │
│  (Consume events → mutate [Networked] state via Host)      │
└────────────────────────┬────────────────────────────────────┘
                         │ events (raise / subscribe)
┌────────────────────────▼────────────────────────────────────┐
│                        SERVICES LAYER                       │
│  FusionNetworkService · TurnService · BasinService         │
│  TileService · CardService · DecisionService               │
│  ARService · MinigameService                               │
│  (Handle network callbacks → raise NetworkEventDefinitions) │
└────────────────────────┬────────────────────────────────────┘
                         │ [Networked] auto-sync (IL Weaver)
┌────────────────────────▼────────────────────────────────────┐
│                        MODELS LAYER                         │
│  PlayerSessionData · BasinData · TileData · CardData       │
│  ProjectData · RoundData · RegionData                      │
│  (Pure data — no logic, all [Networked] where synced)      │
└─────────────────────────────────────────────────────────────┘
```

### Namespace map

| Namespace                   | Contents                                                                                                                               |
| --------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| `Sumak.Networking.Services` | `FusionNetworkService`, `TurnService`, `BasinService`, `TileService`, `CardService`, `DecisionService`, `ARService`, `MinigameService` |
| `Sumak.Networking.Managers` | `GameManager`, `TurnManager`, `BasinManager`, `CardManager`, `ProjectManager`, `ScoreManager`, `MinigameManager`                       |
| `Sumak.Networking.Models`   | `PlayerSessionData`, `BasinData`, `TileData`, `CardData`, `ProjectData`, `RoundData`, `RegionData`                                     |
| `Sumak.Networking.Events`   | `NetworkEventDefinitions` (ScriptableObject singleton)                                                                                 |
| `Sumak.Networking.UI`       | All UI controllers — `HUDController`, `TurnUIController`, `BasinUIController`, `DecisionUIController`                                  |
| `Sumak.AR`                  | `ARScanController`, `AROverlayController`                                                                                              |

---

## 2. State Machine — Game Lifecycle

The `GameManager` owns the authoritative state machine. State transitions are **host-only** (`if (Object.HasStateAuthority)`).

```
 ┌──────────────┐
 │  LOBBY       │  Players connect (2–6), host starts session
 └──────┬───────┘
        │ OnAllPlayersReady
 ┌──────▼───────┐
 │  SETUP       │  Assign regions · shuffle decks · set Basin = 100%
 └──────┬───────┘
        │ OnSetupComplete
 ┌──────▼───────────────────────────────────────────────┐
 │  ROUND  (repeats up to 10 times)                     │
 │                                                      │
 │  ┌──────────────┐                                    │
 │  │ ROLL_ORDER   │ All players roll 1d10 → sort turns │
 │  └──────┬───────┘                                    │
 │         │ OnTurnOrderResolved                        │
 │  ┌──────▼───────┐                                    │
 │  │ PLAYER_TURN  │ (loops through each player)        │
 │  │  Phase 1: ROLLING      → player rolls 1d10        │
 │  │  Phase 2: MOVING       → advance N tiles          │
 │  │  Phase 3: TILE_RESOLVE → activate tile effect     │
 │  │  Phase 4: DECISION     │ (if tile requires it)    │
 │  │  Phase 5: BASIN_CHECK  → if Basin == 0 → DEFEAT   │
 │  └──────┬───────┘                                    │
 │         │ OnAllTurnsComplete                         │
 │  ┌──────▼───────┐                                    │
 │  │ MINIGAME     │ All players participate            │
 │  └──────┬───────┘                                    │
 │         │ OnMinigameComplete                         │
 │  ┌──────▼───────┐                                    │
 │  │ PASSIVE_FX   │ Apply project passive effects      │
 │  └──────┬───────┘                                    │
 │         │ OnPassiveFxComplete                        │
 └─────────┼─────────────────────────────────────────────┘
           │
           ├── (round < 10 AND Basin > 0) → next ROUND
           │
           ├── (round == 10) → VICTORY_SCREEN
           │
           └── (Basin == 0 at any point) → DEFEAT_SCREEN
```

### GameState enum

```csharp
// Networking.Models
public enum GameState
{
    Lobby,
    Setup,
    RollOrder,
    PlayerTurn,
    TileResolve,
    Decision,
    BasinCheck,
    Minigame,
    PassiveEffects,
    Victory,
    Defeat
}
```

---

## 3. Data Models

All models in `Sumak.Networking.Models`. Properties marked `[Networked]` are auto-synced via IL Weaver to all clients.

### 3.1 PlayerSessionData

```csharp
public class PlayerSessionData : NetworkBehaviour
{
    [Networked] public PlayerRef Owner          { get; set; }
    [Networked] public int       WaterAmount    { get; set; }  // primary resource
    [Networked] public int       MoneyAmount    { get; set; }  // secondary resource
    [Networked] public int       BoardPosition  { get; set; }  // tile index (0–N)
    [Networked] public int       RegionIndex    { get; set; }  // 0–5 (Colombia regions)
    [Networked] public int       ActiveProjects { get; set; }  // bitmask (max 3 slots)
    [Networked] public int       TurnOrder      { get; set; }  // resolved per round
    [Networked] public int       DecisionScore  { get; set; }  // tiebreaker accumulator
    [Networked] public bool      HasActedThisTurn { get; set; }
}
```

### 3.2 BasinData

```csharp
public class BasinData : NetworkBehaviour
{
    [Networked] public float BasinLevel    { get; set; }  // 0.0–100.0 (percentage)
    [Networked] public bool  IsCritical    { get; set; }  // Basin <= 20%
    [Networked] public bool  IsCollapsed   { get; set; }  // Basin == 0 → defeat trigger
    [Networked] public int   CurrentRound  { get; set; }  // 1–10
    [Networked] public int   CurrentTurnIndex { get; set; } // index into turn order
}
```

### 3.3 TileData

```csharp
public enum TileType
{
    HydricZone,       // +water fixed
    EventCard,        // draw from event deck
    Trivia,           // AR scan → answer question
    CatastrophicZone, // -water fixed
    ProjectZone       // buy a project (optional)
}

[Serializable]
public struct TileData
{
    public int      TileIndex;
    public TileType Type;
    public int      RegionIndex;   // which of the 6 Colombian regions
    public string   ARMarkerID;    // empty if tile has no AR content
    public int      FixedValue;    // water delta for Hydric/Catastrophic tiles
}
```

### 3.4 CardData

```csharp
public enum CardType { Event, Project }
public enum CardTarget { Self, AllPlayers, Basin, Opponent }

[Serializable]
public struct CardData
{
    public string   CardID;
    public CardType Type;
    public string   NameKey;         // localization key
    public string   DescriptionKey;
    public CardTarget Target;
    public int      WaterDelta;      // positive = gain, negative = loss
    public int      MoneyDelta;
    public float    BasinDelta;      // basin impact (negative = harm)
    public bool     RequiresDecision;
    public int      ProjectCost;     // money cost (ProjectCards only)
    public int      PassiveWaterPerRound; // ProjectCards passive income
}
```

### 3.5 RoundData

```csharp
[Serializable]
public struct RoundData
{
    public int     RoundNumber;         // 1–10
    public int[]   TurnOrder;           // PlayerRef indices sorted by roll
    public int[]   DiceRolls;           // roll result per player (for ordering)
    public string  MinigameID;          // which minigame plays this round
    public float   BasinAtRoundStart;   // snapshot for analytics / tiebreaker
    public float   BasinAtRoundEnd;
}
```

---

## 4. Core Systems

### 4.1 Session & Networking

**Service:** `FusionNetworkService`
**Manager:** `GameManager`

| Responsibility                          | Handler                                       |
| --------------------------------------- | --------------------------------------------- |
| Host/join Fusion room (2–6 players)     | `FusionNetworkService`                        |
| Spawn `PlayerSessionData` per player    | Host only                                     |
| Spawn `BasinData` (singleton networked) | Host only                                     |
| Broadcast `GameState` changes           | `GameManager` (host authority)                |
| Handle player disconnect mid-game       | `FusionNetworkService` → `OnPlayerLeft` event |

**Key rule:** Never spawn or mutate network-critical state before `NetworkRunner` is initialized. Gate all spawns inside `OnPlayerJoined` callback.

```csharp
// FusionNetworkService.cs (pattern)
public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
    if (runner.IsServer)
    {
        var data = runner.Spawn(playerDataPrefab, inputAuthority: player);
        NetworkEventDefinitions.Instance.OnPlayerJoinedEvent?.Invoke(player, data);
    }
}
```

---

### 4.2 Turn System

**Service:** `TurnService`
**Manager:** `TurnManager`

#### Turn order resolution (start of each round)

1. Each player rolls 1d10 → result stored in `RoundData.DiceRolls[playerIndex]`
2. Sort descending → populate `RoundData.TurnOrder[]`
3. Ties: re-roll only the tied players (recursive until resolved)
4. Host broadcasts `OnTurnOrderResolvedEvent` with the final order array

#### Individual turn phases

```
Phase 1 — ROLLING
  Host grants roll permission to active player
  Player rolls 1d10 → result sent via RPC to host
  Host validates roll (anti-cheat: host re-rolls if client result is suspect)

Phase 2 — MOVING
  Host advances PlayerSessionData.BoardPosition += rollResult (mod totalTiles)
  Region is recalculated: PlayerSessionData.RegionIndex = tile.RegionIndex
  OnPlayerMovedEvent fired → clients animate piece movement

Phase 3 — TILE_RESOLVE
  TileService.ResolveTile(playerRef, tileData) called on host
  Routes to appropriate subsystem depending on TileType

Phase 4 — DECISION (conditional)
  Only if tile or card sets RequiresDecision = true
  DecisionService starts 60-second timer
  All players see options; active player (or all) vote

Phase 5 — BASIN_CHECK
  BasinManager.CheckCollapse() on host
  If BasinData.BasinLevel <= 0 → set IsCollapsed = true → GameState = Defeat
```

#### Timer enforcement

```csharp
// TurnManager.cs (host only)
private IEnumerator EnforceDecisionTimer(float seconds)
{
    yield return new WaitForSeconds(seconds);
    if (!DecisionService.HasResolved)
        DecisionService.AssignRandomVote(); // GDD §3.6: no decision = random
}
```

---

### 4.3 Board & Tile System

**Service:** `TileService`
**Asset:** `BoardConfig.asset` (ScriptableObject with `TileData[]`)

The board is a **circular path** of N tiles split across 6 Colombian regions. Players always move clockwise.

#### Tile resolution routing

```csharp
// TileService.cs
public void ResolveTile(PlayerRef player, TileData tile)
{
    switch (tile.Type)
    {
        case TileType.HydricZone:
            ResourceService.AddWater(player, tile.FixedValue);
            NetworkEventDefinitions.Instance.OnHydricZoneActivatedEvent?.Invoke(player, tile.FixedValue);
            break;

        case TileType.EventCard:
            CardService.DrawEventCard(player);
            break;

        case TileType.Trivia:
            ARService.StartTriviaSequence(player, tile.ARMarkerID);
            // 15-second timer enforced inside ARService
            break;

        case TileType.CatastrophicZone:
            ResourceService.AddWater(player, -tile.FixedValue); // negative delta
            BasinService.ApplyBasinDelta(-tile.FixedValue * BasinConfig.CatastropheBasinMultiplier);
            NetworkEventDefinitions.Instance.OnCatastrophicZoneActivatedEvent?.Invoke(player, tile.FixedValue);
            break;

        case TileType.ProjectZone:
            // Only offer purchase if player has enough money
            var playerData = GetPlayerData(player);
            if (playerData.MoneyAmount >= ProjectService.MinProjectCost)
                NetworkEventDefinitions.Instance.OnProjectZoneActivatedEvent?.Invoke(player);
            break;
    }
}
```

#### Colombian regions (6 zones)

| Index | Region    | Primary hazard                       |
| ----- | --------- | ------------------------------------ |
| 0     | Amazonía  | Deforestation                        |
| 1     | Andina    | Mining / soil erosion                |
| 2     | Caribe    | Drought / salinization               |
| 3     | Orinoquía | Monoculture / flooding               |
| 4     | Pacífico  | Over-extraction / pollution          |
| 5     | Insular   | Coral degradation / tourism pressure |

---

### 4.4 Shared Basin (Cuenca Compartida)

**Service:** `BasinService`
**Manager:** `BasinManager`
**Model:** `BasinData` (single networked instance, host authority)

The basin is the **global lose condition** and the core educational mechanic.

#### Basin mutation rules

```csharp
// BasinService.cs — all mutations go through this method; host only
public void ApplyBasinDelta(float delta)
{
    if (!Runner.IsServer) return;

    float newLevel = Mathf.Clamp(BasinData.BasinLevel + delta, 0f, 100f);
    BasinData.BasinLevel = newLevel;
    BasinData.IsCritical = newLevel <= 20f;

    NetworkEventDefinitions.Instance.OnBasinLevelChangedEvent?.Invoke(newLevel);

    if (newLevel <= 0f)
    {
        BasinData.IsCollapsed = true;
        NetworkEventDefinitions.Instance.OnBasinCollapsedEvent?.Invoke();
    }
}
```

#### Basin impact sources

| Source                          | Basin delta                            |
| ------------------------------- | -------------------------------------- |
| Hydric Zone tile                | +`BasinConfig.HydricZoneBonus`         |
| Catastrophic Zone tile          | `-BasinConfig.CatastropheImpact`       |
| Event card with BasinDelta != 0 | card's `BasinDelta` value              |
| Collective decision — protect   | +`BasinConfig.CollectiveProtectBonus`  |
| Collective decision — ignore    | `-BasinConfig.CollectiveIgnorePenalty` |
| Minigame — collective win       | +`BasinConfig.MinigameWinBonus`        |
| Minigame — collective loss      | `-BasinConfig.MinigameLossPenalty`     |
| Project passive effects         | variable per project card              |

#### UI feedback thresholds

```
100% – 60%  →  Healthy   (blue/green visual, calm AR animation)
59%  – 21%  →  Warning   (amber tint, pulsing AR overlay)
20%  –  1%  →  Critical  (red tint, warning sound, UI alert)
0%          →  Collapsed →  DEFEAT_SCREEN immediately
```

---

### 4.5 Resource System

**Service:** `ResourceService`
**Model:** `PlayerSessionData.WaterAmount`, `PlayerSessionData.MoneyAmount`

Resources are **always mutated through ResourceService** (never directly). The service enforces floor of 0 (players cannot go negative).

```csharp
// ResourceService.cs
public void AddWater(PlayerRef player, int delta)
{
    if (!Runner.IsServer) return;
    var data = GetPlayerData(player);
    data.WaterAmount = Mathf.Max(0, data.WaterAmount + delta);
    NetworkEventDefinitions.Instance.OnWaterChangedEvent?.Invoke(player, data.WaterAmount);
}

public void AddMoney(PlayerRef player, int delta)
{
    if (!Runner.IsServer) return;
    var data = GetPlayerData(player);
    data.MoneyAmount = Mathf.Max(0, data.MoneyAmount + delta);
    NetworkEventDefinitions.Instance.OnMoneyChangedEvent?.Invoke(player, data.MoneyAmount);
}
```

#### Starting resources (configurable in `GameConfig.asset`)

| Resource | Starting value |
| -------- | -------------- |
| Water    | 10 units       |
| Money    | 5 units        |

---

### 4.6 Card System

**Service:** `CardService`
**Manager:** `CardManager`
**Decks:** `EventDeck` (shuffled ScriptableObject list), `ProjectDeck`

#### Event card flow

```
1. TileService calls CardService.DrawEventCard(player)
2. CardManager draws top card from EventDeck (host pops index [Networked])
3. CardData is broadcast via OnEventCardDrawnEvent(player, cardData)
4. If card.RequiresDecision == false → apply effects immediately
5. If card.RequiresDecision == true  → route to DecisionService (§4.8)
6. Effects applied by host via ResourceService / BasinService
7. Card goes to discard pile; deck reshuffled when empty
```

#### Event card effect application

```csharp
// CardManager.cs
private void ApplyCardEffects(PlayerRef player, CardData card)
{
    if (!Runner.IsServer) return;

    switch (card.Target)
    {
        case CardTarget.Self:
            ResourceService.AddWater(player, card.WaterDelta);
            ResourceService.AddMoney(player, card.MoneyDelta);
            break;

        case CardTarget.AllPlayers:
            foreach (var p in Runner.ActivePlayers)
            {
                ResourceService.AddWater(p, card.WaterDelta);
                ResourceService.AddMoney(p, card.MoneyDelta);
            }
            break;

        case CardTarget.Basin:
            BasinService.ApplyBasinDelta(card.BasinDelta);
            break;

        case CardTarget.Opponent:
            // Randomly pick one other active player
            var target = PickRandomOpponent(player);
            ResourceService.AddWater(target, card.WaterDelta);
            break;
    }
}
```

---

### 4.7 Project System

**Service:** `ProjectService`
**Manager:** `ProjectManager`

Projects are persistent upgrades: bought once on a ProjectZone tile, they generate **passive resources each round**.

#### Purchase flow

```
1. Player lands on ProjectZone tile
2. TileService fires OnProjectZoneActivatedEvent(player)
3. UI shows available projects with costs from ProjectDeck
4. Player selects a project (or skips — purchase is optional)
5. Host validates:
   - player.MoneyAmount >= card.ProjectCost
   - player.ActiveProjects count < 3  (bitmask check)
6. If valid:
   - ResourceService.AddMoney(player, -card.ProjectCost)
   - ProjectManager.RegisterProject(player, card)
   - PlayerSessionData.ActiveProjects bitmask updated
   - Card physically taken from deck, scanned via AR to register
7. OnProjectPurchasedEvent(player, card) broadcast
```

#### Passive effect application (end of each round, PassiveEffects state)

```csharp
// ProjectManager.cs
public void ApplyPassiveEffects()
{
    if (!Runner.IsServer) return;

    foreach (var player in Runner.ActivePlayers)
    {
        var playerData = GetPlayerData(player);
        var projects = GetActiveProjects(playerData); // read bitmask

        foreach (var project in projects)
        {
            ResourceService.AddWater(player, project.PassiveWaterPerRound);
            BasinService.ApplyBasinDelta(project.BasinDelta); // projects may help or harm
        }
    }

    NetworkEventDefinitions.Instance.OnPassiveEffectsAppliedEvent?.Invoke();
}
```

#### Project slot rule

- Maximum **3 active projects per player** at any time
- Enforced via bitmask on `PlayerSessionData.ActiveProjects`
- If slots are full, ProjectZone tile shows "slot full" message and player cannot buy

---

### 4.8 Decision System

**Service:** `DecisionService`
**Manager:** _(handled inline by TurnManager / CardManager)_

Two types of decisions exist (GDD §3.6):

#### Individual decisions

- Only the **active player** votes
- Timer: **60 seconds**
- If timer expires → `DecisionService.AssignRandomVote(player)`
- `DecisionScore` on `PlayerSessionData` updated based on choice (for tiebreaker)

```
Decision screen shows:
  Option A: benefit self   (higher personal water gain, basin harm)
  Option B: protect basin  (lower personal gain, basin +delta)
```

#### Collective decisions

- **All players vote simultaneously**
- Timer: **60 seconds**
- If a player doesn't vote → random vote assigned to that player
- Winning option = majority vote (ties: random tiebreak)

```csharp
// DecisionService.cs
public void ResolveCollectiveDecision(int[] votes, CardData card)
{
    if (!Runner.IsServer) return;

    int optionA = votes.Count(v => v == 0);
    int optionB = votes.Count(v => v == 1);

    int winner = optionA > optionB ? 0
               : optionB > optionA ? 1
               : UnityEngine.Random.Range(0, 2); // tie → random

    ApplyDecisionOutcome(winner, card);
    NetworkEventDefinitions.Instance.OnCollectiveDecisionResolvedEvent?.Invoke(winner);
}
```

---

### 4.9 Minigame System

**Service:** `MinigameService`
**Manager:** `MinigameManager`
**Trigger:** End of every round (after all player turns complete)

All players participate. Minigames are **water/basin-themed** and educational.

#### Minigame lifecycle

```
1. OnAllTurnsCompleteEvent fires
2. MinigameManager selects MinigameID for this round
   (can be random, sequential, or region-contextual)
3. GameState → Minigame
4. All clients load minigame scene/overlay
5. Minigame runs with its own internal timer
6. Each player's result (win/loss/score) sent to host via RPC
7. Host applies rewards/penalties:
   - Individual component: per-player water delta based on performance
   - Collective component: basin delta based on group aggregate result
8. OnMinigameCompleteEvent(results[]) broadcast
9. GameState → PassiveEffects
```

#### Minigame result application

```csharp
// MinigameManager.cs
public void ApplyMinigameResults(Dictionary<PlayerRef, MinigameResult> results)
{
    if (!Runner.IsServer) return;

    int winners = results.Values.Count(r => r.Passed);
    int total   = results.Count;

    // Individual rewards
    foreach (var kvp in results)
    {
        ResourceService.AddWater(kvp.Key, kvp.Value.WaterReward);
    }

    // Collective basin impact: majority win = basin bonus, majority loss = basin penalty
    float basinDelta = winners > total / 2f
        ? BasinConfig.MinigameWinBonus
        : -BasinConfig.MinigameLossPenalty;

    BasinService.ApplyBasinDelta(basinDelta);
}
```

#### Planned minigame types (to be expanded in Minigame-specific docs)

| ID             | Name               | Mechanic                            | AR? |
| -------------- | ------------------ | ----------------------------------- | --- |
| `MG_WaterFlow` | Flujo del agua     | Direct water flow puzzle            | No  |
| `MG_Trivia`    | Trivia hídrica     | Group quiz on water facts           | No  |
| `MG_ARClean`   | Limpieza AR        | AR scan to remove pollution markers | Yes |
| `MG_Vote`      | Votación ambiental | Collective decision under pressure  | No  |

---

### 4.10 Scoring & Win/Loss Conditions

**Manager:** `ScoreManager`

#### Win condition (round 10 complete, Basin > 0)

```csharp
// ScoreManager.cs
public PlayerRef DetermineWinner()
{
    var standings = Runner.ActivePlayers
        .Select(p => (player: p, water: GetPlayerData(p).WaterAmount))
        .OrderByDescending(x => x.water)
        .ToList();

    if (standings[0].water != standings[1].water)
        return standings[0].player;  // clear winner

    // Tiebreaker: DecisionScore accumulator
    return standings
        .Where(x => x.water == standings[0].water)
        .OrderByDescending(x => GetPlayerData(x.player).DecisionScore)
        .First().player;
}
```

#### DecisionScore tiebreaker logic

`DecisionScore` accumulates throughout the game based on the _quality_ of individual decisions:

| Decision type                                         | Score delta |
| ----------------------------------------------------- | ----------- |
| Chose basin protection over personal gain             | +2          |
| Chose personal gain (self-interest)                   | +0          |
| Did not vote (random assigned)                        | -1          |
| Voted correctly in collective decision (winning side) | +1          |

#### Lose condition (Basin == 0)

Triggered in `BasinCheck` phase after any tile resolution or passive effect:

```csharp
// BasinManager.cs
[Networked] public void CheckCollapse()
{
    if (!Runner.IsServer) return;
    if (BasinData.IsCollapsed)
    {
        GameManager.Instance.TransitionTo(GameState.Defeat);
        NetworkEventDefinitions.Instance.OnBasinCollapsedEvent?.Invoke();
    }
}
```

---

## 5. Event Definitions

All events are centralized in `NetworkEventDefinitions.asset` (Resources/Events/).
Access pattern: `NetworkEventDefinitions.Instance.OnXxxEvent`

| Event name                          | Payload                         | Raised by              | Consumed by                                      |
| ----------------------------------- | ------------------------------- | ---------------------- | ------------------------------------------------ |
| `OnPlayerJoinedEvent`               | `PlayerRef, PlayerSessionData`  | `FusionNetworkService` | `GameManager`, `HUDController`                   |
| `OnPlayerLeftEvent`                 | `PlayerRef`                     | `FusionNetworkService` | `GameManager`, `TurnManager`                     |
| `OnGameStateChangedEvent`           | `GameState`                     | `GameManager`          | All UI controllers                               |
| `OnTurnOrderResolvedEvent`          | `int[]` (turn order)            | `TurnService`          | `TurnManager`, `TurnUIController`                |
| `OnPlayerMovedEvent`                | `PlayerRef, int tileIndex`      | `TurnService`          | `TurnManager`, `ARService`                       |
| `OnHydricZoneActivatedEvent`        | `PlayerRef, int waterGain`      | `TileService`          | `ResourceService`, `HUDController`               |
| `OnCatastrophicZoneActivatedEvent`  | `PlayerRef, int waterLoss`      | `TileService`          | `ResourceService`, `BasinService`                |
| `OnEventCardDrawnEvent`             | `PlayerRef, CardData`           | `CardService`          | `CardManager`, `DecisionService`                 |
| `OnProjectZoneActivatedEvent`       | `PlayerRef`                     | `TileService`          | `ProjectManager`, UI                             |
| `OnProjectPurchasedEvent`           | `PlayerRef, CardData`           | `ProjectManager`       | `HUDController`, `ARService`                     |
| `OnBasinLevelChangedEvent`          | `float newLevel`                | `BasinService`         | `BasinManager`, `BasinUIController`, `ARService` |
| `OnBasinCollapsedEvent`             | _(none)_                        | `BasinService`         | `GameManager` → Defeat state                     |
| `OnDecisionStartedEvent`            | `DecisionType, CardData`        | `DecisionService`      | `DecisionUIController`                           |
| `OnCollectiveDecisionResolvedEvent` | `int winnerOption`              | `DecisionService`      | `CardManager`, `BasinService`                    |
| `OnMinigameCompleteEvent`           | `MinigameResult[]`              | `MinigameService`      | `MinigameManager`, `ScoreManager`                |
| `OnPassiveEffectsAppliedEvent`      | _(none)_                        | `ProjectManager`       | `GameManager` (advance round)                    |
| `OnRoundCompleteEvent`              | `RoundData`                     | `GameManager`          | `ScoreManager`, `HUDController`                  |
| `OnWaterChangedEvent`               | `PlayerRef, int newAmount`      | `ResourceService`      | `HUDController`, `ScoreManager`                  |
| `OnMoneyChangedEvent`               | `PlayerRef, int newAmount`      | `ResourceService`      | `HUDController`                                  |
| `OnARScanCompleteEvent`             | `string markerID, bool success` | `ARService`            | `TileService`, `ProjectManager`                  |
| `OnTriviaAnsweredEvent`             | `PlayerRef, bool correct`       | `ARService`            | `ResourceService`                                |

---

## 6. Augmented Reality Integration

**Service:** `ARService`
**Controllers:** `ARScanController`, `AROverlayController`
**Platform:** Android 8.0+ (API 26+)

AR is used in 3 contexts:

### 6.1 Trivia tile scan

```
Player lands on Trivia tile
→ App prompts to scan physical tile marker
→ ARScanController detects markerID
→ TriviaQuestion loaded from Resources by markerID
→ 15-second countdown shown on screen
→ Player taps answer
→ OnTriviaAnsweredEvent(player, correct) fired
→ ResourceService applies reward if correct
```

### 6.2 Project card registration

```
Player buys a project card (physical card)
→ App prompts: "Scan your project card"
→ ARScanController detects card's AR marker
→ CardData matched by markerID
→ ProjectManager.RegisterProject(player, card) confirmed
→ AR overlay shows project's 3D Low Poly visual on card
→ OnARScanCompleteEvent fired
```

### 6.3 Basin AR overlay (ambient / informational)

```
At any time, player can scan the physical board center
→ AROverlayController renders 3D basin model on board
→ Basin water level visualized as fill percentage
→ Color-coded to basin health (blue → amber → red)
→ Passive — no game state change, purely informational
```

### AR permission handling

```csharp
// ARScanController.cs — called on scene start
private IEnumerator RequestCameraPermission()
{
    yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
    if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        UIManager.ShowPermissionError();
}
```

---

## 7. Scene & Flow Diagram

```
Scenes (Unity):
  LobbyScene      → player discovery, room setup, character select
  GameScene       → main board, turns, AR, HUD
  MinigameScene   → (loaded additively over GameScene per minigame)

Navigation flow:
  LobbyScene
    ├── Host creates room → waits for 2–6 players
    ├── Clients join room → see lobby list
    └── Host presses Start → all load GameScene

  GameScene
    ├── Setup phase (host spawns all networked objects)
    ├── Round loop (managed by GameManager state machine)
    │     ├── TurnUIController shows current player
    │     ├── BasinUIController shows basin fill bar
    │     ├── HUDController shows per-player water/money
    │     └── AROverlayController active when camera permission granted
    ├── Minigame (loaded additively by MinigameManager)
    └── End → Victory or Defeat screen → return to LobbyScene
```

---

## 8. Host Authority Rules

Following the established project convention, **all state changes are host-only**. Clients are input sources only.

| Operation               | Authority                     | Pattern                              |
| ----------------------- | ----------------------------- | ------------------------------------ |
| Spawn PlayerSessionData | Host                          | `OnPlayerJoined` callback            |
| Roll dice result        | Client sends → Host validates | RPC → host re-derives                |
| Move player piece       | Host                          | After validation                     |
| Apply tile effects      | Host                          | `TileService.ResolveTile`            |
| Mutate Basin level      | Host                          | `BasinService.ApplyBasinDelta`       |
| Draw / apply cards      | Host                          | `CardService.DrawEventCard`          |
| Purchase project        | Host                          | After client request RPC             |
| Apply passive effects   | Host                          | `ProjectManager.ApplyPassiveEffects` |
| Determine winner        | Host                          | `ScoreManager.DetermineWinner`       |
| Transition GameState    | Host                          | `GameManager.TransitionTo`           |

```csharp
// Always gate host-only logic:
if (!Object.HasStateAuthority) return;
// or equivalently:
if (!Runner.IsServer) return;
```

---

## 9. Edge Cases & Pitfalls

### Game logic edge cases

| Scenario                                     | Handling                                                                                         |
| -------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| Player disconnects mid-turn                  | `OnPlayerLeftEvent` → TurnManager skips their turns for remaining rounds; their resources frozen |
| Basin reaches 0 during passive effects phase | `BasinManager.CheckCollapse()` called after every `ApplyBasinDelta`                              |
| All projects purchased (deck empty)          | ProjectZone shows "no projects available" — tile becomes a no-op                                 |
| All event cards drawn (deck empty)           | Reshuffle discard pile; fire `OnEventDeckReshuffledEvent`                                        |
| Decision timer: all players disconnect       | Host auto-resolves with random for all vacant slots                                              |
| Tiebreaker: DecisionScore also tied          | Random selection between tied players (host uses seeded random for determinism)                  |
| Player has 0 money on ProjectZone            | Purchase UI not shown; tile resolves silently                                                    |
| Trivia 15s timer: player exits app           | ARService fires `OnTriviaAnsweredEvent(player, false)` after timeout                             |
| Round 10 ends with Basin at 1%               | Victory condition applies — Basin > 0 is sufficient                                              |

### Networking pitfalls (from project conventions)

- **Do not** spawn or mutate network-critical state before `NetworkRunner` is initialized
- **Do not** update host-authoritative state from non-authority clients — always use RPCs
- **Do not** remove `[Networked]` from `PlayerSessionData` or `BasinData` properties
- **Do not** rename scenes without updating hardcoded `"LobbyScene"` and `"GameScene"` references
- **Do not** leave `NetworkEventDefinitions` event references unassigned — always use the centralized asset

### AR-specific pitfalls

- **Do not** start AR scanning before camera permission is confirmed
- **Do not** block the main game thread waiting for AR scan — use coroutines with timeout fallback
- **Always** provide a manual fallback input for trivia (in case AR scan fails on low-end devices)
- AR overlays are **client-side only** — never route AR scan results through networked state without host validation

---

_Document version 1.0 — generated from GDD v1 — Sumak development team_
_Keep in sync with GDD updates. Living document._

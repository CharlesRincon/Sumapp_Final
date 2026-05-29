# Plan: Implementación de Minijuego de Reparación de Tubería (Pipe Minigame)

## Project Overview
- **Game Title:** Sumapp Final
- **High-Level Concept:** Minijuego de carrera multijugador donde los jugadores deben reparar hoyos en una tubería haciendo clics rápidos. El primero en completar todas las reparaciones gana dinero.
- **Players:** Multijugador (Host-Client) usando Photon Fusion.
- **Render Pipeline:** URP (PC_RPAsset).
- **Target Platform:** Android.

## Game Mechanics
### Core Gameplay Loop
1.  Transición desde el Lobby al finalizar una ronda.
2.  Aparición de hoyos aleatorios en una tubería central.
3.  Los jugadores hacen clic en los hoyos (cada hoyo requiere múltiples clics).
4.  Cada hoyo reparado incrementa el `MinigameClickCount` del jugador.
5.  El primer jugador en llegar al límite de reparaciones gana una recompensa en dinero y todos regresan al Lobby.

### Controls
- **Input:** Clic/Touch en elementos de UI (Hoyos).

## UI
- **Pipe Scene:** Fondo con una tubería central.
- **Hole UI:** Botones que aparecen sobre la tubería con estados visuales (Dañado/Reparado).
- **Leaderboard/Progress:** Barra lateral que muestra cuántas reparaciones lleva cada jugador.

## Key Asset & Context
- **Scripts a crear:** 
    - `PipeMinigameManager.cs` (Lógica de red y victoria).
    - `PipeMinigameUI.cs` (Control visual y spawn de hoyos).
    - `PipeHole.cs` (Lógica individual de cada hoyo).
- **Scripts a modificar:** 
    - `PlayerSessionData.cs` (Para soportar carga de escenas dinámicas).
    - `GameManager.cs` (Para elegir el minijuego).
- **Escena:** `PipeMinigame.unity`.

## Implementation Steps

### 1. Infraestructura de Red (Refactorización)
1.  **Modificar `PlayerSessionData.cs`**:
    - Cambiar `RPC_LoadMinigameScene()` para que acepte un parámetro `string sceneName`.
    - Asegurar que `RPC_LoadMinigameScene` use `SceneManager.LoadScene(sceneName)`.
    - **Dependencia:** Ninguna.
2.  **Modificar `GameManager.cs`**:
    - En `LoadMinigameWhenReady`, implementar una lógica de selección (aleatoria o rotativa) para elegir entre `"Minigame"` (el actual) y `"PipeMinigame"`.
    - Pasar el nombre de la escena seleccionada a `data.RPC_LoadMinigameScene(selectedScene)`.
    - **Dependencia:** Paso 1.

### 2. Lógica del Minijuego (`PipeMinigameManager`)
1.  **Crear `PipeMinigameManager.cs`**:
    - Heredar de `NetworkBehaviour`.
    - Definir `[Networked] NetworkBool IsRaceEnded { get; set; }`.
    - Definir meta de reparaciones (ej. `_requiredRepairs = 10`).
    - En `FixedUpdateNetwork`, el Host monitorea si algún jugador alcanzó `MinigameClickCount >= _requiredRepairs`.
    - Al detectar un ganador:
        - Establecer `IsRaceEnded = true`.
        - Otorgar recompensa: `winnerData.MoneyAmount += _rewardAmount`.
        - Notificar a todos y llamar a `ReturnToLobbyAfterDelay`.
    - **Dependencia:** `PlayerSessionData`.

### 3. Componentes de Juego
1.  **Crear `PipeHole.cs`**:
    - Script para el prefab del hoyo.
    - Maneja clics locales para "reparar" el hoyo (ej. 3 clics).
    - Al reparar, llama a `localPlayerData.RPC_IncrementMinigameClickCount()`.
2.  **Crear `PipeMinigameUI.cs`**:
    - Gestiona el spawn de `PipeHole` en posiciones aleatorias de la tubería.
    - Escucha el estado de red para mostrar al ganador.

### 4. Creación de Escena y Prefabs
1.  Crear la escena `PipeMinigame.unity`.
2.  Configurar el `MinigameInitializer` con el nuevo manager.
3.  Crear el prefab del hoyo (`PipeHolePrefab`) basado en un UI Button.

## Verification & Testing
1.  **Carga:** Verificar que el `GameManager` carga correctamente la nueva escena.
2.  **Sincronización:** Confirmar que el `MinigameClickCount` se incrementa en el Host al hacer clic en los hoyos.
3.  **Victoria:** Validar que el dinero se añade solo al ganador y que todos vuelven al Lobby.
4.  **UI:** Verificar que los hoyos aparecen y desaparecen correctamente.

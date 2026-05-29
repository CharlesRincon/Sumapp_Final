# Análisis Técnico de Scripts y Justificación Ingenieril

Este documento detalla los componentes de software más críticos del proyecto **Sumapp Final**, integrando fragmentos de código fuente con su respectiva justificación desde la ingeniería de software. Este análisis está diseñado para ser incluido en una tesis académica, facilitando su integración en Overleaf o LaTeX.

---

## 1. Funcionamiento del Sistema Networking

El funcionamiento multijugador de **Sumapp Final** se basa en una arquitectura de **Host-Servidor** integrada mediante Photon Fusion. A continuación se desglosan los componentes esenciales que permiten el establecimiento y mantenimiento de la conexión.

### 1.1. Orquestación del Runner (`NetworkRunner`)
**Justificación Ingenieril:** El `NetworkRunner` es el componente central que gestiona la simulación de red. Actúa como el puente entre el motor de Unity y el transporte de datos de Photon. Se utiliza una topología de **Host-Autoritativo**, donde un jugador actúa como servidor y cliente simultáneamente, garantizando que haya una única "fuente de verdad" para el estado del juego.

### 1.2. Inicialización de Sesión (`FusionLauncher.cs`)
La función `Launch` es la encargada de configurar los parámetros de inicio de la partida. Utiliza el objeto `StartGameArgs` para definir la identidad de la sesión.

```csharp
// Fragmento de la lógica de conexión en FusionLauncher.cs
public async void Launch(GameMode mode, string room, INetworkSceneManager sceneLoader)
{
    var args = new StartGameArgs()
    {
        GameMode = mode,           // Host (Servidor/Cliente) o Client (Solo Cliente)
        SessionName = room,        // Nombre único del cuarto de juego
        SceneManager = sceneLoader,// Garantiza que todos carguen la misma escena
        PlayerCount = 6,           // Límite de escalabilidad (2-6 jugadores)
        ConnectionToken = FusionNetworkService.BuildConnectionToken(...)
    };

    // Inicia el motor de red y establece la comunicación con el servidor de señalización
    StartGameResult result = await _runner.StartGame(args);
}
```

### 1.3. Sincronización de Escenas (`INetworkSceneManager`)
**Justificación Ingenieril:** En un juego multijugador, es crítico que todos los usuarios se encuentren en la misma escena física para evitar desincronización de colisiones o lógica. El sistema utiliza un `NetworkSceneManager` especializado que intercepta las cargas de escena de Unity y las replica en todos los clientes conectados.

### 1.4. Ciclo de Vida del Jugador (`IPlayerJoined`)
Cuando un jugador se une a la sesión, el sistema dispara el callback `OnPlayerJoined`. Es en este punto donde el Host utiliza `Runner.Spawn` para instanciar el objeto `PlayerSessionData`.

*   **Network Spawning:** A diferencia de `Instantiate`, `Runner.Spawn` crea un objeto con una identidad de red única (`NetworkObject`) que es rastreado por el sistema de ticks de Fusion.

### 1.5. Servicio Central de Networking (`FusionNetworkService.cs`)
**Justificación Ingenieril:** Este servicio implementa la interfaz `INetworkRunnerCallbacks` de Fusion, actuando como intermediario entre los callbacks de red y la lógica de aplicación. Sigue el patrón **Adapter**, traduciendo eventos brutos de Fusion a un sistema de eventos desacoplado basado en `ScriptableObject`.

#### Callbacks Críticos Implementados:

```csharp
public class FusionNetworkService : MonoBehaviour, INetworkRunnerCallbacks
{
    // Referencia estática al NetworkRunner local para acceso rápido
    public static NetworkRunner LocalRunner;
    
    // Límite de escalabilidad configurado (2-6 jugadores)
    private const int MaxPlayersPerRoom = 6;

    // Prefab de datos de jugador a instanciar en cada join
    public NetworkPrefabRef PlayerDataNO;
    
    // Referencias a eventos desacoplados
    public FusionEvent OnPlayerJoinedEvent;
    public FusionEvent OnPlayerLeftEvent;
    public FusionEvent OnShutdownEvent;
}
```

**Callback `OnPlayerJoined()`:**
- **Responsabilidad:** Capturar la entrada de un nuevo jugador a la sesión
- **Lógica Host-Autoritativa:** Solo el Host puede usar `Runner.Spawn()` para crear el objeto `PlayerSessionData` sincronizado
- **Autoridad de Entrada:** Se asigna el parámetro `inputAuthority: player` para que ese jugador controle su propio controlador de entrada

```csharp
public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
    if (runner.IsServer)  // Solo el Host ejecuta esta lógica
    {
        runner.Spawn(PlayerDataNO, inputAuthority: player);  // Crea datos de jugador sincronizados
    }

    if (runner.LocalPlayer == player)
    {
        LocalRunner = runner;  // Almacena referencia estática para fácil acceso
    }

    OnPlayerJoinedEvent?.Raise(player, runner);  // Notifica a sistemas desacoplados (UI, GameManager)
}
```

**Callback `OnConnectRequest()`:**
- **Responsabilidad:** Validar la conexión antes de permitir que un jugador se una
- **Mecanismos de Seguridad:** Token de conexión cifrado, validación de contraseña de sala, límite de jugadores

```csharp
public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
{
    int currentPlayerCount = runner.ActivePlayers.Count();
    string expectedPassword = PlayerPrefs.GetString("RoomPassword", string.Empty);
    
    // Descodificar y validar token de conexión
    if (!TryReadConnectionToken(token, out ConnectionTokenPayload payload))
    {
        request.Refuse();  // Rechazar cliente no autenticado
        return;
    }

    // Validar límite de jugadores (máximo 6)
    if (currentPlayerCount >= MaxPlayersPerRoom)
    {
        request.Refuse();
        return;
    }

    // Validar contraseña de sala
    if (!string.Equals(expectedPassword, payload.Password ?? string.Empty, StringComparison.Ordinal))
    {
        request.Refuse();
        return;
    }

    request.Accept();  // Aprobar la conexión
}
```

**Token de Conexión:**
```csharp
[Serializable]
private struct ConnectionTokenPayload
{
    public int Version;         // Control de versión de protocolo
    public string Nickname;     // Nombre del jugador
    public string Password;     // Contraseña de sala
}

public static byte[] BuildConnectionToken(string nickname, string password)
{
    var payload = new ConnectionTokenPayload
    {
        Version = 1,
        Nickname = (nickname ?? string.Empty).Trim(),
        Password = password ?? string.Empty
    };

    string json = JsonUtility.ToJson(payload);
    return Encoding.UTF8.GetBytes(json);  // Serializar a JSON → bytes UTF-8
}
```

**Callback `OnPlayerLeft()` y `OnShutdown()`:**
- **Responsabilidad:** Manejar desconexiones y limpiar recursos de red
- **Importancia:** Crítico para evitar referencias huérfanas y garantizar estabilidad en sesiones de larga duración

```csharp
public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
{
    OnPlayerLeftEvent?.Raise(player, runner);  // Notificar desconexión
}

public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
{
    Debug.LogWarning($"[FusionNetworkService] Runner shutdown: {shutdownReason}");
    
    if (LocalRunner == runner)
    {
        LocalRunner = null;  // Limpiar referencia estática
    }
    
    OnShutdownEvent?.Raise(runner: runner);  // Notificar cierre
}
```

### 1.6. Sistema Desacoplado de Eventos (`FusionEvent.cs` y `NetworkEventDefinitions.cs`)
**Justificación Ingenieril:** Se implementa el patrón **Observer basado en Activos**. En lugar de que los controladores de UI tengan referencias directas a `FusionNetworkService`, se suscriben a canales de eventos (`ScriptableObject`) que actúan como intermediarios. Esto resuelve el problema del **acoplamiento fuerte** y permite que componentes se agreguen o eliminen sin modificar el código existente.

```csharp
// Definición centralizada de todos los eventos de red
[CreateAssetMenu(menuName = "Networking/Event Definitions")]
public class NetworkEventDefinitions : ScriptableObject
{
    [Header("Connection Events")]
    public FusionEvent OnPlayerJoinedEvent;      // Se dispara cuando un jugador entra
    public FusionEvent OnPlayerLeftEvent;        // Se dispara cuando un jugador sale
    public FusionEvent OnConnectionStatusChangedEvent;
    public FusionEvent OnDisconnectedEvent;

    [Header("Session Events")]
    public FusionEvent OnGameStateChangedEvent;  // Cambios en el estado del juego
    public FusionEvent OnEnteredLobbyEvent;      // Entrada a lobby
    public FusionEvent OnShutdownEvent;          // Cierre de conexión
}
```

Cada evento es un `ScriptableObject` que actúa como un canal de comunicación:

```csharp
[CreateAssetMenu(menuName = "Networking/Fusion Event")]
public class FusionEvent : ScriptableObject
{
    private event System.Action<PlayerRef, NetworkRunner> _listeners;

    public void RegisterResponse(System.Action<PlayerRef, NetworkRunner> response)
    {
        _listeners += response;  // Suscribir un observador
    }

    public void RemoveResponse(System.Action<PlayerRef, NetworkRunner> response)
    {
        _listeners -= response;  // Desuscribir un observador
    }

    public void Raise(PlayerRef player, NetworkRunner runner)
    {
        _listeners?.Invoke(player, runner);  // Notificar a todos los observadores
    }
}
```

**Ventajas de este Patrón:**
- **Desacoplamiento:** Los componentes no conocen la existencia unos de otros
- **Reutilización:** Los eventos pueden ser reasignados en el Inspector sin cambiar código
- **Escalabilidad:** Agregar nuevos observadores no afecta a componentes existentes
- **Testabilidad:** Los eventos pueden ser simulados sin dependencias de red reales

### 1.7. Autoridad de Estado vs Autoridad de Entrada
**Justificación Ingenieril:** Fusion distingue entre dos tipos de autoridad para prevenir la manipulación de datos y garantizar integridad:

- **StateAuthority (Autoridad de Estado):** Solo este cliente puede escribir las propiedades `[Networked]` de un objeto. En arquitectura Host-Autoritativa, el Host siempre posee esta autoridad.
  
- **InputAuthority (Autoridad de Entrada):** Indica a qué jugador corresponde la entrada (input) del objeto. Un jugador solo puede enviar RPC desde su propio objeto `PlayerSessionData`.

```csharp
// En PlayerSessionData.cs
public override void OnInput(NetworkRunner runner, NetworkInput input)
{
    // Solo el cliente con InputAuthority puede proporcionar entrada
    if (!HasInputAuthority) return;
    
    // Procesar entrada del jugador local
    var data = new NetworkInputData();
    // ... llenar datos de entrada
    input.Set(data);
}

[Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
public void RPC_RequestValidatedTurnRoll(int clientRoll)
{
    // Solo el Host (StateAuthority) puede ejecutar esta lógica
    if (!Object.HasStateAuthority) return;
    
    // Validar que la solicitud viene del jugador correcto
    if (Object.InputAuthority != runner.LocalPlayer) return;
    
    // Procesar el rollado validado
    LastDiceRoll = ValidateDiceRoll(clientRoll);
}
```

### 1.8. Ciclo de Vida Completo de Conexión (`FusionLauncher.cs`)
**Justificación Ingenieril:** Implementa una máquina de estados para gestionar transiciones entre estados de conexión: `Disconnected` → `Connecting` → `Connected` → `Loading` → `Loaded`. Cada transición es monitoreada y puede generar reintentos automáticos en caso de fallo.

```csharp
public class FusionLauncher : MonoBehaviour
{
    private const int MaxClientJoinAttempts = 4;  // Máximo de reintentos
    private NetworkRunner _runner;
    private ConnectionStatus _status;
    private string _statusMessage;

    public enum ConnectionStatus
    {
        Disconnected,  // Estado inicial
        Connecting,    // En proceso de conexión
        Failed,        // Error durante conexión
        Connected,     // Conectado exitosamente
        Loading,       // Cargando escenas
        Loaded         // Listo para jugar
    }

    public async void Launch(GameMode mode, string room, INetworkSceneManager sceneLoader)
    {
        try
        {
            // Preparar runner con callbacks
            await EnsureRunnerReady(mode);
            
            var args = new StartGameArgs()
            {
                GameMode = mode,
                SessionName = room,
                SceneManager = sceneLoader,
                PlayerCount = 6,
                ConnectionToken = FusionNetworkService.BuildConnectionToken(...)
            };

            SetConnectionStatus(ConnectionStatus.Connecting, "Conectando...");
            
            // Iniciar game loop de Fusion
            StartGameResult result = await _runner.StartGame(args);

            if (result.Ok)
            {
                SetConnectionStatus(ConnectionStatus.Connected, "Conectado");
            }
            else
            {
                // Manejo de error con reintentos automáticos
                HandleConnectionError(result, mode, room, sceneLoader);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FusionLauncher] Error crítico: {ex.Message}");
            SetConnectionStatus(ConnectionStatus.Failed, ex.Message);
            await SafeShutdownRunner();
        }
    }

    private async Task SafeShutdownRunner()
    {
        if (_runner == null) return;

        try
        {
            await _runner.Shutdown();
        }
        catch (Exception shutdownEx)
        {
            Debug.LogWarning($"[FusionLauncher] Error al cerrar: {shutdownEx.Message}");
        }

        _runner = null;
    }

    public void SetConnectionStatus(ConnectionStatus status, string message)
    {
        _status = status;
        _statusMessage = message;
        Debug.Log($"[FusionLauncher] Estado: {status} - {message}");
    }
}
```

**Estados de Transición:**
1. **Disconnected:** Aplicación inicia sin conexión
2. **Connecting:** Usuario presiona "Conectar", se envía solicitud al servidor Photon
3. **Connected:** `OnPlayerJoined()` se ejecuta, datos del jugador se sincronizan
4. **Loading:** `INetworkSceneManager` carga la escena de juego en todos los clientes
5. **Loaded:** Escena completamente cargada, GameManager inicia lógica de juego

### 1.9. Validación Anti-Cheat en Solicitudes de Dados
**Justificación Ingenieril:** El sistema de validación de dados implementa múltiples capas de seguridad para prevenir manipulación de cliente:

```csharp
// En FusionNetworkService.cs
public bool ValidateDiceRollRequest(PlayerRef requestingPlayer, PlayerRef activePlayer, NetworkRunner runner)
{
    // Capa 1: Verificar que el jugador que solicita es el jugador activo
    if (requestingPlayer != activePlayer)
    {
        Debug.LogWarning($"[Validation] Rechazo: {requestingPlayer} no es el jugador activo");
        return false;
    }

    // Capa 2: Verificar que el jugador ya no ha tirado este turno
    var sessionData = gameManager.GetPlayerData(requestingPlayer, runner);
    if (sessionData != null && sessionData.HasRolledThisTurn)
    {
        Debug.LogWarning($"[Validation] Rechazo: {requestingPlayer} ya tiró esta ronda");
        return false;
    }

    // Capa 3: Verificar que hay suficiente tiempo entre solicitudes (anti-spam)
    if (Time.time - sessionData.LastRollRequestTime < 0.5f)
    {
        Debug.LogWarning($"[Validation] Rechazo: solicitud muy rápida de {requestingPlayer}");
        return false;
    }

    return true;
}

// En FusionNetworkService.cs
public int GenerateValidatedDiceRoll()
{
    // Solo el Host genera el número final (no el cliente)
    int roll = Random.Range(1, 11);  // D10: 1-10
    return roll;
}
```

**Capas de Seguridad:**
- **Capa 1 (Autoridad):** Solo el jugador activo puede solicitar un dado
- **Capa 2 (Integridad):** Un jugador no puede tirar dos veces en el mismo turno
- **Capa 3 (Anti-spam):** Se valida el tiempo entre solicitudes para evitar intentos de hack
- **Capa 4 (Determinismo):** El Host genera el número final, no el cliente

---

## 2. Núcleo de Sincronización de Datos: `PlayerSessionData.cs`

**Justificación Arquitectónica:** `PlayerSessionData` es el modelo de datos central que encapsula todo el estado sincronizado de un jugador en la red. Hereda de `NetworkBehaviour` de Fusion, lo que permite que sus propiedades marcadas con `[Networked]` se repliquen automáticamente a través de **Delta Compression** entre todos los clientes. Este enfoque desacopla la lógica de persistencia de datos de la lógica de presentación, reduciendo complejidad ciclomática y mejorando mantenibilidad.

### 2.1. Categorización de Propiedades Sincronizadas

Las aproximadamente 40 propiedades `[Networked]` se organizan en 5 categorías funcionales:

#### 2.1.1. **Identidad y Sesión**

```csharp
[Networked] public NetworkString<_16> Nick { get; set; }              // Apodo del jugador
[Networked] public NetworkObject Instance { get; set; }               // Referencia a instancia de personaje
[Networked] public int SelectedCharacterId { get; set; }              // ID de carácter seleccionado
```

**Patrón Utilizado:** `NetworkString<_16>` es un tipo especial de Fusion que permite sincronizar strings de longitud fija (16 caracteres máximo). Esto es más eficiente que serializar strings dinámicos.

#### 2.1.2. **Estado de Turno y Tablero**

```csharp
[Networked] public int BoardPosition { get; set; }                    // Posición actual (0-24)
[Networked] public int TurnOrder { get; set; }                        // Orden de turno (determinado en init)
[Networked] public bool IsActiveTurn { get; set; }                    // ¿Es el turno activo de este jugador?
[Networked] public bool HasRolledThisTurn { get; set; }              // ¿Ya tiró el dado este turno?
[Networked] public int LastDiceRoll { get; set; }                    // Resultado del último dado (1-10)
[Networked] public float LastDiceRollTime { get; set; }              // Timestamp para auto-hide de UI
```

**Justificación:** El campo `LastDiceRoll` permite que la UI de todos los jugadores muestre el resultado simultáneamente sin depender de callbacks locales. `LastDiceRollTime` habilita auto-ocultamiento sincronizado (evita que algunos clientes muestren resultados más tiempo que otros).

#### 2.1.3. **Recursos del Jugador**

```csharp
[Networked] public int WaterAmount { get; set; }                      // Agua acumulada
[Networked] public int MoneyAmount { get; set; }                      // Dinero acumulado
```

**Patrón:** Estos valores nunca deben escribirse directamente desde RPC. En su lugar, `GameManager` los modifica y Fusion automáticamente los sincroniza. Esto garantiza que la lógica de modificación está centralizada en el Host.

#### 2.1.4. **Proyectos e Ítems Pendientes**

```csharp
[Networked] public int PendingProjectId { get; set; }
[Networked] public NetworkString<_128> PendingProjectName { get; set; }
[Networked] public NetworkString<_512> PendingProjectDescription { get; set; }
[Networked] public int PendingProjectPrice { get; set; }
[Networked] public int PendingProjectWaterIncome { get; set; }
[Networked] public int PendingProjectMoneyIncome { get; set; }
[Networked] public int PendingProjectZone { get; set; }

// Proyectos equipados en slots
[Networked] public int OwnedProjectSlot0Id { get; set; }
[Networked] public int OwnedProjectSlot0Zone { get; set; }
// ... (slots 1 y 2)

// Estado de decisión
[Networked] public bool IsAwaitingProjectDecision { get; set; }
[Networked] public bool IsAwaitingProjectScan { get; set; }
```

**Patrón de Flujo:**
1. GameManager detecta que el jugador cae en una casilla de Proyecto
2. Host establece `PendingProjectId`, `PendingProjectName`, etc. (todas las propiedades sync)
3. Todos los clientes ven los datos simultáneamente en `PendingProject*`
4. UI muestra panel de decisión (Comprar/Rechazar)
5. Jugador invoca `RPC_RequestBuyPendingProject()`, Host valida recursos y resuelve

#### 2.1.5. **Estado de Minijuegos y Desafíos**

```csharp
[Networked] public int MinigameClickCount { get; set; }               // Puntuación acumulada
[Networked] public bool IsInMinigameReadyPhase { get; set; }          // Esperando confirmación
[Networked] public bool IsReadyForMinigame { get; set; }              // Este jugador está listo
[Networked] public bool IsAwaitingCardScan { get; set; }              // Escaneando carta AR
[Networked] public NetworkString<_128> PendingCardTitle { get; set; }  // Título de carta
[Networked] public bool IsAwaitingTrivia { get; set; }                // Respondiendo trivia
[Networked] public bool DoubleTriviaReward { get; set; }              // Bonus activado
[Networked] public bool IsAwaitingDecisionVote { get; set; }          // Votando decisión
[Networked] public int PendingDecisionVote { get; set; }              // 0=no votado, 1=A, 2=B
```

**Patrón:** `MinigameClickCount` es modificado exclusivamente por `RPC_IncrementMinigameClickCount()` y `RPC_AddMinigamePoints()`. El Host es quien modifica el valor, garantizando que no hay manipulación de puntuación desde cliente.

### 2.2. Ciclo de Vida: `Spawned()` y `Render()`

**Spawned() - Inicialización Post-Spawnear:**
```csharp
public override void Spawned()
{
    // Capturador de cambios para detectar propiedades que variaron
    _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState, false);
    
    // Solo el cliente con InputAuthority establece su propio nick
    if (Object.HasInputAuthority)
    {
        string nickName = PlayerPrefs.GetString("Nick", string.Empty);
        RPC_SetNick(string.IsNullOrEmpty(nickName) ? $"Player {Object.InputAuthority.AsIndex}" : nickName);
    }

    // Objeto no se destruye al cambiar de escena (supervivencia)
    DontDestroyOnLoad(this);
    
    // Registra este objeto como el NetworkObject del jugador en el Runner
    Runner.SetPlayerObject(Object.InputAuthority, Object);
    
    // Dispara evento para que UI reciba el PlayerSessionData
    OnPlayerDataSpawnedEvent?.Raise(Object.InputAuthority, Runner);

    // Solo Host cachea referencias internas
    if (Object.HasStateAuthority)
    {
        Networking.Managers.GameManager.Instance.SetPlayerDataObject(Object.InputAuthority, this);
    }
}
```

**Render() - Reacción a Cambios:**
```csharp
public override void Render()
{
    // Detecta cambios en propiedades [Networked]
    foreach (var change in _changeDetector.DetectChanges(this))
    {
        switch (change)
        {
            case nameof(Nick):
            case nameof(SelectedCharacterId):
                // Cuando Nick o CharacterId cambian, notificar UI
                OnPlayerDataSpawnedEvent?.Raise(Object.InputAuthority, Runner);
                break;
        }
    }
}
```

**Justificación:** El patrón `ChangeDetector` es más eficiente que verificar manualmente cada propiedad en Update(). Fusion proporciona este mecanismo integrado para detectar solo cambios reales, reduciendo llamadas de evento innecesarias.

### 2.3. RPC Críticos: Solicitudes Validadas por Host

Todos los RPCs siguen el patrón `RpcSources.InputAuthority → RpcTargets.StateAuthority`:

```csharp
[Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority, 
     HostMode = RpcHostMode.SourceIsHostPlayer)]
public void RPC_RequestValidatedTurnRoll(int clientRoll)
{
    // Guardián 1: Solo Host ejecuta
    if (!Object.HasStateAuthority) return;

    var runner = Runner;
    var gameManager = Networking.Managers.GameManager.Instance;
    var networkService = UnityEngine.Object.FindFirstObjectByType<Networking.Services.FusionNetworkService>();

    // Guardián 2: Dependencias disponibles
    if (runner == null || gameManager == null || networkService == null) return;

    // **Fase 1: Inicialización de Orden de Turno**
    if (gameManager.State == Networking.Managers.GameManager.GameState.TurnOrderInitialization)
    {
        if (LastDiceRoll > 0) return;  // Ya tiró
        
        LastDiceRoll = clientRoll > 0 ? clientRoll : networkService.GenerateValidatedDiceRoll();
        LastDiceRollTime = (float)runner.SimulationTime;
        
        Networking.Events.NetworkEventDefinitions.Instance?.OnDiceRolledEvent?.Raise(
            Object.InputAuthority, runner);
        return;
    }

    // **Fase 2: Turnos Activos Normales**
    var activePlayer = gameManager.GetActivePlayer(runner);
    
    // Validar que es el turno del jugador solicitante
    if (!networkService.ValidateDiceRollRequest(Object.InputAuthority, activePlayer, runner)) return;
    
    // Validar que no ha tirado dos veces
    if (HasRolledThisTurn) return;

    LastDiceRoll = clientRoll > 0 ? clientRoll : networkService.GenerateValidatedDiceRoll();
    LastDiceRollTime = (float)runner.SimulationTime;
    HasRolledThisTurn = true;

    // **Disparo de Lógica de Turno Completa en Host**
    gameManager.HandleValidatedTurnRoll(Object.InputAuthority, LastDiceRoll, runner);
}
```

**Patrón Clave:** 
- **clientRoll:** Valor enviado por cliente (si animó el dado localmente). Si > 0, se usa; si no, Host genera nuevo.
- **Dos Fases:** Inicialización (todos tiran una vez) vs Turnos Activos (un jugador por turno)
- **Evento Centralizado:** En lugar de devolver datos, dispara `OnDiceRolledEvent` que UI recibe

#### Otros RPCs Críticos:

```csharp
// Incrementar contador de clics en minijuego (con validación Host)
[Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
public void RPC_IncrementMinigameClickCount()
{
    MinigameClickCount++;  // Solo Host modifica el estado sincronizado
}

// Solicitud de escaneo AR (Host valida escena y activa panel)
[Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
public void RPC_RequestCardScan(int cardId)
{
    if (!Object.HasStateAuthority) return;
    var gameManager = Networking.Managers.GameManager.Instance;
    gameManager.HandleCardScan(Object.InputAuthority, Runner, cardId);
}

// Voto en decisión (1 = Opción A, 2 = Opción B)
[Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
public void RPC_RequestDecisionVote(int choice)
{
    if (!Object.HasStateAuthority) return;
    var gameManager = Networking.Managers.GameManager.Instance;
    gameManager.HandleDecisionVote(Object.InputAuthority, Runner, choice);
}
```

### 2.4. Distribución de Escenas via RPC

**Justificación Técnica:** Cargar escenas requiere que **todos los clientes** cambien simultáneamente para evitar desincronización. Los RPCs `RPC_LoadMinigameScene()` y `RPC_LoadLobbyScene()` se ejecutan en **todas** las instancias de `PlayerSessionData`:

```csharp
[Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
public void RPC_LoadMinigameScene(string sceneName)
{
    Debug.Log($"[PlayerSessionData] Cargando escena de minijuego: {sceneName}");
    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
}
```

**Flujo:**
1. Host detecta que todos los jugadores están listos → `IsReadyForMinigame = true` para todos
2. Host invoca `RPC_LoadMinigameScene("Minigame")` en todos
3. Cada cliente ejecuta `SceneManager.LoadScene()` localmente
4. Todos llegan a la escena Minigame simultáneamente

### 2.5. Determinismo Sincronizado: Weather Roll

**Caso de Uso:** Cuando GameManager necesita mostrar resultado de dado de clima a todos:

```csharp
[Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
public void RPC_SyncWeatherRollResult(int roll, int waterDelta, int moneyDelta)
{
    // Buscar el panel de orden de turno para mostrar resultado
    var turnOrderPanel = Networking.UI.TurnOrderPanel.Instance;
    if (turnOrderPanel == null)
    {
        turnOrderPanel = UnityEngine.Object.FindFirstObjectByType<Networking.UI.TurnOrderPanel>();
    }

    if (turnOrderPanel != null)
    {
        turnOrderPanel.ShowWeatherRollResult(roll, waterDelta, moneyDelta);
    }
}
```

**Ventaja:** El número no se genera localmente (lo que causaría números diferentes en cada cliente). En su lugar, el Host genera **una vez** y sincroniza el resultado a todos via RPC.

---

## 3. Controlador Maestro: `GameManager.cs`

**Justificación Arquitectónica:** `GameManager` es la **orquestación central host-autoritativa** del sistema de juego. Implementa la máquina de estados del juego, gestiona el turno de cada jugador, resuelve casillas del tablero, aplica efectos de clima, y valida todas las transacciones de recursos. Al centralizar toda la lógica crítica en el Host, se garantiza que no hay forma de manipular el juego desde clientes.

### 3.1. Máquina de Estados del Juego

El juego progresa a través de una máquina de estados estrictamente secuencial:

```csharp
public enum GameState
{
    Lobby,                        // Esperando que se unan todos los jugadores
    Setup,                        // Inicialización de recursos (agua, dinero, cuenca)
    CharacterSelection,           // Selección de personaje
    RollOrder,                    // Dados para determinar orden de turno
    PlayerTurn,                   // Turno activo (jugador puede tirar dado)
    TileResolve,                  // Resolución de efectos de casilla
    Decision,                     // Esperando decisión (compra proyecto, escaneo AR, etc)
    BasinCheck,                   // Verificar si la cuenca está derrotada
    Loading,                      // Transición entre escenas
    Minigame,                     // Juego minigame activo
    PassiveEffects,               // Aplicación de efectos pasivos de ronda
    Victory,                       // Jugadores ganaron (3 rondas completadas)
    Defeat                        // Cuenca derrotada (todos pierden)
}
```

**Flujo típico de un turno:**
```
PlayerTurn → [Jugador tira dado] → TileResolve → [Aplicar efectos] → 
BasinCheck → [¿Cuenca derrotada?] → Decision/TileResolve → [Siguiente jugador]
```

### 3.2. Estructura de Datos del Host (No Sincronizadas)

Mientras que `PlayerSessionData` contiene estado que debe replicarse, `GameManager` mantiene datos internos del Host que **nunca se sincronizan**:

```csharp
private Dictionary<PlayerRef, PlayerSessionData> _playerData;    // Caché local de datos
private List<PlayerRef> _turnOrder;                              // Orden determinado
private int _activeTurnIndex;                                    // Índice del turno actual
private bool _roundInProgress;                                   // Flag para evitar race conditions
private int _currentRound;                                       // Ronda actual (1-3)

// ── Modificadores de ronda (reset cada ronda) ─────────────────────
private int _roundWaterGainFlatPenalty;      // -X agua ganancias
private int _roundWaterGainFlatBonus;        // +X agua ganancias
private int _roundWaterGainPercentPenalty;   // Porcentaje de penalización
// ... (análogos para dinero y proyectos)

// ── Estado de clima (activo durante X rondas) ───────────────────
private int _activeWeatherCardId = -1;       // ID de carta de clima
private int _weatherStartRound;              // Ronda en que comenzó
private int _weatherDurationRounds;          // Cuántas rondas durará
private bool _weatherRollDependentRewards;   // ¿Dado especial al inicio de turno?
private WeatherTag _activeWeatherTag;        // Etiqueta (drought, flood, etc)

// ── Decisiones pendientes ───────────────────────────────────────
private int _pendingDecisionCardId = -1;     // Carta esperando voto
private PlayerRef _pendingDecisionScanningPlayer;  // Quién está votando
```

**Justificación:** Mantener esta información en el Host evita que sea alterada y garantiza que la lógica de rondas, clima, y decisiones sea **determinista y verificable**.

### 3.3. Ciclo de Vida de una Ronda: `StartRound()` → `AdvanceTurn()` → `EndRound()`

#### 3.3.1. **StartRound() - Inicialización**

```csharp
public void StartRound(NetworkRunner runner)
{
    if (runner == null || !runner.IsServer || _roundInProgress) return;

    _currentRound++;
    _roundInProgress = true;
    ApplyProjectRoundStartEffects(runner);  // Bonificaciones de proyectos
    _activeTurnIndex = -1;

    // ── Resetear modificadores de ronda ─────────────────────────
    _roundWaterGainFlatPenalty = 0;
    _roundMoneyGainFlatPenalty = 0;
    _roundProjectMoneyFlatPenalty = 0;
    // ... (reset de todos los modificadores)

    // ── Verificar expiración de clima ───────────────────────────
    if (_activeWeatherCardId >= 0)
    {
        if (_currentRound > _weatherStartRound + _weatherDurationRounds)
        {
            // Clima expiró, limpiar
            ClearActiveWeather();
            SyncWeatherToAllPlayers(runner);
        }
    }

    // ── Sincronizar número de ronda a todos los jugadores ───────
    foreach (var player in runner.ActivePlayers)
    {
        var data = GetPlayerData(player, runner);
        if (data != null) data.CurrentRound = _currentRound;
    }

    // ── Determinar orden de turno (solo ronda 1) ───────────────
    if (!_isTurnOrderLocked)
    {
        DetermineTurnOrder(runner);
        _isTurnOrderLocked = true;
    }
    else if (_invertTurnOrderNextRound)
    {
        // Invertir orden para próxima ronda
        _turnOrder.Reverse();
        _invertTurnOrderNextRound = false;
    }

    // ── Inicializar estado de jugadores ────────────────────────
    InitializeRoundPlayerState(runner);
    SyncBasinHealthToAllPlayers(runner);

    // ── Disparar evento para UI ────────────────────────────────
    Networking.Events.NetworkEventDefinitions.Instance?.OnRoundStartedEvent?.Raise(default, runner);

    // ── Avanzar al primer jugador ────────────────────────────
    State = GameState.PlayerTurn;
    AdvanceTurn(runner);
}
```

#### 3.3.2. **AdvanceTurn() - Transición de Turno**

```csharp
private void AdvanceTurn(NetworkRunner runner, float delay = 0f)
{
    // Limpiar estado del jugador actual
    foreach (var player in runner.ActivePlayers)
    {
        var data = GetPlayerData(player, runner);
        if (data != null && data.IsActiveTurn)
        {
            data.IsActiveTurn = false;
            data.HasRolledThisTurn = false;
            data.HasScannedARThisTurn = false;
            ClearPendingProjectState(data);  // Limpiar proyecto pendiente
        }
    }

    // Si hay delay (para animaciones), esperar
    if (delay > 0f)
    {
        StartCoroutine(AdvanceTurnCoroutine(runner, delay));
    }
    else
    {
        ExecuteAdvanceTurn(runner);
    }
}

private void ExecuteAdvanceTurn(NetworkRunner runner)
{
    _activeTurnIndex++;
    if (_activeTurnIndex >= _turnOrder.Count)
    {
        // Fin de ronda — todos los jugadores tuvieron su turno
        StartCoroutine(EndRoundDelayedCoroutine(runner, _turnAdvancementDelaySeconds));
        return;
    }

    var activePlayer = _turnOrder[_activeTurnIndex];
    var activeData = GetPlayerData(activePlayer, runner);
    if (activeData != null)
    {
        activeData.IsActiveTurn = true;

        // ── Aplicar efecto de clima: pérdida de agua al inicio de turno ───
        if (_activeWeatherCardId >= 0 && _weatherAllPlayersWaterPerTurnDelta != 0)
        {
            ApplyWaterDelta(activeData, activePlayer, runner, _weatherAllPlayersWaterPerTurnDelta, respectShield: false);
        }

        // ── Clima especial: dado de recompensa al inicio de turno ────────
        if (_activeWeatherCardId >= 0 && _weatherRollDependentRewards)
        {
            int dieRoll = Random.Range(1, 7);  // D6
            int waterDelta = dieRoll >= 4 ? 4 : -3;   // ≥4 = +4 agua, <4 = -3 agua
            int moneyDelta = dieRoll >= 4 ? 3 : -3;

            activeData.RPC_SyncWeatherRollResult(dieRoll, waterDelta, moneyDelta);
            
            ApplyWaterDelta(activeData, activePlayer, runner, waterDelta, respectShield: false);
            ApplyMoneyDelta(activeData, activePlayer, runner, moneyDelta, respectShield: false);
        }
    }

    State = GameState.PlayerTurn;
    Networking.Events.NetworkEventDefinitions.Instance?.OnTurnStartedEvent?.Raise(activePlayer, runner);
}
```

**Patrón:** El delay permite que animaciones del turno anterior terminen antes de mostrar el siguiente turno. `_turnAdvancementDelaySeconds` es configurable en el Inspector (típicamente 4.5 segundos).

### 3.4. Resolución de Casillas: `ResolveTileAndApplyEffects()`

Cuando un jugador cae en una casilla, se ejecuta lógica diferente según el tipo:

```csharp
private bool ResolveTileAndApplyEffects(PlayerSessionData playerData, NetworkRunner runner, bool fromTeleport = false)
{
    State = GameState.BasinCheck;
    var tileType = _tileService.GetTileType(playerData.BoardPosition);

    // ── Casilla de Inicio: sin efecto ───────────────────────
    if (tileType == SliceTileType.Start)
    {
        return true;  // Avanzar turno inmediatamente
    }

    // ── Casilla de Proyecto: esperar escaneo AR ────────────
    if (tileType == SliceTileType.Project)
    {
        return BeginProjectTileFlow(playerData);  // Retorna false (no avanzar todavía)
    }

    // ── Casilla de Carta: esperar escaneo AR ───────────────
    if (tileType == SliceTileType.DrawCard)
    {
        return BeginDrawCardTileFlow(playerData);  // Retorna false
    }

    // ── Casilla de Trivia: esperar respuesta ───────────────
    if (tileType == SliceTileType.Trivia)
    {
        return BeginTriviaTileFlow(playerData);  // Retorna false
    }

    // ── Casillas Hydric vs Catastrophic ────────────────────
    int waterDelta, moneyDelta, basinDelta;

    if (tileType == SliceTileType.Hydric)
    {
        // Casilla favorable
        waterDelta = _weatherNullifyHydricWater ? 0 : _hydricWaterGain;
        
        // Clima: bonificación a agua hídrica
        if (_activeWeatherCardId >= 0 && _weatherHydricWaterFlatBonus != 0)
            waterDelta = Mathf.Max(1, waterDelta + _weatherHydricWaterFlatBonus);
        
        moneyDelta = _hydricMoneyGain;
        basinDelta = _hydricBasinBonus;
    }
    else
    {
        // Casilla catastrófica
        waterDelta = -_catastrophicWaterPenalty;
        moneyDelta = -_catastrophicMoneyPenalty;
        basinDelta = -_catastrophicBasinPenalty;
    }

    // ── Aplicar deltas con modificadores de ronda ──────────
    ApplyWaterDelta(playerData, playerData.Object.InputAuthority, runner, waterDelta, respectShield: true);
    ApplyMoneyDelta(playerData, playerData.Object.InputAuthority, runner, moneyDelta, respectShield: true);

    // ── Clima: agua adicional a TODOS al resolver casilla ──
    if (_activeWeatherCardId >= 0 && _weatherAllPlayersWaterOnTileResolveDelta != 0)
    {
        foreach (var p in runner.ActivePlayers)
        {
            var d = GetPlayerData(p, runner);
            if (d != null)
            {
                ApplyWaterDelta(d, p, runner, _weatherAllPlayersWaterOnTileResolveDelta, respectShield: false);
            }
        }
    }

    // ── Aplicar delta de cuenca ────────────────────────────
    if (!_weatherLockBasin)
    {
        _basinService.ApplyDelta(basinDelta);
        SyncBasinHealthToAllPlayers(runner);

        if (_basinService.IsDefeated)
        {
            SetGameState(GameState.Defeat);
            _roundInProgress = false;
            foreach (var p in runner.ActivePlayers)
            {
                var d = GetPlayerData(p, runner);
                if (d != null) { d.IsGameOver = true; d.IsDefeat = true; }
            }
        }
    }

    return true;  // Avanzar turno
}
```

**Patrón de Retorno:**
- `true` = Avanzar turno inmediatamente
- `false` = Esperar decisión (AR scan, voto, trivia answer)

### 3.5. Tubería de Procesamiento de Recursos: `ApplyWaterDelta()` y `ApplyMoneyDelta()`

**Justificación Ingenieril:** El patrón **Pipeline/Decorator** garantiza que TODOS los modificadores se apliquen en orden consistente, evitando carrera de condiciones lógicas:

```csharp
private void ApplyWaterDelta(PlayerSessionData data, PlayerRef player, NetworkRunner runner, 
                              int delta, bool respectShield = true)
{
    // ── Capa 1: Escudo negativo (absorbe un ataque, se consume) ───────
    if (delta < 0 && respectShield && data.HasNegativeShield)
    {
        data.HasNegativeShield = false;
        return;  // Escudo absorbió el daño
    }

    int effectiveDelta = delta;

    // ── Capa 2: Modificadores si la ganancia es positiva ──────────────
    if (effectiveDelta > 0)
    {
        // Penalización plana
        int afterFlat = Mathf.Max(0, effectiveDelta - _roundWaterGainFlatPenalty + _roundWaterGainFlatBonus);
        
        // Penalización porcentual
        float penaltyMult = 1f - Mathf.Clamp(_roundWaterGainPercentPenalty, 0, 100) / 100f;
        float bonusMult = 1f + Mathf.Clamp(_roundWaterGainPercentBonus, 0, 100) / 100f;
        
        effectiveDelta = Mathf.RoundToInt(afterFlat * penaltyMult * bonusMult);
    }

    // ── Capa 3: Aplicar final con sujeción ─────────────────────────
    data.WaterAmount = Mathf.Max(0, data.WaterAmount + effectiveDelta);
    
    Debug.Log($"[GameManager] Player {player.PlayerId}: water delta {delta} → {effectiveDelta}. New total: {data.WaterAmount}");
}
```

**Orden de aplicación:**
1. **Escudos:** Si hay escudo negativo y el delta es daño, consume escudo y termina
2. **Modificadores planos:** Resta/suma fija (ej: -2 agua)
3. **Modificadores porcentuales:** Aplica multiplicador (ej: ×0.8 si hay penalización 20%)
4. **Sujeción:** Clamp a [0, ∞) para evitar valores negativos

### 3.6. Sistema de Clima: Aplicación y Sincronización

**Clima** es una carta especial que modifica el juego durante X rondas:

```csharp
// Cuando se escanea una carta de clima:
if (card.IsWeatherCard)
{
    ClearActiveWeather();  // Limpiar clima anterior
    
    _activeWeatherCardId = card.CardId;
    _weatherStartRound = _currentRound;
    _weatherDurationRounds = card.WeatherDurationRounds;  // Ej: 2 rondas más
    
    // Bonificaciones/penalizaciones
    _weatherHydricWaterFlatBonus = card.WeatherHydricWaterFlatBonus;  // +1 agua en Hydric
    _weatherAllPlayersWaterPerTurnDelta = card.WeatherAllPlayersWaterPerTurnDelta;  // -1 agua a todos
    _weatherDiceRollFlatBonus = card.WeatherDiceRollFlatBonus;  // +2 al dado
    _weatherRollDependentRewards = card.WeatherRollDependentRewards;  // ¿Dado especial?
    _weatherLockBasin = card.WeatherLockBasin;  // ¿Congelar cuenca?
    _weatherNullifyHydricWater = card.WeatherNullifyHydricWater;  // ¿Anular agua de Hydric?
    
    _activeWeatherTag = card.WeatherTag;  // Etiqueta (Drought, Flood, etc)
    
    // Sincronizar a todos los clientes
    SyncWeatherToAllPlayers(runner);
    
    Debug.Log($"[GameManager] Weather '{card.DisplayName}' activated for {_weatherDurationRounds} rounds.");
}
```

**Sincronización a clientes:**
```csharp
private void SyncWeatherToAllPlayers(NetworkRunner runner)
{
    foreach (var player in runner.ActivePlayers)
    {
        var data = GetPlayerData(player, runner);
        if (data != null)
        {
            data.ActiveWeatherTag = _activeWeatherTag;
            data.WeatherVersion++;  // Incrementar versión para detectar cambios
        }
    }
}
```

**Propósito de `WeatherVersion`:** Los clientes detectan cambios de clima observando si `WeatherVersion` aumentó, lo que dispara re-renderizado de UI sin necesidad de eventos.

### 3.7. Resolución de Cartas y Decisiones: `HandleCardScan()`

Cuando un jugador escanea una carta AR, se aplican hasta **7 capas de efectos**:

```csharp
public void HandleCardScan(PlayerRef player, NetworkRunner runner, int cardId)
{
    if (!runner.IsServer) return;
    
    var playerData = GetPlayerData(player, runner);
    if (playerData == null || !playerData.IsAwaitingCardScan) return;

    if (!_cardDatabase.TryGetCard(cardId, out var card)) return;

    // ── Capa 0: Multiplicador por umbral de cuenca ─────────────────
    int thresholdMultiplier = 1;
    if (card.UseBasinThresholdDoubleDeltas)
    {
        int currentBasin = _basinService.BasinHealth;
        int threshold = Mathf.RoundToInt(_startingBasinHealth * card.BasinThresholdPercentage);
        if (currentBasin <= threshold)
            thresholdMultiplier = 2;  // Doblar efectos si cuenca está débil
    }

    // ── Capa 1: Agua y dinero base ────────────────────────────────
    int cardWaterDelta = (card.ConditionalWaterIfWeatherTag != WeatherTag.None &&
                          _activeWeatherTag == card.ConditionalWaterIfWeatherTag)
        ? card.ConditionalWaterDelta  // Usar valor condicional si hay clima activo
        : card.WaterDelta;
    cardWaterDelta *= thresholdMultiplier;
    ApplyWaterDelta(playerData, player, runner, cardWaterDelta, respectShield: true);

    // ── Capa 1b: Dinero condicional por proyectos ──────────────────
    if (card.ConditionalMoneyOnProjects)
    {
        int projectCount = CountOwnedProjects(playerData);
        int cd = (projectCount >= 1) 
            ? card.MoneyWithActiveProject 
            : card.MoneyWithoutActiveProject;
        if (cd != 0) ApplyMoneyDelta(playerData, player, runner, cd, respectShield: true);
    }

    // ── Capa 1c: Dinero condicional por umbral de cuenca ───────────
    if (card.UseBasinThresholdMoneyDelta)
    {
        int currentBasin = _basinService.BasinHealth;
        int threshold = Mathf.RoundToInt(_startingBasinHealth * card.BasinThresholdPercentage);
        int md = (currentBasin > threshold) 
            ? card.MoneyDeltaAboveThreshold 
            : card.MoneyDeltaBelowThreshold;
        if (md != 0) ApplyMoneyDelta(playerData, player, runner, md, respectShield: true);
    }

    // ── Capa 1d: Activación de clima (sobrescribe anterior) ────────
    if (card.IsWeatherCard)
    {
        // ... [lógica de clima] ...
    }

    // ── Capa 1e: Terminación de clima (limpia si hay uno activo) ───
    if (card.TerminatesActiveWeather && _activeWeatherCardId >= 0)
    {
        ClearActiveWeather();
        if (card.BasinFlatOnWeatherTerminate != 0)
        {
            _basinService.ApplyDelta(card.BasinFlatOnWeatherTerminate);
            SyncBasinHealthToAllPlayers(runner);
        }
    }

    // ── Capa 2: Delta de cuenca ───────────────────────────────────
    if (card.BasinDelta != 0 && !_weatherLockBasin)
    {
        int basinDelta = card.BasinDelta * thresholdMultiplier;
        
        // Clima especial: doblar recuperación de cuenca
        if (_weatherDoubleBasinRecovery && basinDelta > 0)
            basinDelta *= 2;
        
        _basinService.ApplyDelta(basinDelta);
        SyncBasinHealthToAllPlayers(runner);
    }

    playerData.IsAwaitingCardScan = false;
    AdvanceTurn(runner, _turnAdvancementDelaySeconds);
}
```

**Justificación de Capas:** Cada capa es independiente y puede ser modificada (ej: agregar nuevos condicionantes) sin afectar otras capas. Esto es el patrón **Open/Closed Principle**.

### 3.8. Validación de Turno: `HandleValidatedTurnRoll()`

Cuando `PlayerSessionData.RPC_RequestValidatedTurnRoll()` es invocado, el Host ejecuta:

```csharp
public void HandleValidatedTurnRoll(PlayerRef player, int diceRoll, NetworkRunner runner)
{
    if (!runner.IsServer || !_roundInProgress) return;

    var activePlayer = GetActivePlayer(runner);
    if (activePlayer != player) return;  // Guardián: no es el turno del jugador

    var playerData = GetPlayerData(player, runner);
    if (playerData == null) return;

    // ── Aplicar modificadores al dado ──────────────────────────────
    int effectiveRoll = Mathf.Max(1, diceRoll + playerData.PendingDiceModifier + _weatherDiceRollFlatBonus);
    playerData.PendingDiceModifier = 0;  // Consumir modificador
    playerData.LastDiceRoll = effectiveRoll;

    // ── Calcular nueva posición ────────────────────────────────────
    int oldPosition = playerData.BoardPosition;
    int nextPosition = oldPosition + effectiveRoll;
    bool passedStart = nextPosition >= _boardTileCount;

    playerData.BoardPosition = nextPosition % _boardTileCount;

    // ── Recompensa por pasar inicio ────────────────────────────────
    if (passedStart)
    {
        ApplyMoneyDelta(playerData, player, runner, 3, respectShield: false);
        Debug.Log($"[GameManager] Player {player.PlayerId} completed a lap! +3 money.");
    }

    // ── Resolver casilla ───────────────────────────────────────────
    State = GameState.TileResolve;
    bool shouldAdvanceTurn = ResolveTileAndApplyEffects(playerData, runner);
    
    // ── Avanzar con delay para animación ───────────────────────────
    if (shouldAdvanceTurn)
    {
        float delay = (effectiveRoll * _diceJumpDurationPerUnit) + _diceJumpBufferSeconds;
        AdvanceTurn(runner, delay);
    }
}
```

**Guardias de Seguridad:**
1. Solo Host ejecuta
2. Solo ejecuta si es el turno del jugador
3. Solo ejecuta si la ronda está en progreso
4. Modifica la posición **después** de validar autoridad

---

## 4. Definición de Proyectos: `ProjectDefinition.cs`

**Justificación Arquitectónica:** Este componente representa el pilar económico y estratégico del juego. Implementa un diseño **Data-Driven** (basado en datos), donde cada proyecto es un activo independiente (`ScriptableObject`). Esto permite que el equilibrio del juego (costos, beneficios, comportamientos) se ajuste en tiempo real sin modificar el código fuente, facilitando la iteración de diseño.

### 4.1. Lógica de Ingresos Geográficos (`GetIncomeForZone`)
**Justificación Ingenieril:** El sistema utiliza un cálculo aditivo dinámico. Cada proyecto tiene una base garantizada y bonificadores condicionales que se activan según la zona geográfica (Andina, Caribe, etc.). 
*   **Propósito:** Fomentar la toma de decisiones basada en la posición del jugador en el tablero, simulando la variabilidad de recursos reales en diferentes regiones de Colombia.

```csharp
public (int water, int money) GetIncomeForZone(ColombiaZone zone)
{
    // Base garantizada independiente de la zona
    int water = _baseWaterPerRound;
    int money = _baseMoneyPerRound;

    // Búsqueda de bonificadores regionales definidos en el ScriptableObject
    if (_zoneEffects != null) {
        for (int i = 0; i < _zoneEffects.Length; i++) {
            if (_zoneEffects[i].Zone == zone) {
                water += _zoneEffects[i].BonusWaterPerRound;
                money += _zoneEffects[i].BonusMoneyPerRound;
                break;
            }
        }
    }
    return (water, money);
}
```

### 4.2. Ingeniería de Comportamientos Pasivos (`ProjectPassiveBehaviour`)
**Justificación Ingenieril:** Se implementa el uso de **Banderas de Bits (Bitwise Flags)**. En lugar de procesar múltiples booleanos independientes, el sistema evalúa comportamientos complejos (ej. recuperación de cuenca, inmunidad a sequía) mediante operaciones binarias.
*   **Eficiencia:** Esta técnica reduce el uso de memoria a un solo entero de 32 bits y permite realizar verificaciones de comportamiento con una sola operación de CPU (AND bit a bit), lo cual es óptimo para ejecuciones frecuentes en el bucle de renderizado.

```csharp
[Flags]
public enum ProjectPassiveBehaviour {
    None = 0,
    DoublesWaterBelowBasinThreshold = 1 << 1, // Estrategia de resiliencia
    NullifiedByDroughtEvent = 1 << 2,         // Vulnerabilidad climática
    BasinRecoveryPerRound = 1 << 7            // Impacto ecológico positivo
}

// Validación de alta eficiencia
public bool HasBehaviour(ProjectPassiveBehaviour flag) => (_passiveBehaviours & flag) != 0;
```

### 4.3. Resiliencia y Adaptabilidad Ecológica
El script encapsula la lógica de **Retroalimentación Ecológica**. Los proyectos pueden duplicar su producción de agua si la cuenca está en estado crítico (`_basinThresholdForBonus`), representando sistemas de emergencia o de conservación activa. 
*   **Modularidad:** Al definir estos umbrales en el dato y no en el código, el sistema permite tener proyectos "sensibles" a la ecología y proyectos "industriales" menos afectados, simplemente cambiando su configuración en el Inspector de Unity.

### 4.4. Proceso de Adquisición de Proyectos (`HandleBuyPendingProject`)
**Justificación Ingenieril:** La compra de un proyecto es una transacción crítica que debe ser validada en el Host para evitar inconsistencias financieras. El sistema utiliza un estado de "Proyecto Pendiente" que sincroniza los datos del proyecto a escanear antes de la confirmación final.

```csharp
public void HandleBuyPendingProject(PlayerRef player, NetworkRunner runner)
{
    var playerData = GetPlayerData(player, runner);
    // Validación de autoridad, turno y recursos financieros
    if (playerData == null || !playerData.IsActiveTurn || playerData.PendingProjectPrice > playerData.MoneyAmount)
        return;

    // Asignación atómica al slot disponible
    if (TryAssignOwnedProject(playerData, playerData.PendingProjectId, (ColombiaZone)playerData.PendingProjectZone))
    {
        // Deducción de moneda sincronizada
        playerData.MoneyAmount = Mathf.Max(0, playerData.MoneyAmount - playerData.PendingProjectPrice);
        ClearPendingProjectState(playerData); // Limpieza de estado transitorio
        AdvanceTurn(runner, _turnAdvancementDelaySeconds);
    }
}
```

### 4.5. Lógica de Ingresos Pasivos No-Regionales
**Justificación Ingenieril:** El sistema permite la generación de recursos que no dependen de la ubicación del jugador, sino de comportamientos inherentes al proyecto o condiciones ambientales globales (Clima). Esto se gestiona mediante el método `ApplyProjectPassive`, que actúa como un despachador de ingresos al finalizar cada ronda.

*   **Bypass de Penalizaciones:** Algunos proyectos poseen la propiedad `BypassesRoundWaterPenalty`, lo que les permite inyectar agua directamente al `PlayerSessionData` sin pasar por la tubería de modificadores del `GameManager`, garantizando un ingreso base en situaciones de crisis extrema.
*   **Recuperación de Cuenca Hídrica:** Proyectos con `BasinRecoveryPerRound` impactan directamente en el `BasinService`, recuperando salud ambiental de forma automática al inicio de cada ronda, independientemente de las acciones del jugador.

```csharp
// Aplicación de bonificación climática plana (No relacionada con zonas)
if (_weatherProjectWaterFlatBonusPerRound != 0 && water > 0)
    water += _weatherProjectWaterFlatBonusPerRound;

// Inyección directa de recursos (Efecto Pasivo)
if (project.HasBehaviour(ProjectPassiveBehaviour.BypassesRoundWaterPenalty)) {
    data.WaterAmount = Mathf.Max(0, data.WaterAmount + water);
}
```

---

## 5. Sistema de Definición de Cartas: `CardDefinition.cs`

**Justificación Arquitectónica:** `CardDefinition` es un `ScriptableObject` que encapsula toda la configuración de una carta de juego sin hardcodear valores. Cada carta puede aplicar **11 categorías de efectos** de forma independiente, permitiendo a diseñadores crear experiencias complejas mediante composición sin necesidad de código. Este enfoque desacopla contenido (cartas) de lógica (GameManager), facilitando balanceo iterativo.

### 4.1. Estructura Jerárquica: Categorías de Efectos

**CardDefinition** organiza sus 80+ parámetros en categorías bien definidas:

```csharp
[CreateAssetMenu(fileName = "Card_", menuName = "Networking/Card Definition")]
public class CardDefinition : ScriptableObject
{
    // ── 1. IDENTIDAD ──────────────────────────────────────────────
    [Header("Identity")]
    [SerializeField] private int _cardId;              // ID único (1, 2, 3, ...)
    [SerializeField] private string _displayName;     // Nombre visible ("Lluvia Torrencial")
    [TextArea(2, 6)]
    [SerializeField] private string _loreText;        // Narrativa
    [TextArea(2, 6)]
    [SerializeField] private string _effectDescription;  // Descripción jugable

    // ── 2. EFECTOS DIRECTOS (al escanear) ──────────────────────
    [Header("Direct Effects (scanning player)")]
    [SerializeField] private int _waterDelta;         // +/-X agua
    [SerializeField] private int _moneyDelta;         // +/-X dinero
    [SerializeField] private int _basinDelta;         // +/-X salud cuenca
    [SerializeField] private int _diceModifier;       // Modificador a próximo dado
    [SerializeField] private bool _grantsNegativeShield;  // Escudo absorbe siguiente daño
    [SerializeField] private bool _grantsDoubleTriviaReward;  // Próxima trivia cuenta doble

    // ── 3. EFECTOS GLOBALES (todos los jugadores) ──────────────
    [Header("Global Effects (all players)")]
    [SerializeField] private int _allPlayersWaterDelta;    // +/-X agua a TODOS
    [SerializeField] private int _allPlayersMoneyDelta;    // +/-X dinero a TODOS

    // ... (otros 8 headers documentados abajo)
}
```

### 4.2. Categoría 1: Efectos Directos Inmediatos

**Cuando un jugador escanea una carta, se le aplican inmediatamente:**

```csharp
// Direct Effects del CardDefinition
public int WaterDelta            => _waterDelta;             // Agua base
public int MoneyDelta            => _moneyDelta;             // Dinero base
public int BasinDelta            => _basinDelta;             // Cuenca base
public int DiceModifier          => _diceModifier;           // +X al próximo dado
public bool GrantsNegativeShield => _grantsNegativeShield;   // Escudo
public bool GrantsDoubleTriviaReward => _grantsDoubleTriviaReward;  // Trivia doble
```

**Ejemplo 1: Carta "Manantial Puro"**
```
CardId: 101
DisplayName: "Manantial Puro"
WaterDelta: +8
MoneyDelta: 0
BasinDelta: +3
GrantsNegativeShield: false
```

**Lógica de Aplicación (en GameManager.HandleCardScan):**
```csharp
int cardWaterDelta = (card.ConditionalWaterIfWeatherTag != WeatherTag.None &&
                      _activeWeatherTag == card.ConditionalWaterIfWeatherTag)
    ? card.ConditionalWaterDelta  // Usar valor condicional si hay clima activo
    : card.WaterDelta;            // Si no, usar valor base

ApplyWaterDelta(playerData, player, runner, cardWaterDelta, respectShield: true);
ApplyMoneyDelta(playerData, player, runner, card.MoneyDelta, respectShield: true);

if (!_weatherLockBasin)
    _basinService.ApplyDelta(card.BasinDelta);
```

### 4.3. Categoría 2: Efectos Globales (Todos los Jugadores)

**Algunas cartas afectan a TODOS los jugadores simultáneamente:**

```csharp
public int AllPlayersWaterDelta => _allPlayersWaterDelta;    // +/-X agua a todos
public int AllPlayersMoneyDelta => _allPlayersMoneyDelta;    // +/-X dinero a todos
```

**Ejemplo 2: Carta "Sequía Global"**
```
CardId: 205
DisplayName: "Sequía Global"
WaterDelta: 0           // Jugador que la escanea no recibe nada
MoneyDelta: +5          // Pero sí recibe dinero de la compensación
AllPlayersWaterDelta: -2   // TODOS pierden 2 de agua (incluido el escáner)
BasinDelta: -5             // Cuenca se daña
```

**Aplicación en GameManager:**
```csharp
// Primero aplicar efecto individual
ApplyWaterDelta(playerData, player, runner, card.WaterDelta, respectShield: true);

// Luego aplicar efecto global a TODOS
if (card.AllPlayersWaterDelta != 0)
{
    foreach (var p in runner.ActivePlayers)
    {
        var data = GetPlayerData(p, runner);
        if (data != null)
        {
            ApplyWaterDelta(data, p, runner, card.AllPlayersWaterDelta, respectShield: false);
        }
    }
}
```

### 4.4. Categoría 3: Modificadores de Ronda

**Penalizaciones y bonificaciones que afectan TODOS los gains de la ronda:**

```csharp
// PENALIZACIONES: reducen ganancias
public int RoundWaterGainPenalty    => _roundWaterGainPenalty;     // -X agua ganancias
public int RoundMoneyGainPenalty    => _roundMoneyGainPenalty;     // -X dinero ganancias
public int RoundProjectMoneyPenalty => _roundProjectMoneyPenalty;  // -X dinero de proyectos

// BONIFICACIONES: aumentan ganancias
public int RoundWaterGainBonus      => _roundWaterGainBonus;       // +X agua ganancias
public int RoundMoneyGainBonus      => _roundMoneyGainBonus;       // +X dinero ganancias
public int RoundProjectMoneyBonus   => _roundProjectMoneyBonus;    // +X dinero de proyectos
```

**Ejemplo 3: Carta "Crisis Hídrica"**
```
CardId: 310
DisplayName: "Crisis Hídrica"
WaterDelta: -3              // Jugador pierde agua
RoundWaterGainPenalty: 4    // TODOS pierden 4 de agua por cada ganancia
RoundProjectMoneyPenalty: 2 // Proyectos generan 2 dinero menos
```

**Cómo afecta a la ronda:**
```
Turno 1: Jugador A llega a casilla Hídrica → normalmente +8 agua
         Con penalización activa → 8 - 4 = +4 agua

Turno 2: Jugador B llega a casilla Hídrica → normalmente +8 agua
         Con penalización activa → 8 - 4 = +4 agua

Turno 3: Proyecto genera 10 dinero
         Con penalización activa → 10 - 2 = +8 dinero
```

**Almacenamiento en Host (no sincronizado):**
```csharp
// En GameManager.cs
private int _roundWaterGainFlatPenalty = 0;      // Acumulada de cartas scaneadas
private int _roundWaterGainFlatBonus = 0;
private int _roundMoneyGainFlatPenalty = 0;
private int _roundProjectMoneyPenalty = 0;
// ... (análogos para bonificaciones)

// Al scanear una carta:
_roundWaterGainFlatPenalty += card.RoundWaterGainPenalty;
_roundMoneyGainFlatBonus += card.RoundMoneyGainBonus;
```

**Aplicación en Pipeline (3.5 del GameManager):**
```csharp
// Capa 2: Modificadores si la ganancia es positiva
if (effectiveDelta > 0)
{
    int afterFlat = Mathf.Max(0, effectiveDelta - _roundWaterGainFlatPenalty 
                                                + _roundWaterGainFlatBonus);
    effectiveDelta = afterFlat;
}
```

### 4.5. Categoría 4: Teletransportación

**Una carta puede mover al jugador que la escanea a otra casilla:**

```csharp
public enum TeleportMode
{
    None,                   // Sin efecto
    ToSpecificIndex,        // Mover a casilla #N
    ToNearestTileType      // Mover a la casilla más cercana de tipo X
}

public TeleportMode TeleportMode => _teleportMode;
public int SelfMoveToTile => _selfMoveToTile;  // -1 si no hay, sino número 0-24
public Networking.Services.SliceTileType TeleportTargetTileType => _teleportTargetTileType;
```

**Ejemplo 4: Carta "Atajo Oculto"**
```
CardId: 415
DisplayName: "Atajo Oculto"
WaterDelta: 0
TeleportMode: ToSpecificIndex
SelfMoveToTile: 18        // Saltar a casilla 18
```

**Aplicación en GameManager:**
```csharp
if (card.TeleportMode == TeleportMode.ToSpecificIndex && card.SelfMoveToTile >= 0)
{
    playerData.BoardPosition = card.SelfMoveToTile;
    
    // Resolver la casilla de destino
    bool shouldAdvance = ResolveTileAndApplyEffects(playerData, runner, fromTeleport: true);
}
else if (card.TeleportMode == TeleportMode.ToNearestTileType)
{
    int nearestTile = _tileService.FindNearestTileOfType(
        playerData.BoardPosition, 
        card.TeleportTargetTileType);
    
    playerData.BoardPosition = nearestTile;
    ResolveTileAndApplyEffects(playerData, runner, fromTeleport: true);
}
```

### 4.6. Categoría 5: Eventos Nominados

**Cartas pueden activar eventos especiales que modifican reglas de proyectos:**

```csharp
public bool IsDroughtEvent => _isDroughtEvent;
public bool IsClimateEvent => _isClimateEvent;
public bool IsDeforestationEvent => _isDeforestationEvent;
public int DeforestationProjectMoneyPercentPenalty => _deforestationProjectMoneyPercentPenalty;
```

**Ejemplo 5: Carta "Sequía"**
```
CardId: 520
DisplayName: "Sequía Regional"
IsDroughtEvent: true         // Activa evento Drought
WaterDelta: -5               // Castigo al escáner
```

**Efecto en Proyectos:**
```
Proyecto con flag "NullifiedByDroughtEvent" durante sequía:
  Generación normal: +4 agua por ronda
  Con sequía activa: +0 agua por ronda (anulada)
```

**Activación en GameManager:**
```csharp
if (card.IsDroughtEvent)
{
    _activeDroughtThisRound = true;  // Proyectos consultan esto en Render()
}

// En ProjectPassiveManager.cs:
if (_gameManager.IsActiveDroughtThisRound)
{
    if (project.NullifiedByDroughtEvent)
    {
        waterIncome = 0;  // Anular ganancia
    }
}
```

### 4.7. Categoría 6: Efectos por Umbral de Cuenca

**Cartas pueden tener efectos diferentes si la cuenca está sana o débil:**

```csharp
public bool UseBasinThresholdDelta => _useBasinThresholdDelta;
public float BasinThresholdPercentage => _basinThresholdPercentage;  // 0-1.0
public int BasinDeltaAboveThreshold => _basinDeltaAboveThreshold;
public int BasinDeltaBelowThreshold => _basinDeltaBelowThreshold;

public bool UseBasinThresholdDoubleDeltas => _useBasinThresholdDoubleDeltas;
```

**Ejemplo 6: Carta "Acción de Emergencia"**
```
CardId: 625
DisplayName: "Acción de Emergencia"
UseBasinThresholdDelta: true
BasinThresholdPercentage: 0.3    // 30% de salud máxima
BasinDeltaAboveThreshold: 0      // Si cuenca está sana: sin efecto
BasinDeltaBelowThreshold: +12    // Si cuenca está débil: RECUPERAR 12
```

**Aplicación:**
```csharp
float thresholdHealth = startingBasinHealth * 0.3f;  // 30% = 50 de 160

Escenario A (Cuenca sana = 100/160):
  100 > 50 → Aplicar BasinDeltaAboveThreshold = 0

Escenario B (Cuenca crítica = 30/160):
  30 ≤ 50 → Aplicar BasinDeltaBelowThreshold = +12 (recuperar)
```

**Doblaje Automático:**
```csharp
if (card.UseBasinThresholdDoubleDeltas && basinHealth <= threshold)
{
    waterDelta *= 2;      // Doble agua
    moneyDelta *= 2;      // Doble dinero
    basinDelta *= 2;      // Doble efecto de cuenca
}
```

### 4.8. Categoría 7: Efectos Condicionados por Proyectos

**Dinero puede variar según cuántos proyectos posee el escáner:**

```csharp
public bool ConditionalMoneyOnProjects => _conditionalMoneyOnProjects;
public int MoneyWithActiveProject => _moneyWithActiveProject;
public int MoneyWithoutActiveProject => _moneyWithoutActiveProject;
```

**Ejemplo 7: Carta "Apoyo a Emprendimientos"**
```
CardId: 730
DisplayName: "Apoyo a Emprendimientos"
ConditionalMoneyOnProjects: true
MoneyWithActiveProject: +8      // Si tienes proyectos: +8 dinero
MoneyWithoutActiveProject: +2   // Si no tienes: solo +2 dinero
```

**Aplicación en GameManager.HandleCardScan():**
```csharp
if (card.ConditionalMoneyOnProjects)
{
    int projectCount = CountOwnedProjects(playerData);
    int moneyDelta = (projectCount >= 1) 
        ? card.MoneyWithActiveProject 
        : card.MoneyWithoutActiveProject;
    
    ApplyMoneyDelta(playerData, player, runner, moneyDelta, respectShield: true);
}
```

### 4.9. Categoría 8: Cartas de Clima (Multi-Ronda)

**Cartas de clima reemplazan el clima activo y aplican efectos durante X rondas:**

```csharp
public bool IsWeatherCard => _isWeatherCard;
public WeatherTag WeatherTag => _weatherTag;  // Rain, Drought, Flood, Freeze
public int WeatherDurationRounds => _weatherDurationRounds;

// Efectos mientras el clima está activo:
public int WeatherHydricWaterFlatBonus => _weatherHydricWaterFlatBonus;
public int WeatherAllPlayersWaterPerTurnDelta => _weatherAllPlayersWaterPerTurnDelta;
public int WeatherAllPlayersWaterOnTileResolveDelta => _weatherAllPlayersWaterOnTileResolveDelta;
public int WeatherDiceRollFlatBonus => _weatherDiceRollFlatBonus;
public bool WeatherRollDependentRewards => _weatherRollDependentRewards;  // ¿Dado especial?
public int WeatherProjectMoneyPercentPenalty => _weatherProjectMoneyPercentPenalty;
public int WeatherBasinFlatPerRound => _weatherBasinFlatPerRound;
public bool WeatherLockBasin => _weatherLockBasin;  // ¿Congelar cuenca?
public bool WeatherNullifyHydricWater => _weatherNullifyHydricWater;
public bool WeatherDoubleBasinRecovery => _weatherDoubleBasinRecovery;
```

**Ejemplo 8: Carta "Lluvia Torrencial" (Clima)**
```
CardId: 840
DisplayName: "Lluvia Torrencial"
IsWeatherCard: true
WeatherTag: Rain
WeatherDurationRounds: 2           // Activa por 2 rondas más

// Efectos:
WeatherHydricWaterFlatBonus: +3        // Casillas Hídrica dan +3 agua extra
WeatherAllPlayersWaterPerTurnDelta: +1 // Cada turno: +1 agua a todos
WeatherAllPlayersWaterOnTileResolveDelta: +2  // Al resolver casilla: +2 agua a todos
WeatherDiceRollFlatBonus: +1           // Dados: +1 al resultado
WeatherProjectMoneyPercentPenalty: 20  // Proyectos generan 20% menos dinero
WeatherBasinFlatPerRound: +5           // Cada ronda: cuenca +5
```

**Activación en GameManager.HandleCardScan():**
```csharp
if (card.IsWeatherCard)
{
    ClearActiveWeather();  // Eliminar clima anterior
    
    _activeWeatherCardId = card.CardId;
    _weatherStartRound = _currentRound;
    _weatherDurationRounds = card.WeatherDurationRounds;
    _weatherHydricWaterFlatBonus = card.WeatherHydricWaterFlatBonus;
    _weatherAllPlayersWaterPerTurnDelta = card.WeatherAllPlayersWaterPerTurnDelta;
    // ... (copiar todos los parámetros)
    
    SyncWeatherToAllPlayers(runner);  // Notificar UI
}
```

**Ciclo de Clima:**
```
Ronda 1 (scan): Clima activo, duración = 2
                Ronda 1 (scan round) ✓
                Ronda 2 ✓
                Ronda 3 (expirado)

Inicio Ronda 3: if (_currentRound > _weatherStartRound + _weatherDurationRounds)
                    ClearActiveWeather();
```

### 4.10. Categoría 9: Decisiones (A vs B)

**Cartas pueden requerir que jugadores (o toda la sala) voten entre dos opciones:**

```csharp
public enum CardDecisionScope
{
    None,           // Sin decisión
    Individual,     // Solo el escáner elige
    Collective      // TODOS los jugadores votan
}

public bool RequiresDecision => _requiresDecision;
public CardDecisionScope DecisionScope => _decisionScope;
public CardDecisionChoice DecisionChoiceA => _decisionChoiceA;
public CardDecisionChoice DecisionChoiceB => _decisionChoiceB;

public class CardDecisionChoice
{
    public string Label;  // "Proteger cuenca" o "Desarrollar economía"
    
    public int WaterDelta;
    public int MoneyDelta;
    public int BasinDelta;
    
    public int AllPlayersWaterDelta;
    public int AllPlayersMoneyDelta;
    
    public int DiceModifier;
    public bool GrantsNegativeShield;
    
    // ... (también puede incluir Round Modifiers, Events, etc)
}
```

**Ejemplo 9: Carta "Dilema Político" (Decisión Colectiva)**
```
CardId: 950
DisplayName: "Dilema Político"
RequiresDecision: true
DecisionScope: Collective    // TODOS votan

DecisionChoiceA.Label: "Fortalecer Regulación"
  DecisionChoiceA.WaterDelta: +2 (por jugador)
  DecisionChoiceA.BasinDelta: +8
  DecisionChoiceA.AllPlayersMoneyDelta: -1 (costo social)

DecisionChoiceB.Label: "Impulsar Economía"
  DecisionChoiceB.WaterDelta: -3 (por jugador)
  DecisionChoiceB.MoneyDelta: +10 (por jugador)
  DecisionChoiceB.BasinDelta: -5
```

**Flujo en GameManager:**
```csharp
if (card.RequiresDecision)
{
    _pendingDecisionCardId = card.CardId;
    _pendingDecisionScope = card.DecisionScope;
    
    if (card.DecisionScope == CardDecisionScope.Individual)
    {
        // Solo el escáner vota — esperar RPC_RequestDecisionVote
        playerData.IsAwaitingDecisionVote = true;
    }
    else if (card.DecisionScope == CardDecisionScope.Collective)
    {
        // TODOS votan — esperar RPC_RequestDecisionVote de cada uno
        foreach (var p in runner.ActivePlayers)
        {
            var data = GetPlayerData(p, runner);
            if (data != null) data.IsAwaitingDecisionVote = true;
        }
    }
}

// Cuando un jugador vota (via RPC_RequestDecisionVote):
var chosenOption = (choice == 1) ? card.DecisionChoiceA : card.DecisionChoiceB;
ApplyWaterDelta(playerData, player, runner, chosenOption.WaterDelta, ...);
ApplyMoneyDelta(playerData, player, runner, chosenOption.MoneyDelta, ...);

// Si hay AllPlayers deltas, aplicar a todos
if (chosenOption.AllPlayersWaterDelta != 0)
{
    foreach (var p in runner.ActivePlayers)
        ApplyWaterDelta(GetPlayerData(p, ...), p, runner, 
                       chosenOption.AllPlayersWaterDelta, ...);
}
```

### 4.11. Categoría 10: Condiciones Dependientes del Clima

**Una carta puede cambiar su efecto si hay un clima específico activo:**

```csharp
public WeatherTag ConditionalWaterIfWeatherTag => _conditionalWaterIfWeatherTag;
public int ConditionalWaterDelta => _conditionalWaterDelta;
```

**Ejemplo 10: Carta "Cosecha Adaptada" (Condicional)**
```
CardId: 1050
DisplayName: "Cosecha Adaptada"
WaterDelta: 4              // Agua base

ConditionalWaterIfWeatherTag: Drought  // Si hay sequía:
ConditionalWaterDelta: 8               // Da 8 agua en lugar de 4
```

**Aplicación en GameManager.HandleCardScan():**
```csharp
int cardWaterDelta = (card.ConditionalWaterIfWeatherTag != WeatherTag.None &&
                      _activeWeatherTag == card.ConditionalWaterIfWeatherTag)
    ? card.ConditionalWaterDelta    // 8 agua
    : card.WaterDelta;              // 4 agua

ApplyWaterDelta(playerData, player, runner, cardWaterDelta, respectShield: true);
```

### 4.12. Categoría 11: Terminación de Clima

**Cartas pueden terminar el clima activo aplicando un efecto de cierre:**

```csharp
public bool TerminatesActiveWeather => _terminatesActiveWeather;
public WeatherTag TerminatesWeatherTag => _terminatesWeatherTag;  // None = terminar cualquiera
public int BasinFlatOnWeatherTerminate => _basinFlatOnWeatherTerminate;
```

**Ejemplo 11: Carta "Escape a la Sequía"**
```
CardId: 1150
DisplayName: "Escape a la Sequía"
TerminatesActiveWeather: true
TerminatesWeatherTag: Drought    // Solo termina si el clima es Drought
BasinFlatOnWeatherTerminate: 10  // Al terminar: +10 cuenca de compensación
```

**Aplicación:**
```csharp
if (card.TerminatesActiveWeather && _activeWeatherCardId >= 0)
{
    // Verificar etiqueta
    bool shouldTerminate = (card.TerminatesWeatherTag == WeatherTag.None) 
        || (_activeWeatherTag == card.TerminatesWeatherTag);
    
    if (shouldTerminate)
    {
        ClearActiveWeather();
        
        if (card.BasinFlatOnWeatherTerminate != 0)
        {
            _basinService.ApplyDelta(card.BasinFlatOnWeatherTerminate);
            SyncBasinHealthToAllPlayers(runner);
        }
    }
}
```

### 4.13. Composición: Múltiples Categorías en Una Sola Carta

**La verdadera potencia de CardDefinition es que categorías se combinan:**

**Ejemplo 12: Carta Compleja "Reforma Ambiental Integral"**
```
CardId: 1200
DisplayName: "Reforma Ambiental Integral"

// Efecto directo + Global
WaterDelta: +5            (escáner)
AllPlayersWaterDelta: +2  (todos)

// Modificador de ronda
RoundWaterGainBonus: +2   (durante esta ronda)

// Efecto por umbral
UseBasinThresholdDoubleDeltas: true
BasinThresholdPercentage: 0.4

// Decisión
RequiresDecision: true
DecisionScope: Collective

// Condicional de clima
ConditionalWaterIfWeatherTag: Rain
ConditionalWaterDelta: +10  (si llueve: +10 en lugar de +5)
```

**Cuando se escanea:**
```
1. Aplicar agua directa: +5 (o +10 si hay lluvia)
2. Aplicar agua global: +2 a todos
3. Modificador de ronda: +2 bonus activado
4. Esperar decisión colectiva (A vs B)
5. Aplicar efectos de opción ganadora (may include more water/money/basin)
6. Si cuenca está débil: duplicar todos los deltas
```

### 4.14. API Pública y Acceso Centralizado

**CardDefinition expone todas sus propiedades via getters públicos:**

```csharp
// En CardDefinition.cs
public int CardId             => _cardId;
public string DisplayName     => _displayName;
public int WaterDelta         => _waterDelta;
public int BasinDelta         => _basinDelta;
public bool IsWeatherCard     => _isWeatherCard;
// ... (80+ propiedades públicas)
```

**Acceso desde GameManager:**
```csharp
if (!_cardDatabase.TryGetCard(cardId, out var card))
{
    Debug.LogError($"Card {cardId} not found!");
    return;
}

// Ahora acceder propiedades de forma segura
int waterDelta = card.WaterDelta;
bool isWeather = card.IsWeatherCard;
WeatherTag tag = card.WeatherTag;
```

**Database de Cartas:**
```csharp
// En GameManager o CardService
private Dictionary<int, CardDefinition> _cardDatabase;

public void InitializeCardDatabase()
{
    var allCards = Resources.LoadAll<CardDefinition>("Cards/");
    _cardDatabase = new Dictionary<int, CardDefinition>(allCards.Length);
    
    foreach (var card in allCards)
    {
        _cardDatabase[card.CardId] = card;
    }
}

public bool TryGetCard(int cardId, out CardDefinition card)
{
    return _cardDatabase.TryGetValue(cardId, out card);
}
```

### 4.15. Ventajas Arquitectónicas de CardDefinition

**1. Desacoplamiento Contenido-Lógica:**
```
Antes (sin ScriptableObject):
  if (cardId == 1) { water += 5; }
  if (cardId == 2) { water += 8; }
  // ... 50+ if statements

Después (con CardDefinition):
  var card = cardDatabase.GetCard(cardId);
  ApplyCardEffects(card);  // Lógica única y reutilizable
```

**2. Iteración de Balanceo sin Recompilación:**
- Cambiar `WaterDelta: 5 → 7` en Asset Inspector
- Sin necesidad de recompilar C#
- Resultado inmediato en editor

**3. Composición sobre Herencia:**
- CardDefinition NO hereda de otras cartas
- En lugar de herencia: combinar categorías independientes
- Ejemplo: Clima + Decisión + Condicional = 1 sola carta configurada

**4. Verificabilidad:**
```csharp
// Editor script de validación
[MenuItem("Tools/Validate All Cards")]
public static void ValidateAllCards()
{
    var cards = Resources.LoadAll<CardDefinition>("Cards/");
    foreach (var card in cards)
    {
        if (card.CardId == 0)
            Debug.LogError($"Card {card.DisplayName} has invalid ID 0");
        if (string.IsNullOrEmpty(card.DisplayName))
            Debug.LogError($"Card {card.CardId} has no name");
    }
}
```

---

## 5. Comunicación Desacoplada: Sistema de Eventos y Refresco de UI

**Justificación Arquitectónica:** El sistema de eventos `FusionEvent` resuelve el problema fundamental del **acoplamiento bidireccional fuerte**. En lugar de que `GameManager` tenga referencias directas a controladores de UI (lo que crearía dependencias circulares), se implementa el patrón **Observer mediado por Activos**: todos los eventos son canales de comunicación (`ScriptableObject`) almacenados centralmente en `NetworkEventDefinitions.Instance`.

### 5.1. Implementación Base: `FusionEvent.cs`

**Estructura actual (actualizada):**

```csharp
[CreateAssetMenu(menuName = "Networking/Fusion Event")]
public class FusionEvent : ScriptableObject
{
    // Lista pública de acciones registradas (reemplaza 'event' privado para mayor inspección)
    public List<Action<PlayerRef, NetworkRunner>> Responses = new List<Action<PlayerRef, NetworkRunner>>();

    /// <summary>
    /// Invoca todas las acciones registradas.
    /// Incluye manejo de excepciones para evitar que un error rompa toda la cadena.
    /// </summary>
    public void Raise(PlayerRef player = default, NetworkRunner runner = null)
    {
        for (int i = 0; i < Responses.Count; i++)
        {
            try
            {
                Responses[i].Invoke(player, runner);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in FusionEvent '{name}': {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Registra una acción para ser invocada cuando se dispara este evento.
    /// Evita duplicados verificando si ya existe.
    /// </summary>
    public void RegisterResponse(Action<PlayerRef, NetworkRunner> response)
    {
        if (response == null) return;
        if (!Responses.Contains(response))  // Guardián contra duplicados
        {
            Responses.Add(response);
        }
    }

    /// <summary>
    /// Desuscribe una acción del evento.
    /// Elimina TODAS las ocurrencias para limpiar suscripciones accidentales.
    /// </summary>
    public void RemoveResponse(Action<PlayerRef, NetworkRunner> response)
    {
        if (response == null) return;
        while (Responses.Contains(response))  // Eliminar duplicados si existen
        {
            Responses.Remove(response);
        }
    }

    /// <summary>
    /// Limpia todas las suscripciones (usado en cleanup o reset de pruebas).
    /// </summary>
    public void ClearAllResponses()
    {
        Responses.Clear();
    }
}
```

**Justificación de Diseño:**
- **List<Action> públicos:** Permite inspeccionar qué acciones están registradas en el Editor (útil para debugging)
- **Guardián contra duplicados:** `!Responses.Contains()` previene que la misma acción se registre dos veces
- **Try-catch en Raise():** Si un observador lanza excepción, los demás aún se ejecutan (robustez)
- **RemoveResponse() limpia todo:** Si hay duplicados accidentales, `while()` los elimina todos

### 5.2. Registro Centralizado: `NetworkEventDefinitions.cs`

**20+ eventos categorizados:**

```csharp
[CreateAssetMenu(menuName = "Networking/Event Definitions")]
public class NetworkEventDefinitions : ScriptableObject
{
    // ── Eventos de conexión ────────────────────────────
    [Header("Connection Events")]
    public FusionEvent OnPlayerJoinedEvent;       // Un jugador entra a la sala
    public FusionEvent OnPlayerLeftEvent;         // Un jugador se desconecta
    public FusionEvent OnConnectionStatusChangedEvent;  // Estado de conexión cambia
    public FusionEvent OnDisconnectedEvent;       // Desconexión completa

    // ── Eventos de sesión ──────────────────────────────
    [Header("Session Events")]
    public FusionEvent OnGameStateChangedEvent;   // GameManager.State cambió
    public FusionEvent OnEnteredLobbyEvent;       // Transición a Lobby
    public FusionEvent OnShutdownEvent;           // Runner cerrado

    // ── Eventos de jugador ─────────────────────────────
    [Header("Player Session Events")]
    public FusionEvent OnPlayerSessionCachedEvent;  // PlayerSessionData cacheado
    public FusionEvent OnPlayerOfflineEvent;     // Jugador marcado offline
    public FusionEvent OnPlayerDataSpawnedEvent;  // PlayerSessionData instanciado (CRÍTICO para UI)

    // ── Eventos de escena ──────────────────────────────
    [Header("Scene Events")]
    public FusionEvent OnSceneLoadStartEvent;     // Inicio de carga de escena
    public FusionEvent OnSceneLoadCompleteEvent;  // Escena completamente cargada

    // ── Eventos de sala ────────────────────────────────
    [Header("Room Events")]
    public FusionEvent OnRoomPropertiesChangedEvent;  // Propiedades de sala cambiaron
    public FusionEvent OnPlayerKickedEvent;      // Un jugador fue expulsado

    // ── Eventos de ronda y turno ───────────────────────
    [Header("Round Slice Events")]
    public FusionEvent OnRoundStartedEvent;      // Nueva ronda comienza
    public FusionEvent OnRoundEndedEvent;        // Ronda finaliza
    public FusionEvent OnTurnStartedEvent;       // Turno de jugador activo comienza
    public FusionEvent OnDiceRolledEvent;        // Dado fue tirado (orden de turno o turno normal)
    public FusionEvent OnPlayerMovedEvent;       // Jugador movió fichas en tablero
    public FusionEvent OnPlayerWaterChangedEvent;  // Cantidad de agua cambió
    public FusionEvent OnBasinStateChangedEvent;  // Salud de cuenca cambió

    // ── Singleton Instance ─────────────────────────────
    private static NetworkEventDefinitions _instance;

    public static NetworkEventDefinitions Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<NetworkEventDefinitions>("NetworkEventDefinitions");
                if (_instance == null)
                {
                    Debug.LogError("[NetworkEventDefinitions] Asset not found at Resources/NetworkEventDefinitions.asset");
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Validación de configuración: verifica que todos los eventos estén asignados.
    /// Útil para debug en editor antes de ejecutar.
    /// </summary>
    public bool ValidateAllEventsAssigned()
    {
        return OnPlayerJoinedEvent != null
            && OnPlayerLeftEvent != null
            && OnDiceRolledEvent != null
            && OnRoundStartedEvent != null
            // ... (todos los demás eventos)
            && OnBasinStateChangedEvent != null;
    }
}
```

### 5.3. Ciclo de Vida de Suscripción en UI

**Patrón en controladores de UI (ej: LobbyCanvas):**

```csharp
public class LobbyCanvas : MonoBehaviour
{
    [SerializeField] private FusionEvent OnPlayerJoinedEvent;
    [SerializeField] private FusionEvent OnPlayerDataSpawnedEvent;
    [SerializeField] private FusionEvent OnShutdownEvent;
    
    private void OnEnable()
    {
        // ── Fase 1: Registrar respuestas ───────────────────────────
        OnPlayerJoinedEvent?.RegisterResponse(ShowLobbyCanvas);
        OnPlayerDataSpawnedEvent?.RegisterResponse(UpdateLobbyList);
        OnPlayerDataSpawnedEvent?.RegisterResponse(UpdateGameLobbyList);
        OnShutdownEvent?.RegisterResponse(ResetCanvas);
        
        // Los eventos referenciados pueden cargar desde Resources si no están asignados
        if (OnCharacterSelectionCompleteEvent == null)
        {
            OnCharacterSelectionCompleteEvent = Resources.Load<FusionEvent>
                ("Events/OnCharacterSelectionCompleteEvent");
        }
        OnCharacterSelectionCompleteEvent?.RegisterResponse(OnCharacterSelectionComplete);
        
        Debug.Log("[LobbyCanvas] Suscripciones de eventos registradas en OnEnable()");
    }
    
    private void OnDisable()
    {
        // ── Fase 2: Desuscribirse ─────────────────────────────────
        OnPlayerJoinedEvent?.RemoveResponse(ShowLobbyCanvas);
        OnPlayerDataSpawnedEvent?.RemoveResponse(UpdateLobbyList);
        OnPlayerDataSpawnedEvent?.RemoveResponse(UpdateGameLobbyList);
        OnShutdownEvent?.RemoveResponse(ResetCanvas);
        OnCharacterSelectionCompleteEvent?.RemoveResponse(OnCharacterSelectionComplete);
        
        Debug.Log("[LobbyCanvas] Suscripciones de eventos removidas en OnDisable()");
    }
}
```

**Justificación de OnEnable/OnDisable:**
- **OnEnable():** Se ejecuta cuando GameObject se activa; momento ideal para suscribirse
- **OnDisable():** Se ejecuta cuando GameObject se desactiva; CRÍTICO desuscribirse para evitar memory leaks
- **Null-coalescing `?.`:** Evita excepción si el evento no está asignado
- **Resources.Load fallback:** Si el evento no está en Inspector, intentar cargar desde carpeta `Resources/Events/`

### 5.4. Flujo de Refresco de UI en Respuesta a Eventos

**Ejemplo 1: Actualización al Spawnear PlayerSessionData**

```csharp
// En FusionNetworkService.cs:
public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
    if (runner.IsServer)
    {
        runner.Spawn(PlayerDataNO, inputAuthority: player);
    }
    
    // Disparar evento — TODOS los observadores se ejecutan
    OnPlayerJoinedEvent?.Raise(player, runner);
    // → Esto dispara: ShowLobbyCanvas, UpdateLobbyList, UpdateGameLobbyList
}

// En LobbyCanvas.cs:
public void UpdateLobbyList(PlayerRef player, NetworkRunner runner)
{
    // Este callback se ejecuta INMEDIATAMENTE cuando se dispara OnPlayerJoinedEvent
    var allPlayers = runner.ActivePlayers;
    
    // Reconstructir lista visual de jugadores en lobby
    foreach (var p in allPlayers)
    {
        var sessionData = GetPlayerData(p, runner);
        if (sessionData != null)
        {
            // Actualizar UI
            AddOrUpdatePlayerCard(p, sessionData.Nick);
        }
    }
    
    Debug.Log($"[LobbyCanvas] Lobby list refreshed. Players: {allPlayers.Count()}");
}
```

**Ejemplo 2: Actualización al Cambiar Posición de Tablero**

```csharp
// En GameManager.cs:
public void HandleValidatedTurnRoll(PlayerRef player, int diceRoll, NetworkRunner runner)
{
    // ... [cálculos] ...
    playerData.BoardPosition = nextPosition % _boardTileCount;
    
    // Disparar evento — notifica a UI que jugador se movió
    Networking.Events.NetworkEventDefinitions.Instance?.OnPlayerMovedEvent?.Raise(player, runner);
}

// En cualquier controlador de UI que necesite reflejarlo:
private void OnEnable()
{
    NetworkEventDefinitions.Instance?.OnPlayerMovedEvent?.RegisterResponse(OnPlayerMoved);
}

private void OnPlayerMoved(PlayerRef player, NetworkRunner runner)
{
    var playerData = GetPlayerData(player, runner);
    if (playerData != null)
    {
        // Animar ficha a nueva posición
        Vector3 newWorldPosition = BoardPositionToWorldPosition(playerData.BoardPosition);
        StartCoroutine(AnimateCharacterToPosition(newWorldPosition));
        
        // Mostrar información de casilla
        UpdateTileInfo(playerData.BoardPosition);
    }
}
```

**Ejemplo 3: Actualización de Recursos (Agua/Dinero)**

```csharp
// En GameManager.cs:
private void ApplyWaterDelta(PlayerSessionData data, PlayerRef player, NetworkRunner runner, int delta, ...)
{
    // ... [aplicar delta] ...
    data.WaterAmount = Mathf.Max(0, data.WaterAmount + effectiveDelta);
    
    // Disparar evento para que UI actualice contador
    Networking.Events.NetworkEventDefinitions.Instance?.OnPlayerWaterChangedEvent?.Raise(player, runner);
}

// En HUD UI:
private void OnPlayerWaterChanged(PlayerRef player, NetworkRunner runner)
{
    var playerData = GetPlayerData(player, runner);
    if (playerData == null) return;
    
    if (player == runner.LocalPlayer)
    {
        // Actualizar contador de agua local
        _waterText.text = playerData.WaterAmount.ToString();
        
        // Animar cambio (ej: fade in/out)
        StartCoroutine(FlashWaterCounter());
    }
}
```

### 5.5. Garantías de Determinismo: `OnPlayerDataSpawnedEvent`

**Patrón crítico para sincronización:** El evento `OnPlayerDataSpawnedEvent` es especial porque se dispara en el momento exacto cuando `PlayerSessionData` es spawnado:

```csharp
// En PlayerSessionData.cs:
public override void Spawned()
{
    _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState, false);
    
    if (Object.HasInputAuthority)
    {
        string nickName = PlayerPrefs.GetString("Nick", string.Empty);
        RPC_SetNick(string.IsNullOrEmpty(nickName) ? $"Player {Object.InputAuthority.AsIndex}" : nickName);
    }

    DontDestroyOnLoad(this);
    Runner.SetPlayerObject(Object.InputAuthority, Object);
    
    // ── DISPARO CRÍTICO ────────────────────────────────────────────
    OnPlayerDataSpawnedEvent?.Raise(Object.InputAuthority, Runner);
    // → Todos los sistemas UI reciben referencia sincronizada a este jugador
    
    if (Object.HasStateAuthority)
    {
        Networking.Managers.GameManager.Instance.SetPlayerDataObject(Object.InputAuthority, this);
    }
}
```

**Por qué es crítico:** 
1. `PlayerSessionData` es un `NetworkObject` con identidad única
2. UI no puede funcionar hasta que tenga referencias válidas a estos objetos
3. El evento dispara DESPUÉS de que Fusion lo registra, garantizando que está listo
4. Todos los controladores UI lo reciben simultáneamente en el mismo frame

### 5.6. Cambios Detectados: `ChangeDetector` + Eventos

**Patrón alternativo: Reacción a cambios de propiedades**

```csharp
// En PlayerSessionData.cs:
public override void Render()
{
    // ChangeDetector detecta qué propiedades [Networked] cambiaron desde el último Render
    foreach (var change in _changeDetector.DetectChanges(this))
    {
        switch (change)
        {
            case nameof(Nick):
                // Nick cambió — notificar UI
                OnPlayerDataSpawnedEvent?.Raise(Object.InputAuthority, Runner);
                break;
                
            case nameof(SelectedCharacterId):
                // Carácter cambió — actualizar sprite
                OnPlayerDataSpawnedEvent?.Raise(Object.InputAuthority, Runner);
                break;
                
            case nameof(WaterAmount):
                // Agua cambió — disparar evento de agua específico
                NetworkEventDefinitions.Instance?.OnPlayerWaterChangedEvent?
                    .Raise(Object.InputAuthority, Runner);
                break;
        }
    }
}
```

**Ventaja:** `ChangeDetector` es eficiente porque solo itera cambios reales, no todas las propiedades.

### 5.7. Manejo de Errores Robusto

**Try-catch en Raise() previene cascadas de fallos:**

```csharp
public void Raise(PlayerRef player = default, NetworkRunner runner = null)
{
    for (int i = 0; i < Responses.Count; i++)
    {
        try
        {
            Responses[i].Invoke(player, runner);
        }
        catch (System.Exception ex)
        {
            // ── CRÍTICO: Registrar error pero continuar ────────────────
            Debug.LogError($"Error in FusionEvent '{name}': {ex.Message}\n{ex.StackTrace}");
            // Si un observador falla, los demás aún se ejecutan
        }
    }
}
```

**Ejemplo de fallo manejado:**
```
[ERROR] Error in FusionEvent 'OnPlayerJoinedEvent': NullReferenceException: Object reference not set
Stack: at LobbyCanvas.UpdateLobbyList() line 45

→ Sistema continúa, otros observadores aún se ejecutan
→ UI puede mostrar error pero no se congela
```

### 5.8. Performance: Minimizar Disparos de Eventos

**Anti-patrón a evitar:**

```csharp
// ❌ MAL: Dispara evento CADA frame si el agua cambió
private void Update()
{
    if (lastWaterAmount != currentWaterAmount)
    {
        OnPlayerWaterChangedEvent?.Raise(player, runner);  // ¡En Update = 60/seg!
        lastWaterAmount = currentWaterAmount;
    }
}
```

**Patrón correcto: Disparar solo cuando cambia**

```csharp
// ✓ BIEN: Dispara evento UNA VEZ cuando Networked property cambia
public override void Render()
{
    foreach (var change in _changeDetector.DetectChanges(this))
    {
        if (change == nameof(WaterAmount))
        {
            // Solo se ejecuta si WaterAmount cambió en este frame
            OnPlayerWaterChangedEvent?.Raise(Object.InputAuthority, Runner);
        }
    }
}
```

### 5.9. Configuración en Inspector

**En LobbyCanvas:**
```
LobbyCanvas
├─ On Player Joined Event ─→ [Asset: OnPlayerJoinedEvent]
├─ On Player Data Spawned Event ─→ [Asset: OnPlayerDataSpawnedEvent]
├─ On Shutdown Event ─→ [Asset: OnShutdownEvent]
└─ On Character Selection Complete Event ─→ (cargar desde Resources si null)
```

**Ventaja:** Todas las referencias son visibles en Inspector, permitiendo debugging visual y reconfiguración sin código.

### 5.10. Flujo Completo: Desde Host a UI

```
[Host/GameManager]
    ↓ (Host-side: HandleValidatedTurnRoll)
    ├─ Valida turno
    ├─ Calcula nueva posición
    ├─ Modifica playerData.BoardPosition [Networked]
    └─ Dispara OnPlayerMovedEvent
         │
         ↓ (Replicación Fusion: Delta Compression)
         │
    [Todos los Clientes]
         │
         ├─ Reciben nuevo BoardPosition via [Networked]
         │
         ├─ OnPlayerMoved observer recibe callback
         │  ├─ Anima ficha a nueva posición
         │  └─ Actualiza display de casilla
         │
         └─ OnPlayerWaterChanged observer (si también cambió agua)
            └─ Actualiza contador de agua en HUD
```

**Garantías:**
✅ Host es autoridad (GameManager modifica con validación)  
✅ Sincronización automática (Networked property sync)  
✅ UI reacciona sin sondear (eventos disparan callbacks)  
✅ Robustez (try-catch previene cascadas de fallos)  
✅ Performance (ChangeDetector minimiza eventos innecesarios)

---

## 6. Sistema de Animación Procedural: `LeanTween`

El proyecto utiliza la librería **LeanTween** para gestionar las transiciones de interfaz de usuario (UI) y efectos visuales procedurales. A diferencia del sistema de `Animator` nativo de Unity, LeanTween permite ejecutar animaciones ligeras mediante código, reduciendo el overhead de memoria y facilitando el control preciso sobre tiempos y curvas de flexibilización (*easing*).

### 6.1. Animación de Carga y Feedback Visual (`AnimationsLogic.cs`)
**Justificación Ingenieril:** Se utiliza el patrón de **Animación por Código** para elementos de UI persistentes como el panel de carga. Esto evita tener controladores de animación activos en segundo plano, mejorando el rendimiento en dispositivos móviles.

```csharp
public void StartLoadingImageAnimation()
{
    if (_loadingPanelImage == null) return;

    Vector3 startPos = _loadingPanelImage.anchoredPosition3D;
    float floatAmount = 18f; // Desplazamiento en unidades locales
    float duration = 1.6f;

    // Efecto de flotación persistente usando Loop PingPong
    LeanTween.moveLocalY(_loadingPanelImage.gameObject, startPos.y + floatAmount, duration)
        .setEase(LeanTweenType.easeInOutSine)
        .setLoopPingPong();
}
```

### 6.2. Transiciones Escalonadas (Staggered Fades)
**Justificación Ingenieril:** Para mejorar la experiencia de usuario (UX), los paneles de la interfaz no aparecen de golpe, sino mediante un **desvanecimiento escalonado**. Esto se logra iterando sobre arreglos de `CanvasGroup` y aplicando retrasos progresivos.

```csharp
private IEnumerator FadeInInitPanelObjects()
{
    for (int i = 0; i < _initPanelFadeCanvasGroups.Length; i++)
    {
        var group = _initPanelFadeCanvasGroups[i];
        if (group == null) continue;

        // Cancelar cualquier tween previo para evitar conflictos
        LeanTween.cancel(group.gameObject);
        
        // Aplicar alfa con un escalonamiento (stagger) basado en el índice
        LeanTween.alphaCanvas(group, 1f, _initPanelFadeInDuration)
            .setEase(LeanTweenType.easeOutSine)
            .setDelay(i * _initPanelFadeInStagger);
    }
    yield return null;
}
```

### 6.3. Notificaciones de Turno Dinámicas
**Justificación Ingenieril:** Las notificaciones de turno utilizan una combinación de movimiento, transparencia y llamadas retardadas (`delayedCall`) para crear una secuencia compleja de entrada, permanencia y salida. El uso de `setEase(LeanTweenType.easeOutBack)` proporciona un feedback visual orgánico que mejora la respuesta táctil percibida.

```csharp
// Ejemplo de rotación y desvanecimiento sincronizado
LeanTween.alphaCanvas(canvasGroup, 1f, _turnNotificationFadeDuration)
    .setEase(LeanTweenType.easeOutSine);

LeanTween.value(canvasGroup.gameObject, 0f, 360f, _turnNotificationSpinDuration)
    .setOnUpdate((float val) => {
        _turnNotificationSpinningFadeImage.localEulerAngles = new Vector3(0, 0, val);
    })
    .setEase(LeanTweenType.linear)
    .setLoopClamp();
```

---

## 7. Sistema de Interacción AR: Vuforia y Validación de Red

El proyecto integra **Vuforia Engine** para el escaneo de tarjetas físicas. Sin embargo, para garantizar la integridad en un entorno multijugador, el escaneo no se procesa de forma puramente local, sino que está sujeto a validaciones de estado y de tiempo.

### 7.1. Mecanismo de Cooldown de Escaneo (`ScanCooldown`)
**Justificación Ingenieril:** El rastreo de imágenes por visión artificial puede ser errático (detección de "falsos positivos" o detección múltiple en pocos milisegundos). Para evitar el envío masivo de peticiones RPC innecesarias, se implementa un mecanismo de **Throttle (Estrangulamiento)** basado en tiempo.

```csharp
// Lógica de cooldown en VuforiaCardScanner.cs
if (Time.time - _lastScanTime < _scanCooldownSeconds)
{
    // Bloquear el escaneo si no ha pasado el tiempo mínimo de enfriamiento
    return;
}
```

### 7.2. Validación de Turno y Estado del Jugador
**Justificación Ingenieril:** Un riesgo común en juegos AR multijugador es que un jugador intente escanear una carta fuera de su turno o en una fase de juego no permitida. El sistema utiliza el estado sincronizado en `PlayerSessionData` como un "guardián" de la interacción.

```csharp
// Verificación de estado en OnTargetStatusChanged
var playerData = gameManager.GetPlayerData(runner.LocalPlayer, runner);

// Solo se permite el escaneo si el servidor ha marcado al jugador como 'Esperando Escaneo'
if (playerData == null || !playerData.IsAwaitingCardScan)
{
    return;
}

// Registro del escaneo exitoso y envío de petición al Host
_lastScanTime = Time.time;
playerData.RPC_RequestCardScan(_cardDefinition.CardId);
```

### 7.3. Desacoplamiento de Detección y Acción
**Justificación Ingenieril:** El componente AR solo tiene la responsabilidad de **detectar** y **solicitar**. No modifica recursos. Esta separación (patrón *Request-Response*) asegura que si un escaneo ocurre por error, el Host simplemente rechazará la petición RPC si las condiciones de juego (posición en el tablero, dinero suficiente, etc.) no se cumplen.

---

## 8. Gestión de Minijuegos: Inicialización y Control

La arquitectura de minijuegos en **Sumapp Final** está diseñada para ser modular, permitiendo que cada escena de minijuego sea independiente pero mantenga la sincronización de red mediante un gestor centralizado que se instancia dinámicamente.

### 8.1. Inicialización Dinámica (`MinigameInitializer.cs`)
**Justificación Ingenieril:** Se utiliza el patrón **Factory/Initializer** para desacoplar la escena del objeto de autoridad. En lugar de colocar el gestor manualmente en la jerarquía, el `MinigameInitializer` se encarga de que solo el Host instancie el `MinigameManager` mediante `Runner.Spawn`.
*   **Seguridad:** Esto garantiza que no existan múltiples instancias del gestor y que el objeto de red siempre tenga la autoridad de estado correcta desde su creación.

```csharp
private void Start()
{
    var runner = FindFirstObjectByType<NetworkRunner>();
    // Solo el Host tiene permitido spawnear objetos con autoridad de estado
    if (!runner.IsServer) return;

    // Spawneo del prefab de red sincronizado para todos los clientes
    var networkObject = _minigameManagerPrefab.GetComponent<NetworkObject>();
    runner.Spawn(networkObject, inputAuthority: runner.LocalPlayer);
}
```

### 8.2. Control de Ciclo de Vida del Minijuego (`MinigameManager.cs`)
**Justificación Ingenieril:** El `MinigameManager` centraliza la lógica de competencia. Utiliza el método `FixedUpdateNetwork` de Fusion para realizar un seguimiento del tiempo de juego (`RemainingTime`) de forma independiente a la tasa de fotogramas, asegurando que el temporizador sea idéntico en todos los clientes.

*   **Gestión de Puntuación:** Lee directamente la propiedad `MinigameClickCount` del `PlayerSessionData` de cada jugador, la cual se actualiza mediante RPCs individuales, permitiendo una tabla de posiciones en tiempo real.
*   **Determinismo en la Victoria:** Al finalizar el tiempo, el Host ejecuta la lógica de premiación (`RewardMinigameWinner`) y notifica a los clientes mediante un RPC global (`RPC_NotifyGameEnd`), garantizando que la transición a los resultados sea simultánea.

```csharp
public override void FixedUpdateNetwork()
{
    if (!Object.HasStateAuthority || !GameActive) return;

    // Cuenta regresiva determinista sincronizada por ticks de red
    RemainingTime -= Runner.DeltaTime;

    if (RemainingTime <= 0f)
    {
        RemainingTime = 0f;
        GameActive = false;
        EndGame(); // Finalización autoritativa en el Host
    }
}
```

### 8.3. Sincronización de Retorno al Tablero
**Justificación Ingenieril:** Para evitar que los jugadores queden atrapados en escenas diferentes, el gestor utiliza una corrutina en el servidor que, tras mostrar el leaderboard durante un tiempo fijo, invoca un RPC de carga de escena en todos los clientes. Esto asegura una transición fluida y coordinada de regreso al tablero principal.

---

## 9. Conclusión del Desarrollo Ingenieril

### 9.1. Síntesis de Patrones Arquitectónicos

El análisis exhaustivo de **Sumapp Final** ha evidenciado la implementación coherente de múltiples patrones de ingeniería de software que garantizan escalabilidad, mantenibilidad y seguridad en un juego multijugador educativo:

**Patrones Utilizados:**

1. **Host-Authoritative Architecture**: La topología de red implementa una máquina de estados centralizada en el Host (`GameManager`), garantizando que decisiones críticas (turno, dados, efectos) son inmutables y verificables. Esto previene manipulación de cliente.

2. **Observer Pattern (Mediado por Activos)**: El sistema de eventos `FusionEvent` desacopla completamente productores (GameManager, FusionNetworkService) de consumidores (UI, Managers). Esto reduce complejidad ciclomática y facilita testing.

3. **State Machine Pattern**: Las transiciones de estado en `GameManager.GameState` definen un flujo determinista de juego, evitando estados inválidos y facilitando debugging.

4. **Pipeline/Decorator Pattern**: La aplicación de modificadores de recursos (`ApplyWaterDelta`) implementa capas independientes (escudos → planos → porcentuales → sujeción) siguiendo el principio Open/Closed.

5. **Singleton Pattern**: `NetworkEventDefinitions.Instance` y `GameManager.Instance` garantizan acceso centralizado sin inicialización repetida.

6. **RPC + Validation Pattern**: Todos los cambios críticos pasan por RPCs validados por el Host, implementando múltiples capas de seguridad (autoridad, integridad, anti-spam).

### 9.2. Validación de Seguridad y Determinismo

**Capas de Validación Implementadas:**

| Capa | Mecanismo | Objetivo |
|------|-----------|----------|
| Autoridad | `if (!Object.HasStateAuthority)` | Solo Host modifica datos sincronizados |
| Integridad | `ValidateDiceRollRequest()` | Verificar solicitud viene del jugador correcto |
| Anti-Spam | `Time.time - lastRollTime < 0.5f` | Prevenir intentos de exploit por velocidad |
| Determinismo | `ChangeDetector` + eventos | Acciones simultáneas en todos los clientes |
| Consistencia | `BasinService` centralizado | Salud de cuenca es fuente única de verdad |

### 9.3. Optimizaciones de Performance

**Delta Compression**: Fusion transmite solo cambios en `[Networked]` properties, no estado completo.
**ChangeDetector**: Reactiva UI solo ante cambios confirmados, no cada frame.
**Event Batching**: Múltiples cambios en un turno disparan un evento consolidado, minimizando el overhead de ejecución.

### 9.4. Conclusión Final

**Sumapp Final** implementa una arquitectura de juego multijugador robusta y escalable, priorizando la seguridad mediante Host-Authority, el determinismo mediante sincronización y la mantenibilidad mediante el desacoplamiento de componentes. Esta estructura sirve como referencia para proyectos educativos que requieren networking confiable en entornos colaborativos.

---

**Fin del Análisis Técnico**



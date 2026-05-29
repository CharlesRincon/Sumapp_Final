# Sumapp Final - Arquitectura del Proyecto

## 1. Modelo de Networking (Photon Fusion)
El proyecto utiliza un modelo de **State-Authority (Host-Client)** proporcionado por Photon Fusion.
- **Host (Servidor):** Tiene la autoridad sobre el estado del juego y las variables `[Networked]`.
- **Clientes:** Envían peticiones mediante `RPC` (Remote Procedure Calls) para interactuar con el mundo.
- **Sincronización:** El estado se mantiene a través de `PlayerSessionData` y managers centralizados.

## 2. Gestión del Estado (GameManager)
El `GameManager` es el núcleo lógico del juego, gestionando una máquina de estados compleja:
- `Lobby`: Inicialización de red y emparejamiento.
- `CharacterSelection`: Selección sincronizada de personajes.
- `PlayerTurn`: Gestión de turnos y movimiento en el tablero.
- `TileResolve`: Resolución de efectos de casillas (Hídricas, Catastróficas, Trivia).
- `Minigame`: Transición a escenas de minijuegos competitivos.

## 3. Sistema de Realidad Aumentada (Vuforia)
Integración de Vuforia Engine para el escaneo de elementos físicos:
- **VuforiaCardScanner:** Detecta cartas físicas para activar eventos o decisiones.
- **VuforiaProjectScanner:** Gestiona la construcción de proyectos en el tablero AR.

## 4. Estructura de Datos (ScriptableObjects)
El juego sigue un diseño basado en datos para facilitar el equilibrio y la escalabilidad:
- `CardDefinition`: Define efectos de cartas (agua, dinero, salud de la cuenca).
- `ProjectDefinition`: Define costes y beneficios de proyectos por zona geográfica.
- `TriviaDatabase`: Almacén de preguntas y respuestas para el sistema de trivia.
- `FusionEvent`: Sistema desacoplado para notificar cambios de estado a la UI.

## 5. Capa de Interfaz (UI System)
Basado en **uGUI** con un enfoque basado en eventos:
- **Controllers:** Scripts como `WeatherUIController` o `TriviaUIController` escuchan eventos de red y actualizan la vista local.
- **Responsive Layouts:** Uso de `Layout Groups` y `Content Size Fitters` para adaptarse a diferentes resoluciones de dispositivos Android.

## 6. Flujo de Escenas
1. `LobbyScene`: El tablero principal y toda la lógica de gestión de recursos.
2. `Minigame`: Escenas independientes para retos de clics rápidos.
3. El estado de los jugadores persiste entre escenas gracias a que el `GameManager` y los objetos de sesión no se destruyen (`DontDestroyOnLoad`).

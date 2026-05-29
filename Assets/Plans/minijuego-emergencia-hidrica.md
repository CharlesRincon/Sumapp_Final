# Proyecto: Emergencia Hídrica Regional (Minijuego)

## Project Overview
- **Game Title:** Sumapp Final
- **High-Level Concept:** Un minijuego multijugador cooperativo-competitivo donde los jugadores deben mantener el nivel de agua de las 6 regiones naturales de Colombia mientras compiten por puntos.
- **Players:** Multijugador (Host-Client) vía Photon Fusion.
- **Target Platform:** Android.
- **Render Pipeline:** URP.

## Game Mechanics
### Core Gameplay Loop
1.  **Monitoreo:** Se presentan 6 barras de agua, una por cada región (Andina, Caribe, Pacífica, Orinoquía, Amazonía, Insular).
2.  **Sequía:** Los niveles de agua bajan constantemente de forma sincronizada para todos los jugadores.
3.  **Acción:** Los jugadores hacen clic en las regiones para "bombear agua" (recargarlas).
4.  **Puntuación:** 
    *   Recargar una región con agua crítica (<30%): **10 puntos**.
    *   Recargar una región normal: **2 puntos**.
5.  **Fin del Juego:** Al terminar el tiempo, el jugador con más puntos acumulados gana el bono de agua para la partida principal.

### Controls and Input Methods
- **Toque/Clic:** Interacción directa sobre los botones de cada región en la pantalla táctil.

## UI
- **Contenedor Principal:** Un `GridLayoutGroup` con 2 columnas y 3 filas.
- **Botón de Región:** 
    *   Imagen de fondo (Sprite regional o genérico).
    *   Etiqueta de texto (Nombre de la región).
    *   Barra de progreso (Slider) que indica el nivel de agua actual.
    *   Indicador visual de alerta (Flash rojo cuando el nivel es crítico).

## Key Asset & Context
- **Scripts:**
    - `RegionDroughtManager.cs`: Maneja la lógica de niveles de agua y sincronización de red.
    - `RegionRefillButton.cs`: Script para los botones de las regiones.
- **Assets:**
    - Sprites de gotas y fondos existentes en `Assets/Project/Resources/Art/RainMinigame/`.
    - Prefab `PlayerSessionData` para el registro de puntos vía RPC.

## Implementation Steps
1.  **Crear el Script `RegionDroughtManager.cs`**:
    - Heredar de `MinigameManager` para reutilizar el cronómetro y el leaderboard.
    - Implementar `[Networked]` array para los 6 niveles de agua.
    - Implementar lógica de decrecimiento en `FixedUpdateNetwork`.
2.  **Crear el Script `RegionRefillButton.cs`**:
    - Detectar clic y llamar a un RPC en el Manager para sumar puntos y rellenar agua.
    - Actualizar visualmente la barra de progreso local basada en los datos en red.
3.  **Configurar la Escena `Minigame`**:
    - Reemplazar el `ClickButton` por el nuevo sistema de rejilla regional.
    - Configurar el `MinigameInitializer` para usar el nuevo prefab del Manager.
4.  **Ajustar UI de Feedback**:
    - Asegurar que el `Timer` y el `PlayerListContainer` sigan visibles y funcionales.

## Verification & Testing
- **Prueba de Sincronización:** Verificar que los niveles de agua bajen igual en Host y Cliente.
- **Prueba de Puntuación:** Confirmar que los puntos se sumen correctamente al `PlayerSessionData` del jugador que hace clic.
- **Prueba de Fin de Juego:** Verificar que al terminar el tiempo se muestre el leaderboard con los resultados finales.

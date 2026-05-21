# Project Overview
- Game Title: Sumapp Final
- High-Level Concept: AR multiplayer board game about resource management in Colombia.
- Players: Multiplayer (Photon Fusion)
- Inspiration / Reference Games: Board games with AR elements.
- Tone / Art Direction: Colombian zones (Andean, Caribbean, etc.), educational/management.
- Target Platform: Android
- Screen Orientation / Resolution: Landscape
- Render Pipeline: URP (PC_RPAsset)

# Game Mechanics
## Core Gameplay Loop
The player rolls dice, moves on a board, and triggers events (Cards or Projects) by scanning physical markers.

# UI
A debug scene will be created to visualize how Event Cards and Project Decision panels look with different content. This scene will allow simulating card scans and seeing the resulting game effects without network requirements.

# Key Asset & Context
- `ProjectFlowUIController`: The existing controller that handles these panels.
- `CardDatabase`: Contains all event cards.
- `ProjectDatabase`: Contains all project definitions.
- `CardDebugUI.cs`: (New) A debug script to drive the UI and simulate effects.

# Implementation Steps
1. **Create Debug Scene**:
    - Create `Assets/Project/Scenes/Debug/CardDebugScene.unity`.
    - Setup a basic Canvas with `CanvasScaler` (Scale with Screen Size, 1920x1080).
2. **Setup UI Hierarchy**:
    - Instantiate the `LobbyCanvas` or copy its `EventCardPanel` and `ProjectDesicionPanel` into the new scene.
    - Ensure all `TextMeshProUGUI` fields are correctly referenced.
3. **Implement Debug Script (`CardDebugUI.cs`)**:
    - **Asset Browsing**: Buttons to cycle through all Cards in `CardDatabase` and Projects in `ProjectDatabase`.
    - **Simulated Scanning**: A "Simulate Scan" button that:
        - Ignores all game cooldowns.
        - Triggers the visual display of the panel immediately.
        - Logic to show/hide panels on demand.
    - **Effect Simulation**:
        - Maintain a "Simulated Player State" (Water, Money, Basin).
        - When a card/project is "Accepted" or "Scanned", print a detailed `Debug.Log` of all deltas that would be applied (e.g., "Applying Card 5: +10 Money, -5 Water, Global Basin -2").
        - Log teleportation targets and weather changes.
4. **Verification**:
    - Open the scene in the editor.
    - Verify that every card's description fits the dynamic layout.
    - Verify that "Simulate Scan" correctly triggers the panels and logs the expected effects.

# Verification & Testing
- Manual check: Verify that the `BGTable` expands/contracts correctly for the longest and shortest cards.
- Logic check: Verify that the `Debug.Log` output matches the deltas defined in the ScriptableObject assets.
- UX check: Verify that the show/hide behavior is responsive.

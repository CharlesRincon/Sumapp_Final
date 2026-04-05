# Minigame Setup Guide

This guide walks you through setting up the click-competition minigame with real-time scoring and leaderboard.

## Overview

**Game Flow:**
1. Player clicks a shared button as many times as possible in 15 seconds
2. All players see real-time click counts for each player
3. At game end, leaderboard shows final rankings
4. Players can return to lobby

**Network Synchronization:**
- MinigameManager runs on host only (controls timer and game state)
- Click RPC calls sent to host for registration
- Host broadcasts updated click counts to all clients in real-time
- All clients see synchronized timer and scoreboard

## Part 1: Create Minigame Scene

### Step 1: Create New Scene
1. In **Project** window, right-click → **Create** → **Scene**
2. Name it `Minigame`
3. Save it in `Assets/Project/Scenes/` folder
4. Open the Minigame scene (double-click)

### Step 2: Set Scene Name in Code
1. Open `Assets/Project/Scripts/Networking/UI/MinigameUI.cs`
2. Find the `ReturnToLobby()` method (around line 200)
3. Verify the scene name matches:
   ```csharp
   SceneManager.LoadScene("Minigame");  // Must match your scene name exactly
   ```
4. If your scene is named differently, update this line

### Step 3: Add NetworkRunner to Scene
1. In the Minigame scene, create empty GameObject: `GameObject` → `Create Empty`
2. Name it `NetworkRunner`
3. Add component: `Network Runner` (from Fusion)
4. Configure:
   - **IsPlayer**: Unchecked (this is just a runner, not a player spawner)
   - **Simulation Config**: Leave default (or assign if you have custom config)

---

## Part 2: Create Canvas UI Structure

### Step 4: Create Main Canvas
1. Right-click in hierarchy → **UI** → **Canvas** (TextMesh Pro)
2. Name it `MinigameCanvas`
3. Add component `MinigameUI` script to this Canvas
4. Canvas settings:
   - **Canvas Scaler**: UI Scale Mode = `Scale With Screen Size`
   - **Render Mode**: `Screen Space - Overlay`

### Step 5: Create Timer Text
1. Right-click Canvas → **UI** → **Text - TextMesh Pro**
2. Name it `TimerText`
3. Position: Top center of screen
4. Size: `400 x 100`
5. Text settings:
   - **Text**: "15.0s" (placeholder)
   - **Font Size**: `60`
   - **Alignment**: Center, Middle
   - **Color**: White
6. Assign to MinigameUI script:
   - Select Canvas (MinigameCanvas)
   - In Inspector, find `MinigameUI` component
   - Drag `TimerText` into the **Timer Text** field

### Step 6: Create Click Button
1. Right-click Canvas → **UI** → **Button - TextMesh Pro**
2. Name it `ClickButton`
3. Position: Center of screen
4. Size: `300 x 200`
5. Button text settings:
   - **Text**: "CLICK HERE!"
   - **Font Size**: `50`
   - **Text Color**: Black
6. Button color: Any color you prefer (e.g., green)
7. Add component **MinigamePlayerButton** to this button:
   - Select `ClickButton` in hierarchy
   - In Inspector, click **Add Component**
   - Search for and add `MinigamePlayerButton`
   - This component will automatically find MinigameManager and NetworkRunner at runtime

### Step 7: Create Player List Container
1. Right-click Canvas → Create empty GameObject
2. Name it `PlayerListContainer`
3. Position: Below timer, left side
4. Size: `400 x 300`
5. Add component: **Vertical Layout Group**
   - **Spacing**: `10`
   - **Child Force Expand**: Height = `False`
   - **Child Preferred Size**: Use Preferred Size = `True`
6. Assign to MinigameUI script:
   - In MinigameUI component, drag `PlayerListContainer` into the **Player List Container** field

### Step 8: Create Player Card Prefab
1. Right-click Project folder → Create → Folder named `Prefabs` (inside Assets/Project/)
2. Right-click in **PlayerListContainer** → **UI** → **Text - TextMesh Pro**
3. Name it `PlayerCardPrefab`
4. Size: `400 x 60`
5. Text settings:
   - **Text**: "Player 1: 0"
   - **Font Size**: `30`
   - **Color**: White
   - **Alignment**: Left, Middle
6. Layout settings (optional):
   - **Preferred Height**: `60`
7. Drag this text element from hierarchy into `Prefabs` folder to create prefab
8. Delete the instance from hierarchy (keep only the prefab)
9. Assign to MinigameUI script:
   - In MinigameUI component, drag the **PlayerCardPrefab** file into the **Player Card Prefab** field

### Step 9: Create Leaderboard Panel
1. Right-click Canvas → **UI** → **Image**
2. Name it `LeaderboardPanel`
3. Position: Center of screen
4. Size: `600 x 500`
5. Image color: Semi-transparent dark (e.g., RGBA: 0, 0, 0, 200)
6. Add component: **Vertical Layout Group**
   - **Spacing**: `15`
7. Create children in this order (stay inside LeaderboardPanel):
   - **Text - TextMesh Pro**: Name `LeaderboardTitle`
     - Text: "FINAL LEADERBOARD"
     - Font Size: `40`
     - Color: Gold/Yellow
   - **Empty GameObject**: Name `LeaderboardContainer`
     - Add component: **Vertical Layout Group**
       - **Spacing**: `10`
       - **Child Force Expand**: Height = `False`
     - This container holds the dynamically created entry cards
   - **Button - TextMesh Pro**: Name `ReturnButton` (optional placement)
     - Text: "Return"
     - Font Size: `30`
     - **Important**: Can be placed here as last child OR as separate sibling of LeaderboardPanel at Canvas level
8. Set LeaderboardPanel inactive initially:
   - Uncheck the checkbox next to `LeaderboardPanel` in hierarchy
9. Assign to MinigameUI script:
   - In MinigameUI component, drag `LeaderboardPanel` into the **Leaderboard Panel** field
   - Drag `LeaderboardContainer` into the **Leaderboard Container** field (NOT "Leaderboard Content")

### Step 10: Create Return Button
1. Right-click **Canvas** (NOT LeaderboardPanel) → **UI** → **Button - TextMesh Pro**
   - **Important**: ReturnButton must be a direct child of Canvas, not inside LeaderboardPanel
2. Name it `ReturnButton`
3. Position: Bottom right of screen
4. Size: `200 x 80`
5. Button text:
   - **Text**: "Return"
   - **Font Size**: `30`
6. Assign to MinigameUI script:
   - In MinigameUI component, drag `ReturnButton` into the **Return Button** field
7. Wire the button in Inspector:
   - Select the `ReturnButton` in hierarchy
   - In Inspector, find **Button** component → **OnClick()**
   - Click **+** to add callback
   - Drag `MinigameCanvas` into the object field
   - From dropdown, select `MinigameUI` → `ReturnToLobby()`

---

## Part 3: Create MinigameManager Prefab

### Step 11: Create MinigameManager Prefab
1. In Project window, right-click in `Assets/Project/Prefabs/` → Create → Folder `Managers`
2. Create empty GameObject in scene: `GameObject` → `Create Empty`
3. Name it `MinigameManager`
4. Add component: `MinigameManager` script
5. Configure in Inspector:
   - **Game Duration Seconds**: `15` (seconds, configurable)
   - **On Game End Event**: Drag the `OnGameEndEvent` from Resources into this field
     - If not found, create it: Right-click `Assets/Project/Resources/Events/` → Create → FusionEvent
     - Name it `OnGameEndEvent`
6. Drag this GameObject from hierarchy into `Assets/Project/Prefabs/Managers/` to create prefab
7. Delete the instance from hierarchy (MinigameInitializer will spawn it)
8. **IMPORTANT**: Delete the local GameObject from the scene after creating the prefab

### Step 12: Register MinigameManager in Fusion
1. Open `Assets/Project/Resources/` folder
   - If `Resources` folder doesn't exist, create it in `Assets/Project/`
2. Inside Resources, create folder: `NetworkPrefabs`
3. Move the `MinigameManager` prefab into `Assets/Project/Resources/NetworkPrefabs/`
4. Rename it to exactly: `MinigameManager` (Fusion will use this name)
5. In `MinigameManager` prefab Inspector:
   - Add component: **Network Object**
   - Set **Prefab Source**: `Kinematic` (since it's spawned at runtime)
   - **Network Properties**: Leave default

### Step 13: Add MinigameInitializer to Scene
1. Create empty GameObject in Minigame scene: `GameObject` → `Create Empty`
2. Name it `MinigameInitializer`
3. Add component: `MinigameInitializer` script
4. Assign in Inspector:
   - **Minigame Manager Prefab**: Drag the `MinigameManager` prefab from `Resources/NetworkPrefabs/` into this field
5. This script will automatically find the NetworkRunner and spawn MinigameManager on host when scene loads

---

## Part 4: Final Wiring & Testing

### Step 14: Verify All References
1. Select `MinigameCanvas` and check `MinigameUI` component:
   - ✅ Timer Text: Assigned
   - ✅ Player List Container: Assigned
   - ✅ Player Card Prefab: Assigned
   - ✅ Leaderboard Panel: Assigned
   - ✅ Leaderboard Content: Assigned
   - ✅ Return Button: Assigned and wired to ReturnToLobby()
   - ✅ Runner: Assigned
   - ✅ Minigame Manager: (leave empty, assigned at runtime)
   - ✅ On Game End Event: Should auto-load from Resources

2. Verify Click Button:
   - ✅ ClickButton has `MinigamePlayerButton` component attached
   - ✅ MinigamePlayerButton will auto-find MinigameManager and NetworkRunner at runtime

3. Verify NetworkRunner:
   - ✅ NetworkRunner component in scene
   - ✅ IsPlayer: Unchecked

4. Verify MinigameInitializer:
   - ✅ MinigameInitializer script on GameObject
   - ✅ Minigame Manager Prefab: Assigned (from Resources/NetworkPrefabs/)

### Step 15: Scene Build Settings
1. File → **Build Settings**
2. Add scenes to build:
   - Find your Launcher scene (Level 0)
   - Find your Lobby scene (Level 1)
   - Find Minigame scene → Click **Add Open Scenes** or drag
   - Scene order should be:
     - 0: Launcher/MainMenu
     - 1: LobbyScene
     - 2: Minigame

### Step 16: Testing Single Player
1. Create test setup:
   - Open Launcher/MainMenu scene
   - Start game in Editor
   - Go through character selection
   - Click "Load Game" button
2. Verify:
   - ✅ Minigame scene loads
   - ✅ Timer displays "15.0s" and counts down
   - ✅ Click button is clickable
   - ✅ Click counter increments after each click
   - ✅ At 0 seconds, leaderboard appears
   - ✅ Return button loads lobby scene

### Step 17: Testing Multiplayer (Local or Network)
1. Run 6 instances of the game (or use Playmode Tests)
2. All players go through character selection together
3. All click "Load Game" button simultaneously
4. Verify:
   - ✅ All see minigame scene
   - ✅ Timer synchronized (all count down together)
   - ✅ Player list shows all 6 players with click counts
   - ✅ When player 1 clicks, all see their count increase
   - ✅ Leaderboard appears for all at same time
   - ✅ Rankings show correct final order
   - ✅ Return button works for all players

---

## Additional Notes

### Customization

**Change Timer Duration:**
- Select `MinigameManager` prefab
- In Inspector: Modify **Game Duration** field (in seconds)

**Change Button Text/Color:**
- Select `ClickButton` in scene
- Edit text in Text Mesh Pro component
- Edit button colors in Button component

**Add Sound Effects (Optional):**
- Add `AudioSource` to `ClickButton`
- In `MinigamePlayerButton.OnClickButton()`, add:
  ```csharp
  GetComponent<AudioSource>().PlayOneShot(clickSound);
  ```

### Troubleshooting

**"MinigameManager not spawned" error:**
- Check NetworkRunner is in scene
- Check MinigameInitializer is attached and Runner is assigned
- Check MinigameManager prefab is in `Resources/NetworkPrefabs/`

**Click counts not syncing:**
- Check `MinigamePlayerButton` is on the `ClickButton`
- Check NetworkRunner assignment in MinigameUI
- Verify MinigameManager RPC calls are working (check Debug logs)

**Leaderboard doesn't appear:**
- Check `LeaderboardPanel` is assigned in MinigameUI
- Check `LeaderboardContent` is a child of LeaderboardPanel
- Game might be ending too quickly; check timer duration

**Scene doesn't load:**
- Check scene name in `MinigameUI.ReturnToLobby()` matches exactly
- Check Build Settings has all scenes added
- Check path in `SceneManager.LoadScene()` is correct

---

## Scene Checklist

- [ ] Minigame scene created and named "Minigame"
- [ ] NetworkRunner in scene (IsPlayer unchecked)
- [ ] Canvas with MinigameUI component
- [ ] Timer Text assigned and positioned
- [ ] Click Button assigned with MinigamePlayerButton component
- [ ] Player List Container assigned with VerticalLayoutGroup
- [ ] Player Card Prefab created and assigned
- [ ] Leaderboard Panel created and assigned
- [ ] Leaderboard Content assigned
- [ ] Return Button assigned and wired to ReturnToLobby()
- [ ] MinigameManager prefab in Resources/NetworkPrefabs/
- [ ] MinigameInitializer in scene with Runner assigned
- [ ] All scenes added to Build Settings in correct order
- [ ] Single player test: timer counts, clicks register, leaderboard shows
- [ ] Multiplayer test: all players see synchronized game state


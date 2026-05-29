# Bug Fix: Event Card Scan Panel appearing prematurely

## Problem Analysis
The user reports that when scanning an event card for the second time, the UI panel showing the effect from the *first* scan appears immediately, even before the second card is scanned. This prevents scanning a new card and blocks progress.

### Root Causes
1. **Stale Data in PlayerSessionData:** The `PendingCardTitle` and related properties are set during a successful scan but are not consistently cleared when the turn ends or when a new scan phase (Project or Card) begins.
2. **UI Logic in ProjectFlowUIController:** The UI logic shows the card info panel whenever `hasCardInfo` is true (non-empty title) and Vuforia is open. If stale data is present, the panel appears as soon as the player opens the AR scanner.
3. **Scan Blockage:** While the code doesn't explicitly block scanning when the panel is visible, the presence of the stale info panel likely confuses the player or visually obstructs the experience, leading to the "cannot scan" perception.

## Proposed Changes

### 1. GameManager.cs
- Update `AdvanceTurn` to clear the card display state for the player whose turn just ended. This ensures that the next time they (or anyone) land on a tile, the display state is clean.
- Update `BeginProjectTileFlow` to clear the card display state. This prevents old card info from showing up when scanning for a project.
- (Already exists) `BeginDrawCardTileFlow` already clears the state, but we'll ensure it's robust.

### 2. ProjectFlowUIController.cs
- Update the condition for showing the card info panel (`showCardInfo`). It should only be true if `isAwaitingCardScan` is **false**. If we are currently awaiting a scan, we shouldn't be showing any "result" yet, even if `PendingCardTitle` has a value.
- This effectively "masks" any stale data or sync latency until the server actually processes the new scan and sets `isAwaitingCardScan` to false.

## Implementation Steps

### Step 1: Update GameManager.cs
- In `AdvanceTurn`, call `ClearPendingCardDisplay(data)`.
- In `BeginProjectTileFlow`, call `ClearPendingCardDisplay(playerData)`.

### Step 2: Update ProjectFlowUIController.cs
- Modify the `showCardInfo` assignment to include `!isAwaitingCardScan`.

## Verification & Testing
1. **Manual Test (Multiplayer):**
   - Player A lands on a Card tile and scans a card. Verify panel appears and hides.
   - Player A finishes turn.
   - Player A lands on another Card tile in a later round.
   - Verify that opening the AR scanner DOES NOT show the previous card's info immediately.
   - Scan a new card and verify the new info appears correctly.
2. **Edge Case:**
   - Land on a Project tile after having scanned a card previously. Verify no card info appears during project scan.

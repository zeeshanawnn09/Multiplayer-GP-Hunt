# Crosshair Setup Guide

## Overview
I've created a complete crosshair system that will display a crosshair UI element in the center of the screen for the local player, and automatically hide the cursor when the game starts.

## What Was Created

### 1. **CrosshairUI.cs** - New UI Manager Script
Located at: `Assets/Scripts/Core/UI/CrosshairUI.cs`

This script manages the crosshair display with features like:
- Setting crosshair color
- Adjusting crosshair scale
- Show/hide crosshair visibility
- Inspector-configurable properties

### 2. **PlayerControls.cs** - Updated Script
The script now:
- Hides the cursor and locks it to the center when the game starts
- Calls `SetupCrosshair()` method for the local player
- Only affects the local player (remote players don't show a crosshair on your screen)

## How to Set Up in the Editor

### Step 1: Open the PlayerCapsule Prefab
1. Navigate to `Assets/Resources/PlayerCapsule.prefab`
2. Double-click to open it in the prefab editor (or Ctrl+Click and select "Open Prefab")

### Step 2: Create a Canvas for the Crosshair
1. Right-click in the Hierarchy
2. Select **UI > Canvas** (or choose any UI Canvas option)
3. Rename it to "CrosshairCanvas"
4. Set the canvas scale to match your game (suggests: Scale = 1)
5. The Canvas should have RenderMode set to **"Overlay"** (default)

### Step 3: Add an Image to the Canvas
1. Right-click on "CrosshairCanvas" in the hierarchy
2. Select **UI > Image**
3. Rename it to "Crosshair"

### Step 4: Configure the Crosshair Image
1. Select the "Crosshair" Image object
2. In the Inspector, find the **Image** component
3. Set the image to your crosshair texture in the **Source Image** field
4. Adjust the image size as needed (typically 50x50 to 100x100 pixels)
5. You can change the color in the **Color** field if desired

### Step 5: Add the CrosshairUI Script
1. Select the "Crosshair" Image object (not the Canvas)
2. In the Inspector, click **Add Component**
3. Search for and add **CrosshairUI**
4. In the CrosshairUI component:
   - Set the **Crosshair Image** field to the "Crosshair" Image component
   - Set **Crosshair Color** to your desired color (default: white)
   - Set **Crosshair Scale** to adjust size (default: 1)

### Step 6: Save the Prefab
1. Press Ctrl+S or go to **File > Save**
2. The prefab is now updated

## Result
- When the game starts, the cursor will automatically become invisible and locked to the center
- Each local player will see a crosshair in the center of their screen
- Other players' crosshairs won't be visible on your screen (only your own)
- The crosshair can be toggled on/off via the `CrosshairUI` component methods if needed

## Optional: Customization
You can adjust the crosshair appearance by:
- Changing the `Crosshair Color` in the inspector
- Changing the `Crosshair Scale` for larger/smaller crosshairs
- Using different images for different player roles (Priest/Ghost)

## Troubleshooting
- **Crosshair not appearing?** Make sure the Canvas is set to "Overlay" mode and the Image has a texture assigned
- **Cursor still visible?** Verify that `photonView.IsMine` returns true for your local player
- **Script errors?** Ensure the CrosshairUI component is added to the Image, not the Canvas

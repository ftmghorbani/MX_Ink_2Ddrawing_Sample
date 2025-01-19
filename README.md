
# MX Ink 2D Drawing Sample

## Overview

This Unity project allows users to create a 2D drawing canvas in a mixed reality (MR) environment using spatial anchors and interact with it using a stylus on the Oculus Quest. Users can:

- Place spatial anchors to define the corners of the drawing canvas.  
- Automatically generate a canvas from the anchors.  
- Draw on the canvas with varying line widths.  
- Save and reload anchors and the canvas across sessions.  
- Undo or clear drawn lines with stylus interactions.  

## Features

### 1. Spatial Anchor Placement  
- **Stylus Tip Press:** Press the stylus tip (pressure > 0.98) near the desired position to create up to 3 anchors.  
- **Canvas Generation:** After placing 3 anchors, the next stylus press creates a canvas defined by those anchors.  

### 2. Drawing on Canvas  
- **Stylus Drawing:** Draw on the canvas by pressing the stylus tip.  
- **Dynamic Line Width:** Line thickness adjusts based on pressure.  

### 3. Undo and Clear Drawings  
- **Undo Last Line:** Double-tap the stylus back button or press it once to undo the last line.  
- **Clear All Lines:** Hold the stylus back button for more than 1 second to erase all drawings.  

### 4. Anchor and Canvas Persistence  
- **Save:** Anchors and canvas positions are automatically saved using `PlayerPrefs`.  
- **Reload:** Press the **Y button** (left controller) to reload the saved anchors and canvas.  
- **Reset:** Press the **X button** (left controller) to reset and clear all anchors and canvas.

## Key Bindings & Controls

| **Control**                            | **Action**                               |
|----------------------------------------|-----------------------------------------|
| **Stylus Tip Press (Pressure > 0.98)** | Create anchors or generate canvas        |
| **Stylus Draw on Canvas**              | Start/continue drawing                   |
| **Stylus Back Button (Double Tap)**    | Undo the last drawn line                |
| **Stylus Back Button (Hold 1s)**       | Clear all drawings                      |
| **Left Controller - X Button**         | Reset anchors and canvas                |
| **Left Controller - Y Button**         | Reload saved anchors and canvas         |

## Project Structure

- **CanvasSetupManager.cs**: Handles the creation of spatial anchors, canvas generation, saving, and loading of anchors.  
- **LineDrawing.cs**: Manages line drawing on the canvas, including undo/clear functionality.  
- **StylusHandler.cs**: Tracks stylus input (pressure, button presses, pose).  
- **Prefabs**: Contains the anchor and canvas prefabs.  
- **Materials**: Defines line and canvas appearance.

## Setup Instructions

1. **Clone the Repository:**  
   ```bash
   git clone https://github.com/ftmghorbani/MX_Ink_2Ddrawing_Sample.git
   ```

2. **Open in Unity:**  
   - Open the project in Unity Editor (version 2022.3.50f1 or compatible).  

3. **Configure Oculus XR:**  
   - Ensure Oculus XR Plugin is enabled.  
   - Set up Android build target for Oculus Quest.  

4. **Run the Project:**  
   - Connect the Oculus Quest via ADB.  
   - Press **Play** in Unity or build and deploy to the headset.

## Prerequisites for Spatial Anchors

This project requires the setup of **Meta Spatial Anchors**. For detailed instructions, refer to the official Meta documentation: [Unity Spatial Anchors](https://developers.meta.com/horizon/documentation/unity/unity-sf-spatial-anchors).

## Credits

Developed in collaboration with **Logitech**, based on the official [Logitech MX Ink Unity Integration Guide](https://logitech.github.io/mxink/UnityIntegration.html) and the **Logitech MX Ink Official Sample**.

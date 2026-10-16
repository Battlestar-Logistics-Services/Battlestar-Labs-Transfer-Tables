# Battlestar Labs Transfer Table System for Railroader
**Version 1.0.0 — Official Release**  
*Authored and Maintained by Alina Battlestar, Chewie, Battlestar Locomotive Works & Battlestar Logistics Services*

---

## 🚂 Welcome to the Battlestar Transfer Table System

The **Battlestar Labs Transfer Table System** introduces the original full-featured, modular transfer tables (traversers) to *Railroader*! Now seeing a full release after some performance improvements and customer feedback, we now proudly share our worh with the public! Custom engineered from the ground up for enhanced realism, fidelity, and performance, this mod enables yards, roundhouses, and repair shops to slide locomotives and rolling stock between parallel tracks with complete track graph integration, working winches, Auto Engineer pathfinding, and interactive UI controls.

---

## ✨ Key Features

### 🏗️ Fully Modular Pit & Bridge Construction
* **Scalable Bridge Lengths:** Seamlessly expandable from a compact **64-foot** starter table (1x Mid section) up to **100x Mid sections** (over 2,000 feet) to accommodate everything from switchers and yard cuts to massive modern consists. (I advise keeping to the smaller scaled sizes unless you have a computer that can handle the larger sizes, but I did what I could, this is as optimized as I could get everything! Building larger than 50 stalls or more than 20 middle sections can and will cause FPS to drop, but that is beyond my control, sorry!)
* **Configurable Stalls:** Support for 2 to 50+ stalls with customizable track pitch and modular endcaps.
* **Realistic Pit Foundations:** Recessed concrete retaining walls, integrated guide rails, pit floor textures, and approach lip alignment.

### 🛤️ Standard & Dual Gauge Support
* **Full Compatibility:** Out-of-the-box integration with the *Fuse Narrow Gauge Mod*.
* **Dynamic Third Rail:** Clean standard gauge and dual gauge toggle with proper 3rd rail rendering and endcap railhead transitions.
* **Asymmetrical Node Splitting:** Automatically balances front and rear track nodes (Dual R / Dual L) so narrow gauge equipment navigates the table without derailment or misalignment.

### 🪝 Integrated Bridge Winch / Car Mule
* **Dead-Engine & Unpowered Car Spotting:** Move rolling stock without a locomotive! Just drop it off within 50ft of the table, and let the high-powered service winch do the rest! Especially helpful for those hard-to-reach areas! 
* **Multiple Operating Modes:**
  * **Pull to Center:** Winches an unpowered car or coupled cut directly onto the exact center of the table.
  * **Push Off Front / Rear:** Pushes cars smoothly off the bridge and clear of the pit lip. All you need to do is set the handbrake once it stops.
* **Dynamic Visual Cable:** Real-time 3D winch cable rendering with physics-based creep deceleration for precision stops. (Improved visual effects in the works for later)

### 🤖 AutoEngineer AI Waypoint Routing
* **Full AE Integration:** Railroader's AutoEngineer AI seamlessly navigates across the transfer table!
* **Autonomous Stall Switching:**
  * Setting a waypoint to a destination in a different stall will call the table to your current stall, allowing the desired locomotive onto the table
  * AE will then be momentarily redirected to a temporary waypoint that will center it on the table bridge, and allowing the bridge to unlock and begin movement to the desired stall. Once at desired stall, the bridge will lock and allow for the original waypoint to resume, and the AE will continue the planned path.
  * As of right now, it is NOT fully Waypoint Queue friendly, so only place one waypoint at a time thru the table until I can (hopefully) further dial in the logic.

### 🎨 Visuals, Lighting & Paint Customization
* **16 Classic Railroad Paint Schemes:** Pre-configured authentic railroad color presets (PRR Tuscan Red, Pullman Green, BN Green, UP Armor Yellow, Caboose Red, Shop Black, Safety Orange, and more).
* **Custom Hex Color Picker:** Enter any custom HTML `#RRGGBB` hex code for precise railroad matching (also works for the Lighting!).
* **Illumination & Lighting Modes:**
  * High-intensity overhead spotlight arches and warning strobes.
  * Lighting automation: *Auto (Night Only)*, *Always On*, or *Always Off*.
  * Color temperature tuning (2200K Sodium, warm incandescent, clean white).

### ⚡ High-Performance Dynamic Track Graph
* **Minimal-Lag Traversal:** Powered by an optimized FUSE graph adapter with cached geometry and localized ghost graph synchronization—no stutter or breakage of the table, and minimal frame drops during table movement.
* **Automatic Game Save Persistence:** All table locations, customized styling, dimensions, and bridge positions persist across game saves, can be saved in other mods OR just inside of this mod via the UI, and reload seamlessly.

---

## ⌨️ Controls & Hotkeys Quick Reference

| Action | Control / Keybind | Notes |
| :--- | :--- | :--- |
| **Toggle Transfer Table UI** | **`\` (Backslash)** | Opens / closes the main Remote & Builder window. |
| **Cycle Active Table** | **`Tab`** | Cycles selection when multiple tables are placed. |
| **Spawn Transfer Table** | **`Keypad Divide (/)`** | Quick-spawns a new table at ground level under camera (Builder Mode). |
| **Rotate Table Left / Right** | **`Keypad 4` / `Keypad 6`** | Rotates table placement by current step angle. |
| **Cycle Rotation Step Angle** | **`Keypad 5`** | Cycles between 0.1°, 1°, 5°, 15°, 45°, and 90°. |
| **Raise / Lower Elevation** | **`Keypad 8` / `Keypad 2`** *(or `Page Up` / `Page Down`)* | Fine elevation adjustments (+Y / -Y). |
| **Cancel Movement / Aim** | **`Right Mouse Button` / `Escape`** | Cancels active move mode and restores previous location. |
| **Bulldoze Table (Safety Lock)**| **`Left Shift + Delete`** | Bulldozes the table (Builder Mode only). |

---

## 📖 Operational Guide

### 1. Spawning & Placing a Table
1. Press **`\` (Backslash)** to open the Transfer Table window.
2. Switch to **Builder Mode** at the top.
3. Click **"Spawn New Table"** or press **`Keypad /`** to drop a table onto the terrain under your camera.
4. Click **"Move Table"** to aim and reposition the table freely. Use the **Local Transform Pad** or **Numpad hotkeys** to dial in rotation and rail height.
5. Click **"✔ Confirm"** to lock the table in place.

### 2. Sizing & Styling
* **Table Length:** Open the **Setup** section in Builder Mode and choose your nominal length (64', 84', 105', etc.).
* **Stalls:** Adjust the stall slider to match the number of tracks entering your facility.
* **Gauge:** Toggle **Dual Gauge** if connecting 3-foot narrow gauge track.
* **Appearance:** Select a railroad color preset or input custom hex values for the bridge girders and deck trim.

### 3. Connecting Tracks (FUSE)
* Use Railroader's FUSE / track building tools to run approach tracks directly up to the pit lip at each stall.
* The table's graph adapter automatically detects and aligns track nodes to the bridge deck when the table locks into a stall.

### 4. Running Operations (Ops Mode)
1. Switch to **Ops Mode** in the window (or operate the physical controls on the bridge).
2. Click any **Stall Number Button** (e.g., `[Stall 1]`, `[Stall 2]`) to command the table to traverse to that stall.
3. Drive locomotives onto the bridge, stop, and select the target stall.
4. When moving unpowered cars, align the bridge, click **"Pull to Center"** under the Winch section, and let the mule spot the car on the bridge!

---

## Planned Updates and Long-Term Roadmap (With Checkpoints!)

### 🛠️ Work in Progress
 🕹️ Interactive 3D World Controls
* Animations coming in a later update, but figured why not tease them in here, for those of you that decide to read me!
* **Physical Control Stands:** Clickable operator console mounted directly on the bridge endcaps:
  * **Throttle Lever:** Click and hold or step to jog the table; features physical tilting animations that reflect table motion.
  * **Emergency Stop:** Red console button for immediate motion override.
* **Bridge Safety Gates:** Interactive endcap safety gates that swing open and closed

### 🗺️ Roadmap
- ✔ Basic ~100' fixed-size transfer table
- ✔ Second, larger, fixed-size transfer table
- Building-adjacent/building surrounded transfer table, w/o attachable nodes/fixed splines for industries
- ✔ Resizable transfer tables, variable lengths and widths, modular building components
- if all else works and this is somehow feasible AND not completely gamebreaking, then a conceptual transfer-turntable hybrid, allowing for 180* turnaround AND transversals

---

## 📦 Requirements & Installation

### Requirements:
* **Railroader** (PC / Steam)
* **Unity Mod Manager (UMM)** (v0.33.0.0 or higher)
* **FUSE** (Track & scenery framework mod)
* *(Optional)* **Narrow Gauge Mod** (for 3ft narrow gauge operations)

### Installation:
1. Download `BattlestarLabs.TransferTable_v1.0.0.zip`.
2. Open **Unity Mod Manager**, navigate to the **Mods** tab, and drag-and-drop the `.zip` file into the installer window.
3. Launch *Railroader* and ensure the mod displays a green status dot in the UMM menu.

---

## ⚖️ Copyright & License

```text
Copyright © 2026 Alina Battlestar / Chewie / Battlestar Locomotive Works / 
Battlestar Logistics Services. All rights reserved.

This software, to include but not limited to, the custom code and custom assets, 
is authored and maintained by Alina Battlestar and/or parties mentioned above. 
Unauthorized redistribution, modification, repackaging, or use of this software 
or its source code is prohibited except as expressly permitted by the accompanying 
license. Reverse engineering, decompilation, or disassembly for purposes not 
permitted by applicable law or the license is not authorized.
```

*For complete terms and conditions, refer to the included `LICENSE.txt` file.*

---

## 💬 Community & Support
Have questions, suggestions, or want to share screenshots of your transfer table yard setups?  
Join our community on Discord: [**Travelers' Network Discord**](https://discord.gg/XpEne9Cdje)

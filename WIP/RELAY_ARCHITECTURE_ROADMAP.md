# Battlestar Labs Transfer Table: Relay Architecture & Migration Roadmap

## 1. Executive Summary

As Keystone Studios transitions Railroader toward **Relay** (the native, sandboxed scripting and definition language highlighted in the dev update), mods that rely on direct C# memory hacking, Harmony hooks, and private reflection (such as overriding `Track.Turntable` internals) will need to be modernized.

This document outlines:
1. **The current C# Native Architecture** implemented in this rewrite.
2. **The 1:1 Mapping to Relay**, showing how the transfer table components translate to Relay declarations, signals, and mover states.
3. **The Relay Definition Specification** ready to drop in once Relay modding tools launch.

---

## 2. Architectural Comparison: C# vs. Relay

| System Component | Current C# Implementation | Upcoming Relay Equivalent |
| :--- | :--- | :--- |
| **Scenery & Definition** | `TransferTableDefinition.cs` + StrangeCustoms JSON | `definition transfer_table { ... }` declarative schema |
| **Platform Movement** | `TransferTableController.cs` (Lerp along X) | Native `mover` / `platform_axis` kinematics block |
| **Track Graph Binding** | `TransferTableGraphAdapter.cs` (free nodes + stall node linking) | `track_connector` ports and `route_align` directives |
| **Interactive Placement** | `TransferTablePlacement.cs` (Raycast + R to rotate) | Native placement tool hook (`tool.placement`) |
| **Player Controls** | `TransferTableInput.cs` (active table filtering) | Contextual interaction prompt (`interactable.control_stand`) |
| **Deck Visuals** | `TransferTableVisuals.cs` (procedural rails) | Native mesh profile attachment or model asset pack |

---

## 3. Relay Concept Prototype (Pseudocode / DSL)

When Relay is released, the transfer table will be authored as a declarative package:

```relay
// ====================================================================
// Battlestar Labs Transfer Table Definition (Relay DSL Prototype)
// ====================================================================

scenery "BST_TransferTable_100ft_5Stall" {
    name: "100ft 5-Stall Transfer Table"
    author: "Battlestar Labs"
    version: "2.0"

    model: "BattlestarLabsTransferTableTEST.fbx"
    bridge_node: "Bridge_Root"

    // Kinematic Parameters
    parameters {
        stalls: 5
        track_spacing: 4.572  // 15 feet in meters
        bridge_length: 30.48  // 100 feet in meters
        travel_speed: 1.5     // meters/sec
        lock_tolerance: 0.076 // ~3 inches
        rail_offset_y: 0.13
    }

    // Platform Mover Logic
    mover lateral_bridge {
        axis: Vector3(1, 0, 0)
        speed: parameters.travel_speed
        
        on_command_stall(target_index) {
            if (self.is_gap_occupied()) {
                notify_player("Cannot move: Rolling stock is straddling the gap!")
                return cancel
            }
            self.unlock()
            self.move_to(target_index * parameters.track_spacing)
        }

        on_arrived_at_stall(stall_index) {
            self.lock()
            track_graph.connect_ports(
                bridge.entry_port, pit_stalls[stall_index].front_port,
                bridge.exit_port,  pit_stalls[stall_index].back_port
            )
            play_sound("mag_lock_engage")
        }
    }

    // Interactive Control Stand (Cabin or Ground Switcher)
    interactable control_stand {
        prompt: "Operate Transfer Table"
        on_interact(player) {
            open_radial_menu([
                { label: "Stall 1", action: () => lateral_bridge.command_stall(0) },
                { label: "Stall 2", action: () => lateral_bridge.command_stall(1) },
                { label: "Stall 3", action: () => lateral_bridge.command_stall(2) },
                { label: "Stall 4", action: () => lateral_bridge.command_stall(3) },
                { label: "Stall 5", action: () => lateral_bridge.command_stall(4) },
                { label: "Step Left", action: () => lateral_bridge.step_left() },
                { label: "Step Right", action: () => lateral_bridge.step_right() }
            ])
        }
    }
}
```

---

## 4. How the Current C# Rewrite Bridges the Gap

1. **Decoupled Architecture**: Instead of inheriting monolithic game classes directly, each responsibility (Kinematics, Placement, Visuals, Graph Linking) is partitioned into clean, isolated components that directly mirror Relay modules.
2. **Zero Hardcoded Spawning**: Tables are managed through a unified instance registry, allowing full interactive positioning, rotation, and bulldozing.
3. **Multi-Table Safe**: Active table selection prevents all tables in the world from moving at once.
4. **Instant Visual Feedback**: Procedural 3D steel rails guarantee the bridge track is visible and functional right now, giving you the perfect testing bed while the 3D model FBX is tuned.

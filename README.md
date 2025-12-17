# LandFall - RTS Technical Prototype

LandFall is a high-performance Real-Time Strategy (RTS) game prototype built on **Unity DOTS** (Data-Oriented Technology Stack). 

The project demonstrates the use of **ECS (Entity Component System)**, **Burst Compiler**, and **C# Job System** to handle unit logic, movement, and formations efficiently. It is designed with a deterministic architecture in mind, separating simulation logic from presentation views.

> **⚠️ Work In Progress**: This project is currently in active development. Features are subject to change, and optimizations are ongoing.

## ✨ Key Features

* **DOTS-Based Architecture:** All core gameplay logic (movement, state management, transforms) runs on the main simulation thread using high-performance ECS systems.
* **RTS Controls:** * Classic "Box Selection" implemented via screen-to-world raycasting and spatial queries.
    * Right-click move commands.
* **Formation System:** * Units automatically arrange themselves into a Box formation upon movement.
    * Includes a custom `FormationSystem` that calculates slot positions relative to the group's centroid and direction.
* **Hybrid Visualization:**
    * Uses interpolation (smooth visual transitions) separate from the raw simulation tick rate (`RTSGameTime`), ensuring smooth rendering even if the simulation runs at a fixed tick.
* **Spatial Querying:** Custom systems for efficient entity lookup and selection.

## 🚧 Known Issues & Roadmap

As this is a prototype, the following areas are currently being iterated on:

* **Pathfinding & Formations:** The unit avoidance and formation logic are functional but currently simplistic. You may observe units overlapping or taking suboptimal paths when navigating complex terrain. Improving the steering behaviors is a top priority.
* **UI:** Basic debug UI implementation; lacking a full HUD.
* **Combat:** Health and damage systems are stubbed but not fully visualized.

## 📂 Project Structure

* `Authoring/`: MonoBehaviours that convert standard GameObjects into ECS Entities (Baking).
* `Components/`: Pure data structs (`IComponentData`) defining unit state (e.g., `MoveDestination`, `FormationSettings`).
* `Systems/`: Logic processing.
    * `Simulation/`: Handles the fixed time step logic.
    * `Input/`: Handles mouse interaction and command issuance.
    * `Gameplay/`: Contains `FormationSystem` and `UnitMovementSystem`.

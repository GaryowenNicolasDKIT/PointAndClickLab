# COMP I8014 - 3D Game Engine Development

This repository holds the notes, labs, Unity demonstration project and shared code for 3D Game Engine Development, where you learn to design and build the software components of a 3D game in Unity and C#. It is for Year 3 Games Development students and assumes the C# covered in Weeks 1 and 2 (classes, references, collections, interfaces, delegates and events). Read the notes for each topic before class, work through the matching lab in class, and reuse the shared code in `Assets/GD/Common` in your group project.

## 1. How to Use This Repository

1. Read the notes for the topic before class, because each class starts from the framing question in the notes.
2. In class, follow the cycle: framing question, theory, demonstration, then the lab.
3. Complete each lab in the order listed in Section 2, because the notes build on each other. Each lab builds its own scene from the shared code in `Assets/GD/Common`, so no lab needs another lab's work.
4. Name assets in your group project using the [Unity Asset Naming Conventions](Docs/conventions/naming-conventions.md), because a shared naming scheme lets every group member find and identify assets by name.

### Repository layout

| Folder | Purpose |
|:-------|:--------|
| `Docs/notes/` | Topic notes, numbered in teaching order |
| `Docs/labs/` | Lab handouts, numbered to match the notes |
| `Docs/assessments/` | Assessment briefs |
| `Docs/conventions/` | Project conventions, including [asset naming conventions](Docs/conventions/naming-conventions.md) |

## 2. Contents

| Nr | Topic | Notes | Lab / Exercises |
|:-----|:------|:------|:----------------|
| 1 | **Unity Foundations** - lifecycle, frame loop, Stats, attributes, debugging and project conventions | [Notes 01](Docs/notes/01_unity_foundations.md) | None |
| 2 | **ScriptableObjects** - separating data from behaviour with read-only data assets | [Notes 02](Docs/notes/02_scriptable_objects.md) | [Lab 2](Docs/labs/lab_2_scriptable_objects.md) |
| 3 | **Shared Variables and Variable References** - sharing runtime state between systems without direct references | [Notes 03](Docs/notes/03_shared_variables.md) | [Lab 3](Docs/labs/lab_3_shared_variables.md) |
| 4 | **Event Channels** - announcing moments to listeners in any scene or prefab | [Notes 04](Docs/notes/04_event_channels.md) | [Lab 4](Docs/labs/lab_4_event_channels.md) |
| 5 | **Additive Scenes and Persistent Systems** - Bootstrap, UI and level scenes connected through shared assets | [Notes 05](Docs/notes/05_additive_scenes.md) | [Lab 5 Part A](Docs/labs/lab_5_additive_scenes_part_a.md), [Lab 5 Part B](Docs/labs/lab_5_additive_scenes_part_b.md) |
| 6 | **Selection Systems and SOLID Design** - refactoring an all-in-one selection manager into swappable ray providers, selectors and responses | [Notes 06](Docs/notes/06_selection_systems.md) | [Lab 6](Docs/labs/lab_6_selection_systems.md) |
| 7 | **Movement Systems** - Transform, Rigidbody, CharacterController and NavMesh movers behind one interface, chosen by the gameplay problem | [Notes 07](Docs/notes/07_movement_systems.md) | [Lab 7](Docs/labs/lab_7_movement_systems.md) |
| 8 | **Collision Detection and Contact Events** - triggers, collisions and CharacterController contacts, filtered by tag and layer mask and wired to UnityEvents | [Notes 08](Docs/notes/08_collision_detection.md) | [Lab 8](Docs/labs/lab_8_recharge_platform.md) |
| 9 | **First and Third Person Controllers** - reading, configuring and tuning the Starter Assets controllers, and extending them by composition without editing them | [Notes 09](Docs/notes/09_starter_assets_controllers.md) | [Lab 9](Docs/labs/lab_9_third_person_controller.md) |
| 10 | **Inventory Systems** - item data, stacking and a hotbar, integrating ScriptableObjects, event channels, selection and the first person controller | [Notes 10](Docs/notes/10_inventory_systems.md) | [Lab 10 Part A](Docs/labs/lab_10_inventory_part_a.md), [Lab 10 Part B](Docs/labs/lab_10_inventory_part_b.md) |
| 11 | **Interpolation, Easing and Tweening** - frame-rate independent smoothing, easing, `AnimationCurve`, and DOTween with timing held in reusable assets | [Notes 11](Docs/notes/11_interpolation_easing_tweening.md) | [Lab 11](Docs/labs/lab_11_eased_selection_and_door.md) |
| 12 | **Character Animation** - states, parameters, blend trees and animation events on the third person controller, extended without editing it | [Notes 12](Docs/notes/12_character_animation.md) | [Lab 12](Docs/labs/lab_12_third_person_animator.md) |
| 13 | **AudioManager** - a mixer, sound cues as assets, pooled sources behind an event channel, and ducking added with no change to the manager | [Notes 13](Docs/notes/13_audio_manager.md) | [Lab 13](Docs/labs/lab_13_audio_manager.md) |
| 14 | **CameraManager** - Cinemachine priority, blends and impulses, and camera effects as swappable assets | [Notes 14](Docs/notes/14_camera_manager.md) | [Lab 14](Docs/labs/lab_14_camera_manager.md) |
| 15 | **Orchestration and Game Flow** - coroutines, `Time.timeScale`, `Func<T>`, a flow built with the Builder pattern, and a complete loop of title, play, game over, restart and pause | [Notes 15](Docs/notes/15_orchestration_game_flow.md) | [Lab 15](Docs/labs/lab_15_orchestration_builder.md) |
| 16 | **State Machines and the State Pattern** - from an enum and a `switch` to states as classes, with transitions as `Func<bool>` from triggers, variables and events | [Notes 16](Docs/notes/16_state_machines.md) | [Lab 16](Docs/labs/lab_16_enemy_state_machine.md) |
| 17 | **Game Conditions and End States** - rules as assets combined with AND, OR, XOR and NOT, and win, lose, toast and restart driven by them | [Notes 17](Docs/notes/17_game_conditions.md) | [Lab 17](Docs/labs/lab_17_game_conditions.md) |
| 18 | **Tick Systems** - one clock for gameplay rules that run at a fixed rate, with `TickBehaviour` handlers, per-second rates, dropped ticks after a hitch, and spreading the load | [Notes 18](Docs/notes/18_tick_systems.md) | [Lab 18](Docs/labs/lab_18_tick_hazards_and_sensors.md) |

### Supplementary Material

These sit outside the numbered sequence. Use them if your game needs them.

| Topic | Notes | Lab / Exercises |
|:------|:------|:----------------|
| **NavMesh Agents in Player-Built Levels** - runtime NavMesh updates, area costs and carving, so that enemies navigate a level the player lays out during play | [Notes](Docs/notes/s_navmesh_runtime_levels.md) | [Lab S](Docs/labs/lab_s_navmesh_runtime_levels.md) |
| **Point-and-Click Movement and Interaction** - click to walk or to use an object, with a NavMesh agent, the selection pipeline and an animated third person character, in rooms with fixed cameras | [Notes](Docs/notes/s_point_and_click_rooms.md) | [Lab S](Docs/labs/lab_s_point_and_click_rooms.md) |
| **Timed Light Effects** - lightning and other timed changes to Realtime lights, built from flows, tweens with envelope curves and event channels, with random variation and a measured idle cost | [Notes](Docs/notes/s_timed_light_effects.md) | [Lab S](Docs/labs/lab_s_lightning_storm.md) |

## 3. Tools and Setup

| Tool | Version | Notes |
|:-----|:--------|:------|
| Unity Editor | 6000.6.3f1 | Open the repository folder as a project in Unity Hub |
| Universal Render Pipeline | 17.6.0 | Installed with the project |
| Input System | 1.20.0 | Installed with the project; `Active Input Handling` is `Input System Package (New)` ([Notes 01](Docs/notes/01_unity_foundations.md), Section 4.11) |
| AI Navigation | 2.0.14 | Installed with the project; provides `NavMeshSurface` for baking NavMeshes ([Notes 07](Docs/notes/07_movement_systems.md), Section 4.5) |
| Cinemachine | 6.6.0 | Installed with the project; the Starter Assets follow cameras use `CinemachineCamera` ([Notes 09](Docs/notes/09_starter_assets_controllers.md), Section 4.7) |
| Starter Assets | First Person and Third Person | Included under `Assets/ThirdParty/Scripts/Controllers`; do not edit these files ([Notes 09](Docs/notes/09_starter_assets_controllers.md), Section 4.10) |
| DOTween | 1.3.045, free version | Installed with the project; check for updates and run its setup panel ([Lab 11](Docs/labs/lab_11_eased_selection_and_door.md), Step 1) |
| IDE | Visual Studio or Rider | Set in `Edit > Preferences > External Tools`; the labs use the debugger; setup is in [Notes 01](Docs/notes/01_unity_foundations.md), Section 4.9 |
| Git | Any current version | Required for the GCA; the repository includes a Unity `.gitignore` |

### Editor tools

| Tool | Menu |
|:-----|:-----|
| Create Project Folders | `Tools > GD > Setup > Create Project Folders` (see [Notes 01](Docs/notes/01_unity_foundations.md), Section 4.1) |
| Create Scene Hierarchy | `Tools > GD > Setup > Create Scene Hierarchy` (see [Notes 01](Docs/notes/01_unity_foundations.md), Section 4.2) |
| Create Scene Info Overlay | `Tools > GD > Setup > Create Scene Info Overlay` |
| ScriptableObject Browser | `Tools > GD > Navigate > ScriptableObject Browser` |
| Selection Bookmarks | `Tools > GD > Navigate > Selection Bookmarks` |
| Component Dependency Viewer | `Tools > GD > Analyse > Component Dependency Viewer` |
| Script Usage Finder | `Tools > GD > Analyse > Script Usage Finder` |
| Mesh and Renderer Inspector | `Tools > GD > Analyse > Mesh and Renderer Inspector` |
| State and Flow Viewer | `Tools > GD > Analyse > State and Flow Viewer`; during Play, shows each flow's current step, and each state machine's state, transitions and recent changes |
| Evidence Screenshot | `Tools > GD > Evidence > Evidence Screenshot` |
| GIF Capture | `Tools > GD > Evidence > GIF Capture`; records a camera in Play Mode as a looping GIF, for example to show an iteration in the GCA |

## 4. Conventions Used in the Notes

- **Key Concepts** open each note: ten one-line principles for the topic.
- **Bold** marks a term defined in the note's glossary.
- `Code` marks a class, member, file, folder, asset or editor setting.
- Tables of skills state how you will know you can do each one.
- Worked examples follow the sequence initial implementation, problem, change, improvement.
- Each lab step ends with a **Checkpoint**: the observable result to check before moving on.
- **Record** steps ask for a short answer in your `LabNotes.md`.
- Production checklist tables list what must be true when a lab is complete.
- Reflective questions ask you to justify a decision, not recall a fact.
- Exercises are graded Core, Apply and Challenge.
- An HTML comment starting `IMAGE:` marks an image still to be added.

## 5. Getting Help

1. Re-read the relevant notes, the worked example and the lab's Debugging and Pitfalls table.
2. Raise it in class or with your group.
3. Post on the module Moodle forum.
4. If still blocked, contact the lecturer, stating what you tried.

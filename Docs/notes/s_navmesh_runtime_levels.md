---
title: "Supplementary - NavMesh Agents in Player-Built Levels"
doc-type: "note"
module-code: "COMP I8014"
module-title: "3D Game Engine Development"
programme: "BSc (Hons) in Computing in Games Development"
stage: 3
institution: "Dundalk Institute of Technology (DkIT)"
author: "Niall McGuinness"
prerequisites: ["04_event_channels.md", "07_movement_systems.md", "15_orchestration_game_flow.md"]
feeds_into: ["GCA - Gameplay Systems & Integration - Gameplay System 1", "GCA - Gameplay Systems & Integration - Systems Integration"]
last-updated: "2026-11-06"
---

# Supplementary - NavMesh Agents in Player-Built Levels

Lab: [Lab S - NavMesh Agents in a Player-Built Dungeon](../labs/lab_s_navmesh_runtime_levels.md)

## Key Concepts

1. **A baked NavMesh knows nothing about what happens after the bake** - a wall placed during play does not exist as far as an agent's path is concerned.
2. **Build at runtime with `NavMeshSurface`** - `BuildNavMesh` builds everything at once, and `UpdateNavMesh` updates over several frames.
3. **Rebuild only what navigation needs** - choose layers carefully, and use `Physics Colliders`, so that agents, effects and props stay out of the map.
4. **Combine requests** - many changes can share one update, because only the latest layout matters.
5. **Cannot and would rather not are different** - carving forbids an area, and area cost makes an area expensive.
6. **Cost is multiplied by distance** - an agent crosses a trap when the detour costs more than the trap does.
7. **A new cost does not change a path an agent already has** - tell agents to plan again after an update.
8. **A carved hole needs no rebuild** - `NavMeshObstacle` changes the map locally, and closes the hole when the obstacle is gone.
9. **A player can seal the level** - check the route after each change, and undo a change that breaks it.
10. **Agents must be placed on the map** - build first, spawn second, and check `isOnNavMesh` before you call `SetDestination`.

---

## 1. Framing Question

In your game, the player lays out the dungeon after the game starts: walls, traps and explosives, placed on a grid. Then the enemies arrive. They must find a way through a level that did not exist when you built the game, walk around traps when it is worth it, stay clear of explosives, and keep coping as the player changes the level around them. A NavMesh is usually baked in the Editor. How do you use one here, and what can the player do to break it?

Think about a game where you place defences and enemies route around them. What happens when you try to block the path completely? Does the game stop you, or do the enemies do something else?

---

## 2. What You Will Learn

| Skill | You can... |
|:------|:-----------|
| Build a NavMesh at runtime | Choose between `BuildNavMesh` and `UpdateNavMesh`, and give the reason |
| Configure a runtime surface | Set `Collect Objects`, `Include Layers` and `Use Geometry` for a level built during play |
| Combine rebuild requests | Update once for a burst of changes, with no overlapping updates |
| Make agents avoid an area | Choose between area cost, area masks and carving |
| Vary behaviour by agent | Give a careful and a reckless enemy different costs, as data |
| Keep paths current | Make agents plan again after the map changes |
| Prevent sealed levels | Test a route with `NavMesh.CalculatePath`, and undo a placement that breaks it |

---

## 3. Why This Matters

Letting the player build the level makes navigation a live system rather than a fixed asset. If enemies walk through a new wall, or stand still because the only door has been blocked, the player stops believing in the game at once. If they visibly choose a longer way round to avoid a trap, the player feels clever for placing it. That reaction is the game.

This topic also shows how choosing the right tool matters. A trap and an explosive look like the same problem, but one should be avoided when it is worth it and the other never crossed, and they need different techniques with different costs. A clear reason for each is strong evidence for a **Gameplay System** in the GCA, and the same techniques apply to any level that changes, such as doors that lock, bridges that collapse or barricades that break.

Note 07 baked a NavMesh in the Editor, and this note assumes you have read its Section 4.5. Everything here uses `NavMeshSurface` and `NavMeshAgent`, with event channels, `IRayProvider` and `FlowBehaviour` from `Assets/GD/Common`.

---

## 4. Core Ideas

### 4.1 Building at Runtime

`NavMeshSurface`, in the `Unity.AI.Navigation` namespace, has the same job at runtime as the `Bake` button has in the Editor. It offers two methods:

| Method | Runs | Result | Use it |
|:--|:--|:--|:--|
| `surface.BuildNavMesh()` | All at once, in this frame | A new NavMesh replaces the old one | Once, at a moment when a pause is acceptable, such as a level starting |
| `surface.UpdateNavMesh(surface.navMeshData)` | Over several frames, on other threads, and returns an `AsyncOperation` | The existing NavMesh changes where the level changed | During play, after the first build |

```csharp
// Before any enemy exists.
surface.BuildNavMesh();

// During play. A coroutine can wait for this, as in Note 05.
yield return surface.UpdateNavMesh(surface.navMeshData);
```

`UpdateNavMesh` needs existing data, so build once first. Treat the time taken as something to measure on your own level: it depends on the size of the level and the voxel size, so time it with `System.Diagnostics.Stopwatch` rather than assuming.

### 4.2 Setting Up the Surface

Three settings decide what the build sees:

| Setting | Use | Why |
|:--|:--|:--|
| `Collect Objects` | `All Game Objects` | Pieces are placed during play, so they may not be in one hierarchy. `Current Object Hierarchy` also works if every piece is a child of the surface |
| `Include Layers` | Only a `Level` layer | Characters, props and effects stay out of the map. It also makes each build faster |
| `Use Geometry` | `Physics Colliders` | Render meshes need `Read/Write` enabled to be read at runtime, or a build fails to use them. Colliders do not, and they are usually simpler |

By default the surface ignores objects with a `NavMeshAgent` or `NavMeshObstacle`, so enemies and carved obstacles do not become part of the walkable map.

Note that `Include Layers` also filters `NavMeshModifierVolume` components: a volume on an excluded layer has no effect, with no warning. Section 5 puts each trap's volume on a child object on the `Level` layer for this reason.

### 4.3 What an Agent Does When the Map Changes

An agent already following a path does not always notice a change:

| Change | Effect on an agent with a path |
|:--|:--|
| Its path crosses a new hole or wall | The path is no longer valid, and with `Auto Repath` on (the default) the agent plans again |
| A cost changes, but the path can still be walked | It keeps the old path, even through a new trap |
| A shorter route opens | It keeps the old path |
| The ground under it is removed | It is no longer on the NavMesh, and `SetDestination` logs an error until you move it back |

So a walkable change needs a nudge: call `SetDestination` again after an update. A channel suits this, because all enemies need to hear it, and the system that updates the map does not need to know they exist.

### 4.4 Ways to Avoid Something

There are four tools, and they do different jobs:

| Tool | What it does | Needs a rebuild? | Use it for |
|:--|:--|:--|:--|
| `NavMeshObstacle`, no carving | Agents steer around it at the last moment, and paths ignore it | No | A moving object that blocks briefly, such as another character |
| `NavMeshObstacle`, carving | Cuts a hole in the map, and paths go around it | No | Something agents must never cross, particularly if it can disappear |
| Area with a high cost, from `NavMeshModifierVolume` | Paths avoid it when the detour is cheaper | Yes | Danger the agent may accept, such as a trap in the only corridor |
| Area excluded by `agent.areaMask` | This agent never paths through it | Yes, to create the area | Rules by agent type, such as only flying enemies crossing lava |

The key distinction is between **cannot** and **would rather not**. A trap needs "would rather not": if it is the only way through, the enemies should walk into it, because that is the point of placing it. An explosive needs "cannot", because it is much clearer to the player if enemies stay away from it.

### 4.5 Areas and Costs

A **NavMesh area** is a label on part of the map, with a **cost**. Add one in `Window > AI > Navigation`, on the `Areas` tab, named for example `Trap`. A `NavMesh Modifier Volume` marks a box of the map with that area when the surface is built. Cost multiplies distance: the length of a path through an area counts as length times cost, and the agent picks the lowest total.

With a trap cost of 20 and a trap 4 metres across:

| Route | Calculation | Total | Chosen? |
|:--|:--|:--|:--|
| Through the trap | 4 x 20 | 80 | |
| Detour of 30 metres | 30 x 1 | 30 | Yes |
| Detour of 90 metres | 90 x 1 | 90 | No: it crosses the trap |

Unity's costs must be at least 1, and a low cost cannot make a route cheaper than the same distance at cost 1. Costs can differ per agent, which turns behaviour into data:

```csharp
int trapArea = NavMesh.GetAreaFromName("Trap");

// Only this agent. A value set with NavMesh.SetAreaCost would apply to all of them.
agent.SetAreaCost(trapArea, 20f);
```

`GetAreaFromName` returns -1 for a name that does not exist, so check it.

### 4.6 Carving

A **carving** obstacle cuts a hole in the map while it exists, with no rebuild. Add `NavMesh Obstacle`, then tick `Carve`. The hole allows for the agent's radius, so agents keep their distance. When the obstacle is destroyed or disabled, the map returns to how it was.

| Setting | Effect |
|:--|:--|
| `Carve Only Stationary` | On by default. The hole is removed while the obstacle is moving, and cut again after it has rested for `Time To Stationary` seconds. Agents only steer around it while it moves. |
| `Move Threshold` | How far it must move before it counts as moving |

Placed traps and explosives do not move, so the default suits them. Carving has its own cost, and many carving obstacles can be slower than a rebuild, so measure if you have a lot of them.

### 4.7 Sealed Levels

Someone will block the only corridor. Agents then have nowhere to go, and `pathStatus` shows why:

| `NavMeshPathStatus` | Meaning | The agent |
|:--|:--|:--|
| `PathComplete` | Reaches the destination | Walks there |
| `PathPartial` | Gets as close as it can | Walks to the nearest point, then stops |
| `PathInvalid` | No path was found | Does not move |

`NavMesh.CalculatePath` tests a route with no agent, so you can check the level directly. Your design then has three options:

| Rule | How | Reads as |
|:--|:--|:--|
| Refuse the placement | Update the map, test the route, and remove the piece if it fails | Clear and fair, with the standard tower defence feel |
| Allow it, and enemies break through | On `PathPartial`, a state makes them attack whatever is blocking | Exciting, and more work |
| Allow it, and the level ends | Treat a sealed level as a win or a loss | Only with a game designed around it |

Section 5 uses the first. The Apply exercise builds the second with `StateMachine` from `Assets/GD/Common`.

### 4.8 Spawning Agents

An agent must start on the map. A `NavMeshAgent` enabled off the NavMesh logs `Failed to create agent because it is not close enough to the NavMesh`, and it will not move.

- **Build first, spawn second.** The order is part of your game flow, which Note 15 handles.
- **Spawn in position.** Use `Instantiate(prefab, position, rotation)`. `ObjectPool<T>` from `Assets/GD/Common` activates an object before you can move it, so a pooled agent is enabled at its old position. If you pool agents, use `agent.Warp(position)` after `Get`, and test it.
- **Snap to the map.** `NavMesh.SamplePosition` finds the nearest point on the map, which helps if your spawn marker is at a different height.

---

## 5. Worked Example

The demo builds a dungeon on a grid floor: the player places walls, traps and explosives, and enemies walk from a spawn point to a goal. The design decision comes first:

| Piece | Tool | Reason |
|:--|:--|:--|
| Wall | Collider on the `Level` layer, with an update | It is part of the level, and it must change the route |
| Trap | `NavMeshModifierVolume` with a `Trap` area, and an update | Enemies accept it when the detour is too long, and enemies can differ |
| Explosive | `NavMeshObstacle` with `Carve` | Enemies must never cross it, and its hole closes when it detonates, with no rebuild |

### Stage 1 - Initial Implementation: A Baked Map

Bake the floor in the Editor as in Note 07, and give an enemy a destination:

```csharp
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WalkToGoal : MonoBehaviour
{
    [SerializeField] private Transform goal;

    private void Start()
    {
        GetComponent<NavMeshAgent>().SetDestination(goal.position);
    }
}
```

The enemy walks straight to the goal, as it should.

### Stage 2 - Problem: The Map Is Out of Date

Now place walls in Play mode:

| Observation | Cause |
|:--|:--|
| The enemy walks through or pushes into new walls | The path comes from the baked map, which has no walls |
| Calling `BuildNavMesh` on every placement gives a visible stutter as the level grows | It rebuilds everything, in one frame |
| The enemy walks straight through a new trap | A cost does not invalidate a path, and it has no area yet anyway |
| The player can seal the corridor, and enemies stand still | Nothing checks the route |

### Stage 3 - Change: A Rebuilder and Placement Rules

The rebuilder owns all map updates. Any system raises `LevelChanged`, and it raises `NavMeshUpdated` when the map is ready:

```csharp
using System.Collections;
using GD.Types;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// Updates the NavMesh when the level changes. Requests during an update share one more
/// update, because only the latest layout matters.
/// </summary>
[RequireComponent(typeof(NavMeshSurface))]
public class NavMeshRebuilder : MonoBehaviour
{
    [SerializeField, Tooltip("Raise this after changing the level")]
    private VoidEventChannel levelChanged;

    [SerializeField, Tooltip("Raised when the NavMesh matches the level")]
    private VoidEventChannel navMeshUpdated;

    private NavMeshSurface surface;
    private Coroutine updating;
    private bool updateAgain;

    private void Awake()
    {
        surface = GetComponent<NavMeshSurface>();
    }

    private void OnEnable()
    {
        levelChanged.Raised += HandleLevelChanged;
    }

    private void OnDisable()
    {
        levelChanged.Raised -= HandleLevelChanged;
    }

    private void Start()
    {
        // UpdateNavMesh needs existing data, so build once first.
        surface.BuildNavMesh();
        navMeshUpdated.Raise();
    }

    private void HandleLevelChanged()
    {
        if (updating != null)
        {
            updateAgain = true;
            return;
        }

        updating = StartCoroutine(UpdateUntilCurrent());
    }

    private IEnumerator UpdateUntilCurrent()
    {
        do
        {
            updateAgain = false;
            yield return surface.UpdateNavMesh(surface.navMeshData);
        }
        while (updateAgain);

        updating = null;
        navMeshUpdated.Raise();
    }
}
```

A small component tests the route, and has no other job:

```csharp
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Reports whether a complete route exists between two points on the NavMesh.
/// </summary>
public class RouteCheck : MonoBehaviour
{
    [SerializeField] private Transform start;
    [SerializeField] private Transform goal;

    [SerializeField, Min(0.1f), Tooltip("How far a point may be from the NavMesh, in metres")]
    private float sampleDistance = 2f;

    private NavMeshPath path;

    private void Awake()
    {
        path = new NavMeshPath();
    }

    public bool HasCompleteRoute()
    {
        if (!NavMesh.SamplePosition(start.position, out NavMeshHit startHit, sampleDistance, NavMesh.AllAreas))
            return false;

        if (!NavMesh.SamplePosition(goal.position, out NavMeshHit goalHit, sampleDistance, NavMesh.AllAreas))
            return false;

        return NavMesh.CalculatePath(startHit.position, goalHit.position, NavMesh.AllAreas, path)
            && path.status == NavMeshPathStatus.PathComplete;
    }
}
```

The placer snaps pieces to a grid, then waits for the map before accepting them. It uses `IRayProvider` from `Assets/GD/Common`, so a `MouseScreenRayProvider` beside it supplies the ray:

```csharp
using System.Collections.Generic;
using GD;
using GD.Types;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Places pieces on a grid. A piece that would seal the route is removed again.
/// </summary>
public class DungeonPlacer : MonoBehaviour
{
    [SerializeField] private InputAction placeAction = new("Place", binding: "<Mouse>/leftButton");
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private RouteCheck routeCheck;

    [SerializeField, Tooltip("Layers a piece can be placed on")]
    private LayerMask buildLayers;

    [SerializeField, Min(0.5f), Tooltip("Size of one grid cell, in metres")]
    private float cellSize = 4f;

    [Header("Channels")]
    [SerializeField] private VoidEventChannel levelChanged;
    [SerializeField] private VoidEventChannel navMeshUpdated;
    [SerializeField] private VoidEventChannel placementRefused;

    private readonly Dictionary<Vector2Int, GameObject> pieces = new();

    private IRayProvider rayProvider;
    private GameObject pendingPiece;
    private Vector2Int pendingCell;

    private void Awake()
    {
        rayProvider = GetComponent<IRayProvider>();
    }

    private void OnEnable()
    {
        navMeshUpdated.Raised += HandleNavMeshUpdated;
        placeAction.performed += HandlePlacePressed;
        placeAction.Enable();
    }

    private void OnDisable()
    {
        navMeshUpdated.Raised -= HandleNavMeshUpdated;
        placeAction.performed -= HandlePlacePressed;
        placeAction.Disable();
    }

    /// <summary>Wire to UI buttons, to choose what the next click places.</summary>
    public void SelectPiece(GameObject prefab)
    {
        piecePrefab = prefab;
    }

    private void HandlePlacePressed(InputAction.CallbackContext context)
    {
        // One piece at a time, because each one must pass the route check.
        if (pendingPiece != null || !rayProvider.TryCreateRay(out Ray ray))
            return;

        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, buildLayers))
            return;

        Vector2Int cell = new(Mathf.FloorToInt(hit.point.x / cellSize), Mathf.FloorToInt(hit.point.z / cellSize));

        if (pieces.ContainsKey(cell))
            return;

        Vector3 position = new((cell.x + 0.5f) * cellSize, hit.point.y, (cell.y + 0.5f) * cellSize);

        pendingPiece = Instantiate(piecePrefab, position, Quaternion.identity);
        pendingCell = cell;
        pieces[cell] = pendingPiece;

        levelChanged.Raise();
    }

    private void HandleNavMeshUpdated()
    {
        if (pendingPiece == null)
            return;

        GameObject piece = pendingPiece;
        pendingPiece = null;

        if (routeCheck.HasCompleteRoute())
            return;

        pieces.Remove(pendingCell);

        // Deactivate now, because Destroy waits for the end of the frame, and the
        // next update must not collect this piece.
        piece.SetActive(false);
        Destroy(piece);

        levelChanged.Raise();
        placementRefused.Raise();
    }
}
```

The decisions here:

- **Channels at both ends.** `DungeonPlacer` does not call `NavMeshRebuilder`, and enemies do not know about either one.
- **`pendingPiece` allows one change at a time.** Otherwise a refused piece could not be identified from a pair placed together.
- **The route check waits for the update.** Testing straight after `Instantiate` would read the old map.

### Stage 4 - Improvement: Traps, Explosives and Enemies with Different Judgement

**The pieces** are prefabs:

| Prefab | Root | Child |
|:--|:--|:--|
| `PF_Wall` | Cube, `Box Collider`, layer `Level` | None |
| `PF_Trap` | Empty, trigger `Box Collider`, `ContactTrigger`, layer `Default`, so that it is not part of the map | `Visual`, a flat cube with no collider; `CostVolume`, layer `Level`, `NavMesh Modifier Volume`, `Area Type` `Trap`, `Size` (4, 2, 4) |
| `PF_Explosive` | Cube, `Box Collider`, `NavMesh Obstacle` with `Carve` ticked, layer `Default`, `Explosive` component | None |

**The enemy's judgement** is data, as in Note 02:

```csharp
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// How an enemy values danger. One careful and one reckless asset give two behaviours
/// from one enemy prefab.
/// </summary>
[CreateAssetMenu(fileName = "NavigationProfile", menuName = "GD/Enemies/Navigation Profile")]
public class NavigationProfile : ScriptableObject
{
    [SerializeField, Tooltip("Must match a name on the Areas tab")]
    private string trapAreaName = "Trap";

    [SerializeField, Min(1f), Tooltip("How many metres of normal floor one metre of trap is worth")]
    private float trapCost = 20f;

    public void ApplyTo(NavMeshAgent agent)
    {
        int area = NavMesh.GetAreaFromName(trapAreaName);

        if (area < 0)
        {
            Debug.LogError($"{name}: there is no NavMesh area named {trapAreaName}.", this);
            return;
        }

        agent.SetAreaCost(area, trapCost);
    }
}
```

**The enemy** plans again whenever the map changes, and at a slow regular interval to catch carving, which raises no event:

```csharp
using GD.Types;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Walks to a goal across a level that can change at any time.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class DungeonWalker : MonoBehaviour, IExplosionTarget
{
    [SerializeField] private NavigationProfile profile;
    [SerializeField] private VoidEventChannel navMeshUpdated;

    [SerializeField, Min(0.1f), Tooltip("Seconds between path updates when nothing else requests one")]
    private float repathSeconds = 1f;

    [SerializeField, Min(0.1f), Tooltip("How far the agent may be from the NavMesh and still return to it")]
    private float recoverDistance = 2f;

    private NavMeshAgent agent;
    private Transform goal;
    private float repathTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        profile.ApplyTo(agent);
    }

    private void OnEnable()
    {
        navMeshUpdated.Raised += Repath;
    }

    private void OnDisable()
    {
        navMeshUpdated.Raised -= Repath;
    }

    /// <summary>Called by the spawner, straight after creating this enemy.</summary>
    public void SetGoal(Transform newGoal)
    {
        goal = newGoal;
        Repath();
    }

    public void HandleExplosion()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
            Repath();
    }

    private void Repath()
    {
        repathTimer = repathSeconds;

        if (goal == null)
            return;

        // The ground under this enemy may have been removed.
        if (!agent.isOnNavMesh && !TryReturnToNavMesh())
            return;

        agent.SetDestination(goal.position);
    }

    private bool TryReturnToNavMesh()
    {
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, recoverDistance, NavMesh.AllAreas))
            return false;

        return agent.Warp(hit.position);
    }
}
```

**The explosive** carves while it exists, and detonation closes the hole. An interface keeps it independent of any enemy class:

```csharp
/// <summary>
/// Anything an explosion can affect.
/// </summary>
public interface IExplosionTarget
{
    void HandleExplosion();
}
```

```csharp
using GD.Types;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Keeps agents away by carving the NavMesh, and affects everything in range when detonated.
/// </summary>
[RequireComponent(typeof(NavMeshObstacle))]
public class Explosive : MonoBehaviour
{
    [SerializeField, Tooltip("All explosives listening to this channel detonate together")]
    private VoidEventChannel detonate;

    [SerializeField, Min(0f), Tooltip("Blast radius, in metres")]
    private float radius = 5f;

    [SerializeField] private LayerMask targetLayers = ~0;

    private void OnEnable()
    {
        detonate.Raised += Explode;
    }

    private void OnDisable()
    {
        detonate.Raised -= Explode;
    }

    private void Explode()
    {
        foreach (Collider hit in Physics.OverlapSphere(transform.position, radius, targetLayers))
        {
            if (hit.TryGetComponent(out IExplosionTarget target))
                target.HandleExplosion();
        }

        // The carved hole closes with this object. The enemies' next regular repath uses the new map.
        Destroy(gameObject);
    }
}
```

**Starting the level** is a short flow, built with the classes from Note 15. The map already exists at this point, because `NavMeshRebuilder` builds it in `Start`:

```csharp
using GD.Flow;
using GD.Types;
using UnityEngine;

/// <summary>
/// Spawns one wave of enemies once the player presses Start, one at a time.
/// </summary>
public class EnemyWaveFlow : FlowBehaviour
{
    [SerializeField] private VoidEventChannel startPressed;
    [SerializeField] private DungeonWalker enemyPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform goal;

    [SerializeField, Min(1)] private int enemyCount = 5;
    [SerializeField, Min(0f)] private float secondsBetweenEnemies = 1.5f;

    protected override FlowSequence BuildSequence()
    {
        FlowBuilder builder = new FlowBuilder().WaitFor(startPressed);

        for (int i = 0; i < enemyCount; i++)
            builder.Do(SpawnEnemy).Wait(secondsBetweenEnemies);

        return builder.Build();
    }

    private void SpawnEnemy()
    {
        DungeonWalker enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        enemy.SetGoal(goal);
    }
}
```

Tick `Play On Start` on `EnemyWaveFlow`, because it waits for `startPressed` itself. To place a trap or explosive during play, keep `DungeonPlacer` enabled, and enemies respond straight away: traps through `LevelChanged` and then `NavMeshUpdated`, and explosives through carving.

Two enemy prefab variants, `Careful` with `Trap Cost` 20 and `Reckless` with `Trap Cost` 1, now take visibly different routes from one class.

| Problem | Solution | Benefit |
|:--|:--|:--|
| A baked map could not follow a level built during play, and all danger looked the same to enemies | One rebuilder with combined requests, areas for "would rather not", carving for "cannot", and a route check on placement | Enemies respond to the player's choices; the player cannot seal the level; judgement varies by data |

---

## 6. Common Pitfalls

| Symptom | Cause | Fix |
|:--------|:------|:----|
| `Failed to create agent because it is not close enough to the NavMesh` | The agent was enabled before the build, or away from the map | Build first; spawn on the map |
| `SetDestination can only be called on an active agent that has been placed on a NavMesh` | Same, or the ground under it was removed | Check `isOnNavMesh`; use `SamplePosition` and `Warp` |
| Enemies walk through new walls | The wall is not on an included layer, or no update was requested | Check `Include Layers`; raise `LevelChanged` |
| A trap has no effect | The volume's layer is not in `Include Layers`, or `Area Type` is `Walkable` | Put the volume on `Level`; set its area |
| Enemies walk on top of traps' meshes as raised ground | The trap's mesh is on the `Level` layer | Keep the root on another layer |
| Enemies ignore a trap they could avoid | They kept their old path, or the detour costs more | Call `SetDestination` after the update; check the cost sum |
| All enemies change behaviour together | `NavMesh.SetAreaCost` was used | Use `agent.SetAreaCost` |
| The map is missing pieces in a build, but not in the Editor | `Use Geometry` is `Render Meshes`, and the meshes are not readable | Use `Physics Colliders` |
| Errors about a NavMesh update, or changes that are missing | `UpdateNavMesh` was called with no data, or overlapping updates | Build first; request updates through one rebuilder |
| A removed piece is still in the map | `Destroy` waits until the end of the frame, and the update ran first | `SetActive(false)` before `Destroy` |
| Corridors have no NavMesh | A gap is narrower than the agent's diameter | Widen the grid, or reduce `Radius` in the agent type |
| A UI click also places a piece | The input action does not know about the UI | Check `EventSystem.current.IsPointerOverGameObject()` from `Update`, or use a build mode that disables placement over UI |
| A trap's `ContactTrigger` never fires for an enemy | A `NavMeshAgent` alone does not produce trigger events, and neither object has a `Rigidbody` | Add a `Rigidbody` with `Is Kinematic` ticked to the enemy prefab |
| A volume has the wrong size | `NavMeshModifierVolume` multiplies `Size` by the object's scale | Keep its parent at scale 1 |
| Enemies pass close to an explosive's edge | The hole's size comes from the obstacle's shape and the agent's radius | Make the obstacle's shape larger than the visible object |

---

## 7. Software Development Parallels

- **Single responsibility.** `NavMeshRebuilder` updates, `RouteCheck` tests, `DungeonPlacer` places, and `NavigationProfile` values danger. A new rule for placement does not touch how the map updates.
- **Observer and dependency inversion.** `LevelChanged` and `NavMeshUpdated` connect these parts with no references between them. `Explosive` depends on `IExplosionTarget`, so crates and barrels can react with no change to it.
- **Strategy.** A `NavigationProfile` asset selects one enemy's judgement, as a `TweenProfile` selects timing.
- **Debouncing and coalescing.** Combining requests in `NavMeshRebuilder` is a common technique: search boxes wait for typing to stop, and build tools rebuild once for many saved files.
- **Weighted graph search.** Area costs are edge weights in a shortest-path search. Road routing that avoids tolls unless they save enough time makes the same trade.
- **Commercial games.** *Fieldrunners* is a well-known example of mazing: enemies re-route around towers placed in an open field, so placing them is the core of the game. *Orcs Must Die!* gives the player traps in fixed levels with enemies that follow routes, which shows the appeal of placing danger where the enemies must walk.

---

## 8. Exercises

### Core - Careful and Reckless

Create `Careful` and `Reckless` `NavigationProfile` assets and two enemy prefab variants. Build a level with a trap in a short corridor and a longer way round. Then find, by experiment, the trap cost at which a careful enemy accepts the trap instead of the detour.

**Expected result:** the two variants take different routes through the same level. In 80 to 120 words, show the sum from Section 4.5 for your level, compare it with what you measured, and explain any difference.

### Apply - Enemies That Break Through

Remove the route check from `DungeonPlacer`. Using `StateMachine` and `IState` from `Assets/GD/Common`, give an enemy `Walking` and `Breaking` states. When `agent.pathStatus` is `PathPartial`, it walks to the end of its path, attacks the nearest wall for a set time, destroys it, and raises `LevelChanged`.

**Expected result:** sealing the level no longer stops the game: enemies break a wall and continue, and the others then re-route through the gap. In 80 to 120 words, say how you found which wall to attack, and why `pathStatus` needs a check after `pathPending` is false.

### Challenge - Measure Your Update

Time `BuildNavMesh` and `UpdateNavMesh` with `System.Diagnostics.Stopwatch`, at floor sizes of 20, 50 and 100 metres, with at least 30 pieces. Compare that with 30 carved obstacles, using frame time in the `Stats` panel. Change the surface's `Voxel Size` or set `Collect Objects` to `Volume`, and measure again.

**Expected result:** a table of your results, with a recommendation for your game. In 150 to 200 words, justify which pieces should use an update and which should carve, based on your evidence, and state any cost your measurements could not show.

---

## 9. Reflective Questions

1. A trap uses an area, and an explosive uses carving. Justify each choice. What would the player see if you swapped them?
2. `NavMeshRebuilder` combines requests rather than updating for each one. What could go wrong if a system assumed that the map was current immediately after raising `LevelChanged`? How does this design prevent that?
3. Refusing a placement, breaking through and allowing a sealed level are three designs. Which suits your game, and what does each cost to build?
4. `DungeonWalker` both reacts to a channel and repaths at an interval. Justify having both, or argue for removing one, and state what evidence would settle it.
5. `Explosive` depends on `IExplosionTarget`, not on `DungeonWalker`. Which SOLID principle does this show, and what new object could you add without changing it?

---

## 10. How This Builds

This is supplementary material, outside the weekly sequence, and no other note depends on it.

- **[Lab S](../labs/lab_s_navmesh_runtime_levels.md)** builds this dungeon, with walls, traps, explosives and two types of enemy.
- **Note 15 - Orchestration and Game Flow** provides `FlowBehaviour` for the build phase, the waves and the end of the game.
- **Note 16 - State Machines and the State Pattern** provides the states for enemies that do more than walk, as in the Apply exercise.
- **GCA:** enemies that visibly respond to the player's choices are strong evidence for a **Gameplay System**. A rebuilder connected by channels supports **Systems Integration**, and a recorded before-and-after of a tuned trap cost supports **Iteration & Improvement**.

---

## Glossary

| Term | Definition |
|:-----|:-----------|
| Area cost | A multiplier applied to distance through a NavMesh area when planning a path |
| Carving | Cutting a hole in the NavMesh with a `NavMeshObstacle`, with no rebuild |
| Coalescing | Combining several requests into one piece of work |
| Mazing | Shaping an enemy's route by placing objects in its way |
| NavMesh area | A label on part of a NavMesh, with a cost |
| `NavMeshModifierVolume` | A component that marks a box of the NavMesh with an area when the surface is built |
| `NavMeshObstacle` | A component that makes agents steer around an object, or carves a hole for it |
| Path status | Whether a planned path reaches its destination: complete, partial or invalid |

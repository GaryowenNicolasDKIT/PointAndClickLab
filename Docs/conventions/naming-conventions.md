# Unity Asset Naming Conventions

> **Prerequisites:** Basic familiarity with the Unity Editor, the Project window, GameObjects, prefabs, and C# scripts.

## 1. Introduction

A consistent naming convention makes a Unity project easier to understand, navigate, maintain, and share with other developers.

Unity does not enforce a single naming convention for all assets. Instead, development teams establish conventions appropriate to their projects and ensure that everyone follows them.

This document proposes a naming convention suitable for undergraduate game development projects.

The objectives are to:

- Make asset types easy to identify.
- Give related assets consistent names.
- Improve project organisation.
- Make assets easier to find and search for.
- Support collaboration between programmers, artists, and designers.

For additional guidance, see [Unity's recommendations for organising a project](https://unity.com/how-to/organizing-your-project#code-standards-continued).

## 2. General Naming Rules

Follow these rules when naming assets in a Unity project.

1. **Use PascalCase** for asset names, with each word beginning with a capital letter.
2. **Avoid spaces** in asset and folder names.
3. **Use descriptive names** that communicate an asset's purpose.
4. **Use consistent prefixes** where they help identify asset types.
5. **Use consistent suffixes** to distinguish asset variants or provide additional information.
6. **Avoid meaningless names**, such as `NewObject`, `Test2`, or `Asset15`.
7. **Avoid revision names**, such as `PlayerFinal`, `PlayerFinal2`, or `PlayerFinalReallyFinal`. Use version control to manage revisions.

### Examples

| Avoid | Prefer |
|:--|:--|
| `new material` | `M_Env_Stone_Opaque` |
| `model3` | `SM_StoneWall` |
| `texture final` | `T_StoneWall_Albedo` |
| `player final 2` | `PF_Player` |
| `scene1` | `SCN_MainMenu` |
| `PlayerScript.cs` | `PlayerController.cs` |

## 3. Recommended Asset Naming Convention

Use the following general format:

`[Prefix]_[AssetName]_[Descriptor]_[Variant]`

Each element has a specific purpose.

| Element | Purpose | Example |
|:--|:--|:--|
| Prefix | Identifies the asset type. | `T` |
| Asset name | Identifies the asset or related asset group. | `StoneWall` |
| Descriptor | Provides additional information about the asset. | `Normal` |
| Variant | Distinguishes between related alternatives. | `01` |

For example:

`T_StoneWall_Normal`

This identifies a normal map texture associated with a stone wall.

Not every asset needs every element. Use the elements that make the name clearer.

Two asset groups use a different pattern:

- **C# scripts** follow C# naming conventions, with no prefix (Section 4.1).
- **Materials and Shader Graphs** are named by how they shade, not by the asset they are applied to (Section 6).

**Important:** These prefixes are conventions, not requirements imposed by Unity. Teams may use different prefixes or omit them entirely. Consistency within a project is more important than adopting a particular set of abbreviations.

## 4. Asset Naming Reference

The following table provides a recommended convention for common Unity assets.

| Asset type | Prefix | Example |
|:--|:--|:--|
| Scene | `SCN_` | `SCN_MainMenu` |
| C# script | None | `PlayerController.cs` |
| Prefab | `PF_` | `PF_EnemyGoblin` |
| Static 3D model | `SM_` | `SM_StoneWall` |
| Shader Graph | `SG_` | `SG_Env_Stone_Opaque` |
| Sub Graph | `SGS_` | `SGS_UV_Triplanar` |
| Shader (code) | `SH_` | `SH_FX_PBR_Add` |
| Material | `M_` | `M_Env_Stone_Opaque` |
| Material Variant | `MV_` | `MV_Env_Stone_Opaque` |
| Texture | `T_` | `T_StoneWall_Albedo` |
| Normal map | `T_` | `T_StoneWall_Normal` |
| Sprite | `SPR_` | `SPR_HealthBar` |
| Animation clip | `AN_` | `AN_PlayerWalk` |
| Animator Controller | `AC_` | `AC_Player` |
| Sound effect | `SFX_` | `SFX_DoorOpen` |
| Music | `MUS_` | `MUS_MainTheme` |
| ScriptableObject asset | `SO_` | `SO_Weapon_Pistol` |
| Particle system prefab | `PF_` | `PF_SmokeEffect` |
| UI asset | `UI_` | `UI_HealthBar` |
| Render Texture | `RT_` | `RT_SecurityCamera` |
| Physics Material | `PM_` | `PM_Ice` |
| Font asset | `FNT_` | `FNT_Heading` |
| Timeline asset | `TL_` | `TL_IntroSequence` |

These prefixes are a proposed project standard. Other naming conventions are equally valid if they are applied consistently.

### 4.1 C# Scripts

C# scripts should follow C# naming conventions rather than the asset prefix convention.

Use PascalCase for class names and ensure that a script's filename matches its principal class.

For example:

```csharp
public class PlayerController : MonoBehaviour
{
}
```

The corresponding filename is:

`PlayerController.cs`

Avoid names such as:

- `playercontroller.cs`
- `player_controller.cs`
- `script1.cs`
- `PlayerScript.cs`

Name a script by its role, not by the fact that it is a script. Keep one public class per file. Avoid generic suffixes such as `Manager`, `Handler`, and `Helper` unless the class genuinely coordinates other objects.

| Purpose | Pattern | Example |
|:--|:--|:--|
| Main controller for an entity | `<Entity>Controller` | `PlayerController`, `EnemyController` |
| Single responsibility component | `<Entity><Concern>` | `PlayerHealth`, `EnemyMovement` |
| Trigger or interaction zone | `<Purpose>Zone` / `<Purpose>Trigger` | `DamageZone`, `CheckpointTrigger` |
| Holds a reference to shared data | `<Data>Holder` / `<Data>Provider` | `PlayerDataHolder` |
| Scene-wide coordinator | `<Domain>Manager` | `AudioManager`, `GameStateManager` |
| UI component | `<Element>UI` / `<Element>View` | `HealthBarUI`, `InventoryView` |

Avoid class names that collide with Unity types, such as `Camera`, `Light`, or `Input`. They cause ambiguous references.

For additional guidance, see [Unity's naming and code style recommendations](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity).

### 4.2 Class Members

| Item | Convention | Example |
|:--|:--|:--|
| Private serialised field | camelCase | `[SerializeField] private float moveSpeed;` |
| Public property | PascalCase | `public float MoveSpeed => moveSpeed;` |
| Method | PascalCase, verb first | `ApplyDamage()`, `SpawnEnemy()` |
| Boolean | `is` / `has` / `can` prefix | `isGrounded`, `hasKey`, `canJump` |
| Event | PascalCase, past tense or `On` prefix | `Died`, `OnHealthChanged` |
| Constant | PascalCase | `const int MaxLives = 3;` |

Prefer `[SerializeField] private` over `public` fields. Expose values through read-only properties.

### 4.3 ScriptableObjects

A ScriptableObject has two names: the C# class that defines it, and each asset created from it. The class follows the C# rules in Section 4.1. Each asset takes the `SO_` prefix.

| Item | Pattern | Example |
|:--|:--|:--|
| Class (data definition) | `<Thing>Data` | `WeaponData`, `PlayerData` |
| Class (configuration) | `<Thing>Config` / `<Thing>Settings` | `DifficultyConfig`, `AudioSettings` |
| Class (event channel) | `<Payload>EventChannel` | `VoidEventChannel`, `IntEventChannel` |
| Class (runtime set) | `<Thing>RuntimeSet` | `EnemyRuntimeSet` |
| Asset | `SO_<Thing>_<Variant>` | `SO_Weapon_Pistol`, `SO_Difficulty_Hard` |

Do not add an `SO` suffix to the class name. The asset prefix already identifies it, and `SO_PlayerDataSO` is redundant.

`CreateAssetMenu` paths should mirror the folder structure:

```csharp
[CreateAssetMenu(fileName = "SO_Weapon_New", menuName = "Game/Data/Weapon")]
public class WeaponData : ScriptableObject
{
}
```

## 5. Naming Related Assets

Related assets should share a common base name.

This makes it easier to identify which assets belong to a particular character, environment, or object.

For example, a player character might have the following assets:

```text
PF_Player
SM_Player
T_Player_Albedo
T_Player_Normal
AN_PlayerIdle
AN_PlayerWalk
AN_PlayerRun
AC_Player
```

The shared name `Player` identifies the related assets.

The prefixes and suffixes distinguish their individual purposes.

Materials are the exception. The player's material is `M_Char_Skin_Subsurface` or `M_Char_PBR_Opaque`, named by how it shades (Section 6). It is linked to the player by being stored in the player's folder, for example `Characters/Player/Materials/`.

### 5.1 Texture Descriptors

Textures often require additional descriptors because a model may use several texture maps.

| Texture type | Example |
|:--|:--|
| Albedo or base colour | `T_StoneWall_Albedo` |
| Normal map | `T_StoneWall_Normal` |
| Ambient occlusion | `T_StoneWall_AO` |
| Metallic map | `T_StoneWall_Metallic` |
| Roughness map | `T_StoneWall_Roughness` |
| Emission map | `T_StoneWall_Emission` |

Use descriptors that match the texture's actual purpose.

The exact terminology may depend on the rendering pipeline, shader, or asset import workflow.

Textures keep the asset-based pattern because a texture set belongs to one asset, whereas a material can be shared by many.

## 6. Materials and Shader Graphs

Materials and Shader Graphs use a three-field pattern: **Domain**, **Surface**, and **Usage**. This matches the convention used for Unreal Engine materials, so names read the same in both engines.

- **Shader Graph:** `SG_<Domain>_<Surface>_<Usage>`
- **Material:** `M_<Domain>_<Surface>_<Usage>`
- **Material Variant:** `MV_<Domain>_<Surface>_<Usage>`

A material normally shares the three fields of the Shader Graph it uses.

Do not add asset names, colours, seasons, or quality levels as extra fields. Store specific materials in folders named for the asset, and handle differences through parameters and Material Variants.

| Unity | Unreal equivalent |
|:--|:--|
| `SG_` Shader Graph | `MM_` Master Material |
| `M_` Material | `MI_` Material Instance |
| `MV_` Material Variant | `MI_` child instance |
| `SGS_` Sub Graph | `MF_` Material Function |

### 6.1 Field Glossary

Pick one value per field.

**Domain** identifies where the material is used:

`Env` (environment), `Prop`, `Char`, `Foliage`, `FX`, `Decal`, `UI`, `Terrain`

**Surface** identifies what the material represents:

`PBR` (generic), `Metal`, `Wood`, `Stone`, `Glass`, `Water`, `Skin`, `Cloth`, `Leaf`

**Usage** identifies how the material shades:

| Usage | Unity setting (URP) |
|:--|:--|
| `Opaque` | Surface Type: Opaque |
| `Masked` | Surface Type: Opaque, Alpha Clipping on |
| `Trans` | Surface Type: Transparent, Blending Mode: Alpha |
| `Add` | Surface Type: Transparent, Blending Mode: Additive |
| `Subsurface` | HDRP Lit with Subsurface Scattering (no native URP equivalent) |
| `Decal` | Shader Graph Decal target, used with a Decal Projector |

### 6.2 Examples

| Shader Graph | Material |
|:--|:--|
| `SG_Env_PBR_Opaque` | `M_Env_PBR_Opaque` |
| `SG_Env_Stone_Opaque` | `M_Env_Stone_Opaque` |
| `SG_Foliage_Leaf_Masked` | `M_Foliage_Leaf_Masked` |
| `SG_Env_Water_Trans` | `M_Env_Water_Trans` |
| `SG_Char_Skin_Subsurface` | `M_Char_Skin_Subsurface` |
| `SG_Prop_Wood_Opaque` | `M_Prop_Wood_Opaque` |
| `SG_FX_PBR_Add` | `M_FX_PBR_Add` |

Features such as glow, dissolve, or triplanar mapping are properties or keywords inside the Shader Graph, not name fields. A glowing platform is `M_Prop_PBR_Opaque` with its `_Glow` property enabled, stored in the platform's folder.

### 6.3 Material Variants

- Create a Material Variant when only a few values differ from a parent material. Changes to the parent then propagate to the variant automatically.
- The variant keeps the parent's three fields. Store it in a folder named for the asset or variant, for example `Environments/Dungeon/Materials/Mossy/`.
- Duplicate a material only when it needs a different Shader Graph.

### 6.4 Quick Check

- Can you tell **where** it is used (Domain)?
- Can you tell **what** it is (Surface)?
- Can you tell **how** it shades (Usage)?

If the answer to all three is yes, the name is good.

## 7. Shader Graph Properties, Keywords, and Sub Graphs

### 7.1 Properties

Each Shader Graph property has a display name, which appears in the Inspector, and a reference name, which scripts use. These are independent. Changing the display name does not change the reference name, so always set the reference name deliberately.

| Item | Display name | Reference name |
|:--|:--|:--|
| Colour | `Base Colour` | `_BaseColor` |
| Texture | `Base Map` | `_BaseMap` |
| Float | `Glow Intensity` | `_GlowIntensity` |
| Boolean | `Glow` | `_Glow` |
| Vector | `Scroll Speed` | `_ScrollSpeed` |

- Reference names use an underscore prefix followed by PascalCase.
- Match URP's built-in names where the meaning matches, such as `_BaseColor`, `_BaseMap`, `_NormalMap`, `_EmissionColor`, and `_Smoothness`. Fallback materials, the SRP Batcher, and Inspector tooling then behave consistently.
- Keep the US spelling `Color` in reference names to match Unity's API. UK spelling is fine in display names.
- Group related properties using Blackboard categories, such as Surface, Glow, and Animation.
- Never leave auto-generated reference names such as `_Float_3A9C...`. Scripts that target them break as soon as the property is recreated.

### 7.2 Keywords

| Keyword type | Convention | Example |
|:--|:--|:--|
| Boolean keyword | `_<FEATURE>_ON` | `_GLOW_ON` |
| Enum keyword | `_<FEATURE>_<OPTION>` | `_MODE_ADDITIVE`, `_MODE_MULTIPLY` |

- Use upper snake case for all keyword reference names.
- Use Shader Feature for options set only in the editor. Use Multi Compile for options toggled at runtime, otherwise the variant may be stripped from builds.
- Prefer a Boolean property over a keyword for simple runtime toggles. Every keyword doubles the number of shader variants.

### 7.3 Sub Graphs

Name Sub Graphs as `SGS_<Category>_<Operation>`, matching the Unreal `MF_` pattern.

```text
SGS_UV_Triplanar
SGS_UV_ScreenSpace
SGS_Math_RemapRange
SGS_Lighting_Fresnel
```

Use plain words for input and output port names, such as `UV`, `Strength`, and `Out`.

Store Sub Graphs in a shared folder so they are found and reused rather than duplicated.

## 8. Naming Variants

Use numbered suffixes when several assets represent variations of the same object.

For example:

```text
SM_Rock_01
SM_Rock_02
SM_Rock_03
```

This is appropriate when the models represent different variations of the same type of rock.

However, do not use numbers simply to distinguish unrelated objects.

For example:

```text
SM_Rock_01
SM_Tree_01
SM_Barrel_01
```

These names communicate the purpose of each model more effectively.

Use meaningful descriptors when the differences between variants are important.

For example:

```text
SM_Wall_Stone
SM_Wall_Brick
SM_Wall_Wood
```

For materials, the Surface field already carries this distinction (`M_Env_Stone_Opaque`, `M_Env_Wood_Opaque`). Variants within the same surface are handled by Material Variants and folders (Section 6.3).

## 9. Asset Names and GameObject Names

Asset filenames and GameObject names in the Hierarchy serve different purposes.

An asset filename identifies a file in the Project window. A GameObject name identifies an object in a scene.

These names do not need to follow exactly the same convention.

For example, a prefab might be named:

```text
PF_EnemyGoblin
```

When instantiated in a scene, the GameObject could be named:

```text
EnemyGoblin
```

The prefix is useful for identifying the prefab asset, but may be unnecessary when identifying the instance in the Hierarchy.

Use descriptive GameObject names so that another developer can understand the scene structure.

## 10. Folder Organisation

Naming conventions should complement a sensible folder structure.

For small projects, the following structure provides a useful starting point:

```text
Assets/
├── Animations/
├── Audio/
│   ├── Music/
│   └── SoundEffects/
├── Data/            (ScriptableObject assets)
├── Materials/
├── Models/
├── Prefabs/
├── Scenes/
├── Scripts/
│   └── Data/        (ScriptableObject classes)
├── Shaders/
│   ├── Graphs/
│   └── SubGraphs/
├── Textures/
└── UI/
```

For larger projects, consider grouping assets by gameplay system, character, or environment.

For example:

```text
Assets/
├── Characters/
│   └── Player/
│       ├── Animations/
│       ├── Materials/
│       ├── Models/
│       └── Textures/
├── Environments/
│   └── Dungeon/
│       ├── Materials/
│       ├── Models/
│       └── Textures/
├── Prefabs/
├── Scenes/
├── Scripts/
└── Shaders/
    ├── Graphs/
    └── SubGraphs/
```

In this structure the folders carry the asset association that material names deliberately leave out.

Choose a structure that suits the size and needs of the project.

Avoid creating unnecessarily deep folder hierarchies for small projects.

## 11. Common Pitfalls

| Pitfall | Fix |
|:--|:--|
| Renaming a script file without renaming its class, or the reverse, breaks the component reference. | Rename both together, ideally using the IDE's rename refactoring. |
| ScriptableObject assets holding mutable runtime state share that state across every object that references them, and the changes persist in the editor after Play mode. | Copy the values in `Awake`, or use `Instantiate(data)` to work on a clone. |
| Adding asset or variant names to material names, such as `M_Env_Stone_Opaque_Mossy`. | Use a folder instead. |
| Encoding shader features in names, such as `SG_Prop_PBR_Opaque_Glow`. | Glow is a property, not a name field. |
| A material's Usage field no longer matches its Shader Graph's Surface Type. | Rename the material whenever its Surface Type changes. |
| Calling `renderer.material` at runtime creates a material instance per object. | Use `sharedMaterial` for edits that apply to every object, or a `MaterialPropertyBlock` for per-object values. |

## 12. Practical Checklist

Before committing assets to version control, check the following.

- [ ] Asset names follow the project's naming convention.
- [ ] Names are descriptive and meaningful.
- [ ] Spaces have been avoided in filenames.
- [ ] Prefixes and suffixes are used consistently.
- [ ] Related assets share a common base name.
- [ ] Variants are clearly distinguished.
- [ ] C# filenames match their principal class names.
- [ ] Scripts are named by role, not with a `Script` suffix.
- [ ] Materials and Shader Graphs follow the Domain, Surface, Usage pattern.
- [ ] Shader Graph reference names have been set by hand.
- [ ] Assets are stored in appropriate folders.
- [ ] Temporary files and unnecessary duplicates have been removed.

## 13. Summary

A good naming convention makes a Unity project easier to understand and maintain.

Remember the following principles:

1. Use consistent naming rules throughout the project.
2. Prefer descriptive names over generic or abbreviated names.
3. Use prefixes where they make asset types easier to identify.
4. Give related assets a common base name.
5. Name scripts by role and materials by how they shade.
6. Use meaningful descriptors and variant suffixes.
7. Organise assets into folders that support the project's workflow.
8. Follow established project conventions when working with an existing codebase.

The purpose of a naming convention is not to make filenames complicated. It is to make the project easier for every team member to understand.

## References

- [Unity: Best practices for organising your project](https://unity.com/how-to/organizing-your-project#code-standards-continued)
- [Unity: Naming and code style tips for C# scripting](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity)
- [Unity Learn: Unity Tips](https://learn.unity.com/tutorial/unity-tips?version=2022.2)
- [Unity Style Guide: Asset Naming Conventions](https://github.com/Calvin-Davidson/unity_styleguide/blob/main/unity-engine/Asset-Naming.md)

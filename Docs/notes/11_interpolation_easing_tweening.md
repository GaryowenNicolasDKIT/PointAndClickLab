---
title: "11 - Interpolation, Easing and Tweening"
doc-type: "note"
module-code: "COMP I8014"
module-title: "3D Game Engine Development"
programme: "BSc (Hons) in Computing in Games Development"
stage: 3
institution: "Dundalk Institute of Technology (DkIT)"
author: "Niall McGuinness"
prerequisites: ["02_scriptable_objects.md", "04_event_channels.md", "06_selection_systems.md", "08_collision_detection.md"]
feeds_into: ["GCA - Technical Craft - Craft Example 1", "GCA - C# & Software Design - Software Design"]
last-updated: "2026-11-10"
---

# 11 - Interpolation, Easing and Tweening

Lab: [Lab 11 - Eased Selection and a Tweened Door](../labs/lab_11_eased_selection_and_door.md)

## Key Concepts

1. **Interpolation answers "where between A and B?"** - `Lerp` turns a fraction from 0 to 1 into a value between two endpoints.
2. **Tracking and transitions are different problems** - a value chasing a moving target needs smoothing; a move with a known end and duration needs a timed transition.
3. **`Lerp` with `rate * Time.deltaTime` depends on frame rate** - use `1 - Mathf.Exp(-rate * Time.deltaTime)` so the result depends only on elapsed time.
4. **Easing reshapes time, not distance** - the endpoints stay the same; a function changes how quickly the value travels between them.
5. **`AnimationCurve` is an easing function a designer can draw** - the shape becomes data in the Inspector.
6. **A tween owns the bookkeeping** - start, end, elapsed time, easing and completion live in one object, so your component states only the intent.
7. **Every tween needs an interruption rule** - decide what happens if the player changes their mind halfway, then kill the old tween before starting the new one.
8. **Never read a resting value from a value being tweened** - store the resting value once, or each interruption shifts it.
9. **Tie a tween to the object it animates** - `SetLink` stops it when that object is destroyed, including on a scene unload.
10. **Timing belongs in data** - a `TweenProfile` asset lets a designer tune feel in one place and reuse it, without editing the component.

---

## 1. Framing Question

Your door works: the player walks up, and it disappears. Your selected object works: it changes instantly to a bigger size. Nothing is broken, but nothing feels made with care. How do you turn an instant change into a deliberate one, without filling every component with timers?

Think of a menu or a door from a game you like. Does it start quickly and settle slowly, or the reverse? What would it feel like if it moved at a constant speed?

---

## 2. What You Will Learn

| Skill | You can... |
|:------|:-----------|
| Explain frame-rate dependent smoothing | Show with numbers why `Lerp(a, b, rate * Time.deltaTime)` differs between 20 and 60 FPS, and fix it |
| Choose an approach | State whether a problem needs tracking or a timed transition |
| Apply easing | Pick an easing function for a door, a highlight and a UI panel, and give the reason |
| Use `AnimationCurve` | Evaluate a curve in code and tune its shape in the Inspector |
| Use DOTween | Create, configure and kill a tween, and run code when it completes |
| Handle interruption | Keep a tweened object correct when the player reverses an action halfway |
| Separate timing from behaviour | Move duration and easing into a reusable `TweenProfile` asset |
| Extend without editing | Add an eased response to `SelectionManager` and a door to `ContactTrigger` without changing either |

---

## 3. Why This Matters

A player reads motion before they read anything else. An object that eases into place reads as physical and responsive. One that snaps reads as a state change in a spreadsheet, even when the logic behind it is correct. This is the difference between **functional** and **refined**.

The GCA gives **Technical Craft** 20% and **Iteration & Improvement** 20%. A door that snapped open in your first version and now opens with tuned easing, recorded before and after, is direct evidence for both. It costs little code, and that is the point: craft comes from timing and shape, not from expensive assets.

There is also a design problem. A component that tweens by hand needs fields for its start, end, timer and easing, plus logic for what happens if the player interrupts it. Repeat that across ten components and most of your code is timers. A tweening library removes that repetition, but only if you understand what it does for you, so this note builds the idea by hand first.

---

## 4. Core Ideas

### 4.1 Interpolation

**Interpolation** finds a value between two endpoints from a fraction `t`. `Mathf.Lerp(a, b, t)` returns `a + (b - a) * t`, with `t` clamped between 0 and 1. `Vector3.Lerp` and `Color.Lerp` do the same for each component.

```csharp
float halfway = Mathf.Lerp(10f, 20f, 0.5f);   // 15
float clamped = Mathf.Lerp(10f, 20f, 1.5f);   // 20, because t is clamped to 1
float over = Mathf.LerpUnclamped(10f, 20f, 1.5f);   // 25
```

`Lerp` has no memory and no sense of time. Everything that makes motion smooth comes from how you choose `t`, and there are two common ways to choose it.

### 4.2 Tracking and Frame-Rate Independent Smoothing

**Tracking** means a value follows a target that may keep moving, so there is no fixed end time. A health bar's displayed value and a follow camera both track. The usual form moves a fraction of the remaining gap each frame:

```csharp
// Common, and frame-rate dependent.
displayed = Mathf.Lerp(displayed, target, rate * Time.deltaTime);
```

The Starter Assets use this form for `_speed` and `_animationBlend` (Note 09, Section 4.6). It looks correct because it contains `Time.deltaTime`, but multiplying by `deltaTime` only corrects a straight-line change. Here the gap shrinks by a percentage each frame, so the percentages compound, and compounding depends on how many steps you take.

With `rate` 10, this is how much of the gap remains after 0.1 s:

| Frame rate | `t` per frame | Frames in 0.1 s | Gap remaining |
|:--|:--|:--|:--|
| 30 FPS | 0.333 | 3 | 29.6% |
| 60 FPS | 0.167 | 6 | 33.5% |
| 144 FPS | 0.069 | 14.4 | 35.5% |
| Exact (continuous) | - | - | 36.8% |

Worse, when `rate * Time.deltaTime` reaches 1, `t` clamps and the value snaps to the target in one frame, so at 10 FPS this smoothing disappears. Your feel values were tuned on one machine and change on another.

The fix uses the exponential, because exponents add across steps:

```csharp
// Same result after the same time, at any frame rate.
displayed = Mathf.Lerp(displayed, target, 1f - Mathf.Exp(-rate * Time.deltaTime));
```

`Mathf.Exp(-rate * dt)` is the fraction of the gap left after `dt`. Two frames of 8 ms leave `exp(-rate * 0.008) * exp(-rate * 0.008)`, which equals `exp(-rate * 0.016)`, exactly what one 16 ms frame leaves. So how the time is split into frames no longer matters. Use this in your own code; do not edit it into the Starter Assets, for the reasons in Note 09, Section 4.10.

For tracking with a sense of velocity, Unity also provides `Mathf.SmoothDamp` and `Vector3.SmoothDamp`, which are frame-rate independent and take a smoothing time in seconds. The third person controller uses the angle version for turning.

### 4.3 Timed Transitions

A **timed transition** has a known start, end and duration, such as a door opening in 0.6 s. Here `t` is simply elapsed time over duration:

```csharp
elapsed += Time.deltaTime;
float t = Mathf.Clamp01(elapsed / duration);
door.localPosition = Vector3.Lerp(closedPosition, openPosition, t);
```

This is frame-rate independent already, because `t` comes from total elapsed time and nothing compounds. It also moves at constant speed, which is **linear** motion and rarely what you want.

| Problem | Approach | Example |
|:--|:--|:--|
| Target keeps changing, no end time | Tracking (Section 4.2) | Displayed health following real health |
| Known endpoints and duration | Timed transition | Door, panel slide, selection scale |

### 4.4 Easing

**Easing** applies a function to `t` before interpolating. It changes how quickly the value travels, not where it starts or ends:

```csharp
float t = Mathf.Clamp01(elapsed / duration);

// Ease out: starts fast, settles slowly.
float eased = 1f - (1f - t) * (1f - t);

door.localPosition = Vector3.LerpUnclamped(closedPosition, openPosition, eased);
```

`LerpUnclamped` matters as soon as an easing function goes past 1. A clamped `Lerp` would flatten the overshoot.

| Family | Shape | Reads as | Typical use |
|:--|:--|:--|:--|
| Linear | Constant speed | Mechanical | Conveyor belts, progress bars |
| Ease out (`OutCubic`) | Fast, then settles | Responsive: reacts now, finishes softly | Things responding to the player, panels arriving |
| Ease in (`InCubic`) | Slow, then fast | Building up, leaving | Panels leaving, things falling |
| Ease in and out (`InOutSine`) | Slow at both ends | Heavy, deliberate | Large doors, lifts |
| Overshoot (`OutBack`) | Passes the target and returns | Playful, springy | Pickups, buttons, selection |
| Bounce and elastic (`OutBounce`, `OutElastic`) | Several oscillations | Very playful, and tiring if overused | Occasional rewards |

As a starting rule, use ease out for anything the player has just caused, because the movement starts on the frame of the input and so feels immediate. Then test it: this table is a starting point, and your playtest is the evidence.

### 4.5 AnimationCurve

An **`AnimationCurve`** is an easing function drawn in the Inspector and stored as data. `Evaluate(t)` returns the curve's height at `t`:

```csharp
[SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

private void ApplyEase(float t)
{
    door.localPosition = Vector3.LerpUnclamped(closedPosition, openPosition, ease.Evaluate(t));
}
```

A designer can add a slight overshoot or a hesitation without asking you for a new formula. Saved presets in the curve editor can be reused across components. Curves also work for anything over time, such as the spin-up of a turret or light intensity over a pulse, which is how one enemy can ease in while another snaps: give each its own curve.

### 4.6 What Hand-Written Tweens Cost

Everything so far works. The cost shows when you count what one component needs:

| You must write | Why |
|:--|:--|
| Fields for start, end, elapsed time and duration | The transition's state lasts across frames |
| An `Update` that advances the timer | Unity will not do it for you |
| A running flag or a null check | So it stops when finished |
| Restart logic | So an interrupted transition starts from the current value |
| One set of all this per value and per object | One set of fields only tracks one transition at a time |

Section 5 shows that last row becoming a visible bug.

### 4.7 Tweens and DOTween

A **tween** is an object that holds one timed transition and advances itself each frame: target, end value, duration, easing and what to do on completion. **DOTween** is a widely used free tweening library for Unity. A central manager updates every active tween, so your component has no `Update` at all.

```csharp
using DG.Tweening;

// Scale this object to 1.2 over 0.25 seconds, overshooting slightly.
transform.DOScale(1.2f, 0.25f).SetEase(Ease.OutBack);
```

`DOScale` is an extension method on `Transform` that creates and starts a tween. It starts from the current value, which removes the restart logic from Section 4.6. Each `Set...` method returns the tween, so calls chain. This style is called a **fluent interface**, and Note 15 uses the same style when it builds a sequence of game events.

| Call | Purpose |
|:--|:--|
| `transform.DOLocalMove(position, duration)` | Tween local position |
| `transform.DOScale(scale, duration)` | Tween scale |
| `light.DOIntensity(value, duration)` | Tween a `Light` |
| `canvasGroup.DOFade(alpha, duration)` | Tween UI transparency |
| `DOTween.To(() => x, value => x = value, end, duration)` | Tween anything, through a getter and a setter |
| `.SetEase(Ease.OutCubic)` or `.SetEase(curve)` | Choose a named ease or an `AnimationCurve` |
| `.SetDelay(seconds)` | Wait before starting |
| `.SetLoops(-1, LoopType.Yoyo)` | Repeat forever, back and forth |
| `.SetUpdate(true)` | Ignore `Time.timeScale`, so UI still animates while the game is paused |

### 4.8 Interruption and Lifetime

Two rules prevent most tween bugs.

**Kill before you start.** Two tweens on the same property both write to it every frame, and the object stutters between them. Kill the old one first. `DOKill` kills every tween whose target is this object:

```csharp
transform.DOKill();
transform.DOLocalMove(openPosition, 0.6f).SetEase(Ease.OutCubic);
```

`DOKill` stops every tween on that `Transform`, including one started by another component. If two components animate one object, use a separate child object for each, or keep a reference to your own tween.

**Link the tween to its object.** Tweens live in DOTween's manager, not on your GameObject, so one can outlive the object it animates, for example when an additive scene unloads (Note 05). `SetLink` kills the tween when the GameObject is destroyed:

```csharp
transform.DOLocalMove(openPosition, 0.6f).SetLink(gameObject);
```

DOTween's safe mode catches most of these cases and logs a warning, but relying on it hides the mistake. `SetLink` states the intent.

### 4.9 Callbacks, Delegates and Action

Often something must happen when a tween ends: enable a collider, or tell the next system. `OnComplete` takes a method to call later:

```csharp
transform.DOLocalMove(openPosition, 0.6f)
    .OnComplete(() => doorCollider.enabled = false);
```

That argument works because a **delegate** is a variable that holds a method. `Action` is C#'s built-in delegate for a method with no return value, and `Action<T>` takes one parameter. You already use one: `EventChannel<T>` in `Assets/GD/Common` declares `event Action<T> Raised`. The `() => ...` syntax is a **lambda**, a small unnamed method written in place.

Your own methods can take a callback too, which lets the caller decide what happens next:

```csharp
using System;
using DG.Tweening;
using UnityEngine;

public class PanelSlider : MonoBehaviour
{
    [SerializeField] private Vector3 hiddenPosition = new(0f, -600f, 0f);

    public void Hide(Action onHidden = null)
    {
        transform.DOKill();
        transform.DOLocalMove(hiddenPosition, 0.3f)
            .SetEase(Ease.InCubic)
            .SetLink(gameObject)
            .OnComplete(() => onHidden?.Invoke());
    }
}
```

`?.Invoke()` calls the delegate only if one was passed. Use a callback when one caller needs to hear about the end of one action. Use an event channel (Note 04) when several unrelated systems might.

### 4.10 Moving Timing into Data

A duration and an easing are design values, and they get tuned far more often than the code changes. Note 02 separated data from behaviour with ScriptableObjects, and the same idea applies. A **tween profile** is an asset that holds timing and knows how to apply it to any tween. It lives in `Assets/GD/Common/Scripts/ScriptableObjects/Types/Tweens`, so every later note can use it:

```csharp
using DG.Tweening;
using UnityEngine;

namespace GD.Types
{
    /// <summary>
    /// Reusable tween timing: a duration, a delay and either a named ease or a drawn curve.
    /// </summary>
    /// <remarks>
    /// The asset holds behaviour as well as data, because <see cref="Apply{T}"/> decides how the
    /// settings reach a tween. Code using a profile does not need to know which ease it holds.
    /// </remarks>
    [CreateAssetMenu(fileName = "TweenProfile", menuName = "GD/Tweens/Tween Profile")]
    public class TweenProfile : ScriptableObject
    {
        #region Fields

        [SerializeField, Min(0f)]
        [Tooltip("Seconds the tween takes")]
        private float duration = 0.25f;

        [SerializeField, Min(0f)]
        [Tooltip("Seconds to wait before it starts")]
        private float delay;

        [SerializeField]
        [Tooltip("Use the curve below instead of the named ease")]
        private bool useCurve;

        [SerializeField]
        private Ease ease = Ease.OutCubic;

        [SerializeField]
        private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        #endregion Fields

        #region Properties

        /// <summary>Seconds the tween takes. Pass this when you create the tween.</summary>
        public float Duration => duration;

        #endregion Properties

        #region Core Methods

        /// <summary>Applies this profile's delay and easing, and returns the tween for chaining.</summary>
        public T Apply<T>(T tween) where T : Tween
        {
            tween.SetDelay(delay);
            return useCurve ? tween.SetEase(curve) : tween.SetEase(ease);
        }

        #endregion Core Methods
    }
}
```

`Apply<T>` is generic so the caller gets back the same type it passed in, and can keep chaining. The constraint `where T : Tween` means it accepts any DOTween tween. The asset holds behaviour as well as data, because `Apply` decides how the settings reach the tween. A component using it does not need to know whether easing came from an enum or a curve, so a new kind of profile needs no change to the component.

---

## 5. Worked Example

The demo eases the selection response from Note 06. The design decision comes first: `SelectionManager` already calls every `ISelectionResponse` on its GameObject, so the new behaviour is one new response class. `SelectionManager` and `HighlightSelectionResponse` in `Assets/GD/Common` do not change. That is the open/closed principle from Note 06 paying off.

Each stage below replaces the one before. The instant version is shown first, because you cannot judge a refinement without its baseline.

### Stage 1 - Initial Implementation: Instant

```csharp
using GD;
using UnityEngine;

public class InstantScaleSelectionResponse : MonoBehaviour, ISelectionResponse
{
    [SerializeField, Min(0f)] private float selectedScale = 1.2f;

    private Vector3 restingScale;

    public void OnSelect(Transform selection)
    {
        restingScale = selection.localScale;
        selection.localScale = restingScale * selectedScale;
    }

    public void OnDeselect(Transform selection)
    {
        selection.localScale = restingScale;
    }
}
```

It works, and it pops. The state change is clear, but it has no feel.

### Stage 2 - Problem: Easing by Hand

Using Sections 4.3 to 4.5, the same response eases with an `AnimationCurve` and its own timer:

```csharp
using GD;
using UnityEngine;

public class HandEasedScaleSelectionResponse : MonoBehaviour, ISelectionResponse
{
    [SerializeField, Min(0f)] private float selectedScale = 1.2f;
    [SerializeField, Min(0.01f)] private float duration = 0.25f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 restingScale;
    private Transform target;
    private Vector3 from;
    private Vector3 to;
    private float elapsed;

    public void OnSelect(Transform selection)
    {
        restingScale = selection.localScale;
        StartEase(selection, restingScale * selectedScale);
    }

    public void OnDeselect(Transform selection)
    {
        StartEase(selection, restingScale);
    }

    private void StartEase(Transform newTarget, Vector3 newScale)
    {
        target = newTarget;
        from = newTarget.localScale;
        to = newScale;
        elapsed = 0f;
    }

    private void Update()
    {
        if (target == null)
            return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        target.localScale = Vector3.LerpUnclamped(from, to, ease.Evaluate(t));

        if (t >= 1f)
            target = null;
    }
}
```

It looks right when you select one object and look away. Then move directly from crate A to crate B:

| Observation | Cause |
|:--|:--|
| Crate A stays enlarged forever | `SelectionManager` calls `OnDeselect(A)` then `OnSelect(B)` in the same frame. `StartEase` overwrites `target`, so A's return is abandoned |
| Quickly looking away and back makes the crate grow a little more each time | `OnSelect` reads `localScale` as the resting scale while it is still mid-transition |
| The class is 45 lines, and most of them are timer state | Each new tweened value needs the same fields again |

The first row would need a list of running transitions, which starts to become a tween library.

### Stage 3 - Change: Tweens

Each DOTween tween keeps its own state, so A and B can animate at once, and the timer code disappears:

```csharp
using DG.Tweening;
using GD;
using UnityEngine;

public class FirstTweenScaleSelectionResponse : MonoBehaviour, ISelectionResponse
{
    [SerializeField, Min(0f)] private float selectedScale = 1.2f;
    [SerializeField, Min(0.01f)] private float duration = 0.25f;

    private Vector3 restingScale;

    public void OnSelect(Transform selection)
    {
        restingScale = selection.localScale;
        selection.DOScale(restingScale * selectedScale, duration).SetEase(Ease.OutBack);
    }

    public void OnDeselect(Transform selection)
    {
        selection.DOScale(restingScale, duration).SetEase(Ease.OutCubic);
    }
}
```

The abandoned crate is fixed, because each tween captured its own end value when it was created. Two problems remain. Reversing quickly leaves two tweens fighting over one `localScale`, because nothing kills the old one. And the crate still grows, because `OnSelect` still reads the resting scale from a value being tweened. Timing is also still hardcoded per component.

### Stage 4 - Improvement: Interruption Rules and Data

A finished copy of this class is in `Assets/GD/Common/Scripts/Selection/Responses`, in the `GD` namespace.

```csharp
using System.Collections.Generic;
using DG.Tweening;
using GD;
using GD.Types;
using UnityEngine;

/// <summary>
/// Eases each selected object to a larger scale and back again. Handles several objects
/// at once, and being interrupted halfway.
/// </summary>
public class TweenScaleSelectionResponse : MonoBehaviour, ISelectionResponse
{
    [SerializeField, Min(0f), Tooltip("Multiple of the object's resting scale when selected")]
    private float selectedScale = 1.2f;

    [SerializeField] private TweenProfile selectProfile;
    [SerializeField] private TweenProfile deselectProfile;

    // Each object's scale, recorded the first time it is selected, while it is at rest.
    private readonly Dictionary<Transform, Vector3> restingScales = new();

    public void OnSelect(Transform selection)
    {
        if (!restingScales.ContainsKey(selection))
            restingScales[selection] = selection.localScale;

        TweenScale(selection, restingScales[selection] * selectedScale, selectProfile);
    }

    public void OnDeselect(Transform selection)
    {
        if (restingScales.TryGetValue(selection, out Vector3 restingScale))
            TweenScale(selection, restingScale, deselectProfile);
    }

    private void TweenScale(Transform target, Vector3 scale, TweenProfile profile)
    {
        // Replace any running tween, so only one writes to this object's scale.
        target.DOKill();

        profile.Apply(target.DOScale(scale, profile.Duration))
            .SetLink(target.gameObject);
    }
}
```

The decisions in this class:

- **A dictionary of resting scales.** The first selection records the true size. Later selections never read a mid-tween value, so nothing grows.
- **`DOKill` before each tween.** The interruption rule is "the newest request wins, starting from wherever the object is now".
- **Two profiles.** Arriving and leaving rarely want the same feel: `OutBack` over 0.25 s to select and `OutCubic` over 0.15 s to deselect is a good first test. Designers can reuse these assets anywhere.
- **`SetLink`.** A crate destroyed mid-tween takes its tween with it. Its dictionary entry stays, which costs a few bytes in a small scene; in a game that spawns thousands of selectable objects, you would remove entries too.

| Problem | Solution | Benefit |
|:--|:--|:--|
| Hand-written easing tracks one transition and has its timing hardcoded | One tween per transition, killed on interruption, with timing in `TweenProfile` assets | Several objects animate correctly; designers tune feel without code; `SelectionManager` is untouched |

---

## 6. Common Pitfalls

| Symptom | Cause | Fix |
|:--------|:------|:----|
| `The type or namespace name 'DG' could not be found` | DOTween is missing from your copy of the project, or a script's assembly definition does not reference it | Pull the latest repository and run DOTween's setup; keep scripts outside assembly definitions, or add the reference |
| An object stutters or ends in the wrong place | Two tweens write to the same property | Call `DOKill` before starting a new one |
| An object slowly grows or drifts with repeated use | The start value is read from a value mid-tween | Store the resting value once |
| `MissingReferenceException` or a DOTween warning after loading a scene | A tween outlived its target | Add `SetLink(gameObject)` |
| An overshoot looks flat | A clamped `Lerp` cut it off | Use `LerpUnclamped` |
| Smoothing feels different on another machine | `rate * Time.deltaTime` used as `t` | Use `1f - Mathf.Exp(-rate * Time.deltaTime)` |
| UI freezes when the game is paused | Tweens use scaled time by default | Add `SetUpdate(true)` to UI tweens |
| A reversed door takes as long to move a small distance as a full one | The tween always uses the full duration | Accept it, or scale duration by the distance left (Challenge exercise) |
| A tween in `Update` never finishes | A new tween starts every frame | Start tweens from events and state changes, not every frame |
| `Material` tweens slowly create many material copies | `renderer.material` creates a copy for each object | Measure before committing; see the remarks on `HighlightSelectionResponse` |

---

## 7. Software Development Parallels

- **Open/closed principle.** `SelectionManager` and `ContactTrigger` gain new behaviour through a new class and an Inspector event, with no edit. This is the same principle as Note 06.
- **Strategy.** An easing function is a small algorithm chosen at runtime, and a `TweenProfile` asset selects one per use. The Challenge exercise takes this further with assets that hold a whole effect. Strategy has a cost: the behaviour is no longer visible in the component that uses it, so name your assets clearly.
- **Fluent interface.** Chained `Set...` calls appear in LINQ, in C# `StringBuilder` and in test frameworks. Note 15 builds its own.
- **Beyond games.** CSS `transition-timing-function` and Android's `Interpolator` classes are the same idea as `Ease`. Most named easing functions, including DOTween's, descend from Robert Penner's easing equations.
- **Commercial practice.** Jan Willem Nijman's talk *The Art of Screenshake* (Vlambeer) adds small feedback changes one at a time to a plain shooter, and shows how much of its feel comes from them, with no change to the rules.

---

## 8. Exercises

### Core - A Beacon That Fades

Note 01's `Beacon` pulsed a light using `Mathf.Sin` and faded it with a speed value. Write `LightPulse`: a component with public `StartPulse` and `StopPulse` methods. `StartPulse` loops `Light.DOIntensity` with `SetLoops(-1, LoopType.Yoyo)`. `StopPulse` kills the loop and fades to zero. Wire both to `ContactTrigger` from `Assets/GD/Common`, using `On Enter` and `On Exit`.

**Expected result:** the light pulses while the player stands in the trigger and fades out smoothly on leaving, including when the player leaves mid-pulse. `Update` does not appear in your class.

### Apply - A Screen Fade Driven by a Channel

Write `ScreenFader`, using `CanvasGroup.DOFade` on a full-screen black `Image`. Add `public void FadeOut(Action onFaded = null)` and `public void FadeIn()`. Use `VoidEventChannelListener` from `Assets/GD/Common` to call `FadeIn` when a `LevelReady` channel is raised. Set `Time.timeScale` to 0 and confirm the fade still works, using `SetUpdate(true)`.

**Expected result:** raising the channel from its context menu fades the screen in during Play mode, with the game paused or running. In 80 to 120 words, say which easing you chose for each direction and why, and when you would use a callback rather than a second channel.

### Challenge - Effects as Assets

A `TweenProfile` holds timing, but `TweenScaleSelectionResponse` still decides that the effect is a scale. Design an abstract ScriptableObject `SelectionEffect` with methods that play and reverse an effect on a `Transform`, and write at least two effects, such as a scale and a punch rotation. Then write one response that plays whichever effect asset it is given. Also make the duration of an interrupted tween proportional to the distance left.

**Expected result:** swapping one asset in the Inspector changes the effect with no code change. In 150 to 200 words, name the pattern and principle involved, and say what you lost by moving this behaviour out of the component. Compare the cost of the extra class hierarchy with one response class per effect, and say which you would use for a game with only two effects.

---

## 9. Reflective Questions

1. `Lerp(a, b, rate * Time.deltaTime)` contains `Time.deltaTime` and is still frame-rate dependent. Explain why, and say when the difference would be big enough to matter in your game.
2. Your group wants all doors to feel heavier next week. Compare the work needed with hardcoded durations and with a shared `TweenProfile`. Which principle explains the difference?
3. `DOKill` makes the newest request win. Describe a gameplay case where that rule is wrong, and what the rule should be instead.
4. `ContactTrigger` raises `On Enter`, and it knows nothing about doors. Justify that using the dependency inversion principle, then give one cost of wiring behaviour in the Inspector rather than in code.
5. Choose one interaction in your group's game. Which easing would you test first, and what would you record to show it improved?

---

## 10. How This Builds

- **[Lab 11](../labs/lab_11_eased_selection_and_door.md)** measures frame-rate dependent smoothing, adds an eased selection response, and adds a tweened door to `ContactTrigger`, all without editing `Assets/GD/Common`.
- **Note 12 - Character Animation** compares tweening with the Animator, and states when to use each.
- **Note 13 - AudioManager** tweens the music volume to duck it under other sounds.
- **Note 14 - CameraManager** tweens camera values for a field of view kick.
- **Note 15 - Orchestration and Game Flow** uses tweens as steps in a gameplay sequence, and compares DOTween's `Sequence` with coroutines.
- **GCA:** record the instant version of an interaction before you ease it. A before-and-after with your reasons for the easing chosen is evidence for **Technical Craft** and **Iteration & Improvement**, and `TweenProfile` is evidence for **Software Design**.

---

## Glossary

| Term | Definition |
|:-----|:-----------|
| `AnimationCurve` | A curve stored as data and drawn in the Inspector, read in code with `Evaluate` |
| Delegate | A variable that holds a method, so the method can be called later |
| DOTween | A free Unity library that creates and updates tweens |
| Easing | A function applied to `t` that changes how quickly a value travels between its endpoints |
| Fluent interface | A style in which each method returns the object, so calls can be chained |
| Functional | Working correctly, with no attention to how it feels |
| Interpolation | Finding a value between two endpoints from a fraction |
| Lambda | A small unnamed method written in place, such as `() => count++` |
| Linear | Moving at constant speed, with no easing |
| Refined | Working correctly and deliberately tuned for how it feels |
| Timed transition | A change with a known start, end and duration |
| Tracking | A value following a target that may keep moving, with no fixed end time |
| Tween | An object that holds one timed transition and advances itself each frame |
| Tween profile | An asset that holds a tween's timing and applies it to any tween |

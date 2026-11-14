using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace GD.Inputs
{
    /// <summary>
    /// Raises UnityEvents when a key, or a combination of keys, is pressed, held and released.
    /// One component handles one action; add the component again for another key or combination.
    /// </summary>
    /// <remarks>
    /// The action is owned by this component, so it is enabled and disabled here. For a combination,
    /// click the + beside Bindings in the Inspector and choose "Add Binding With One Modifier" (for
    /// example Ctrl + R) or "Add Binding With Two Modifiers" (for example Ctrl + Shift + R). Set the
    /// binding's Modifiers Order to Ordered if the modifiers must be pressed before the key, as in
    /// editor shortcuts; Unordered accepts them in any order. Left at Default, the order depends on
    /// the project's Input System settings. Several bindings on one action are alternatives: any one
    /// of them triggers it.
    /// </remarks>
    [AddComponentMenu("GD/Input/Input Action Events")]
    public class InputActionEvents : MonoBehaviour
    {
        #region Fields

        [SerializeField, Tooltip("The key or combination to watch. Add more bindings for alternatives, such as a gamepad button")]
        private InputAction action = CreateDefaultAction();

        [SerializeField, Tooltip("Count held time in real time, so it keeps counting while the game is paused")]
        private bool useUnscaledTime;

        [Header("Events")]
        [SerializeField, Tooltip("Called once, on the frame the key or combination is completed")]
        private UnityEvent onPressed;

        [SerializeField, Tooltip("Called every frame while held, including the frame it was pressed. Passes the seconds held so far")]
        private UnityEvent<float> onHeld;

        [SerializeField, Tooltip("Called once, on the frame the key or combination is broken, or when this component is disabled while held")]
        private UnityEvent onReleased;

        private bool isHeld;
        private float heldSeconds;

        #endregion Fields

        #region Properties

        /// <summary>True while the key or combination is held.</summary>
        public bool IsHeld => isHeld;

        /// <summary>Seconds the key or combination has been held, or 0 when it is not held.</summary>
        public float HeldSeconds => heldSeconds;

        #endregion Properties

        #region Unity Messages

        private void OnEnable()
        {
            action.Enable();
        }

        private void OnDisable()
        {
            action.Disable();

            // Release here, so a listener that started something on press is never left running.
            if (isHeld)
                Release();
        }

        private void Update()
        {
            if (!isHeld && action.WasPressedThisFrame())
            {
                isHeld = true;
                heldSeconds = 0f;
                onPressed.Invoke();
            }

            if (!isHeld)
                return;

            // Read IsPressed rather than WasReleasedThisFrame, so a release in a frame this
            // component missed, for example while the window lost focus, still ends the hold.
            if (!action.IsPressed())
            {
                Release();
                return;
            }

            onHeld.Invoke(heldSeconds);
            heldSeconds += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        #endregion Unity Messages

        #region Core Methods

        private void Release()
        {
            isHeld = false;
            heldSeconds = 0f;
            onReleased.Invoke();
        }

        // Used only when the component is first added, as an example of a combination.
        private static InputAction CreateDefaultAction()
        {
            var defaultAction = new InputAction("KeyCombination", InputActionType.Button);
            // modifiersOrder=1 is Ordered: Ctrl must be pressed before R, as in a shortcut.
            defaultAction.AddCompositeBinding("OneModifier(modifiersOrder=1)")
                .With("Modifier", "<Keyboard>/ctrl")
                .With("Binding", "<Keyboard>/r");
            return defaultAction;
        }

        #endregion Core Methods
    }
}

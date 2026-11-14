using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GD
{
    /// <summary>
    /// Takes control from, and returns it to, either Starter Assets controller, with no edit to it.
    /// </summary>
    [AddComponentMenu("GD/Character/Player Control Lock")]
    [RequireComponent(typeof(StarterAssetsInputs), typeof(PlayerInput))]
    public class PlayerControlLock : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");

        [SerializeField, Tooltip("The ThirdPersonController or FirstPersonController on this object")]
        private Behaviour controller;

        private StarterAssetsInputs input;
        private PlayerInput playerInput;
        private Animator animator;

        private void Awake()
        {
            input = GetComponent<StarterAssetsInputs>();
            playerInput = GetComponent<PlayerInput>();

            // First person characters have no Animator, so this may stay null.
            TryGetComponent(out animator);
        }

        public void SetControlEnabled(bool isEnabled)
        {
            controller.enabled = isEnabled;
            playerInput.enabled = isEnabled;

            // StarterAssetsInputs locks the cursor again on focus unless this is also set.
            input.cursorLocked = isEnabled;
            Cursor.lockState = isEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isEnabled;

            if (isEnabled)
                return;

            // A key held at this moment would otherwise keep its value until control returns.
            input.MoveInput(Vector2.zero);
            input.LookInput(Vector2.zero);
            input.JumpInput(false);
            input.SprintInput(false);

            if (animator != null)
                animator.SetFloat(SpeedId, 0f);
        }
    }
}

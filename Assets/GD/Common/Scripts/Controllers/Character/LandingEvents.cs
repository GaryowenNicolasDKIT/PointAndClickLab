using StarterAssets;
using UnityEngine;
using UnityEngine.Events;

namespace GD
{
    /// <summary>
    /// Raises events when the third person controller leaves the ground and when it lands,
    /// without editing the controller.
    /// </summary>
    /// <remarks>
    /// Reactions such as dust, audio and camera shake are wired in the Inspector, so this
    /// class does not know what a landing means for the game.
    /// </remarks>
    [AddComponentMenu("GD/Character/Landing Events")]
    [RequireComponent(typeof(ThirdPersonController))]
    public class LandingEvents : MonoBehaviour
    {
        #region Fields

        [SerializeField, Min(0f)]
        [Tooltip("Shorter times in the air, such as stepping off a kerb, do not count as a landing")]
        private float minimumAirTime = 0.3f;

        [SerializeField]
        [Tooltip("Called on the frame the character leaves the ground")]
        private UnityEvent onLeftGround;

        [SerializeField]
        [Tooltip("Called on the frame the character lands, after at least Minimum Air Time in the air")]
        private UnityEvent onLanded;

        private ThirdPersonController controller;
        private bool wasGrounded = true;
        private float airTime;

        #endregion Fields

        #region Unity Messages

        private void Awake()
        {
            controller = GetComponent<ThirdPersonController>();
        }

        private void LateUpdate()
        {
            // LateUpdate runs after the controller's Update, so Grounded is this frame's value.
            bool grounded = controller.Grounded;

            if (!grounded)
                airTime += Time.deltaTime;

            if (wasGrounded && !grounded)
                onLeftGround.Invoke();

            if (!wasGrounded && grounded)
            {
                if (airTime >= minimumAirTime)
                    onLanded.Invoke();

                airTime = 0f;
            }

            wasGrounded = grounded;
        }

        #endregion Unity Messages
    }
}

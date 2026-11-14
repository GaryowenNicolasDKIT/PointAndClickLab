using DG.Tweening;
using GD.Types;
using UnityEngine;

namespace GD.UI
{
    /// <summary>
    /// Fades a UI panel in and out. Uses real time, so it works while the game is paused.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("GD/UI/Screen Panel")]
    public class ScreenPanel : MonoBehaviour
    {
        [SerializeField] private TweenProfile showProfile;
        [SerializeField] private TweenProfile hideProfile;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public Tween Show()
        {
            return Fade(1f, showProfile, true);
        }

        public Tween Hide()
        {
            return Fade(0f, hideProfile, false);
        }

        private Tween Fade(float alpha, TweenProfile profile, bool isVisible)
        {
            // Only a visible panel may take clicks.
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;

            canvasGroup.DOKill();

            return profile.Apply(canvasGroup.DOFade(alpha, profile.Duration))
                .SetUpdate(true)
                .SetLink(gameObject);
        }
    }
}

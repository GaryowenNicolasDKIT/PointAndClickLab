using DG.Tweening;
using TMPro;
using UnityEngine;

namespace GD.UI
{
    /// <summary>
    /// Shows a short message on a ScreenPanel, then fades it. A new message replaces the current one.
    /// </summary>
    [AddComponentMenu("GD/UI/Toast View")]
    [RequireComponent(typeof(ScreenPanel))]
    public class ToastView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        [SerializeField, Min(0f), Tooltip("Seconds the message stays fully visible")]
        private float holdSeconds = 2f;

        private ScreenPanel panel;
        private Tween hideTimer;

        private void Awake()
        {
            panel = GetComponent<ScreenPanel>();
        }

        /// <summary>Wire to a StringEventChannelListener, as a dynamic string.</summary>
        public void Show(string message)
        {
            label.text = message;

            if (hideTimer != null && hideTimer.IsActive())
                hideTimer.Kill();

            panel.Show().OnComplete(StartHideTimer);
        }

        private void StartHideTimer()
        {
            // True ignores Time.timeScale, so a toast still fades while the game is paused.
            hideTimer = DOVirtual.DelayedCall(holdSeconds, () => panel.Hide(), true).SetLink(gameObject);
        }
    }
}

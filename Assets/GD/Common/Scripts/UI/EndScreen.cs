using DG.Tweening;
using TMPro;
using UnityEngine;

namespace GD.UI
{
    /// <summary>
    /// Fades in the end of game panel, with the title of the ending reached.
    /// </summary>
    [AddComponentMenu("GD/UI/End Screen")]
    [RequireComponent(typeof(ScreenPanel))]
    public class EndScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;

        private ScreenPanel panel;

        private void Awake()
        {
            panel = GetComponent<ScreenPanel>();
        }

        public Tween Show(string title)
        {
            titleLabel.text = title;
            return panel.Show();
        }
    }
}

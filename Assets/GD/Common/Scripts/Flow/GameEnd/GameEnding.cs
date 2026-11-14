using GD.Conditions;
using UnityEngine;

namespace GD.Flow
{
    /// <summary>
    /// One way the game can end: the rule, and what to tell the player.
    /// </summary>
    [CreateAssetMenu(fileName = "GameEnding", menuName = "GD/Conditions/Game Ending")]
    public class GameEnding : ScriptableObject
    {
        [SerializeField] private Condition condition;
        [SerializeField] private string title = "Game Over";

        [SerializeField, TextArea(1, 3), Tooltip("Shown briefly before the end screen")]
        private string toastMessage = "";

        public string Title => title;
        public string ToastMessage => toastMessage;

        public bool IsReached()
        {
            return condition != null && condition.IsMet();
        }
    }
}

using GD.Types;
using GD.UI;
using UnityEngine;

namespace GD.Flow
{
    /// <summary>
    /// Checks a list of endings in order, and plays the end of the game when the first is reached.
    /// </summary>
    [AddComponentMenu("GD/Flow/Game End Flow")]
    public class GameEndFlow : FlowBehaviour
    {
        [SerializeField, Tooltip("Checked from the top. The first reached wins")]
        private GameEnding[] endings;

        [SerializeField] private EndScreen endScreen;

        [Header("Channels")]
        [SerializeField] private BoolEventChannel controlEnabled;
        [SerializeField] private StringEventChannel toastRequested;
        [SerializeField] private VoidEventChannel gameEnded;

        [SerializeField, Min(0f), Tooltip("Seconds between the toast and the end screen")]
        private float toastSeconds = 1.5f;

        private GameEnding reached;

        private void Update()
        {
            if (reached != null)
                return;

            foreach (GameEnding ending in endings)
            {
                if (!ending.IsReached())
                    continue;

                reached = ending;
                Play();
                return;
            }
        }

        protected override FlowSequence BuildSequence()
        {
            // The ending is not known yet, so these steps read it when they run.
            return new FlowBuilder()
                .Raise(gameEnded)
                .Raise(controlEnabled, false)
                .Do(() => toastRequested.Raise(reached.ToastMessage))
                .Wait(toastSeconds)
                .Tween(() => endScreen.Show(reached.Title))
                .Build();
        }
    }
}

using GD.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GD.Flow
{
    /// <summary>
    /// Restarts or quits the game. Add every variable that holds game state, so that a new
    /// game does not start with the old game's values.
    /// </summary>
    [AddComponentMenu("GD/Flow/Game Session")]
    public class GameSession : MonoBehaviour
    {
        [SerializeField] private BoolVariable[] boolVariables;
        [SerializeField] private IntVariable[] intVariables;
        [SerializeField] private FloatVariable[] floatVariables;

        public void Restart()
        {
            foreach (BoolVariable variable in boolVariables)
                variable.ResetToInitial();

            foreach (IntVariable variable in intVariables)
                variable.ResetToInitial();

            foreach (FloatVariable variable in floatVariables)
                variable.ResetToInitial();

            // Time.timeScale survives a scene load, as the variables do.
            Time.timeScale = 1f;

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void Quit()
        {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
        }
    }
}

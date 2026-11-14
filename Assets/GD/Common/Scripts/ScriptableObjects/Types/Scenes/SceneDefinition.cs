using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
#endif

namespace GD.Types
{
    /// <summary>
    /// Names one scene, such as a level or the UI, as an asset. Code and event channels pass
    /// this asset instead of typing the scene name as a string in many places.
    /// </summary>
    /// <remarks>
    /// The scene name is typed once, here. In the Editor, OnValidate warns if no
    /// enabled scene in the build scene list has that name, so a typo shows up before Play.
    /// </remarks>
    [CreateAssetMenu(fileName = "SceneDefinition", menuName = "GD/Scenes/Scene Definition", order = 0)]
    public class SceneDefinition : ScriptableObject
    {
        #region Fields

        [SerializeField]
        [Tooltip("The exact scene name, as listed in File > Build Profiles > Scene List.")]
        private string sceneName = string.Empty;

        [SerializeField]
        [Tooltip("The name shown to the player, for example on a loading screen. Uses the scene name if empty.")]
        private string displayName = string.Empty;

        #endregion Fields

        #region Properties

        /// <summary>The scene name passed to SceneManager.</summary>
        public string SceneName => sceneName;

        /// <summary>The player-facing name, or the scene name if none is set.</summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? sceneName : displayName;

        #endregion Properties

        #region Unity Messages

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(sceneName))
                return;

            bool inSceneList = EditorBuildSettings.scenes.Any(scene =>
                scene.enabled && Path.GetFileNameWithoutExtension(scene.path) == sceneName);

            if (!inSceneList)
                Debug.LogWarning($"{name}: no enabled scene named '{sceneName}' is in File > Build Profiles > Scene List.", this);
        }
#endif

        #endregion Unity Messages
    }
}

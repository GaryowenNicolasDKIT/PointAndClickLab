using GD.Types;
using UnityEngine;

namespace GD
{
    /// <summary>
    /// Raises event channels when the selection changes, so other systems (UI, audio,
    /// tutorial prompts) can react without a reference to the selection system.
    /// </summary>
    /// <remarks>
    /// Listen with a <see cref="GameObjectEventChannelListener"/> on the reacting object.
    /// </remarks>
    [AddComponentMenu("GD/Selection/Responses/Event Channel Selection Response")]
    public class EventChannelSelectionResponse : MonoBehaviour, ISelectionResponse
    {
        #region Fields

        [SerializeField]
        [Tooltip("Raised with the newly selected object. Leave empty if not needed")]
        private GameObjectEventChannel selected;

        [SerializeField]
        [Tooltip("Raised with the deselected object. Leave empty if not needed")]
        private GameObjectEventChannel deselected;

        #endregion Fields

        #region Core Methods

        public void OnSelect(Transform selection)
        {
            if (selected != null)
                selected.Raise(selection.gameObject);
        }

        public void OnDeselect(Transform selection)
        {
            if (deselected != null)
                deselected.Raise(selection.gameObject);
        }

        #endregion Core Methods
    }
}

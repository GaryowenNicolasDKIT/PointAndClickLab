using UnityEngine;

namespace GD
{
    /// <summary>
    /// Logs basic details of the selected object, to check what a selector is hitting
    /// without adding visible feedback. Click a log entry in the Console to highlight the object.
    /// </summary>
    /// <remarks>
    /// Logs through <see cref="DevLog"/>, so the calls are compiled out of release builds.
    /// </remarks>
    [AddComponentMenu("GD/Selection/Responses/Debug Selection Response")]
    public class DebugSelectionResponse : MonoBehaviour, ISelectionResponse
    {
        #region Core Methods

        public void OnSelect(Transform selection)
        {
            GameObject selected = selection.gameObject;

            // LayerToName converts the layer index to the name shown in the Inspector.
            DevLog.Info(
                $"Selected: {selected.name} | Position: {selection.position} | " +
                $"Tag: {selected.tag} | Layer: {LayerMask.LayerToName(selected.layer)}",
                selected);
        }

        public void OnDeselect(Transform selection)
        {
            DevLog.Info($"Deselected: {selection.name}", selection.gameObject);
        }

        #endregion Core Methods
    }
}

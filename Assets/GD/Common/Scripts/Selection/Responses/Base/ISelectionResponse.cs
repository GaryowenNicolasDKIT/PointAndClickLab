using UnityEngine;

namespace GD
{
    /// <summary>
    /// Any class that implements this interface can respond when the selection changes.
    /// SelectionManager calls OnDeselect on the old selection, then OnSelect on the new one.
    /// </summary>
    /// <see cref="SelectionManager"/>
    /// <see cref="HighlightSelectionResponse"/>
    /// <see cref="EventChannelSelectionResponse"/>
    public interface ISelectionResponse
    {
        void OnSelect(Transform selection);

        void OnDeselect(Transform selection);
    }
}

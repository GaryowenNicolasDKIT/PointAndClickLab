using System.Collections.Generic;
using DG.Tweening;
using GD.Types;
using UnityEngine;

namespace GD
{
    /// <summary>
    /// Eases each selected object to a larger scale and back again. Handles several objects
    /// at once, and being interrupted halfway.
    /// </summary>
    [AddComponentMenu("GD/Selection/Responses/Tween Scale Selection Response")]
    public class TweenScaleSelectionResponse : MonoBehaviour, ISelectionResponse
    {
        [SerializeField, Min(0f), Tooltip("Multiple of the object's resting scale when selected")]
        private float selectedScale = 1.2f;

        [SerializeField] private TweenProfile selectProfile;
        [SerializeField] private TweenProfile deselectProfile;

        // Each object's scale, recorded the first time it is selected, while it is at rest.
        private readonly Dictionary<Transform, Vector3> restingScales = new();

        public void OnSelect(Transform selection)
        {
            if (!restingScales.ContainsKey(selection))
                restingScales[selection] = selection.localScale;

            TweenScale(selection, restingScales[selection] * selectedScale, selectProfile);
        }

        public void OnDeselect(Transform selection)
        {
            if (restingScales.TryGetValue(selection, out Vector3 restingScale))
                TweenScale(selection, restingScale, deselectProfile);
        }

        private void TweenScale(Transform target, Vector3 scale, TweenProfile profile)
        {
            // Replace any running tween, so only one writes to this object's scale.
            target.DOKill();

            profile.Apply(target.DOScale(scale, profile.Duration))
                .SetLink(target.gameObject);
        }
    }
}

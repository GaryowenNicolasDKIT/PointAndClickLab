using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GD
{
    /// <summary>
    /// Selects the front-most UI element under the ray, if it carries the selectable tag.
    /// The UI equivalent of RayCastSelector.
    /// </summary>
    [AddComponentMenu("GD/Selection/Selectors/Graphic Raycast Selector")]
    public class GraphicRaycastSelector : MonoBehaviour, ISelector
    {
        [SerializeField, Tooltip("The GraphicRaycaster on the canvas to search")]
        private GraphicRaycaster raycaster;

        [SerializeField, Tooltip("The camera the ray comes from. Defaults to Camera.main if left empty")]
        private Camera viewCamera;

        [SerializeField, Tooltip("Tag used by selectable UI elements")]
        private string selectableTag = "Selectable";

        // Reused every frame, so selection creates no garbage.
        private readonly List<RaycastResult> results = new List<RaycastResult>();
        private PointerEventData pointerData;

        private void Awake()
        {
            if (viewCamera == null)
                viewCamera = Camera.main;

            if (raycaster == null || viewCamera == null)
            {
                Debug.LogError($"{name}: GraphicRaycastSelector needs a GraphicRaycaster and a camera.", this);
                enabled = false;
            }
        }

        public bool TrySelect(Ray ray, out SelectionHit hit)
        {
            hit = default;

            if (!enabled || EventSystem.current == null)
                return false;

            pointerData ??= new PointerEventData(EventSystem.current);

            // The ray starts on the camera's near plane under the pointer, so projecting
            // its origin back to the screen gives the pointer position the UI needs.
            pointerData.position = viewCamera.WorldToScreenPoint(ray.origin);

            results.Clear();
            raycaster.Raycast(pointerData, results);

            if (results.Count == 0)
                return false;

            // Only the front-most element counts, so a window or tooltip over a slot blocks it.
            GameObject front = results[0].gameObject;
            if (!front.CompareTag(selectableTag))
                return false;

            hit = new SelectionHit(front.transform, results[0].worldPosition, results[0].worldNormal, results[0].distance);
            return true;
        }
    }
}

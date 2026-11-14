using UnityEngine;
using UnityEngine.Events;

namespace GD.Conditions
{
    /// <summary>
    /// Raises an event when a condition becomes met, and another when it stops being met.
    /// </summary>
    [AddComponentMenu("GD/Conditions/Condition Watcher")]
    public class ConditionWatcher : MonoBehaviour
    {
        [SerializeField] private Condition condition;

        [SerializeField, Min(0f), Tooltip("Seconds the condition must stay met before it counts")]
        private float holdSeconds;

        [SerializeField] private UnityEvent onMet;
        [SerializeField] private UnityEvent onNoLongerMet;

        private bool wasMet;
        private float heldFor;

        private void Update()
        {
            bool metNow = condition.IsMet();

            heldFor = metNow ? heldFor + Time.deltaTime : 0f;

            bool counts = metNow && heldFor >= holdSeconds;

            if (counts == wasMet)
                return;

            wasMet = counts;

            if (counts)
                onMet.Invoke();
            else
                onNoLongerMet.Invoke();
        }
    }
}

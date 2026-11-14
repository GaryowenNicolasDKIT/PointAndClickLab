using UnityEngine;
using UnityEngine.Events;

public class ShapedElapsedTime : MonoBehaviour
{
    [SerializeField] private float duration;
    private float currentElapsedTime;

    [SerializeField] private UnityEvent onTick;
    [SerializeField] private AnimationCurve scaleCurve;

    // Update is called once per frame
    void Update()
    {
      //  Time.timeScale = 1;

        currentElapsedTime += Time.unscaledDeltaTime;

        float shapedTime = scaleCurve.Evaluate(currentElapsedTime);

        if(currentElapsedTime > duration)
        {
            currentElapsedTime -= duration;
            onTick.Invoke();
        }
    }
}

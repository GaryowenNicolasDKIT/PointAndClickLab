using GD;
using GD.Types;
using UnityEngine;


public class ExamineInteractable : MonoBehaviour, IInteractable
{
    [SerializeField, TextArea] private string description = "A dusty painting of a house, years ago.";

    [SerializeField, Tooltip("A ToastView listens to this through a StringEventChannelListener")]
    private StringEventChannel messageRequested;

    public void Interact(GameObject interactor)
    {
        if (messageRequested != null)
            messageRequested.Raise(description);
    }
}

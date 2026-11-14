using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.AI;

[RequireComponent (typeof(NavMeshAgent))]
public class ClickToWalk : MonoBehaviour
{
    [SerializeField] private InputAction clickAction = new("Click", binding: "<Mouse>/leftButton");

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        clickAction.Enable();
    }

    private void OnDisable()
    {
        clickAction.Disable();
    }

    private void Update()
    {
        if (!clickAction.WasPressedThisFrame())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if(Physics.Raycast(ray, out RaycastHit hit))
            agent.SetDestination(hit.point);
    }
}

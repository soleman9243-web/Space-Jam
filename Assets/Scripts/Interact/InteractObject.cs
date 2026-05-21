using UnityEngine;
using UnityEngine.Events;
public class InteractObject2D : MonoBehaviour
{
    [Header("Task Lock")]
    public bool requireActiveTask;
    public BaseTask linkedTask;
    [Header("Player Control")]
    [SerializeField] private bool lockPlayerOnInteract = true;
    [Header("Events")]
    public UnityEvent onInteract = new UnityEvent();
    public void Interact()
    {
        if (requireActiveTask)
        {
            if (linkedTask == null)
            {
                return;
            }
            if (!linkedTask.IsActive)
            {
                return;
            }
        }
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.StopMovement();
        }
        onInteract.Invoke();
    }
}
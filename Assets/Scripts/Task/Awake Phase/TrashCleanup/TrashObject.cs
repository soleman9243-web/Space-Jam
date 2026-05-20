using UnityEngine;

public class TrashObject : MonoBehaviour
{
    private TrashCleanupTask task;

    private InteractObject2D interact;

    private void Awake()
    {
        interact = GetComponent<InteractObject2D>();

        interact.onInteract.AddListener(OnInteract);
    }

    public void Setup(TrashCleanupTask ownerTask)
    {
        task = ownerTask;
    }

    private void OnInteract()
    {
        Debug.Log("Sampah di interact");

        if (task == null)
        {
            return;
        }

        task.CleanTrash(this);
    }
}
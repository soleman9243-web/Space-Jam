using UnityEngine;

public class TrashObject : MonoBehaviour
{
    private TrashCleanupTask task;

    private InteractObject2D interact;

    private void Awake()
    {
        interact = GetComponent<InteractObject2D>();

        if (interact != null)
        {
            interact.onInteract.AddListener(OnInteract);
        }
        else
        {
            Debug.LogWarning("[TrashObject] InteractObject2D tidak ditemukan di prefab Trash!");
        }
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
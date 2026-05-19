using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractObject2D : MonoBehaviour
{
    [Header("Task Lock")]
    [SerializeField] private bool requireActiveTask;

    [SerializeField] private BaseTask linkedTask;

    [Header("Events")]
    public UnityEvent onInteract;

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

        onInteract.Invoke();
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractObject2D : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent onInteract;

    public void Interact()
    {
        onInteract.Invoke();
    }
}
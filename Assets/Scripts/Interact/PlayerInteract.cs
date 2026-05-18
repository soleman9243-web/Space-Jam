using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerInteract2D : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject interactBubble;

    [Header("Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private InteractObject2D currentInteractable;

    private void Start()
    {
        if (interactBubble != null)
        {
            interactBubble.SetActive(false);
        }
    }

    private void Update()
    {
        if (currentInteractable == null)
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            currentInteractable.Interact();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        InteractObject2D interactable = other.GetComponent<InteractObject2D>();

        if (interactable != null)
        {
            currentInteractable = interactable;

            if (interactBubble != null)
            {
                interactBubble.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        InteractObject2D interactable = other.GetComponent<InteractObject2D>();

        if (interactable != null && interactable == currentInteractable)
        {
            currentInteractable = null;

            if (interactBubble != null)
            {
                interactBubble.SetActive(false);
            }
        }
    }
}
using UnityEngine;

public class PlayerInteract2D : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private InteractObject2D currentInteractable;

    public bool canInteract = true;

    private void Update()
    {
        if (!canInteract)
        {
            return;
        }

        if (currentInteractable == null)
        {
            return;
        }

        if (!currentInteractable.CanInteract())
        {
            currentInteractable.SetHighlight(false);
            currentInteractable = null;
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

        if (interactable == null)
        {
            return;
        }

        if (!interactable.CanInteract())
        {
            return;
        }

        currentInteractable = interactable;
        currentInteractable.SetHighlight(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        InteractObject2D interactable = other.GetComponent<InteractObject2D>();

        if (interactable == null)
        {
            return;
        }

        if (interactable == currentInteractable)
        {
            currentInteractable.SetHighlight(false);
            currentInteractable = null;
        }
    }
}
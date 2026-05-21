using UnityEngine;
using UnityEngine.Events;

public class InteractObject2D : MonoBehaviour
{
    [Header("Task Lock")]
    public bool requireActiveTask;
    public BaseTask linkedTask;

    [Header("Player Control")]
    [SerializeField] private bool lockPlayerOnInteract = true;

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer outlineRenderer;

    [Header("Outline")]
    [SerializeField] private Color outlineColor = new Color(0f, 0.7f, 1f, 1f);

    [SerializeField] private float outlineSize = 1f;

    [Header("Events")]
    public UnityEvent onInteract = new UnityEvent();

    private Material outlineMaterialInstance;

    private void Awake()
    {
        if (outlineRenderer != null)
        {
            outlineMaterialInstance =
                new Material(outlineRenderer.material);

            outlineRenderer.material = outlineMaterialInstance;

            outlineMaterialInstance.SetColor(
                "_OutlineColor",
                outlineColor
            );

            outlineMaterialInstance.SetFloat(
                "_OutlineSize",
                outlineSize
            );
        }
    }

    private void Start()
    {
        SetHighlight(false);
    }

    public bool CanInteract()
    {
        if (!requireActiveTask)
        {
            return true;
        }

        if (linkedTask == null)
        {
            return false;
        }

        return linkedTask.IsActive;
    }

    public void SetHighlight(bool state)
    {
        if (outlineRenderer == null)
        {
            return;
        }

        outlineRenderer.enabled = state;
    }

    public void Interact()
    {
        if (!CanInteract())
        {
            return;
        }

        if (lockPlayerOnInteract)
        {
            if (PlayerMovement.Instance != null)
            {
                PlayerMovement.Instance.StopMovement();
            }
        }

        onInteract.Invoke();
    }
}
using System.Collections;
using TMPro;
using UnityEngine;

public class PhoneSearchObject : MonoBehaviour
{
    [Header("Dialog Box")]
    [SerializeField] private GameObject dialogBox;
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private float dialogDuration = 2f;

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer outlineRenderer;
    [SerializeField] private Color outlineColor = new Color(0f, 0.7f, 1f, 1f);
    [SerializeField] private float outlineSize = 1f;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private FindPhoneTask linkedTask;
    private bool isCorrect;
    private bool hasBeenSearched;
    private bool playerInRange;
    private Coroutine dialogCoroutine;
    private Material outlineMaterialInstance;

    // =============================================
    // AWAKE — inisialisasi material outline
    // =============================================
    private void Awake()
    {
        if (outlineRenderer != null)
        {
            outlineMaterialInstance = new Material(outlineRenderer.material);
            outlineRenderer.material = outlineMaterialInstance;
            outlineMaterialInstance.SetColor("_OutlineColor", outlineColor);
            outlineMaterialInstance.SetFloat("_OutlineSize", outlineSize);
        }
    }

    private void Start()
    {
        SetHighlight(false);
    }

    // =============================================
    // SETUP
    // =============================================
    public void Setup(FindPhoneTask task, bool correct)
    {
        linkedTask = task;
        isCorrect = correct;
        hasBeenSearched = false;
    }

    public void ResetObject()
    {
        linkedTask = null;
        isCorrect = false;
        hasBeenSearched = false;
        playerInRange = false;
        SetHighlight(false);

        if (dialogCoroutine != null)
        {
            StopCoroutine(dialogCoroutine);
            dialogCoroutine = null;
        }
        if (dialogBox != null) dialogBox.SetActive(false);
    }

    // =============================================
    // HIGHLIGHT
    // =============================================
    public void SetHighlight(bool state)
    {
        if (outlineRenderer == null) return;
        outlineRenderer.enabled = state;
    }

    // =============================================
    // UPDATE + TRIGGER
    // =============================================
    private void Update()
    {
        if (!playerInRange) return;
        if (linkedTask == null || !linkedTask.IsActive) return;
        if (Input.GetKeyDown(interactKey)) OnInteract();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        // Tampilkan outline hanya kalau task sedang aktif
        if (linkedTask != null && linkedTask.IsActive)
            SetHighlight(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        SetHighlight(false);
    }

    // =============================================
    // INTERACT
    // =============================================
    private void OnInteract()
    {
        if (hasBeenSearched && !isCorrect) return;

        hasBeenSearched = true;

        if (isCorrect)
        {
            ShowDialog("You found your phone!");
            SetHighlight(false); // Matikan outline setelah ketemu
            linkedTask.OnPhoneFound();
        }
        else
        {
            ShowDialog("There's no phone here");
        }
    }

    // =============================================
    // DIALOG
    // =============================================
    private void ShowDialog(string message)
    {
        if (dialogBox == null) return;
        if (dialogText != null) dialogText.text = message;

        if (dialogCoroutine != null) StopCoroutine(dialogCoroutine);
        dialogCoroutine = StartCoroutine(DialogRoutine());
    }

    private IEnumerator DialogRoutine()
    {
        dialogBox.SetActive(true);
        yield return new WaitForSeconds(dialogDuration);
        dialogBox.SetActive(false);
        dialogCoroutine = null;
    }
}
using System.Collections;
using TMPro;
using UnityEngine;

public class PhoneSearchObject : MonoBehaviour
{
    [Header("Dialog Box")]
    [SerializeField] private GameObject dialogBox;
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private float dialogDuration = 2f;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private FindPhoneTask linkedTask;
    private bool isCorrect;
    private bool hasBeenSearched;
    private bool playerInRange;
    private Coroutine dialogCoroutine;

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

        if (dialogCoroutine != null)
        {
            StopCoroutine(dialogCoroutine);
            dialogCoroutine = null;
        }

        if (dialogBox != null)
        {
            dialogBox.SetActive(false);
        }
    }

    // =============================================
    // DETEKSI SENDIRI — tidak pakai InteractObject2D
    // =============================================

    private void Update()
    {
        if (!playerInRange)
        {
            return;
        }

        if (linkedTask == null || !linkedTask.IsActive)
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            OnInteract();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    // =============================================
    // INTERACT
    // =============================================

    private void OnInteract()
    {
        if (hasBeenSearched && !isCorrect)
        {
            return;
        }

        hasBeenSearched = true;

        if (isCorrect)
        {
            ShowDialog("You found your phone!");
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
        if (dialogBox == null)
        {
            return;
        }

        if (dialogText != null)
        {
            dialogText.text = message;
        }

        if (dialogCoroutine != null)
        {
            StopCoroutine(dialogCoroutine);
        }

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
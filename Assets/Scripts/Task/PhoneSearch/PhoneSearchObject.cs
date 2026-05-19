using UnityEngine;

public class PhoneSearchObject : MonoBehaviour
{
    [SerializeField] private bool isCorrectPhone;

    [Header("Config")]
    [SerializeField] private string wrongText = "Kamu nggak nemu HP";
    [SerializeField] private float wrongPenalty = 5f;

    private InteractObject2D interact;
    private PhoneSearchTask task;

    private void Awake()
    {
        interact = GetComponent<InteractObject2D>();
        task = GetComponentInParent<PhoneSearchTask>();

        if (interact != null)
        {
            interact.onInteract.AddListener(OnInteract);
        }
    }

    public void SetCorrect(bool value)
    {
        isCorrectPhone = value;
    }

    private void OnInteract()
    {
        if (task == null || !task.IsActive)
        {
            return;
        }

        if (isCorrectPhone)
        {
            task.CompletePhoneSearch(true);
        }
        else
        {
            task.FailSearch(wrongPenalty, wrongText);
            task.CompletePhoneSearch(false);
        }
    }
}
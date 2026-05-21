using UnityEngine;

public class FindPhoneTask : BaseTask
{
    [Header("Phone Search Objects")]
    [SerializeField] private PhoneSearchObject[] searchObjects;

    [Header("Stability")]
    [SerializeField] private float baseReward = 5f;
    [SerializeField] private float rewardIncreasePerLoop = 1f;

    private PhoneSearchObject correctObject;

    // =============================================
    // LIFECYCLE
    // =============================================

    public override void ActivateTask()
    {
        base.ActivateTask();

        taskText = "Find a phone in the room";

        AssignRandomPhone();

        Debug.Log("[FindPhoneTask] Activated. Phone hidden in: " + correctObject.name);
    }

    public override void ResetTask()
    {
        base.ResetTask();

        foreach (var obj in searchObjects)
        {
            obj.ResetObject();
        }

        correctObject = null;
    }

    public override void ForceStopTask()
    {
        foreach (var obj in searchObjects)
        {
            obj.ResetObject();
        }

        DeactivateTask();
    }

    // =============================================
    // SETUP
    // =============================================

    private void AssignRandomPhone()
    {
        if (searchObjects == null || searchObjects.Length == 0)
        {
            Debug.LogError("[FindPhoneTask] No PhoneSearchObjects assigned!");
            return;
        }

        // Reset semua dulu
        foreach (var obj in searchObjects)
        {
            obj.Setup(this, false);
        }

        // Pilih 1 random sebagai yang bener
        int randomIndex = Random.Range(0, searchObjects.Length);
        correctObject = searchObjects[randomIndex];
        correctObject.Setup(this, true);
    }

    // =============================================
    // CALLED BY PhoneSearchObject
    // =============================================

    public void OnPhoneFound()
    {
        if (!IsActive || completed)
        {
            return;
        }

        PhaseLoopManager phaseManager =
            FindObjectOfType<PhaseLoopManager>();

        int loop =
            phaseManager != null
            ? phaseManager.currentLoop
            : 1;

        float reward =
            baseReward + ((loop - 1) * rewardIncreasePerLoop);

        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.IncreaseStability(reward);
        }

        CompleteTask();

        Debug.Log("[FindPhoneTask] Phone found! Task complete.");
    }
}
using System.Collections.Generic;
using UnityEngine;

public class PhoneSearchTask : BaseTask
{
    [Header("Phone Objects")]
    [SerializeField] private List<PhoneSearchObject> allObjects;

    private PhoneSearchObject correctObject;
    private bool finished = false;

    public override void ActivateTask()
    {
        base.ActivateTask();

        finished = false;
        AssignRandomPhone();
    }

    public override void ResetTask()
    {
        base.ResetTask();

        finished = false;
        correctObject = null;
    }

    private void AssignRandomPhone()
    {
        if (allObjects == null || allObjects.Count == 0)
        {
            Debug.LogWarning("No phone objects assigned!");
            return;
        }

        int rand = Random.Range(0, allObjects.Count);

        correctObject = allObjects[rand];

        for (int i = 0; i < allObjects.Count; i++)
        {
            allObjects[i].SetCorrect(allObjects[i] == correctObject);
        }

        Debug.Log("PHONE SPAWNED AT: " + correctObject.name);
    }

    public void CompletePhoneSearch(bool success)
    {
        if (finished)
        {
            return;
        }

        finished = true;

        if (success)
        {
            ShowDialog("Kamu nemu HP!");
            CompleteTask();
        }
        else
        {
            finished = false;
        }
    }

    public void FailSearch(float penalty, string failText)
    {
        ShowDialog(failText);

        if (PlayerStatus.Instance != null)
        {
            PlayerStatus.Instance.ReduceStability(penalty);
        }
    }

    private void ShowDialog(string text)
    {
        Debug.Log(text);
        // nanti ganti ke dialog system kamu
    }
}
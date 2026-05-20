using TMPro;
using UnityEngine;

public class LoopCounterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI loopText;
    [SerializeField] private string prefix = "Loop: ";

    public PhaseLoopManager phaseManager;

    private int lastLoop = -1;

    private void Update()
    {
        if (phaseManager == null || loopText == null) return;

        if (phaseManager.currentLoop != lastLoop)
        {
            lastLoop = phaseManager.currentLoop;
            loopText.text = prefix + lastLoop;
        }
    }
}   
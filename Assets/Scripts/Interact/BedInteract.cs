using System.Collections;
using UnityEngine;

public class BedInteract : MonoBehaviour
{
    [Header("References")]
    public Transform wakeUpPosition;
    public PhaseLoopManager phaseManager;
    public Transform sleepPosition;

    public float sleepDuration = 2f;

    public bool canSleep = false;

    private bool hasSlept = false;

    public void SleepOnBed()
    {
        Debug.Log("BED CLICKED");

        if (!canSleep)
        {
            Debug.Log("BLOCKED canSleep = false");
            return;
        }

        if (hasSlept)
        {
            Debug.Log("BLOCKED hasSlept = true");
            return;
        }
        if (!canSleep || hasSlept)
        {
            return;
        }

        hasSlept = true;

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            player.transform.position =
                sleepPosition != null ? sleepPosition.position : transform.position;

            player.transform.rotation =
                sleepPosition != null ? sleepPosition.rotation : transform.rotation;

            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;

            var collider = player.GetComponent<Collider2D>();
            if (collider != null) collider.enabled = false;
        }

        if (phaseManager == null)
        {
            phaseManager = FindObjectOfType<PhaseLoopManager>();
        }

        if (phaseManager == null)
        {
            Debug.LogError("PhaseLoopManager tidak ditemukan!");
            return;
        }

        StartCoroutine(SleepTransition());
    }

    private IEnumerator SleepTransition()
    {
        yield return new WaitForSeconds(sleepDuration);

        // FIX UTAMA: hanya arahkan ke Dream, jangan toggle
        phaseManager.StartManualTransition(GameState.Dream, wakeUpPosition);

        yield return new WaitForSeconds(1f);

        hasSlept = false;
    }
}
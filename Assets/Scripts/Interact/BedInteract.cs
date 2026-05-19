using System.Collections;
using UnityEngine;

public class BedInteract : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Posisi di mana karakter akan terbangun (misal: di sebelah kasur).")]
    public Transform wakeUpPosition;

    [Tooltip("Referensi ke PhaseLoopManager. Akan dicari otomatis jika kosong.")]
    public PhaseLoopManager phaseManager;

    public Transform sleepPosition;

    public float sleepDuration = 2f;

    public bool canSleep = false;

    private bool hasSlept = false;

    public void SleepOnBed()
    {
        if (!canSleep)
        {
            return;
        }

        if (hasSlept)
        {
            return;
        }

        hasSlept = true;

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            player.transform.position =
                sleepPosition != null
                ? sleepPosition.position
                : transform.position;

            player.transform.rotation =
                sleepPosition != null
                ? sleepPosition.rotation
                : transform.rotation;

            var movement =
                player.GetComponent<PlayerMovement>();

            if (movement != null)
            {
                movement.enabled = false;
            }

            var rb =
                player.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            var collider =
                player.GetComponent<Collider2D>();

            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        if (phaseManager == null)
        {
            phaseManager =
                FindObjectOfType<PhaseLoopManager>();
        }

        if (phaseManager != null)
        {
            GameState targetState =
                GameState.Dream;

            if (phaseManager.CurrentState == GameState.Dream)
            {
                targetState =
                    GameState.Awake;
            }

            StartCoroutine(
                SleepTransition(targetState)
            );
        }
        else
        {
            Debug.LogError(
                "PhaseLoopManager tidak ditemukan di scene!"
            );
        }
    }

    private IEnumerator SleepTransition(GameState targetState)
    {
        yield return new WaitForSeconds(sleepDuration);

        phaseManager.StartManualTransition(
            targetState,
            wakeUpPosition
        );

        yield return new WaitForSeconds(1f);

        hasSlept = false;
    }
}
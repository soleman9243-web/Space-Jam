using System.Collections;
using UnityEngine;

/// <summary>
/// Shadow yang mengikuti jejak player dengan delay.
/// Di fase Liminal: menyentuh player ? ReduceStability (bukan Die).
/// </summary>
public class ShadowMimic : MonoBehaviour
{
    private PlayerRecorder recorder;

    [Header("Difficulty")]
    [SerializeField] private int currentLoop = 1;

    [Tooltip("Delay awal sebelum shadow mengikuti player (detik).")]
    [SerializeField] private float baseDelay = 2f;

    [Tooltip("Pengurangan delay tiap loop (semakin tinggi loop ? shadow makin dekat).")]
    [SerializeField] private float delayDecreasePerLoop = 0.15f;

    [Tooltip("Delay minimum agar shadow tidak pernah benar-benar menempel.")]
    [SerializeField] private float minDelay = 0.3f;

    [Header("Hit Player")]
    [Tooltip("Berapa stability yang dikurangi saat shadow menyentuh player.")]
    [SerializeField] private float stabilityDamage = 15f;

    [Tooltip("Jeda (detik) setelah shadow kena player sebelum bisa kena lagi.")]
    [SerializeField] private float hitCooldown = 1.5f;

    [Header("Grace Period")]
    [Tooltip("Setelah spawn, shadow tidak bisa melukai player selama ini (detik).")]
    [SerializeField] private float spawnGracePeriod = 1.5f;

    [Header("Animation")]
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sr;

    // ?? Runtime ????????????????????????????????????????????????????????????
    private float currentDelay;
    private bool canHit;
    private bool isOnCooldown;

    private Vector2 moveInput;
    private Vector2 lastDirection;

    // ?? Unity ??????????????????????????????????????????????????????????????

    private void Start()
    {
        recorder = PlayerRecorder.Instance;

        SetupDifficulty();

        StartCoroutine(GracePeriod());
    }

    private void Update()
    {
        if (recorder == null) return;

        // Tunggu sampai cukup data terekam
        if (Time.time < currentDelay) return;

        Vector3 target = recorder.GetPositionWithDelay(currentDelay);

        Vector3 delta = target - transform.position;
        moveInput = delta.normalized;

        if (moveInput != Vector2.zero)
        {
            lastDirection = moveInput;
        }

        UpdateAnimation();

        transform.position = target;
    }

    // ?? Setup ??????????????????????????????????????????????????????????????

    private void SetupDifficulty()
    {
        currentDelay = baseDelay - ((currentLoop - 1) * delayDecreasePerLoop);
        currentDelay = Mathf.Max(minDelay, currentDelay);

        Debug.Log(
            $"[ShadowMimic] Loop {currentLoop} | Delay {currentDelay:F2}s"
        );
    }

    private IEnumerator GracePeriod()
    {
        canHit = false;
        yield return new WaitForSeconds(spawnGracePeriod);
        canHit = true;
    }

    // ?? Collision ??????????????????????????????????????????????????????????

    private void OnTriggerEnter2D(Collider2D c)
    {
        if (!canHit || isOnCooldown) return;
        if (!c.CompareTag("Player")) return;
        if (PlayerStatus.Instance == null) return;

        // Fase Liminal ? kurangi stability, bukan mati
        if (PhaseLoopManager.GlobalState == GameState.Liminal)
        {
            PlayerStatus.Instance.ReduceStability(stabilityDamage);

            Debug.Log(
                $"[ShadowMimic] Kena player! -{stabilityDamage} stability"
            );

            StartCoroutine(HitCooldown());
        }
        else
        {
            // Di luar Liminal (kalau shadow tetap aktif karena bug), tetap aman
            PlayerStatus.Instance.ReduceStability(stabilityDamage);
            StartCoroutine(HitCooldown());
        }
    }

    private IEnumerator HitCooldown()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(hitCooldown);
        isOnCooldown = false;
    }

    // ?? Animation ?????????????????????????????????????????????????????????

    private void UpdateAnimation()
    {
        if (anim == null) return;

        anim.SetFloat("MoveX", moveInput.x);
        anim.SetFloat("MoveY", moveInput.y);
        anim.SetFloat("LastMoveX", lastDirection.x);
        anim.SetFloat("LastMoveY", lastDirection.y);
        anim.SetBool("isMoving", moveInput != Vector2.zero);
    }

    // ?? Public API ?????????????????????????????????????????????????????????

    public void SetLoop(int loop)
    {
        currentLoop = loop;
        SetupDifficulty();
    }
}
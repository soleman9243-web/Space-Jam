using System.Collections;
using UnityEngine;

public class ShadowMimic : MonoBehaviour
{
    private PlayerRecorder recorder;

    [Header("Difficulty")]
    [SerializeField] private int currentLoop = 1;

    [SerializeField] private float baseDelay = 2f;

    [SerializeField] private float delayDecreasePerLoop = 0.15f;

    [SerializeField] private float minDelay = 0.3f;

    [Header("Kill")]
    [SerializeField] private float killDelay = 1.5f;

    [Header("Animation")]
    [SerializeField] private Animator anim;

    [SerializeField] private SpriteRenderer sr;

    private float currentDelay;

    private bool canKill;

    private Vector2 moveInput;
    private Vector2 lastDirection;

    private void Start()
    {
        recorder = PlayerRecorder.Instance;

        SetupDifficulty();

        StartCoroutine(EnableKill());
    }

    private IEnumerator EnableKill()
    {
        canKill = false;

        yield return new WaitForSeconds(killDelay);

        canKill = true;
    }

    private void SetupDifficulty()
    {
        currentDelay = baseDelay - ((currentLoop - 1) * delayDecreasePerLoop);

        currentDelay = Mathf.Max(minDelay, currentDelay);

        Debug.Log(
            "Loop: " + currentLoop +
            " | Delay: " + currentDelay
        );
    }

    private void Update()
    {
        if (recorder == null)
        {
            return;
        }

        if (Time.time < currentDelay)
        {
            return;
        }

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

    private void UpdateAnimation()
    {
        anim.SetFloat("MoveX", moveInput.x);
        anim.SetFloat("MoveY", moveInput.y);

        anim.SetFloat("LastMoveX", lastDirection.x);
        anim.SetFloat("LastMoveY", lastDirection.y);

        anim.SetBool("isMoving", moveInput != Vector2.zero);
    }

    private void OnTriggerEnter2D(Collider2D c)
    {
        if (!canKill)
        {
            return;
        }

        if (c.CompareTag("Player"))
        {
            PlayerStatus.Instance.Die();
        }
    }

    public void SetLoop(int loop)
    {
        currentLoop = loop;

        SetupDifficulty();
    }
}
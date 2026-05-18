using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowMimic : MonoBehaviour
{
    private PlayerRecorder recorder;

    [SerializeField] private float delay = 2f;
    [SerializeField] private float moveSpeed = 10f;

    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sr;

    private Vector2 moveInput;
    private Vector2 lastDirection;

    private void Start()
    {
        recorder = PlayerRecorder.Instance;
    }

    private void Update()
    {
        Vector3 target = recorder.GetPositionWithDelay(delay);

        moveInput = (target - transform.position).normalized;

        if (moveInput != Vector2.zero)
        {
            lastDirection = moveInput;
        }

        UpdateAnimation();

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.deltaTime
        );
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
        if (c.CompareTag("Player"))
        {
            PlayerStatus.Instance.Die();
        }
    }
}
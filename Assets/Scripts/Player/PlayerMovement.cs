using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;

    private Vector2 moveInput;
    private Vector2 lastDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(moveX, moveY).normalized;

        if (moveInput != Vector2.zero)
        {
            lastDirection = moveInput;
        }

        HandleFlip();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed;
    }

    private void HandleFlip()
    {
        if (moveInput.x > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (moveInput.x < 0)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void UpdateAnimation()
    {
        anim.SetFloat("MoveX", moveInput.x);
        anim.SetFloat("MoveY", moveInput.y);

        anim.SetFloat("LastMoveX", lastDirection.x);
        anim.SetFloat("LastMoveY", lastDirection.y);

        anim.SetBool("isMoving", moveInput != Vector2.zero);
    }
}
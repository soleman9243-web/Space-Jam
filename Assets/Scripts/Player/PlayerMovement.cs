using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public event Action<bool> OnMovementChanged;

    public static PlayerMovement Instance { get; private set; }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;

    private Vector2 moveInput;
    private bool lastIsMoving;

    public bool IsMoving { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(moveX, moveY).normalized;

        IsMoving = moveInput.sqrMagnitude > 0.01f;

        if (IsMoving != lastIsMoving)
        {
            lastIsMoving = IsMoving;
            OnMovementChanged?.Invoke(IsMoving);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetFootstep(IsMoving);
            }
        }

        HandleFlip();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed;
    }

    // ?? DIPANGGIL SAAT INTERACT
    public void StopMovement()
    {
        moveInput = Vector2.zero;
        rb.velocity = Vector2.zero;

        IsMoving = false;

        if (lastIsMoving)
        {
            lastIsMoving = false;
            OnMovementChanged?.Invoke(false);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetFootstep(false);
            }
        }

        UpdateAnimation();
    }

    private void HandleFlip()
    {
        if (moveInput.x > 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (moveInput.x < 0)
        {
            spriteRenderer.flipX = false;
        }
    }

    private void UpdateAnimation()
    {
        anim.SetFloat("MoveX", moveInput.x);
        anim.SetFloat("MoveY", moveInput.y);

        if (IsMoving)
        {
            anim.SetFloat("LastMoveX", moveInput.x);
            anim.SetFloat("LastMoveY", moveInput.y);
        }
        else
        {
            anim.SetFloat("LastMoveX", 0f);
            anim.SetFloat("LastMoveY", -1f);
        }

        anim.SetBool("isMoving", IsMoving);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashEnemyHalu : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sr;

    private Rigidbody2D rb;
    private Vector2 dir;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 direction, float speed)
    {
        dir = direction.normalized;

        SetVisual();

        rb.velocity = dir * speed;
    }

    private void SetVisual()
    {
        if (dir == Vector2.up)
        {
            anim.SetInteger("Direction", 2);
            sr.flipX = false;
        }
        else if (dir == Vector2.down)
        {
            anim.SetInteger("Direction", 0);
            sr.flipX = false;
        }
        else
        {
            anim.SetInteger("Direction", 1);
            sr.flipX = dir == Vector2.right;
        }
    }

    private void OnTriggerEnter2D(Collider2D c)
    {
        if (c.CompareTag("Player"))
        {
            PlayerStatus.Instance.Die();
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
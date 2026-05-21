using System.Collections;
using UnityEngine;

public class RingBullet : MonoBehaviour
{
    public enum BulletState
    {
        MovingIn,
        Frozen,
        MovingOut
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeedIn = 4f;
    [SerializeField] private float moveSpeedOut = 8f;

    [Header("Reaction")]
    [SerializeField] private float reactSpeedMultiplier = 1f;

    [Header("Settings")]
    [SerializeField] private float despawnDelay = 0.5f;

    private Vector2 moveDir;
    private BulletState state;

    private PlayerMovement player;
    private BulletSpawner spawner;

    private bool isPlayerMoving;
    private bool isDetached;
    private bool canSpawnNextRing;
    private float currentOutSpeed;

    private void Start()
    {
        state = BulletState.MovingIn;
        currentOutSpeed = moveSpeedOut * reactSpeedMultiplier;
    }

    public void SetDirection(Vector2 dir) => moveDir = dir.normalized;
    public void SetPlayer(PlayerMovement p)
    {
        player = p;
        player.OnMovementChanged += HandlePlayerMove;
    }
    public void SetSpawner(BulletSpawner s) => spawner = s;
    public void SetCanSpawnNextRing(bool value) => canSpawnNextRing = value;

    private void Update()
    {
        switch (state)
        {
            case BulletState.MovingIn:
                transform.position +=
                    (Vector3)(moveDir * moveSpeedIn * Time.deltaTime);
                break;

            case BulletState.Frozen:
                if (!isDetached && isPlayerMoving)
                {
                    transform.position +=
                        (Vector3)(moveDir * currentOutSpeed * Time.deltaTime);
                }
                break;

            case BulletState.MovingOut:
                transform.position +=
                    (Vector3)(moveDir * currentOutSpeed * Time.deltaTime);
                break;
        }
    }

    private void HandlePlayerMove(bool isMoving)
    {
        isPlayerMoving = isMoving;

        if (state == BulletState.MovingIn && isMoving)
        {
            state = BulletState.MovingOut;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ring"))
        {
            state = BulletState.Frozen;
        }
        if (other.CompareTag("Player"))
        {
            PlayerStatus.Instance.Die();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!gameObject.activeInHierarchy) return;

        if (other.CompareTag("Ring"))
        {
            isDetached = true;
            state = BulletState.MovingOut;

            if (canSpawnNextRing && spawner != null)
            {
                spawner.SpawnRing();
            }

            StartCoroutine(Despawn());
        }
    }
    private IEnumerator Despawn()
    {
        yield return new WaitForSeconds(despawnDelay);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnMovementChanged -= HandlePlayerMove;
        }

        // Beritahu spawner agar list-nya tetap bersih
        spawner?.UnregisterBullet(this);
    }
}
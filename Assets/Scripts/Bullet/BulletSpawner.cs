using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    [SerializeField] private RingBullet bulletPrefab;
    [SerializeField] private Transform center;

    [Header("Ring Settings")]
    [SerializeField] private int count = 12;

    [SerializeField] private float spawnDistance = 10f;

    [Header("Reference")]
    [SerializeField] private PlayerMovement player;

    private int ringIndex;

    private void Start()
    {
        SpawnRing();
    }

    public void SpawnRing()
    {
        float angleStep = 360f / count;

        float rotationOffset = (angleStep * 0.5f) * ringIndex;

        for (int i = 0; i < count; i++)
        {
            float angle = (i * angleStep) + rotationOffset;
            float rad = angle * Mathf.Deg2Rad;

            Vector2 spawnPos = new Vector2(
                center.position.x + Mathf.Cos(rad) * spawnDistance,
                center.position.y + Mathf.Sin(rad) * spawnDistance
            );

            Vector2 dirToCenter = (center.position - (Vector3)spawnPos).normalized;

            RingBullet bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

            bullet.SetDirection(dirToCenter);
            bullet.SetPlayer(player);
            bullet.SetSpawner(this);

            // cuma bullet pertama yang boleh spawn next ring
            bullet.SetCanSpawnNextRing(i == 0);
        }

        ringIndex++;
    }
}
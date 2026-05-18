using System.Collections;
using System.Collections.Generic;
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            SpawnRing();
        }
    }

    private void SpawnRing()
    {
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep;
            float rad = angle * Mathf.Deg2Rad;

            Vector2 spawnPos = new Vector2(
                center.position.x + Mathf.Cos(rad) * spawnDistance,
                center.position.y + Mathf.Sin(rad) * spawnDistance
            );

            Vector2 dirToCenter = (center.position - (Vector3)spawnPos).normalized;

            RingBullet bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

            bullet.SetDirection(dirToCenter);
            bullet.SetPlayer(player);
        }
    }
}
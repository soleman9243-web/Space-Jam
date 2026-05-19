using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomWASDRotate : MonoBehaviour
{
    [Header("Room")]
    [SerializeField] private Transform roomRoot;

    [Header("Movement")]
    [SerializeField] private float stepDistance = 1f;

    [Header("Rotation")]
    [SerializeField] private float rotateAngle = 90f;
    [SerializeField] private float rotateSpeed = 200f;

    private bool isBusy;

    private Dictionary<KeyCode, Vector3> keyMapping;

    private Vector3[] directions = new Vector3[]
    {
        Vector3.up,
        Vector3.down,
        Vector3.left,
        Vector3.right
    };

    private void Start()
    {
        GenerateRandomMapping();
    }

    private void Update()
    {
        if (isBusy)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            StartCoroutine(MoveAndRotate(KeyCode.W));
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            StartCoroutine(MoveAndRotate(KeyCode.A));
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            StartCoroutine(MoveAndRotate(KeyCode.S));
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            StartCoroutine(MoveAndRotate(KeyCode.D));
        }
    }

    private IEnumerator MoveAndRotate(KeyCode key)
    {
        isBusy = true;

        // ?? move based on random mapping
        Vector3 moveDir = keyMapping[key];
        transform.position += moveDir * stepDistance;

        // ?? random rotation direction
        float direction = Random.value < 0.5f ? -1f : 1f;

        float rotated = 0f;

        while (rotated < rotateAngle)
        {
            float step = rotateSpeed * Time.deltaTime;

            roomRoot.Rotate(0f, 0f, step * direction);

            rotated += step;
            yield return null;
        }

        GenerateRandomMapping();

        isBusy = false;
    }

    private void GenerateRandomMapping()
    {
        List<Vector3> pool = new List<Vector3>(directions);

        keyMapping = new Dictionary<KeyCode, Vector3>();

        KeyCode[] keys = new KeyCode[]
        {
            KeyCode.W,
            KeyCode.A,
            KeyCode.S,
            KeyCode.D
        };

        foreach (KeyCode key in keys)
        {
            int index = Random.Range(0, pool.Count);

            keyMapping[key] = pool[index];

            pool.RemoveAt(index);
        }
    }
}
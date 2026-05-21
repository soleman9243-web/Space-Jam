using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomWASDRotate : MonoBehaviour
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    [Header("Room")]
    [SerializeField] private Transform roomRoot;

    [Header("Movement")]
    [SerializeField] private float stepDistance = 1f;

    [Header("Rotation")]
    [SerializeField] private float rotateAngle = 90f;
    [SerializeField] private float rotateSpeed = 200f;

    [Header("Difficulty")]
    [SerializeField] private Difficulty difficulty = Difficulty.Medium;

    [SerializeField] private float easyRotateDelay = 5f;
    [SerializeField] private float mediumRotateDelay = 3f;
    [SerializeField] private float hardRotateDelay = 1.5f;

    [Header("Phase")]
    [SerializeField] private PhaseLoopManager phaseManager;

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

        if (phaseManager == null)
        {
            phaseManager = FindObjectOfType<PhaseLoopManager>();
        }

        StartCoroutine(RandomRotateLoop());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            Move(KeyCode.W);
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            Move(KeyCode.A);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            Move(KeyCode.S);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            Move(KeyCode.D);
        }
    }

    private void Move(KeyCode key)
    {
        Vector3 moveDir = keyMapping[key];

        transform.position += moveDir * stepDistance;
    }


    private IEnumerator RandomRotateLoop()
    {
        while (true)
        {
            if (PhaseLoopManager.GlobalState != GameState.Dream ||
                (phaseManager != null &&
                 phaseManager.IsBusy))
            {
                yield return null;

                continue;
            }

            yield return new WaitForSeconds(
                GetRotateDelay()
            );

            if (PhaseLoopManager.GlobalState != GameState.Dream ||
                (phaseManager != null &&
                 phaseManager.IsBusy))
            {
                continue;
            }

            yield return StartCoroutine(
                RotateRoom()
            );

            GenerateRandomMapping();
        }
    }

    private IEnumerator RotateRoom()
    {
        float direction = Random.value < 0.5f ? -1f : 1f;

        float rotated = 0f;

        while (rotated < rotateAngle)
        {
            float step = rotateSpeed * Time.deltaTime;

            roomRoot.Rotate(0f, 0f, step * direction);

            rotated += step;

            yield return null;
        }

        float z = roomRoot.eulerAngles.z;

        z = Mathf.Round(z / 90f) * 90f;

        roomRoot.eulerAngles = new Vector3(0f, 0f, z);
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

    private float GetRotateDelay()
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                return easyRotateDelay;

            case Difficulty.Medium:
                return mediumRotateDelay;

            case Difficulty.Hard:
                return hardRotateDelay;
        }

        return mediumRotateDelay;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRecorder : MonoBehaviour
{
    public static PlayerRecorder Instance;

    [SerializeField] private float recordRate = 0.02f;

    private class RecordData
    {
        public Vector3 position;
        public float time;
    }

    private List<RecordData> records = new List<RecordData>();
    private float timer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= recordRate)
        {
            timer = 0f;

            records.Add(new RecordData
            {
                position = transform.position,
                time = Time.time
            });

            if (records.Count > 2000)
            {
                records.RemoveAt(0);
            }
        }
    }

    public Vector3 GetPositionWithDelay(float delay)
    {
        float targetTime = Time.time - delay;

        for (int i = records.Count - 1; i >= 0; i--)
        {
            if (records[i].time <= targetTime)
            {
                return records[i].position;
            }
        }

        return transform.position;
    }
}
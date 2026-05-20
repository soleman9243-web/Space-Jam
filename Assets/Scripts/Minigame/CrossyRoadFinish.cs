using UnityEngine;

namespace SpaceJam.Minigames
{
    [RequireComponent(typeof(Collider2D))]
    public class CrossyRoadFinish : MonoBehaviour
    {
        private CrossyRoadMinigame manager;

        private void Start()
        {
            manager = FindObjectOfType<CrossyRoadMinigame>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                Debug.Log("[CrossyRoadFinish] Player reached the finish line!");
                if (manager != null)
                {
                    manager.CompleteMinigame();
                }
            }
        }
    }
}

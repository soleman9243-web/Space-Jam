using UnityEngine;

namespace SpaceJam.Minigames
{
    [RequireComponent(typeof(Collider2D))]
    public class TVVisionCone : MonoBehaviour
    {
        [SerializeField] private TVStealthTask parentTask;

        private void Awake()
        {
            // CRITICAL: Auto-find parent task if not serialized (fixes null reference at runtime)
            if (parentTask == null)
            {
                parentTask = GetComponentInParent<TVStealthTask>();
            }

            if (parentTask == null)
            {
                Debug.LogWarning("[TVVisionCone] Could not find TVStealthTask on parent! Vision cone collision will not work.");
            }
        }

        public void Initialize(TVStealthTask task)
        {
            parentTask = task;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (parentTask != null && parentTask.IsActive && !parentTask.IsCompleted && IsPlayer(collision))
            {
                Debug.Log("[TVVisionCone] Player ENTERED vision cone!");
                parentTask.OnPlayerDetected();
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (parentTask != null && parentTask.IsActive && !parentTask.IsCompleted && IsPlayer(collision))
            {
                parentTask.OnPlayerDetected();
            }
        }

        private bool IsPlayer(Collider2D other)
        {
            if (other == null) return false;
            if (other.CompareTag("Player")) return true;
            if (other.GetComponent<PlayerMovement>() != null || other.GetComponentInParent<PlayerMovement>() != null) return true;
            if (other.GetComponent<PlayerStatus>() != null || other.GetComponentInParent<PlayerStatus>() != null) return true;
            return false;
        }
    }
}

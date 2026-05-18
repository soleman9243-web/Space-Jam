using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatus : MonoBehaviour
{
    public static PlayerStatus Instance;

    [Header("Stability")]
    public Slider stabilitySlider;
    public Slider easeStabilitySlider;
    public float maxStability = 100f;
    public float stability;
    public float lerpSpeed = 0.02f;

    [Header("Death")]
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float deathDelay = 2f;

    public bool isDead = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        stability = maxStability;

        stabilitySlider.maxValue = maxStability;
        easeStabilitySlider.maxValue = maxStability;
    }

    void Update()
    {
        Stability();
        Test();
    }

    void Test()
    {
        if (Input.GetKeyUp(KeyCode.P))
        {
            ReduceStability(10);
        }
    }

    void Stability()
    {
        stability = Mathf.Clamp(stability, 0, maxStability);

        if (stabilitySlider.value != stability)
        {
            stabilitySlider.value = stability;
        }

        if (stabilitySlider.value != easeStabilitySlider.value)
        {
            easeStabilitySlider.value = Mathf.Lerp(easeStabilitySlider.value,stability,lerpSpeed);
        }

        /*if (stability <= 0 && !isDead)
        {
            Die();
        }*/
    }

    public void ReduceStability(float amount)
    {
        if (isDead)
        {
            return;
        }

        stability -= amount;

        Debug.Log("Player kehilangan stability: " + amount);
    }

    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        playerMovement.enabled = false;

        rb.velocity = Vector2.zero;

        anim.SetTrigger("Dead");

        StartCoroutine(DeathCoroutine());
    }

    IEnumerator DeathCoroutine()
    {
        yield return new WaitForSeconds(deathDelay);

        GameOver.Instance.Show();
    }
}
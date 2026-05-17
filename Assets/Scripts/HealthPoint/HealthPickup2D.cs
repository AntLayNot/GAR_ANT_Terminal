using UnityEngine;

public class HealthPickup2D : MonoBehaviour
{
    [Header("Heal")]
    [SerializeField] private int healAmount = 1;
    [SerializeField] private bool requireMissingHealth = true;

    [Header("Feedback visuel")]
    [SerializeField] private GameObject pickupEffect;

    [Header("Feedback audio")]
    [SerializeField] private AudioSource pickupAudioSource;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 1f;

    [Header("Destroy")]
    [SerializeField] private bool destroyAfterPickup = true;

    [Header("Hide On Pickup")]
    [SerializeField] private bool hideSpriteOnPickup = true;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D pickupCollider;

    private bool consumed = false;

    private void Awake()
    {
        if (pickupAudioSource == null)
            pickupAudioSource = FindFirstObjectByType<AudioSource>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (pickupCollider == null)
            pickupCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        if (!other.CompareTag("Player")) return;

        Health2D health = other.GetComponent<Health2D>();

        if (health == null)
            health = other.GetComponentInParent<Health2D>();

        if (health == null) return;
        if (health.IsDead) return;

        if (requireMissingHealth && health.currentHP >= health.maxHP)
            return;

        consumed = true;

        health.Heal(healAmount);

        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        float soundDuration = PlayPickupSound();

        if (pickupCollider != null)
            pickupCollider.enabled = false;

        if (hideSpriteOnPickup && spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (destroyAfterPickup)
        {
            Destroy(gameObject, soundDuration);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private float PlayPickupSound()
    {
        if (pickupAudioSource == null)
            return 0f;

        AudioClip clipToPlay = pickupClip;

        if (clipToPlay == null)
            clipToPlay = pickupAudioSource.clip;

        if (clipToPlay == null)
            return 0f;

        pickupAudioSource.PlayOneShot(clipToPlay, pickupVolume);

        return clipToPlay.length;
    }
}
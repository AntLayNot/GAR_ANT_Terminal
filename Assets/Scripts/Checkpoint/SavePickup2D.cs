using UnityEngine;

public class SavePickup2D : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private int chargesToGive = 1;
    [SerializeField] private bool destroyOnPickup = true;

    [Header("Feedback visuel")]
    [SerializeField] private GameObject pickupEffect;

    [Header("Feedback audio")]
    [SerializeField] private AudioSource pickupAudioSource;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 1f;

    [Header("Hide On Pickup")]
    [SerializeField] private bool hideSpriteOnPickup = true;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D pickupCollider;

    private bool collected = false;

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
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        PlayerSaveCharges saveCharges = other.GetComponent<PlayerSaveCharges>();

        if (saveCharges == null)
            saveCharges = other.GetComponentInParent<PlayerSaveCharges>();

        if (saveCharges == null) return;

        collected = true;

        saveCharges.AddCharges(chargesToGive);

        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        float soundDuration = PlayPickupSound();

        if (pickupCollider != null)
            pickupCollider.enabled = false;

        if (hideSpriteOnPickup && spriteRenderer != null)
            spriteRenderer.enabled = false;

        Debug.Log("[SavePickup] +" + chargesToGive + " charge(s) de sauvegarde");

        if (destroyOnPickup)
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
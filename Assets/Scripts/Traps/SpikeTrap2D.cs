using UnityEngine;

public class SpikeTrap2D : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageCooldown = 0.5f;

    [Tooltip("Layers qui peuvent prendre des dégâts : Player, Enemy, Boss, etc.")]
    [SerializeField] private LayerMask damageLayers;

    [Header("Knockback")]
    [SerializeField] private bool applyKnockbackOnPlayer = true;

    [Header("Audio")]
    [SerializeField] private AudioSource spikeAudioSource;
    [SerializeField] private AudioClip damageClip;
    [SerializeField, Range(0f, 1f)] private float damageVolume = 1f;

    private float lastDamageTime = -999f;

    private void Awake()
    {
        if (spikeAudioSource == null)
            spikeAudioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (!IsInDamageLayer(other.gameObject.layer))
            return;

        if (Time.time < lastDamageTime + damageCooldown)
            return;

        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable == null)
            damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(damage);
        lastDamageTime = Time.time;

        PlayDamageSound();
        TryApplyPlayerKnockback(other);
    }

    private void PlayDamageSound()
    {
        if (spikeAudioSource == null)
            return;

        AudioClip clipToPlay = damageClip;

        if (clipToPlay == null)
            clipToPlay = spikeAudioSource.clip;

        if (clipToPlay == null)
            return;

        spikeAudioSource.PlayOneShot(clipToPlay, damageVolume);
    }

    private void TryApplyPlayerKnockback(Collider2D other)
    {
        if (!applyKnockbackOnPlayer)
            return;

        if (!other.CompareTag("Player"))
            return;

        PlayerKnockback2D knockback = other.GetComponent<PlayerKnockback2D>();

        if (knockback == null)
            knockback = other.GetComponentInParent<PlayerKnockback2D>();

        if (knockback != null)
            knockback.ApplyKnockback(transform.position);
    }

    private bool IsInDamageLayer(int layer)
    {
        return (damageLayers.value & (1 << layer)) != 0;
    }
}
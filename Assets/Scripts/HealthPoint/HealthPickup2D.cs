using UnityEngine;

public class HealthPickup2D : MonoBehaviour
{
    [Header("Heal")]
    [SerializeField] private int healAmount = 1;
    [SerializeField] private bool requireMissingHealth = true;

    [Header("Feedback")]
    [SerializeField] private GameObject pickupEffect;
    [SerializeField] private AudioSource pickupSound;
    [SerializeField] private bool destroyAfterPickup = true;

    private bool consumed = false;

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

        if (pickupSound != null)
            pickupSound.Play();

        if (destroyAfterPickup)
        {
            if (pickupSound != null)
                Destroy(gameObject, pickupSound.clip != null ? pickupSound.clip.length : 0f);
            else
                Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
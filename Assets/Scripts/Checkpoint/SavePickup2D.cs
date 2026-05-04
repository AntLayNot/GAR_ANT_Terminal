using UnityEngine;

public class SavePickup2D : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private int chargesToGive = 1;
    [SerializeField] private bool destroyOnPickup = true;

    [Header("Feedback")]
    [SerializeField] private GameObject pickupEffect;
    [SerializeField] private AudioSource pickupSound;

    private bool collected = false;

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

        if (pickupSound != null)
            pickupSound.Play();

        Debug.Log("[SavePickup] +" + chargesToGive + " charge(s) de sauvegarde");

        if (destroyOnPickup)
        {
            if (pickupSound != null && pickupSound.clip != null)
                Destroy(gameObject, pickupSound.clip.length);
            else
                Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
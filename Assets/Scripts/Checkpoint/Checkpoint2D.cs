using UnityEngine;

public class Checkpoint2D : MonoBehaviour
{
    [Header("Checkpoint Cost")]
    [SerializeField] private int activationCost = 1;
    [SerializeField] private bool canOnlyBeActivatedOnce = true;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer checkpointRenderer;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.cyan;
    [SerializeField] private Color lockedColor = Color.red;

    [Header("Audio")]
    [SerializeField] private AudioSource checkpointAudioSource;

    [Tooltip("Son joué quand le checkpoint est activé avec succès.")]
    [SerializeField] private AudioClip activationClip;

    [Tooltip("Son joué quand le joueur n'a pas assez de charges.")]
    [SerializeField] private AudioClip lockedClip;

    [SerializeField, Range(0f, 1f)] private float activationVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float lockedVolume = 1f;

    [Header("Optional")]
    [SerializeField] private Transform respawnPointOverride;

    private bool isActivated = false;

    private void Awake()
    {
        if (checkpointAudioSource == null)
            checkpointAudioSource = FindFirstObjectByType<AudioSource>();

        if (checkpointRenderer == null)
            checkpointRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        UpdateVisualState();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerRespawn2D respawn = other.GetComponent<PlayerRespawn2D>();

        if (respawn == null)
            respawn = other.GetComponentInParent<PlayerRespawn2D>();

        if (respawn == null)
            return;

        if (canOnlyBeActivatedOnce && isActivated)
            return;

        PlayerSaveCharges saveCharges = other.GetComponent<PlayerSaveCharges>();

        if (saveCharges == null)
            saveCharges = other.GetComponentInParent<PlayerSaveCharges>();

        if (saveCharges == null)
        {
            Debug.LogWarning("[Checkpoint] Aucun PlayerSaveCharges trouvé sur le joueur.");
            PlaySound(lockedClip, lockedVolume);
            return;
        }

        if (!saveCharges.TrySpendCharges(activationCost))
        {
            Debug.Log("[Checkpoint] Pas assez de charges pour activer ce checkpoint.");
            SetLockedVisual();
            PlaySound(lockedClip, lockedVolume);
            return;
        }

        Vector3 checkpointPosition = respawnPointOverride != null
            ? respawnPointOverride.position
            : transform.position;

        respawn.SetCheckpoint(checkpointPosition);

        isActivated = true;
        UpdateVisualState();

        PlaySound(activationClip, activationVolume);

        Debug.Log("[Checkpoint] Activé avec succès.");
    }

    private void UpdateVisualState()
    {
        if (checkpointRenderer == null)
            return;

        checkpointRenderer.color = isActivated ? activeColor : inactiveColor;
    }

    private void SetLockedVisual()
    {
        if (checkpointRenderer == null)
            return;

        checkpointRenderer.color = lockedColor;
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (checkpointAudioSource == null)
            return;

        AudioClip clipToPlay = clip;

        if (clipToPlay == null)
            clipToPlay = checkpointAudioSource.clip;

        if (clipToPlay == null)
            return;

        checkpointAudioSource.PlayOneShot(clipToPlay, volume);
    }
}
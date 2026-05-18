using UnityEngine;

public class FallingTrapTrigger2D : MonoBehaviour
{
    [SerializeField] private FallingTrap2D trap;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool destroyTriggerAfterActivation = true;

    [Header("Audio")]
    [SerializeField] private AudioSource triggerAudioSource;
    [SerializeField] private AudioClip triggerClip;
    [SerializeField, Range(0f, 1f)] private float triggerVolume = 1f;

    private bool activated;

    private void Awake()
    {
        if (triggerAudioSource == null)
            triggerAudioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated)
            return;

        if (!other.CompareTag(targetTag))
            return;

        activated = true;

        PlaySound();

        if (trap != null)
            trap.TriggerFall();

        if (destroyTriggerAfterActivation)
            Destroy(gameObject, GetSoundDuration());
    }

    private void PlaySound()
    {
        if (triggerAudioSource == null)
            return;

        AudioClip clipToPlay = triggerClip;

        if (clipToPlay == null)
            clipToPlay = triggerAudioSource.clip;

        if (clipToPlay == null)
            return;

        triggerAudioSource.PlayOneShot(clipToPlay, triggerVolume);
    }

    private float GetSoundDuration()
    {
        if (triggerClip != null)
            return triggerClip.length;

        if (triggerAudioSource != null && triggerAudioSource.clip != null)
            return triggerAudioSource.clip.length;

        return 0f;
    }
}
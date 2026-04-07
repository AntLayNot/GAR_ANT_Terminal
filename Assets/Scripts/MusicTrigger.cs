using UnityEngine;

public class MusicTrigger2D : MonoBehaviour
{
    [Header("Temporary music")]
    public AudioClip triggerMusic;
    public bool stopWhenExit = true;
    public bool playOnlyOnce = false;

    private bool hasPlayed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Quelque chose est entré dans le trigger : " + other.name);

        if (!other.CompareTag("Player"))
        {
            Debug.Log("Ce n'est pas le joueur");
            return;
        }

        if (playOnlyOnce && hasPlayed)
        {
            Debug.Log("La musique a déjà été jouée une fois");
            return;
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogError("AudioManager.Instance est NULL");
            return;
        }

        if (triggerMusic == null)
        {
            Debug.LogError("Aucune musique assignée au trigger");
            return;
        }

        Debug.Log("Le joueur est entré dans le trigger, lancement de la musique");
        AudioManager.Instance.PlayTemporaryMusic(triggerMusic);

        hasPlayed = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!stopWhenExit) return;

        AudioManager.Instance.StopTemporaryMusic();
    }
}
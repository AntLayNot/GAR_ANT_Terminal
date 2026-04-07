using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music Sources")]
    public AudioSource mainMusicSource;
    public AudioSource temporaryMusicSource;

    [Header("Default Music")]
    public AudioClip defaultMusic;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (defaultMusic != null && mainMusicSource != null)
        {
            mainMusicSource.clip = defaultMusic;
            mainMusicSource.loop = true;
            mainMusicSource.Play();
        }
    }

    public void PlayMainMusic(AudioClip clip)
    {
        if (mainMusicSource == null || clip == null) return;

        mainMusicSource.clip = clip;
        mainMusicSource.loop = true;
        mainMusicSource.Play();
    }

    public void PauseMainMusic()
    {
        if (mainMusicSource != null && mainMusicSource.isPlaying)
            mainMusicSource.Pause();
    }

    public void ResumeMainMusic()
    {
        if (mainMusicSource != null)
            mainMusicSource.UnPause();
    }

    public void PlayTemporaryMusic(AudioClip clip, bool loop = true)
    {
        if (temporaryMusicSource == null || clip == null) return;

        PauseMainMusic();

        temporaryMusicSource.clip = clip;
        temporaryMusicSource.loop = loop;
        temporaryMusicSource.Play();
    }

    public void StopTemporaryMusic()
    {
        if (temporaryMusicSource == null) return;

        temporaryMusicSource.Stop();
        temporaryMusicSource.clip = null;

        ResumeMainMusic();
    }
}
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private const int SfxVoices = 8;

    [Header("Volums")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

    [Header("Efectes")]
    [SerializeField] private AudioClip jump;
    [SerializeField] private AudioClip doubleJump;
    [SerializeField] private AudioClip coin;
    [SerializeField] private AudioClip stomp;
    [SerializeField] private AudioClip hurt;
    [SerializeField] private AudioClip bossHit;
    [SerializeField] private AudioClip bossDefeated;
    [SerializeField] private AudioClip playerDeath;
    [SerializeField] private AudioClip victory;
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip buttonHover;
    [SerializeField] private AudioClip portal;

    private static AudioManager instance;

    private AudioSource musicSource;
    private AudioSource[] sfxSources;
    private int nextVoice;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                var prefab = Resources.Load<AudioManager>(nameof(AudioManager));
                instance = prefab != null ? Instantiate(prefab) : new GameObject(nameof(AudioManager)).AddComponent<AudioManager>();
                instance.name = nameof(AudioManager);
            }
            return instance;
        }
    }

    public AudioClip Jump => jump;
    public AudioClip DoubleJump => doubleJump;
    public AudioClip Coin => coin;
    public AudioClip Stomp => stomp;
    public AudioClip Hurt => hurt;
    public AudioClip BossHit => bossHit;
    public AudioClip BossDefeated => bossDefeated;
    public AudioClip PlayerDeath => playerDeath;
    public AudioClip Victory => victory;
    public AudioClip ButtonClick => buttonClick;
    public AudioClip ButtonHover => buttonHover;
    public AudioClip Portal => portal;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        sfxSources = new AudioSource[SfxVoices];
        for (int i = 0; i < SfxVoices; i++)
        {
            sfxSources[i] = gameObject.AddComponent<AudioSource>();
            sfxSources[i].playOnAwake = false;
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null)
        {
            musicSource.Stop();
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlaySfx(AudioClip clip, float volume = 1f, float pitchVariation = 0f)
    {
        if (clip == null) return;
        var source = sfxSources[nextVoice];
        nextVoice = (nextVoice + 1) % sfxSources.Length;
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.PlayOneShot(clip, volume * sfxVolume);
    }
}

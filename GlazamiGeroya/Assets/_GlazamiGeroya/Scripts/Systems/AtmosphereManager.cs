using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource sfx2DSource;
    [SerializeField] private AudioSource voiceSource;

    [Header("3D Sound Settings")]
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 20f;


    [Header("Ambience")]
    [SerializeField] private AudioClip startAmbienceClip;
    [SerializeField] private bool playAmbienceOnStart = true;

    private void Start()
    {
        if (playAmbienceOnStart && startAmbienceClip != null)
            SetAmbience(startAmbienceClip);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetAmbience(AudioClip clip, bool loop = true)
    {
        if (ambienceSource == null || clip == null)
            return;

        ambienceSource.clip = clip;
        ambienceSource.loop = loop;
        ambienceSource.spatialBlend = 0f;
        ambienceSource.Play();
    }

    public void Play2D(
    AudioClip clip,
    float volume = 1f,
    float fadeInDuration = 0f,
    float fadeOutDuration = 0f)
{
    if (clip == null)
        return;

    GameObject soundObject = new GameObject("2D Audio");
    soundObject.transform.SetParent(transform);

    AudioSource source = soundObject.AddComponent<AudioSource>();

    source.clip = clip;
    source.volume = 0f;
    source.spatialBlend = 0f;

    source.Play();

    StartCoroutine(PlayWithFade(source, volume, fadeInDuration, fadeOutDuration));

    Destroy(soundObject, clip.length + 0.1f);
}


    public void PlayVoice(AudioClip clip)
    {
        if (voiceSource == null || clip == null)
            return;

        voiceSource.spatialBlend = 0f;
        voiceSource.clip = clip;
        voiceSource.Play();
    }

        public void Play3D(
    AudioClip clip,
    Vector3 position,
    float volume = 1f,
    float minDistance = 1f,
    float maxDistance = 20f,
    AudioRolloffMode rolloffMode = AudioRolloffMode.Linear,
    float fadeInDuration = 0f,
    float fadeOutDuration = 0f)
    {
        if (clip == null)
            return;

        GameObject soundObject = new GameObject("3D Audio");
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();

        source.clip = clip;
        source.volume = 0f;
        source.spatialBlend = 1f;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = rolloffMode;

        source.Play();

        StartCoroutine(PlayWithFade(source, volume, fadeInDuration, fadeOutDuration));

        Destroy(soundObject, clip.length + 0.1f);
    }
    private IEnumerator PlayWithFade(
        AudioSource source,
        float targetVolume,
        float fadeInDuration,
        float fadeOutDuration)
    {
        if (source == null || source.clip == null)
            yield break;

        float clipLength = source.clip.length;

        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);

        float totalFadeDuration = fadeInDuration + fadeOutDuration;

        if (totalFadeDuration > clipLength)
        {
            float scale = clipLength / totalFadeDuration;
            fadeInDuration *= scale;
            fadeOutDuration *= scale;
        }

        if (fadeInDuration > 0f)
        {
            float timer = 0f;

            while (timer < fadeInDuration)
            {
                if (source == null)
                    yield break;

                timer += Time.deltaTime;
                source.volume = Mathf.Lerp(0f, targetVolume, timer / fadeInDuration);
                yield return null;
            }
        }

        if (source != null)
            source.volume = targetVolume;

        float normalPlayTime = clipLength - fadeInDuration - fadeOutDuration;

        if (normalPlayTime > 0f)
            yield return new WaitForSeconds(normalPlayTime);

        if (fadeOutDuration > 0f)
        {
            float timer = 0f;
            float startVolume = source != null ? source.volume : 0f;

            while (timer < fadeOutDuration)
            {
                if (source == null)
                    yield break;

                timer += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, timer / fadeOutDuration);
                yield return null;
            }
        }

        if (source != null)
            source.volume = 0f;
    }
    public void Initialize(GameManager manager)
    {
        // Пока можно оставить пустым.
    }
}
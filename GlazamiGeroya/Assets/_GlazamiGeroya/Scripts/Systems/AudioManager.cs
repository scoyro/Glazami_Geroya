using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource sfx2DSource;
    [SerializeField] private AudioSource voiceSource;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup ambienceMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup voiceMixerGroup;

    [Header("3D Audio Pool")]
    [SerializeField] private int poolSize = 12;

    [Header("Ambience")]
    [SerializeField] private AudioClip startAmbienceClip;
    [SerializeField] private bool playAmbienceOnStart = true;

    private AudioSource[] audio3DPool;
    private Coroutine sfx2DFadeRoutine;
    private Coroutine voiceFadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializeMainSources();
        Create3DPool();
    }

    private void Start()
    {
        if (playAmbienceOnStart && startAmbienceClip != null)
            SetAmbience(startAmbienceClip);
    }

    public void Initialize(GameManager manager)
    {
        // Метод оставлен для единой инициализации через GameManager.
        // Пока AudioManager не требует прямой ссылки на GameManager.
    }

    private void InitializeMainSources()
    {
        if (ambienceSource != null)
        {
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.outputAudioMixerGroup = ambienceMixerGroup;
        }

        if (sfx2DSource != null)
        {
            sfx2DSource.playOnAwake = false;
            sfx2DSource.loop = false;
            sfx2DSource.spatialBlend = 0f;
            sfx2DSource.outputAudioMixerGroup = sfxMixerGroup;
        }

        if (voiceSource != null)
        {
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 0f;
            voiceSource.outputAudioMixerGroup = voiceMixerGroup;
        }
    }

    private void Create3DPool()
    {
        poolSize = Mathf.Max(1, poolSize);
        audio3DPool = new AudioSource[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            GameObject sourceObject = new GameObject($"3D Audio Source {i}");
            sourceObject.transform.SetParent(transform);

            AudioSource source = sourceObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;
            source.dopplerLevel = 0f;
            source.outputAudioMixerGroup = sfxMixerGroup;

            audio3DPool[i] = source;
        }
    }

    public void SetAmbience(AudioClip clip, bool loop = true)
    {
        if (ambienceSource == null || clip == null)
            return;

        ambienceSource.outputAudioMixerGroup = ambienceMixerGroup;
        ambienceSource.clip = clip;
        ambienceSource.loop = loop;
        ambienceSource.spatialBlend = 0f;
        ambienceSource.volume = 1f;
        ambienceSource.Play();
    }

    public void StopAmbience()
    {
        if (ambienceSource == null)
            return;

        ambienceSource.Stop();
        ambienceSource.clip = null;
    }

    public void Play2D(
        AudioClip clip,
        float volume = 1f,
        float fadeInDuration = 0f,
        float fadeOutDuration = 0f)
    {
        if (sfx2DSource == null || clip == null)
            return;

        sfx2DSource.outputAudioMixerGroup = sfxMixerGroup;
        sfx2DSource.spatialBlend = 0f;
        sfx2DSource.pitch = 1f;

        if (fadeInDuration > 0f || fadeOutDuration > 0f)
        {
            if (sfx2DFadeRoutine != null)
                StopCoroutine(sfx2DFadeRoutine);

            sfx2DSource.Stop();
            sfx2DSource.clip = clip;
            sfx2DSource.volume = fadeInDuration > 0f ? 0f : volume;
            sfx2DSource.Play();

            sfx2DFadeRoutine = StartCoroutine(
                PlayWithFade(sfx2DSource, volume, fadeInDuration, fadeOutDuration)
            );
        }
        else
        {
            sfx2DSource.PlayOneShot(clip, volume);
        }
    }

    public void PlayVoice(
        AudioClip clip,
        float volume = 1f,
        float fadeInDuration = 0f,
        float fadeOutDuration = 0f)
    {
        if (voiceSource == null || clip == null)
            return;

        voiceSource.outputAudioMixerGroup = voiceMixerGroup;
        voiceSource.spatialBlend = 0f;
        voiceSource.pitch = 1f;

        if (voiceFadeRoutine != null)
            StopCoroutine(voiceFadeRoutine);

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.volume = fadeInDuration > 0f ? 0f : volume;
        voiceSource.Play();

        if (fadeInDuration > 0f || fadeOutDuration > 0f)
        {
            voiceFadeRoutine = StartCoroutine(
                PlayWithFade(voiceSource, volume, fadeInDuration, fadeOutDuration)
            );
        }
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
        if (clip == null || audio3DPool == null || audio3DPool.Length == 0)
            return;

        AudioSource source = GetFree3DSource();

        if (source == null)
            return;

        source.Stop();

        source.transform.position = position;
        source.outputAudioMixerGroup = sfxMixerGroup;

        source.clip = clip;
        source.volume = fadeInDuration > 0f ? 0f : volume;
        source.pitch = 1f;
        source.spatialBlend = 1f;
        source.minDistance = minDistance;
        source.maxDistance = Mathf.Max(minDistance, maxDistance);
        source.rolloffMode = rolloffMode;
        source.dopplerLevel = 0f;
        source.loop = false;

        source.Play();

        if (fadeInDuration > 0f || fadeOutDuration > 0f)
        {
            StartCoroutine(
                PlayWithFade(source, volume, fadeInDuration, fadeOutDuration)
            );
        }
    }

    private AudioSource GetFree3DSource()
    {
        foreach (AudioSource source in audio3DPool)
        {
            if (source != null && !source.isPlaying)
                return source;
        }

        // Если все источники заняты, переиспользуем первый.
        // Это лучше, чем создавать новый GameObject во время игры.
        return audio3DPool[0];
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

        if (totalFadeDuration > clipLength && totalFadeDuration > 0f)
        {
            float scale = clipLength / totalFadeDuration;
            fadeInDuration *= scale;
            fadeOutDuration *= scale;
        }

        if (fadeInDuration > 0f)
        {
            float time = 0f;

            while (time < fadeInDuration)
            {
                if (source == null)
                    yield break;

                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / fadeInDuration);
                source.volume = Mathf.Lerp(0f, targetVolume, t);

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
            float time = 0f;
            float startVolume = source != null ? source.volume : 0f;

            while (time < fadeOutDuration)
            {
                if (source == null)
                    yield break;

                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / fadeOutDuration);
                source.volume = Mathf.Lerp(startVolume, 0f, t);

                yield return null;
            }
        }

        if (source != null)
            source.volume = 0f;
    }
}
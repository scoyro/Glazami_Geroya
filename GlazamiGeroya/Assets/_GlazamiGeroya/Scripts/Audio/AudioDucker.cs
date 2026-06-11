using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioDucker : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    [Header("Exposed Parameters")]
    [SerializeField] private string ambienceVolumeParam = "AmbienceVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";

    [Header("Volume")]
    [SerializeField] private float normalVolumeDb = 0f;
    [SerializeField] private float duckedVolumeDb = -25f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.35f;

    private Coroutine routine;
    private float currentTargetDb;
    private bool isInitialized;

    private bool hasAmbience;
    private bool hasSfx;

    private void Awake()
    {
        InitializeParameters();
        currentTargetDb = normalVolumeDb;
    }

    private void InitializeParameters()
    {
        if (mixer == null)
        {
            Debug.LogWarning("AudioDucker: AudioMixer is not assigned.");
            return;
        }

        hasAmbience = mixer.GetFloat(ambienceVolumeParam, out _);
        hasSfx = mixer.GetFloat(sfxVolumeParam, out _);

        if (!hasAmbience)
            Debug.LogWarning($"AudioDucker: exposed parameter not found: {ambienceVolumeParam}");

        if (!hasSfx)
            Debug.LogWarning($"AudioDucker: exposed parameter not found: {sfxVolumeParam}");

        isInitialized = true;
    }

    public void Duck()
    {
        StartFade(duckedVolumeDb);
    }

    public void Restore()
    {
        StartFade(normalVolumeDb);
    }

    private void StartFade(float targetDb)
    {
        if (mixer == null)
            return;

        if (!isInitialized)
            InitializeParameters();

        if (Mathf.Approximately(currentTargetDb, targetDb))
            return;

        currentTargetDb = targetDb;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(FadeRoutine(targetDb));
    }

    private IEnumerator FadeRoutine(float targetDb)
    {
        float startAmbienceDb = normalVolumeDb;
        float startSfxDb = normalVolumeDb;

        if (hasAmbience)
            mixer.GetFloat(ambienceVolumeParam, out startAmbienceDb);

        if (hasSfx)
            mixer.GetFloat(sfxVolumeParam, out startSfxDb);

        if (fadeDuration <= 0f)
        {
            ApplyVolume(targetDb, targetDb);
            routine = null;
            yield break;
        }

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);

            float ambienceDb = Mathf.Lerp(startAmbienceDb, targetDb, t);
            float sfxDb = Mathf.Lerp(startSfxDb, targetDb, t);

            ApplyVolume(ambienceDb, sfxDb);

            yield return null;
        }

        ApplyVolume(targetDb, targetDb);

        routine = null;
    }

    private void ApplyVolume(float ambienceDb, float sfxDb)
    {
        if (hasAmbience)
            mixer.SetFloat(ambienceVolumeParam, ambienceDb);

        if (hasSfx)
            mixer.SetFloat(sfxVolumeParam, sfxDb);
    }
}
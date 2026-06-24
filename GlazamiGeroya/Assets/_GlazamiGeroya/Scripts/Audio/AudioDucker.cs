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
    [Tooltip("До скольки децибел приглушать звук")]
    [SerializeField] private float duckedVolumeDb = -25f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.35f;

    private Coroutine routine;
    private bool isInitialized;

    private bool hasAmbience;
    private bool hasSfx;

    private void Awake()
    {
        InitializeParameters();
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
        // Приглушаем оба канала до duckedVolumeDb
        StartFade(duckedVolumeDb, duckedVolumeDb);
    }

    public void Restore()
    {
        // Запрашиваем актуальные настройки пользователя
        float targetAmbienceDb = 0f;
        float targetSfxDb = 0f;

        if (SettingsManager.Instance != null)
        {
            // Берем линейные значения из настроек и переводим их в децибелы
            targetAmbienceDb = Mathf.Log10(SettingsManager.Instance.AmbienceVolume) * 20f;
            targetSfxDb = Mathf.Log10(SettingsManager.Instance.SFXVolume) * 20f;
        }

        // Плавно возвращаем каждый канал к его собственной пользовательской громкости
        StartFade(targetAmbienceDb, targetSfxDb);
    }

    private void StartFade(float targetAmbienceDb, float targetSfxDb)
    {
        if (mixer == null)
            return;

        if (!isInitialized)
            InitializeParameters();

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(FadeRoutine(targetAmbienceDb, targetSfxDb));
    }

    private IEnumerator FadeRoutine(float targetAmbienceDb, float targetSfxDb)
    {
        float startAmbienceDb = 0f;
        float startSfxDb = 0f;

        // Получаем текущие значения прямо из миксера, чтобы начать плавный переход оттуда, где мы сейчас
        if (hasAmbience)
            mixer.GetFloat(ambienceVolumeParam, out startAmbienceDb);

        if (hasSfx)
            mixer.GetFloat(sfxVolumeParam, out startSfxDb);

        if (fadeDuration <= 0f)
        {
            ApplyVolume(targetAmbienceDb, targetSfxDb);
            routine = null;
            yield break;
        }

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);

            float currentAmbienceDb = Mathf.Lerp(startAmbienceDb, targetAmbienceDb, t);
            float currentSfxDb = Mathf.Lerp(startSfxDb, targetSfxDb, t);

            ApplyVolume(currentAmbienceDb, currentSfxDb);

            yield return null;
        }

        ApplyVolume(targetAmbienceDb, targetSfxDb);
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
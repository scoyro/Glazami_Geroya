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
    [SerializeField] private string footstepsVolumeParam = "FootstepsVolume";
    [SerializeField] private string voiceVolumeParam = "VoiceVolume";

    [Header("Volume")]
    [SerializeField] private float normalVolumeDb = 0f;
    [SerializeField] private float duckedVolumeDb = -25f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.35f;

    private Coroutine routine;

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

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(FadeRoutine(targetDb));
    }

    private IEnumerator FadeRoutine(float targetDb)
    {
        mixer.GetFloat(sfxVolumeParam, out float startSfxDb);
        mixer.GetFloat(ambienceVolumeParam, out float startAmbienceDb);
        mixer.GetFloat(footstepsVolumeParam, out float startFootstepsDb);
        mixer.GetFloat(voiceVolumeParam, out float startVoiceDb);

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);

            mixer.SetFloat(sfxVolumeParam, Mathf.Lerp(startSfxDb, targetDb, t));
            mixer.SetFloat(ambienceVolumeParam, Mathf.Lerp(startAmbienceDb, targetDb, t));
            mixer.SetFloat(footstepsVolumeParam, Mathf.Lerp(startFootstepsDb, targetDb, t));
            mixer.SetFloat(voiceVolumeParam, Mathf.Lerp(startVoiceDb, targetDb, t));

            yield return null;
        }

        mixer.SetFloat(sfxVolumeParam, targetDb);
        mixer.SetFloat(ambienceVolumeParam, targetDb);
        mixer.SetFloat(footstepsVolumeParam, targetDb);
        mixer.SetFloat(voiceVolumeParam, targetDb);

        routine = null;
    }
}
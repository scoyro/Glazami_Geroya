using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class ZoneOutVolumeEffect : MonoBehaviour, IPlayableVfx
{
    [SerializeField] private Volume volume;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float holdDuration = 1.4f;
    [SerializeField] private float fadeOutDuration = 0.9f;

    [Header("Strength")]
    [Range(0f, 1f)]
    [SerializeField] private float maxWeight = 1f;

    public float TotalDuration => fadeInDuration + holdDuration + fadeOutDuration;

    private Coroutine routine;

    private void Awake()
    {
        if (volume == null)
            volume = GetComponent<Volume>();

        if (volume != null)
            volume.weight = 0f;
    }

    public void Play()
    {
        if (volume == null)
            return;

        if (routine != null)
            StopCoroutine(routine);

        gameObject.SetActive(true);
        routine = StartCoroutine(PlayRoutine());
    }   
    public void SetWeight(float weight)
    {
        if (volume == null)
            return;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        gameObject.SetActive(true);
        volume.weight = Mathf.Clamp01(weight);
    }

    public void Stop()
    {
        if (routine != null)
            StopCoroutine(routine);

        if (volume != null)
            volume.weight = 0f;

        routine = null;
    }

    private IEnumerator PlayRoutine()
    {
        yield return Fade(volume.weight, maxWeight, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return Fade(volume.weight, 0f, fadeOutDuration);

        routine = null;
    }
    public Coroutine PlayCustom(float customFadeIn, float customHold, float customFadeOut, float customMaxWeight)
    {
        if (volume == null)
            return null;

        if (routine != null)
            StopCoroutine(routine);

        gameObject.SetActive(true);
        routine = StartCoroutine(PlayCustomRoutine(customFadeIn, customHold, customFadeOut, customMaxWeight));
        return routine;
    }

    private IEnumerator PlayCustomRoutine(float customFadeIn, float customHold, float customFadeOut, float customMaxWeight)
    {
        customMaxWeight = Mathf.Clamp01(customMaxWeight);

        yield return Fade(volume.weight, customMaxWeight, customFadeIn);

        if (customHold > 0f)
            yield return new WaitForSeconds(customHold);

        yield return Fade(volume.weight, 0f, customFadeOut);

        routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            volume.weight = to;
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            volume.weight = Mathf.Lerp(from, to, t);
            yield return null;
        }

        volume.weight = to;
    }
}
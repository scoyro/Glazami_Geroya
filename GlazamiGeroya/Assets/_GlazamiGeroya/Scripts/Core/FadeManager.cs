using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float defaultFadeDuration = 0.5f;

    private Coroutine currentFadeRoutine;
    private bool isBusy;

    public bool IsBusy => isBusy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Если нужен один объект на всю игру:
        // DontDestroyOnLoad(gameObject);

        if (fadeImage == null)
        {
            Debug.LogError("ScreenFader: fadeImage не назначен в инспекторе.");
            return;
        }

        SetAlpha(0f);
    }

    public void FadeOut(Action onComplete = null, float duration = -1f)
    {
        if (duration < 0f)
            duration = defaultFadeDuration;

        StartNewFade(Fade(1f, duration, onComplete));
    }

    public void FadeIn(Action onComplete = null, float duration = -1f)
    {
        if (duration < 0f)
            duration = defaultFadeDuration;

        StartNewFade(Fade(0f, duration, onComplete));
    }

    public void FadeAction(Action action, float fadeOutDuration = -1f, float fadeInDuration = -1f, float waitBetween = 0f)
    {
        StartNewFade(FadeActionRoutine(action, fadeOutDuration, fadeInDuration, waitBetween));
    }

    public void FadeAction(IEnumerator actionRoutine, float fadeOutDuration = -1f, float fadeInDuration = -1f, float waitBetween = 0f)
    {
        StartNewFade(FadeActionRoutineCoroutine(actionRoutine, fadeOutDuration, fadeInDuration, waitBetween));
    }

    private void StartNewFade(IEnumerator routine)
    {
        if (currentFadeRoutine != null)
        {
            StopCoroutine(currentFadeRoutine);
        }

        currentFadeRoutine = StartCoroutine(routine);
    }

    private IEnumerator FadeActionRoutine(Action action, float fadeOutDuration, float fadeInDuration, float waitBetween)
    {
        isBusy = true;

        if (fadeOutDuration < 0f)
            fadeOutDuration = defaultFadeDuration;

        if (fadeInDuration < 0f)
            fadeInDuration = defaultFadeDuration;

        yield return FadeRoutine(1f, fadeOutDuration);

        if (waitBetween > 0f)
            yield return new WaitForSeconds(waitBetween);

        action?.Invoke();

        if (waitBetween > 0f)
            yield return new WaitForSeconds(waitBetween);

        yield return FadeRoutine(0f, fadeInDuration);

        isBusy = false;
        currentFadeRoutine = null;
    }

    private IEnumerator FadeActionRoutineCoroutine(IEnumerator actionRoutine, float fadeOutDuration, float fadeInDuration, float waitBetween)
    {
        isBusy = true;

        if (fadeOutDuration < 0f)
            fadeOutDuration = defaultFadeDuration;

        if (fadeInDuration < 0f)
            fadeInDuration = defaultFadeDuration;

        yield return FadeRoutine(1f, fadeOutDuration);

        if (waitBetween > 0f)
            yield return new WaitForSeconds(waitBetween);

        if (actionRoutine != null)
            yield return StartCoroutine(actionRoutine);

        if (waitBetween > 0f)
            yield return new WaitForSeconds(waitBetween);

        yield return FadeRoutine(0f, fadeInDuration);

        isBusy = false;
        currentFadeRoutine = null;
    }

    private IEnumerator Fade(float targetAlpha, float duration, Action onComplete)
    {
        isBusy = true;

        yield return FadeRoutine(targetAlpha, duration);

        onComplete?.Invoke();

        isBusy = false;
        currentFadeRoutine = null;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = fadeImage.color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }
}
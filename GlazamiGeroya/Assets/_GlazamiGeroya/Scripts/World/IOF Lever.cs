using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class FireSuppressionLeverController : MonoBehaviour
{
    [Header("Required Procedure")]
    [SerializeField] private PanelProcedureController panelProcedureController;
    [SerializeField] private bool requirePanelCompleted = true;
    [Header("Siren")]
    [SerializeField] private AudioSource sirenAudioSource;
    [SerializeField] private bool stopSirenOnSuppressionFinished = true;
    [SerializeField] private float sirenFadeOutDuration = 1.5f;
    
    [Header("Lever Visual")]
    [SerializeField] private Transform leverTransform;
    [SerializeField] private Vector3 localRotationAxis = Vector3.forward;
    [SerializeField] private float rotationAngle = 180f;
    [SerializeField] private float rotateDuration = 1.2f;

    [Header("Interaction")]
    [SerializeField] private InteractionTarget interactionTarget;
    [SerializeField] private Collider interactionCollider;
    [SerializeField] private bool disableAfterUse = true;

    [Header("Audio")]
    [SerializeField] private AudioClip leverTurnClip;

    [Tooltip("AudioSource, через который будет играть звук работы системы пожаротушения.")]
    [SerializeField] private AudioSource suppressionWorkAudioSource;

    [Tooltip("Если 0 или меньше, длительность берётся из suppressionWorkAudioSource.clip.length.")]
    [SerializeField] private float suppressionWorkDuration = 0f;

    [Header("Messages")]
    [TextArea]
    [SerializeField] private string notReadyThought =
        "Сначала нужно подготовить помещение: закрыть дверь, отключить вентиляцию и включить герметизацию.";

    [TextArea]
    [SerializeField] private string systemStartedThought =
        "Система пожаротушения запущена.";

    [TextArea]
    [SerializeField] private string fireExtinguishedThought =
        "Пожар потушен!";

    [SerializeField] private string systemStartedUiMessage = "Система пожаротушения активирована";

    [Header("Events")]
    [SerializeField] private UnityEvent onFireSuppressionStarted;
    [SerializeField] private UnityEvent onFireSuppressionFinished;

    private bool isStarted;
    private bool isFinished;
    private bool isRotating;
    private bool isWorking;

    public bool IsStarted => isStarted;
    public bool IsFinished => isFinished;
    public bool IsWorking => isWorking;

    public void TryActivate()
    {
        if (isStarted || isRotating || isWorking)
            return;

        if (requirePanelCompleted && (panelProcedureController == null || !panelProcedureController.ProcedureCompleted))
        {
            RequestThought(notReadyThought, 4f);
            return;
        }

        StartCoroutine(ActivateRoutine());
    }

    private IEnumerator ActivateRoutine()
    {
        isRotating = true;

        if (leverTurnClip != null && AudioManager.Instance != null)
        {
            Vector3 soundPosition = leverTransform != null ? leverTransform.position : transform.position;
            AudioManager.Instance.Play3D(leverTurnClip, soundPosition);
        }

        yield return RotateLever();

        isRotating = false;

        yield return StartSuppressionWorkRoutine();
    }

    private IEnumerator RotateLever()
    {
        if (leverTransform == null)
            yield break;

        Quaternion startRotation = leverTransform.localRotation;

        Vector3 axis = localRotationAxis.normalized;
        Quaternion targetRotation = startRotation * Quaternion.AngleAxis(rotationAngle, axis);

        float time = 0f;

        while (time < rotateDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / rotateDuration);
            t = Smooth(t);

            leverTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        leverTransform.localRotation = targetRotation;
    }

    private IEnumerator StartSuppressionWorkRoutine()
    {
        isStarted = true;
        isWorking = true;

        RequestThought(systemStartedThought, 4f);

        if (!string.IsNullOrWhiteSpace(systemStartedUiMessage))
            GameManager.Instance?.EventManager?.RequestUiMessage(systemStartedUiMessage, 4f);

        onFireSuppressionStarted?.Invoke();

        float duration = suppressionWorkDuration;

        if (duration <= 0f && suppressionWorkAudioSource != null && suppressionWorkAudioSource.clip != null)
            duration = suppressionWorkAudioSource.clip.length;

        if (duration <= 0f)
            duration = 5f;

        if (suppressionWorkAudioSource != null)
        {
            suppressionWorkAudioSource.Stop();
            suppressionWorkAudioSource.loop = false;
            suppressionWorkAudioSource.Play();
        }

        yield return new WaitForSeconds(duration);

        if (suppressionWorkAudioSource != null)
        suppressionWorkAudioSource.Stop();

        if (stopSirenOnSuppressionFinished && sirenAudioSource != null)
            yield return FadeOutAndStopAudio(sirenAudioSource, sirenFadeOutDuration);

        isWorking = false;
        isFinished = true;

        RequestThought(fireExtinguishedThought, 4f);

        onFireSuppressionFinished?.Invoke();

        if (disableAfterUse)
        {
            if (interactionCollider != null)
                interactionCollider.enabled = false;

            if (interactionTarget != null)
                interactionTarget.SetPromptText("");
        }
    }
    private IEnumerator FadeOutAndStopAudio(AudioSource source, float duration)
    {
        if (source == null)
            yield break;

        if (!source.isPlaying)
            yield break;

        float startVolume = source.volume;

        if (duration <= 0f)
        {
            source.Stop();
            source.volume = startVolume;
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            source.volume = Mathf.Lerp(startVolume, 0f, t);

            yield return null;
        }

        source.Stop();
        source.volume = startVolume;
    }
    private void RequestThought(string text, float duration)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        GameManager.Instance?.EventManager?.RequestThought(text, duration);
    }

    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
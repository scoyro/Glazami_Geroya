using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DoorExplosionEndingController : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private Transform doorTransform;
    [SerializeField] private Transform doorSlightlyOpenPoint;
    [SerializeField] private float doorOpenDuration = 0.45f;

    [Header("Interaction")]
    [SerializeField] private InteractionTarget interactionTarget;
    [SerializeField] private Collider interactionCollider;
    [SerializeField] private bool disableAfterUse = true;

    [Header("Player Lock")]
    [SerializeField] private PlayerControlLock playerControlLock;
    [SerializeField] private InteractionSystem interactionSystem;

    [Header("Audio")]
    [SerializeField] private AudioClip doorOpenClip;
    [SerializeField] private AudioClip explosionClip;
    [SerializeField] private Transform soundPoint;

    [Header("Timing")]
    [SerializeField] private float delayBeforeExplosion = 0.6f;
    [SerializeField] private float fadeOutAfterExplosionDelay = 0.15f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Thought")]
    [TextArea]
    [SerializeField] private string beforeOpenThought = "Плохое предчувствие...";

    [SerializeField] private float thoughtDuration = 2f;

    [Header("Ending")]
    [SerializeField] private EndingController endingController;
    [SerializeField] private string endingId = "iof_wrong_solution";
    [SerializeField] private bool playEndingAutomatically = true;
    [Header("Visual Explosion")]
    [SerializeField] private Light explosionLight;
    [SerializeField] private float lightSpikeDuration = 0.05f;
    [SerializeField] private float maxLightIntensity = 50f;
    [Header("Events")]
    [SerializeField] private UnityEvent onDoorStartedOpening;
    [SerializeField] private UnityEvent onExplosion;
    [SerializeField] private UnityEvent onSequenceFinished;

    private bool used;
    private bool isRunning;

    public void TryOpenDoorAndExplode()
    {
        if (used || isRunning)
            return;

        StartCoroutine(ExplosionDoorRoutine());
    }

    private IEnumerator ExplosionDoorRoutine()
    {
        used = true;
        isRunning = true;

        if (disableAfterUse)
        {
            if (interactionCollider != null)
                interactionCollider.enabled = false;

            if (interactionTarget != null)
                interactionTarget.SetPromptText("");
        }

        LockPlayer();

        if (!string.IsNullOrWhiteSpace(beforeOpenThought))
            GameManager.Instance?.EventManager?.RequestThought(beforeOpenThought, thoughtDuration);

        if (thoughtDuration > 0f)
            yield return new WaitForSeconds(thoughtDuration);

        onDoorStartedOpening?.Invoke();

        Play3D(doorOpenClip);

        yield return OpenDoorSlightlyRoutine();

        if (delayBeforeExplosion > 0f)
            yield return new WaitForSeconds(delayBeforeExplosion);

        if (explosionLight != null)
            StartCoroutine(LightFlashRoutine());

        Play3D(explosionClip);

        onExplosion?.Invoke();

        if (fadeOutAfterExplosionDelay > 0f)
            yield return new WaitForSeconds(fadeOutAfterExplosionDelay);

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOut(null, fadeOutDuration);

            while (ScreenFader.Instance.IsBusy)
                yield return null;
        }

        if (playEndingAutomatically && endingController != null)
            endingController.PlayEnding(endingId);

        onSequenceFinished?.Invoke();

        isRunning = false;
    }

    private IEnumerator LightFlashRoutine()
    {
        explosionLight.enabled = true;
        float time = 0f;

        // Резко поднимаем яркость
        while (time < lightSpikeDuration)
        {
            time += Time.deltaTime;
            float t = time / lightSpikeDuration;
            explosionLight.intensity = Mathf.Lerp(0f, maxLightIntensity, t);
            yield return null;
        }

        explosionLight.intensity = maxLightIntensity;

        // Можно добавить небольшое затухание, пока экран чернеет
        time = 0f;
        while (time < fadeOutAfterExplosionDelay + fadeOutDuration)
        {
            time += Time.deltaTime;
            float t = time / (fadeOutAfterExplosionDelay + fadeOutDuration);
            explosionLight.intensity = Mathf.Lerp(maxLightIntensity, 0f, t);
            yield return null;
        }
    }
    
    private IEnumerator OpenDoorSlightlyRoutine()
    {
        if (doorTransform == null || doorSlightlyOpenPoint == null)
            yield break;

        Quaternion startRotation = doorTransform.rotation;
        Quaternion targetRotation = doorSlightlyOpenPoint.rotation;

        float time = 0f;

        while (time < doorOpenDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / doorOpenDuration);
            t = Smooth(t);

            doorTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        doorTransform.rotation = targetRotation;
    }

    private void LockPlayer()
    {
        playerControlLock?.LockControls();
        interactionSystem?.LockInteraction();
    }

    private void Play3D(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;

        Vector3 position = soundPoint != null ? soundPoint.position : transform.position;
        AudioManager.Instance.Play3D(clip, position);
    }

    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
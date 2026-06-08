using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CanonicalValveEndingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ValveCutsceneController valveCutsceneController;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerControlLock playerControlLock;
    [SerializeField] private InteractionSystem interactionSystem;
    [SerializeField] private EndingController endingController;

    [Header("Staging")]
    [SerializeField] private Transform canonicalStartPose;

    [Header("Camera")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform lookAtHandsTarget;
    [SerializeField] private Transform lookAtDoorTarget;

    [Header("Hands")]
    [SerializeField] private GameObject normalHands;
    [SerializeField] private GameObject burnedHands;
    [SerializeField] private Animator burnedHandsAnimator;
    [SerializeField] private string lowerHandsTrigger = "LowerHands";

    [Header("Walk")]
    [SerializeField] private Transform doorWalkTarget;
    [SerializeField] private float woundedWalkSpeed = 0.65f;
    [SerializeField] private float maxWalkDuration = 18f;

    [Header("Timing")]
    [SerializeField] private float delayAfterValveClosed = 0.7f;
    [SerializeField] private float lookAtHandsDuration = 1.1f;
    [SerializeField] private float holdHandsDuration = 1.6f;
    [SerializeField] private float lowerHandsDuration = 0.9f;
    [SerializeField] private float lookAtDoorDuration = 1.0f;
    [SerializeField] private float delayBeforeFade = 0.25f;
    [SerializeField] private float fadeOutDuration = 1.2f;
    [SerializeField] private float knockDelayAfterFadeStart = 0.75f;
    [SerializeField] private float delayAfterKnock = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip knockClip;
    [SerializeField] private AudioClip breathingClip;
    [SerializeField] private AudioSource breathingAudioSource;

    [Header("Ending")]
    [SerializeField] private string endingId = "aldar_canonical";

    [Header("Events")]
    [SerializeField] private UnityEvent onCanonicalEndingStarted;
    [SerializeField] private UnityEvent onHandsShown;
    [SerializeField] private UnityEvent onWalkStarted;
    [SerializeField] private UnityEvent onDoorReached;

    private bool isRunning;
    private Coroutine routine;

    public bool IsRunning => isRunning;

    public void StartEnding()
    {
        if (isRunning)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        isRunning = true;

        onCanonicalEndingStarted?.Invoke();

        interactionSystem?.LockInteraction();
        playerControlLock?.LockControls();

        if (playerController != null)
        {
            playerController.StopCinematicWalk();
            playerController.SetNormalHeadBobSuppressed(true);
            playerController.SetCameraExternallyControlled(true);
        }

        StartBreathing();

        yield return new WaitForSeconds(delayAfterValveClosed);

        // Пока камера ещё у вентиля, незаметно ставим тело Алдара
        // в фиксированную постановочную позицию.
        if (valveCutsceneController != null && canonicalStartPose != null)
            valveCutsceneController.WarpPlayerRootToPose(canonicalStartPose);

        // Возвращаем камеру к телу, но управление не отдаём.
        if (valveCutsceneController != null)
            yield return valveCutsceneController.ReturnCameraToPlayerBodyOnly();

        // Камера остаётся в голове игрока и только поворачивается вниз на руки.
        if (playerCamera != null && lookAtHandsTarget != null)
            yield return RotateCameraToLookTarget(lookAtHandsTarget, lookAtHandsDuration);

        ShowBurnedHands();
        onHandsShown?.Invoke();

        yield return new WaitForSeconds(holdHandsDuration);

        LowerHands();

        yield return new WaitForSeconds(lowerHandsDuration);

        // Камера остаётся в голове игрока и только поворачивается к двери.
        if (playerCamera != null && lookAtDoorTarget != null)
    yield return RotateCameraToLookTarget(lookAtDoorTarget, lookAtDoorDuration);

    // ВАЖНО:
    // Перед ходьбой разворачиваем тело Алдара в сторону двери.
    // Камера в этот момент ещё отсоединена и удерживается forceCameraPose,
    // поэтому игрок не увидит резкого поворота тела.
    if (valveCutsceneController != null && doorWalkTarget != null)
        valveCutsceneController.RotatePlayerRootTowards(doorWalkTarget);

    // Теперь прикрепляем камеру обратно уже к телу,
    // которое смотрит в сторону двери.
    if (valveCutsceneController != null)
        valveCutsceneController.AttachCameraBackToPlayerKeepWorldPose();

    if (playerController != null)
    {
        playerController.SetCameraExternallyControlled(false);
        playerController.StartCinematicWalk(doorWalkTarget, woundedWalkSpeed, true);
    }

        playerControlLock?.UnlockControls();

        onWalkStarted?.Invoke();

        yield return WaitUntilDoorReached();

        if (playerController != null)
        {
            playerController.StopCinematicWalk();
            playerController.SetNormalHeadBobSuppressed(true);
        }

        playerControlLock?.LockControls();
        interactionSystem?.LockInteraction();

        onDoorReached?.Invoke();

        yield return new WaitForSeconds(delayBeforeFade);

        ScreenFader.Instance?.FadeOut(null, fadeOutDuration);

        yield return new WaitForSeconds(knockDelayAfterFadeStart);

        PlayKnock();

        yield return new WaitForSeconds(delayAfterKnock);

        StopBreathing();

        if (endingController != null)
            endingController.PlayEnding(endingId);

        isRunning = false;
        routine = null;
    }

    private IEnumerator RotateCameraToLookTarget(Transform lookTarget, float duration)
    {
        if (playerCamera == null || lookTarget == null)
            yield break;

        Vector3 cameraPosition = playerCamera.position;
        Quaternion startRotation = playerCamera.rotation;

        Vector3 direction = lookTarget.position - cameraPosition;

        if (direction.sqrMagnitude < 0.0001f)
            yield break;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (duration <= 0f)
        {
            if (valveCutsceneController != null)
                valveCutsceneController.SetForcedCameraPose(cameraPosition, targetRotation);
            else
                playerCamera.SetPositionAndRotation(cameraPosition, targetRotation);

            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = Smooth(t);

            Quaternion rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            if (valveCutsceneController != null)
                valveCutsceneController.SetForcedCameraPose(cameraPosition, rotation);
            else
                playerCamera.SetPositionAndRotation(cameraPosition, rotation);

            yield return null;
        }

        if (valveCutsceneController != null)
            valveCutsceneController.SetForcedCameraPose(cameraPosition, targetRotation);
        else
            playerCamera.SetPositionAndRotation(cameraPosition, targetRotation);
    }

    private IEnumerator WaitUntilDoorReached()
    {
        float timer = 0f;

        while (timer < maxWalkDuration)
        {
            timer += Time.deltaTime;

            if (playerController != null && playerController.CinematicReachedTarget)
                yield break;

            yield return null;
        }
    }

    private void ShowBurnedHands()
    {
        if (normalHands != null)
            normalHands.SetActive(false);

        if (burnedHands != null)
            burnedHands.SetActive(true);
    }

    private void LowerHands()
    {
        if (burnedHandsAnimator != null && !string.IsNullOrWhiteSpace(lowerHandsTrigger))
        {
            burnedHandsAnimator.SetTrigger(lowerHandsTrigger);
            return;
        }

        if (burnedHands != null)
            burnedHands.SetActive(false);
    }

    private void StartBreathing()
    {
        if (breathingAudioSource == null)
            return;

        if (breathingClip != null)
            breathingAudioSource.clip = breathingClip;

        breathingAudioSource.loop = true;
        breathingAudioSource.Play();
    }

    private void StopBreathing()
    {
        if (breathingAudioSource != null)
            breathingAudioSource.Stop();
    }

    private void PlayKnock()
    {
        if (knockClip == null)
            return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(knockClip);
            return;
        }

        AudioSource.PlayClipAtPoint(knockClip, transform.position);
    }

    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
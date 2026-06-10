using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CanonicalValveEndingController : MonoBehaviour
{
    [System.Serializable]
    private class WalkThoughtPoint
    {
        [Range(0f, 1f)]
        public float progress = 0.5f;

        [TextArea(2, 4)]
        public string thoughtText;

        public float stopDuration = 1.5f;

        [HideInInspector]
        public bool played;
    }

    [Header("References")]
    [SerializeField] private ValveCutsceneController valveCutsceneController;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerControlLock playerControlLock;
    [SerializeField] private InteractionSystem interactionSystem;
    [SerializeField] private EndingController endingController;

    [Header("UI")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private string walkMessage = "Удерживайте W, чтобы идти";
    [SerializeField] private float walkMessageDuration = 3f;

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
    [SerializeField] private string inspectHandsTrigger = "InspectHands";
    [SerializeField] private float inspectHandsAnimationDuration = 3.2f;
    [SerializeField] private float lookAtHandsDelayAfterAnimationStart = 0.25f;

    [Header("Walk")]
    [SerializeField] private Transform doorWalkTarget;
    [SerializeField] private float woundedWalkSpeed = 0.65f;
    [SerializeField] private float maxWalkDuration = 18f;

    [Header("Walk Thoughts")]
    [SerializeField] private Transform walkStartPoint;
    [SerializeField] private WalkThoughtPoint[] walkThoughts;
    [SerializeField] private float thoughtCheckInterval = 0.05f;

    [Header("Walk Thought Fade")]
    [SerializeField] private bool fadeDuringWalkThoughts = true;
    [SerializeField] private float thoughtFadeOutDuration = 0.45f;
    [SerializeField] private float thoughtBlackHoldDuration = 1.2f;
    [SerializeField] private float thoughtFadeInDuration = 0.8f;
    [SerializeField] private float thoughtDelayAfterFade = 0.15f;

    [Header("Timing")]
    [SerializeField] private float delayAfterValveClosed = 0.7f;
    [SerializeField] private float lookAtHandsDuration = 1.1f;
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
            ShowBurnedHands();

        // Возвращаем камеру к телу, но управление не отдаём.
        if (valveCutsceneController != null)
            yield return valveCutsceneController.ReturnCameraToPlayerBodyOnly();

        // Включаем руки сразу после возврата камеры в тело.
        // В начале анимации руки должны быть внизу / вдоль тела.
        
        PlayInspectHandsAnimation();
        onHandsShown?.Invoke();

        if (lookAtHandsDelayAfterAnimationStart > 0f)
            yield return new WaitForSeconds(lookAtHandsDelayAfterAnimationStart);

        // Камера смотрит на руки, пока они поднимаются.
        if (playerCamera != null && lookAtHandsTarget != null)
            yield return RotateCameraToLookTarget(lookAtHandsTarget, lookAtHandsDuration);

        // Ждём остаток общей анимации рук:
        // поднятие -> осмотр -> опускание.
        float remainingHandsTime =
            inspectHandsAnimationDuration - lookAtHandsDelayAfterAnimationStart - lookAtHandsDuration;

        if (remainingHandsTime > 0f)
            yield return new WaitForSeconds(remainingHandsTime);

       

        // Камера смотрит на дверь.
        if (playerCamera != null && lookAtDoorTarget != null)
            yield return RotateCameraToLookTarget(lookAtDoorTarget, lookAtDoorDuration);

        // Перед ходьбой разворачиваем тело Алдара в сторону двери.
        if (valveCutsceneController != null && doorWalkTarget != null)
            valveCutsceneController.RotatePlayerRootTowards(doorWalkTarget);
         HideBurnedHands();
        // Прикрепляем камеру обратно к телу.
        if (valveCutsceneController != null)
            valveCutsceneController.AttachCameraBackToPlayerKeepWorldPose();

        if (uiManager != null)
            uiManager.SetMessage(walkMessage, walkMessageDuration);

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

    private IEnumerator WaitUntilDoorReached()
    {
        float timer = 0f;
        float checkTimer = 0f;

        ResetWalkThoughts();

        while (timer < maxWalkDuration)
        {
            timer += Time.deltaTime;
            checkTimer += Time.deltaTime;

            if (playerController != null && playerController.CinematicReachedTarget)
                yield break;

            if (checkTimer >= thoughtCheckInterval)
            {
                checkTimer = 0f;

                float progress = GetWalkProgress01();
                WalkThoughtPoint thoughtPoint = GetNextThoughtPoint(progress);

                if (thoughtPoint != null)
                    yield return PlayWalkThoughtStop(thoughtPoint);
            }

            yield return null;
        }
    }

    private IEnumerator PlayWalkThoughtStop(WalkThoughtPoint point)
    {
        if (point == null)
            yield break;

        point.played = true;

        // Останавливаем тяжёлую ходьбу.
        if (playerController != null)
            playerController.StopCinematicWalk();

        playerControlLock?.LockControls();

        // Сначала экран темнеет, как будто Алдар почти теряет сознание.
        if (fadeDuringWalkThoughts && ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeOut(null, thoughtFadeOutDuration);

            yield return new WaitForSeconds(thoughtFadeOutDuration);

            // Мысль появляется уже в темноте.
            ShowWalkThought(point.thoughtText);

            yield return new WaitForSeconds(thoughtBlackHoldDuration);

            ScreenFader.Instance.FadeIn(null, thoughtFadeInDuration);

            yield return new WaitForSeconds(thoughtFadeInDuration + thoughtDelayAfterFade);
        }
        else
        {
            ShowWalkThought(point.thoughtText);

            yield return new WaitForSeconds(point.stopDuration);
        }

        // Если сцена уже закончилась или игрок дошёл до двери — ходьбу не возобновляем.
        if (playerController != null && playerController.CinematicReachedTarget)
            yield break;

        // Возобновляем медленную ходьбу.
        if (playerController != null && doorWalkTarget != null)
        {
            playerController.StartCinematicWalk(doorWalkTarget, woundedWalkSpeed, true);
            playerControlLock?.UnlockControls();
        }
    }

    private float GetWalkProgress01()
    {
        if (playerController == null || doorWalkTarget == null)
            return 0f;

        Vector3 startPosition;

        if (walkStartPoint != null)
            startPosition = walkStartPoint.position;
        else if (canonicalStartPose != null)
            startPosition = canonicalStartPose.position;
        else
            startPosition = playerController.transform.position;

        Vector3 endPosition = doorWalkTarget.position;
        Vector3 currentPosition = playerController.transform.position;

        startPosition.y = 0f;
        endPosition.y = 0f;
        currentPosition.y = 0f;

        float totalDistance = Vector3.Distance(startPosition, endPosition);

        if (totalDistance <= 0.01f)
            return 0f;

        float currentDistance = Vector3.Distance(startPosition, currentPosition);

        return Mathf.Clamp01(currentDistance / totalDistance);
    }

    private WalkThoughtPoint GetNextThoughtPoint(float progress)
    {
        if (walkThoughts == null || walkThoughts.Length == 0)
            return null;

        for (int i = 0; i < walkThoughts.Length; i++)
        {
            WalkThoughtPoint point = walkThoughts[i];

            if (point == null)
                continue;

            if (point.played)
                continue;

            if (progress >= point.progress)
                return point;
        }

        return null;
    }

    private void ResetWalkThoughts()
    {
        if (walkThoughts == null)
            return;

        for (int i = 0; i < walkThoughts.Length; i++)
        {
            if (walkThoughts[i] != null)
                walkThoughts[i].played = false;
        }
    }

    private void ShowWalkThought(string thoughtText)
    {
        if (string.IsNullOrWhiteSpace(thoughtText))
            return;

        if (GameManager.Instance != null && GameManager.Instance.EventManager != null)
            GameManager.Instance.EventManager.RequestThought(thoughtText);
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

    private void ShowBurnedHands()
    {
        if (normalHands != null)
            normalHands.SetActive(false);

        if (burnedHands != null)
            burnedHands.SetActive(true);

        if (burnedHandsAnimator != null)
        {
            burnedHandsAnimator.Rebind();
            burnedHandsAnimator.Update(0f);
        }
    }

    private void PlayInspectHandsAnimation()
    {
        if (burnedHandsAnimator == null)
            return;

        if (string.IsNullOrWhiteSpace(inspectHandsTrigger))
            return;

        burnedHandsAnimator.ResetTrigger(inspectHandsTrigger);
        burnedHandsAnimator.SetTrigger(inspectHandsTrigger);
    }

    private void HideBurnedHands()
    {
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
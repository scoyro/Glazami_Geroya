using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
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
    [SerializeField] private Transform[] walkWaypoints;
    [SerializeField] private float woundedWalkSpeed = 0.65f;
    [SerializeField] private float maxWalkDuration = 18f;

    [Header("Walk Thoughts")]
    [SerializeField] private Transform walkStartPoint;
    [SerializeField] private WalkThoughtPoint[] walkThoughts;
    [SerializeField] private float thoughtCheckInterval = 0.05f;

    [Header("Walk ZoneOut")]
    [SerializeField] private bool useWalkZoneOut = true;
    [SerializeField] private ZoneOutVolumeEffect walkZoneOut;
    [SerializeField] private float walkZoneOutMinWeight = 0.08f;
    [SerializeField] private float walkZoneOutMaxWeight = 0.38f;
    [SerializeField] private float walkZoneOutProgressAdd = 0.22f;
    [SerializeField] private float walkZoneOutPulseAmount = 0.08f;
    [SerializeField] private float walkZoneOutPulseSpeed = 0.75f;

    [Header("Walk Ambient Mixer")]
    [SerializeField] private bool useWalkAmbientMixer = true;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string ambientVolumeParameter = "AmbientVolume";

    [SerializeField] private float ambientMinVolumeDb = -18f;
    [SerializeField] private float ambientMaxVolumeDb = -4f;
    [SerializeField] private float ambientVolumeSmoothSpeed = 2.5f;
    [SerializeField] private float ambientProgressPower = 1.4f;

    [SerializeField] private bool fadeAmbientAfterWalk = true;
    [SerializeField] private float ambientFadeAfterWalkTargetDb = -18f;
    [SerializeField] private float ambientFadeAfterWalkDuration = 2f;

    [Header("Walk Thought ZoneOut Peak")]
    [SerializeField] private float thoughtZoneOutPeakWeight = 1f;
    [SerializeField] private float thoughtZoneOutPeakInDuration = 0.25f;
    [SerializeField] private float thoughtZoneOutPeakHoldDuration = 0.8f;
    [SerializeField] private float thoughtZoneOutPeakOutDuration = 0.75f;
    [SerializeField] private float thoughtDelayBeforeText = 0.15f;
    [SerializeField] private float thoughtDelayAfterZoneOut = 0.15f;

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

    private Coroutine walkZoneOutRoutine;
    private bool walkZoneOutPaused;
    private float currentWalkZoneOutWeight;

    private Coroutine walkAmbientMixerRoutine;
    private float currentAmbientVolumeDb;

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

        // Оставлено как у тебя:
        // руки включаются до возврата камеры к телу, чтобы не появляться на глазах.
        ShowBurnedHands();

        // Возвращаем камеру к телу, но управление не отдаём.
        if (valveCutsceneController != null)
            yield return valveCutsceneController.ReturnCameraToPlayerBodyOnly();

        // Один общий ZoneOut запускается уже здесь:
        // он работает на осмотре рук, взгляде на дверь и ходьбе.
        StartWalkZoneOut();

        if (lookAtHandsDelayAfterAnimationStart > 0f)
            yield return new WaitForSeconds(lookAtHandsDelayAfterAnimationStart);

        PlayInspectHandsAnimation();
        onHandsShown?.Invoke();

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

        // Оставлено как у тебя:
        // руки выключаются до прикрепления камеры обратно.
        HideBurnedHands();

        Transform[] path = GetCompleteWalkPath();

        // Прикрепляем камеру обратно к телу.
        if (valveCutsceneController != null)
            valveCutsceneController.AttachCameraBackToPlayerKeepWorldPose();

        if (uiManager != null)
            uiManager.SetMessage(walkMessage, walkMessageDuration);

        if (playerController != null)
        {
            playerController.SetCameraExternallyControlled(false);
            playerController.StartCinematicWalk(path, woundedWalkSpeed, true);
        }

        StartWalkAmbientMixer();

        playerControlLock?.UnlockControls();

        onWalkStarted?.Invoke();

        yield return WaitUntilDoorReached();

        StopWalkZoneOut();
        StopWalkAmbientMixer();

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
    private Transform[] GetCompleteWalkPath()
    {
        if (walkWaypoints == null || walkWaypoints.Length == 0)
        {
            return new Transform[] { doorWalkTarget };
        }

        Transform[] completePath = new Transform[walkWaypoints.Length + 1];
        for (int i = 0; i < walkWaypoints.Length; i++)
        {
            completePath[i] = walkWaypoints[i];
        }
        completePath[completePath.Length - 1] = doorWalkTarget;
        
        return completePath;
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

        walkZoneOutPaused = true;

        if (useWalkZoneOut && walkZoneOut != null)
        {
            // На момент мысли эффект доходит до максимума.
            yield return FadeWalkZoneOutTo(thoughtZoneOutPeakWeight, thoughtZoneOutPeakInDuration);

            if (thoughtDelayBeforeText > 0f)
                yield return new WaitForSeconds(thoughtDelayBeforeText);

            // Мысль появляется уже на пике состояния.
            ShowWalkThought(point.thoughtText);

            if (thoughtZoneOutPeakHoldDuration > 0f)
                yield return new WaitForSeconds(thoughtZoneOutPeakHoldDuration);

            // Возвращаемся к фоновому уровню эффекта.
            float backgroundWeight = CalculateWalkZoneOutWeight();
            yield return FadeWalkZoneOutTo(backgroundWeight, thoughtZoneOutPeakOutDuration);

            if (thoughtDelayAfterZoneOut > 0f)
                yield return new WaitForSeconds(thoughtDelayAfterZoneOut);
        }
        else
        {
            ShowWalkThought(point.thoughtText);
            yield return new WaitForSeconds(point.stopDuration);
        }

        walkZoneOutPaused = false;

        // Если сцена уже закончилась или игрок дошёл до двери — ходьбу не возобновляем.
        if (playerController != null && playerController.CinematicReachedTarget)
            yield break;

        // Возобновляем медленную ходьбу.
        if (playerController != null && doorWalkTarget != null)
        {
            Transform[] path = GetCompleteWalkPath();
            playerController.StartCinematicWalk(path, woundedWalkSpeed, true);
            playerControlLock?.UnlockControls();
        }
    }

    private void StartWalkZoneOut()
    {
        if (!useWalkZoneOut || walkZoneOut == null)
            return;

        if (walkZoneOutRoutine != null)
            StopCoroutine(walkZoneOutRoutine);

        walkZoneOutPaused = false;
        currentWalkZoneOutWeight = walkZoneOutMinWeight;

        walkZoneOut.SetWeight(currentWalkZoneOutWeight);

        walkZoneOutRoutine = StartCoroutine(WalkZoneOutRoutine());
    }

    private void StopWalkZoneOut()
    {
        if (walkZoneOutRoutine != null)
        {
            StopCoroutine(walkZoneOutRoutine);
            walkZoneOutRoutine = null;
        }

        walkZoneOutPaused = false;
        currentWalkZoneOutWeight = 0f;

        if (walkZoneOut != null)
            walkZoneOut.SetWeight(0f);
    }

    private IEnumerator WalkZoneOutRoutine()
    {
        while (isRunning)
        {
            if (playerController != null && playerController.CinematicReachedTarget)
                break;

            if (!walkZoneOutPaused && walkZoneOut != null)
            {
                currentWalkZoneOutWeight = CalculateWalkZoneOutWeight();
                walkZoneOut.SetWeight(currentWalkZoneOutWeight);
            }

            yield return null;
        }

        walkZoneOutRoutine = null;
    }

    private float CalculateWalkZoneOutWeight()
    {
        float progress = GetWalkProgress01();

        float pulse = Mathf.Sin(Time.time * walkZoneOutPulseSpeed * Mathf.PI * 2f);
        pulse = (pulse + 1f) * 0.5f;

        float progressWeight = progress * walkZoneOutProgressAdd;
        float pulseWeight = pulse * walkZoneOutPulseAmount;

        float weight =
            walkZoneOutMinWeight +
            progressWeight +
            pulseWeight;

        return Mathf.Clamp(weight, walkZoneOutMinWeight, walkZoneOutMaxWeight);
    }

    private IEnumerator FadeWalkZoneOutTo(float targetWeight, float duration)
    {
        if (walkZoneOut == null)
            yield break;

        targetWeight = Mathf.Clamp01(targetWeight);

        float startWeight = currentWalkZoneOutWeight;

        if (duration <= 0f)
        {
            currentWalkZoneOutWeight = targetWeight;
            walkZoneOut.SetWeight(currentWalkZoneOutWeight);
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = Smooth(t);

            currentWalkZoneOutWeight = Mathf.Lerp(startWeight, targetWeight, t);
            walkZoneOut.SetWeight(currentWalkZoneOutWeight);

            yield return null;
        }

        currentWalkZoneOutWeight = targetWeight;
        walkZoneOut.SetWeight(currentWalkZoneOutWeight);
    }

    private void StartWalkAmbientMixer()
    {
        if (!useWalkAmbientMixer || audioMixer == null)
            return;

        if (walkAmbientMixerRoutine != null)
            StopCoroutine(walkAmbientMixerRoutine);

        currentAmbientVolumeDb = ambientMinVolumeDb;
        audioMixer.SetFloat(ambientVolumeParameter, currentAmbientVolumeDb);

        walkAmbientMixerRoutine = StartCoroutine(WalkAmbientMixerRoutine());
    }

    private void StopWalkAmbientMixer()
    {
        if (walkAmbientMixerRoutine != null)
        {
            StopCoroutine(walkAmbientMixerRoutine);
            walkAmbientMixerRoutine = null;
        }

        if (!fadeAmbientAfterWalk || audioMixer == null)
            return;

        walkAmbientMixerRoutine = StartCoroutine(FadeAmbientMixerTo(
            ambientFadeAfterWalkTargetDb,
            ambientFadeAfterWalkDuration
        ));
    }

    private IEnumerator FadeAmbientMixerTo(float targetVolumeDb, float duration)
    {
        if (audioMixer == null)
            yield break;

        float startVolumeDb = currentAmbientVolumeDb;

        if (duration <= 0f)
        {
            currentAmbientVolumeDb = targetVolumeDb;
            audioMixer.SetFloat(ambientVolumeParameter, currentAmbientVolumeDb);
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = Smooth(t);

            currentAmbientVolumeDb = Mathf.Lerp(startVolumeDb, targetVolumeDb, t);
            audioMixer.SetFloat(ambientVolumeParameter, currentAmbientVolumeDb);

            yield return null;
        }

        currentAmbientVolumeDb = targetVolumeDb;
        audioMixer.SetFloat(ambientVolumeParameter, currentAmbientVolumeDb);

        walkAmbientMixerRoutine = null;
    }

    private IEnumerator WalkAmbientMixerRoutine()
    {
        while (isRunning)
        {
            if (playerController != null && playerController.CinematicReachedTarget)
                break;

            float progress = GetWalkProgress01();

            // 1 = линейно.
            // 1.4–1.8 = сильнее нарастает ближе к двери.
            float poweredProgress = Mathf.Pow(progress, ambientProgressPower);

            float targetVolumeDb = Mathf.Lerp(
                ambientMinVolumeDb,
                ambientMaxVolumeDb,
                poweredProgress
            );

            currentAmbientVolumeDb = Mathf.Lerp(
                currentAmbientVolumeDb,
                targetVolumeDb,
                Time.deltaTime * ambientVolumeSmoothSpeed
            );

            audioMixer.SetFloat(ambientVolumeParameter, currentAmbientVolumeDb);

            yield return null;
        }

        walkAmbientMixerRoutine = null;
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
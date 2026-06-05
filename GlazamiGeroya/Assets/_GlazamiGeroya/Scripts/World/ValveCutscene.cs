using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ValveCutsceneController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform valveCameraTarget;

    [Header("Camera Movement")]
    [SerializeField] private float moveToValveDuration = 1.1f;
    [SerializeField] private float moveBackDuration = 0.8f;

    [Header("Player Lock")]
    [SerializeField] private PlayerControlLock playerControlLock;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private InteractionSystem interactionSystem;

    [Header("Player Freeze")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private bool freezePlayerTransform = true;

    [Header("Visibility")]
    [SerializeField] private GameObject playerBody;
    [SerializeField] private GameObject normalHands;
    [SerializeField] private GameObject cutsceneHands;

    [Header("Events")]
    [SerializeField] private UnityEvent onCutsceneStarted;
    [SerializeField] private UnityEvent onCutsceneFinished;
    [SerializeField] private float failZoneOutFadeIn = 0.12f;
    [SerializeField] private float failZoneOutHold = 0.25f;
    [SerializeField] private float failZoneOutFadeOut = 0.35f;

    [Range(0f, 1f)]
    [SerializeField] private float failZoneOutWeight = 1f;

    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;

    private Vector3 frozenPlayerPosition;
    private Quaternion frozenPlayerRotation;

    private bool isActive;
    private bool isMoving;
    private bool originalCameraSaved;

    private Coroutine currentRoutine;

    public bool IsActive => isActive;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        if (cutsceneHands != null)
            cutsceneHands.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!isActive)
            return;

        if (!freezePlayerTransform)
            return;

        if (playerRoot == null)
            return;

        playerRoot.SetPositionAndRotation(frozenPlayerPosition, frozenPlayerRotation);
    }

    public void StartCutscene()
    {
        if (isActive)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(StartCutsceneRoutine());
    }

    public void FinishCutscene()
    {
        if (!isActive || isMoving)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(FinishCutsceneRoutine());
    }

    public void ForceFinishWithoutCameraReturn()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        if (playerController != null)
            playerController.SetCameraExternallyControlled(false);

        playerControlLock?.UnlockControls();
        interactionSystem?.UnlockInteraction();

        SetCutsceneVisibility(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isMoving = false;
        isActive = false;

        currentRoutine = null;

        onCutsceneFinished?.Invoke();
    }

    private IEnumerator StartCutsceneRoutine()
    {
        if (playerCamera == null || valveCameraTarget == null)
            yield break;

        isActive = true;
        isMoving = true;

        SaveOriginalCameraTransform();
        FreezePlayer();

        playerControlLock?.LockControls();

        if (playerController != null)
            playerController.SetCameraExternallyControlled(true);

        interactionSystem?.LockInteraction();

        SetCutsceneVisibility(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        onCutsceneStarted?.Invoke();

        yield return MoveCameraToValve();

        isMoving = false;
        currentRoutine = null;
    }

    private IEnumerator FinishCutsceneRoutine()
    {
        if (playerCamera == null)
            yield break;

        isMoving = true;

        yield return MoveCameraBackToPlayer();

        if (playerController != null)
            playerController.SetCameraExternallyControlled(false);

        playerControlLock?.UnlockControls();
        interactionSystem?.UnlockInteraction();

        SetCutsceneVisibility(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isMoving = false;
        isActive = false;

        onCutsceneFinished?.Invoke();

        currentRoutine = null;
    }

    private void SaveOriginalCameraTransform()
    {
        originalCameraParent = playerCamera.parent;
        originalCameraLocalPosition = playerCamera.localPosition;
        originalCameraLocalRotation = playerCamera.localRotation;
        originalCameraSaved = true;
    }

    private void FreezePlayer()
    {
        if (!freezePlayerTransform || playerRoot == null)
            return;

        frozenPlayerPosition = playerRoot.position;
        frozenPlayerRotation = playerRoot.rotation;
    }

    private IEnumerator MoveCameraToValve()
    {
        Vector3 startPosition = playerCamera.position;
        Quaternion startRotation = playerCamera.rotation;

        Vector3 targetPosition = valveCameraTarget.position;
        Quaternion targetRotation = valveCameraTarget.rotation;

        playerCamera.SetParent(null, true);

        float time = 0f;

        while (time < moveToValveDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / moveToValveDuration);
            t = Smooth(t);

            playerCamera.SetPositionAndRotation(
                Vector3.Lerp(startPosition, targetPosition, t),
                Quaternion.Slerp(startRotation, targetRotation, t)
            );

            yield return null;
        }

        playerCamera.SetPositionAndRotation(targetPosition, targetRotation);
    }

    private IEnumerator MoveCameraBackToPlayer()
    {
        if (!originalCameraSaved || originalCameraParent == null)
            yield break;

        Vector3 startPosition = playerCamera.position;
        Quaternion startRotation = playerCamera.rotation;

        Vector3 targetPosition = originalCameraParent.TransformPoint(originalCameraLocalPosition);
        Quaternion targetRotation = originalCameraParent.rotation * originalCameraLocalRotation;

        playerCamera.SetParent(null, true);

        float time = 0f;

        while (time < moveBackDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / moveBackDuration);
            t = Smooth(t);

            playerCamera.SetPositionAndRotation(
                Vector3.Lerp(startPosition, targetPosition, t),
                Quaternion.Slerp(startRotation, targetRotation, t)
            );

            yield return null;
        }

        playerCamera.SetPositionAndRotation(targetPosition, targetRotation);

        playerCamera.SetParent(originalCameraParent, false);
        playerCamera.localPosition = originalCameraLocalPosition;
        playerCamera.localRotation = originalCameraLocalRotation;
    }

    public IEnumerator DeathFallRoutine(
        Transform nodTarget,
        Transform fallTarget,
        float nodDownDuration,
        float nodRecoverDuration,
        float fallDuration,
        ZoneOutVolumeEffect zoneOut,
        float fadeOutDuration)
    {
        if (playerCamera == null || nodTarget == null || fallTarget == null)
            yield break;

        if (!isActive)
        {
            isActive = true;
            SaveOriginalCameraTransform();
            FreezePlayer();

            playerControlLock?.LockControls();

            if (playerController != null)
                playerController.SetCameraExternallyControlled(true);

            interactionSystem?.LockInteraction();

            SetCutsceneVisibility(true);
        }

        isMoving = true;

        Vector3 startPos = playerCamera.position;
        Quaternion startRot = playerCamera.rotation;

        playerCamera.SetParent(null, true);

        zoneOut?.PlayCustom(
            failZoneOutFadeIn,
            failZoneOutHold,
            failZoneOutFadeOut,
            failZoneOutWeight
        );

        yield return MoveCameraWorld(
            startPos,
            startRot,
            nodTarget.position,
            nodTarget.rotation,
            nodDownDuration
        );

        yield return MoveCameraWorld(
            nodTarget.position,
            nodTarget.rotation,
            startPos,
            startRot,
            nodRecoverDuration
        );

        ScreenFader.Instance?.FadeOut(null, fadeOutDuration);

        yield return MoveCameraWorld(
            startPos,
            startRot,
            fallTarget.position,
            fallTarget.rotation,
            fallDuration
        );

        isMoving = false;
    }

    private IEnumerator MoveCameraWorld(
        Vector3 fromPos,
        Quaternion fromRot,
        Vector3 toPos,
        Quaternion toRot,
        float duration)
    {
        if (duration <= 0f)
        {
            playerCamera.SetPositionAndRotation(toPos, toRot);
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = Smooth(t);

            playerCamera.SetPositionAndRotation(
                Vector3.Lerp(fromPos, toPos, t),
                Quaternion.Slerp(fromRot, toRot, t)
            );

            yield return null;
        }

        playerCamera.SetPositionAndRotation(toPos, toRot);
    }

    private void SetCutsceneVisibility(bool active)
    {
        if (playerBody != null)
            playerBody.SetActive(!active);

        if (normalHands != null)
            normalHands.SetActive(!active);

        if (cutsceneHands != null)
            cutsceneHands.SetActive(active);
    }

    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
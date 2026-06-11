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
    [SerializeField] private CharacterController playerCharacterController;
    [SerializeField] private bool freezePlayerTransform = true;

    [Header("Visibility")]
    [SerializeField] private GameObject playerBody;
    [SerializeField] private GameObject normalHands;
    [SerializeField] private GameObject cutsceneHands;

    [Header("Events")]
    [SerializeField] private UnityEvent onCutsceneStarted;
    [SerializeField] private UnityEvent onCutsceneFinished;

    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;

    private Vector3 frozenPlayerPosition;
    private Quaternion frozenPlayerRotation;

    private bool isActive;
    private bool isMoving;
    private bool originalCameraSaved;

    private bool forceCameraPose;
    private Vector3 forcedCameraPosition;
    private Quaternion forcedCameraRotation;

    private Coroutine currentRoutine;

    public bool IsActive => isActive;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        if (cutsceneHands != null)
            cutsceneHands.SetActive(false);

        if (playerCharacterController == null && playerRoot != null)
            playerCharacterController = playerRoot.GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        if (isActive && freezePlayerTransform && playerRoot != null)
        {
            playerRoot.SetPositionAndRotation(frozenPlayerPosition, frozenPlayerRotation);
        }

        if (forceCameraPose && playerCamera != null)
        {
            playerCamera.SetPositionAndRotation(forcedCameraPosition, forcedCameraRotation);
        }
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

        forceCameraPose = false;

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

        yield return MoveCameraBackToPlayerAndAttach();

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

    private void SaveOriginalCameraTransform()
    {
        if (playerCamera == null)
            return;

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

    public void WarpPlayerRootToPose(Transform pose)
    {
        if (playerRoot == null || pose == null)
            return;

        bool controllerWasEnabled = false;

        if (playerCharacterController != null)
        {
            controllerWasEnabled = playerCharacterController.enabled;
            playerCharacterController.enabled = false;
        }

        playerRoot.SetPositionAndRotation(pose.position, pose.rotation);

        if (playerCharacterController != null)
            playerCharacterController.enabled = controllerWasEnabled;

        FreezePlayer();
    }

    private IEnumerator MoveCameraToValve()
    {
        if (playerCamera == null || valveCameraTarget == null)
            yield break;

        Vector3 startPosition = playerCamera.position;
        Quaternion startRotation = playerCamera.rotation;

        Vector3 targetPosition = valveCameraTarget.position;
        Quaternion targetRotation = valveCameraTarget.rotation;

        playerCamera.SetParent(null, true);
        forceCameraPose = true;

        float time = 0f;

        while (time < moveToValveDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / moveToValveDuration);
            t = Smooth(t);

            SetCameraPose(
                Vector3.Lerp(startPosition, targetPosition, t),
                Quaternion.Slerp(startRotation, targetRotation, t)
            );

            yield return null;
        }

        SetCameraPose(targetPosition, targetRotation);
    }

    private IEnumerator MoveCameraBackToPlayerAndAttach()
    {
        if (!originalCameraSaved || originalCameraParent == null || playerCamera == null)
            yield break;

        Vector3 startPosition = playerCamera.position;
        Quaternion startRotation = playerCamera.rotation;

        Vector3 targetPosition = originalCameraParent.TransformPoint(originalCameraLocalPosition);
        Quaternion targetRotation = originalCameraParent.rotation * originalCameraLocalRotation;

        playerCamera.SetParent(null, true);
        forceCameraPose = true;

        float time = 0f;

        while (time < moveBackDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / moveBackDuration);
            t = Smooth(t);

            SetCameraPose(
                Vector3.Lerp(startPosition, targetPosition, t),
                Quaternion.Slerp(startRotation, targetRotation, t)
            );

            yield return null;
        }

        SetCameraPose(targetPosition, targetRotation);

        forceCameraPose = false;

        playerCamera.SetParent(originalCameraParent, false);
        playerCamera.localPosition = originalCameraLocalPosition;
        playerCamera.localRotation = originalCameraLocalRotation;
    }

    public IEnumerator ReturnCameraToPlayerBodyOnly()
    {
        if (!originalCameraSaved || originalCameraParent == null || playerCamera == null)
            yield break;

        isMoving = true;

        Vector3 startPosition = playerCamera.position;
        Quaternion startRotation = playerCamera.rotation;

        Vector3 targetPosition = originalCameraParent.TransformPoint(originalCameraLocalPosition);
        Quaternion targetRotation = originalCameraParent.rotation * originalCameraLocalRotation;

        playerCamera.SetParent(null, true);
        forceCameraPose = true;

        float time = 0f;

        while (time < moveBackDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / moveBackDuration);
            t = Smooth(t);

            SetCameraPose(
                Vector3.Lerp(startPosition, targetPosition, t),
                Quaternion.Slerp(startRotation, targetRotation, t)
            );

            yield return null;
        }

        SetCameraPose(targetPosition, targetRotation);

        isMoving = false;
    }

    public void AttachCameraBackToPlayerKeepWorldPose()
    {
        if (playerCamera == null || originalCameraParent == null)
            return;

        Vector3 worldPosition = playerCamera.position;
        Quaternion worldRotation = playerCamera.rotation;

        forceCameraPose = false;

        playerCamera.SetParent(originalCameraParent, true);
        playerCamera.SetPositionAndRotation(worldPosition, worldRotation);

        isMoving = false;
        isActive = false;

        SetCutsceneVisibility(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void AttachCameraBackToPlayerWithoutUnlock()
    {
        if (playerCamera == null || originalCameraParent == null)
            return;

        forceCameraPose = false;

        playerCamera.SetParent(originalCameraParent, false);
        playerCamera.localPosition = originalCameraLocalPosition;
        playerCamera.localRotation = originalCameraLocalRotation;

        isMoving = false;
        isActive = false;

        SetCutsceneVisibility(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

            if (!originalCameraSaved)
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
        forceCameraPose = true;

        zoneOut?.Play();

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

    public IEnumerator SuccessCollapseRoutine(
        Transform lookTarget,
        Transform collapseTarget,
        float holdAfterCloseDuration,
        float lookDuration,
        float collapseDuration,
        float fadeOutDuration)
    {
        if (playerCamera == null || lookTarget == null || collapseTarget == null)
            yield break;

        if (!isActive)
        {
            isActive = true;

            if (!originalCameraSaved)
                SaveOriginalCameraTransform();

            FreezePlayer();

            playerControlLock?.LockControls();

            if (playerController != null)
                playerController.SetCameraExternallyControlled(true);

            interactionSystem?.LockInteraction();

            SetCutsceneVisibility(true);
        }

        isMoving = true;

        playerCamera.SetParent(null, true);
        forceCameraPose = true;

        Vector3 startPos = playerCamera.position;
        Quaternion startRot = playerCamera.rotation;

        if (holdAfterCloseDuration > 0f)
            yield return new WaitForSeconds(holdAfterCloseDuration);

        yield return MoveCameraWorld(
            startPos,
            startRot,
            lookTarget.position,
            lookTarget.rotation,
            lookDuration
        );

        Vector3 beforeCollapsePos = playerCamera.position;
        Quaternion beforeCollapseRot = playerCamera.rotation;

        ScreenFader.Instance?.FadeOut(null, fadeOutDuration);

        yield return MoveCameraWorld(
            beforeCollapsePos,
            beforeCollapseRot,
            collapseTarget.position,
            collapseTarget.rotation,
            collapseDuration
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
        if (playerCamera == null)
            yield break;

        playerCamera.SetParent(null, true);
        forceCameraPose = true;

        if (duration <= 0f)
        {
            SetCameraPose(toPos, toRot);
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = Smooth(t);

            SetCameraPose(
                Vector3.Lerp(fromPos, toPos, t),
                Quaternion.Slerp(fromRot, toRot, t)
            );

            yield return null;
        }

        SetCameraPose(toPos, toRot);
    }

    private void SetCameraPose(Vector3 position, Quaternion rotation)
    {
        forcedCameraPosition = position;
        forcedCameraRotation = rotation;

        if (playerCamera != null)
            playerCamera.SetPositionAndRotation(position, rotation);
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
    public void SetForcedCameraPose(Vector3 position, Quaternion rotation)
    {
        forceCameraPose = true;
        SetCameraPose(position, rotation);
    }

    public void DisableForcedCameraPose()
    {
        forceCameraPose = false;
    }

    public void RotatePlayerRootTowards(Transform target)
    {
        if (playerRoot == null || target == null)
            return;

        Vector3 direction = target.position - playerRoot.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        bool controllerWasEnabled = false;

        if (playerCharacterController != null)
        {
            controllerWasEnabled = playerCharacterController.enabled;
            playerCharacterController.enabled = false;
        }

        playerRoot.rotation = targetRotation;

        if (playerCharacterController != null)
            playerCharacterController.enabled = controllerWasEnabled;

        FreezePlayer();
    }
    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
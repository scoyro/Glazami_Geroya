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

    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;

    private Vector3 frozenPlayerPosition;
    private Quaternion frozenPlayerRotation;

    private bool isActive;
    private bool isMoving;

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
        if (originalCameraParent == null)
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
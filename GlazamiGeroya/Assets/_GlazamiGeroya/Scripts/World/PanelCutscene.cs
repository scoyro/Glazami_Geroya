using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PanelCutsceneController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform cameraTargetPoint;
    [SerializeField] private float cameraMoveDuration = 1.1f;
    [SerializeField] private bool holdCameraAtTarget = true;

    [Header("Player Lock")]
    [SerializeField] private PlayerControlLock playerControlLock;
    [SerializeField] private PlayerController playerController;

    [Header("Scripts To Disable While Active")]
    [Tooltip("Сюда можно добавить HeadBob, CameraSway, CameraShake или другие скрипты, которые двигают камеру.")]
    [SerializeField] private Behaviour[] cameraControlScriptsToDisable;

    [Header("Clickable Objects")]
    [SerializeField] private Collider[] collidersToDisableWhileActive;
    [SerializeField] private Collider[] clickableColliders;

    [Header("Events")]
    [SerializeField] private UnityEvent onCutsceneStarted;
    [SerializeField] private UnityEvent onCutsceneFinished;

    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;

    private bool[] disabledScriptsOriginalStates;

    private bool isActive;
    private bool isMoving;
    private bool cameraAtPanel;

    public bool IsActive => isActive;
    public bool IsMoving => isMoving;
    [Header("Exit")]
    [SerializeField] private KeyCode exitKey = KeyCode.Escape;
    [SerializeField] private int exitMouseButton = 1; // 1 = правая кнопка мыши

    private void Awake()
    {
        SetClickableColliders(false);
    }

    private void LateUpdate()
    {
        if (!isActive)
            return;

        if (!cameraAtPanel)
            return;

        if (!holdCameraAtTarget)
            return;

        if (playerCamera == null || cameraTargetPoint == null)
            return;

        playerCamera.position = cameraTargetPoint.position;
        playerCamera.rotation = cameraTargetPoint.rotation;
    }

    private void Update()
    {
        if (!isActive || isMoving)
            return;

        if (Input.GetKeyDown(exitKey) || Input.GetMouseButtonDown(exitMouseButton))
            FinishCutscene();
    }

    public void StartCutscene()
    {
        if (isActive)
            return;

        StartCoroutine(StartRoutine());
    }

    public void FinishCutscene()
    {
        if (!isActive || isMoving)
            return;

        StartCoroutine(FinishRoutine());
    }

    private IEnumerator StartRoutine()
    {
        isActive = true;
        isMoving = true;
        cameraAtPanel = false;

        SaveCameraTransform();

        playerControlLock?.LockControls();

        if (playerController != null)
            playerController.SetCameraExternallyControlled(true);

        DisableCameraControlScripts();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        onCutsceneStarted?.Invoke();

        yield return MoveCameraToPanel();

        cameraAtPanel = true;

        SetClickableColliders(true);

        isMoving = false;
        SetColliders(collidersToDisableWhileActive, false);
        SetColliders(clickableColliders, true);
    }
    private void SetColliders(Collider[] colliders, bool enabled)
    {
        if (colliders == null)
            return;

        foreach (Collider col in colliders)
        {
            if (col != null)
                col.enabled = enabled;
        }
    }
    private IEnumerator FinishRoutine()
    {
        isMoving = true;

        SetClickableColliders(false);

        cameraAtPanel = false;

        yield return ReturnCameraToPlayer();

        RestoreCameraControlScripts();

        if (playerController != null)
            playerController.SetCameraExternallyControlled(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerControlLock?.UnlockControls();

        isMoving = false;
        isActive = false;
        SetColliders(clickableColliders, false);
        SetColliders(collidersToDisableWhileActive, true);
        onCutsceneFinished?.Invoke();
    }

    private void SaveCameraTransform()
    {
        if (playerCamera == null)
            return;

        originalCameraParent = playerCamera.parent;
        originalCameraLocalPosition = playerCamera.localPosition;
        originalCameraLocalRotation = playerCamera.localRotation;
    }

    private IEnumerator MoveCameraToPanel()
    {
        if (playerCamera == null || cameraTargetPoint == null)
            yield break;

        Vector3 startPosition = playerCamera.position;
        Quaternion startRotation = playerCamera.rotation;

        Vector3 targetPosition = cameraTargetPoint.position;
        Quaternion targetRotation = cameraTargetPoint.rotation;

        playerCamera.SetParent(null, true);

        float time = 0f;

        while (time < cameraMoveDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / cameraMoveDuration);
            t = Smooth(t);

            playerCamera.position = Vector3.Lerp(startPosition, targetPosition, t);
            playerCamera.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        playerCamera.position = targetPosition;
        playerCamera.rotation = targetRotation;
    }

    private IEnumerator ReturnCameraToPlayer()
    {
        if (playerCamera == null || originalCameraParent == null)
            yield break;

        Vector3 startPosition = playerCamera.position;
        Quaternion startRotation = playerCamera.rotation;

        Vector3 targetPosition = originalCameraParent.TransformPoint(originalCameraLocalPosition);
        Quaternion targetRotation = originalCameraParent.rotation * originalCameraLocalRotation;

        float time = 0f;

        while (time < cameraMoveDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / cameraMoveDuration);
            t = Smooth(t);

            playerCamera.position = Vector3.Lerp(startPosition, targetPosition, t);
            playerCamera.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        playerCamera.SetParent(originalCameraParent, true);
        playerCamera.localPosition = originalCameraLocalPosition;
        playerCamera.localRotation = originalCameraLocalRotation;
    }

    private void DisableCameraControlScripts()
    {
        if (cameraControlScriptsToDisable == null)
            return;

        disabledScriptsOriginalStates = new bool[cameraControlScriptsToDisable.Length];

        for (int i = 0; i < cameraControlScriptsToDisable.Length; i++)
        {
            Behaviour script = cameraControlScriptsToDisable[i];

            if (script == null)
                continue;

            disabledScriptsOriginalStates[i] = script.enabled;
            script.enabled = false;
        }
    }

    private void RestoreCameraControlScripts()
    {
        if (cameraControlScriptsToDisable == null || disabledScriptsOriginalStates == null)
            return;

        for (int i = 0; i < cameraControlScriptsToDisable.Length; i++)
        {
            Behaviour script = cameraControlScriptsToDisable[i];

            if (script == null)
                continue;

            script.enabled = disabledScriptsOriginalStates[i];
        }
    }

    private void SetClickableColliders(bool enabled)
    {
        if (clickableColliders == null)
            return;

        foreach (Collider col in clickableColliders)
        {
            if (col != null)
                col.enabled = enabled;
        }
    }

    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
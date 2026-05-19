using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PanelCutsceneController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform panelCameraTarget;

    [Header("Camera Movement")]
    [SerializeField] private float moveToPanelDuration = 1.1f;
    [SerializeField] private float moveBackDuration = 0.8f;

    [Header("Player Lock")]
    [SerializeField] private PlayerControlLock playerControlLock;
    [SerializeField] private PlayerController playerController;

    [Header("Player Freeze")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private bool freezePlayerTransform = true;

    [Header("Colliders")]
    [SerializeField] private Collider[] clickableColliders;
    [SerializeField] private Collider[] collidersToDisableWhileActive;

    [Header("Panel Click Raycast")]
    [SerializeField] private Camera raycastCamera;
    [SerializeField] private LayerMask clickableLayerMask = ~0;
    [SerializeField] private float raycastDistance = 5f;

    [Header("Hover")]
    [SerializeField] private PanelHoverText hoverText;

    [Header("Exit")]
    [SerializeField] private KeyCode exitKey = KeyCode.Escape;
    [SerializeField] private int exitMouseButton = 1;

    [Header("Events")]
    [SerializeField] private UnityEvent onCutsceneStarted;
    [SerializeField] private UnityEvent onCutsceneFinished;
    [Header("Interaction Lock")]
    [SerializeField] private InteractionSystem interactionSystem;

    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;

    private Vector3 frozenPlayerPosition;
    private Quaternion frozenPlayerRotation;

    private bool isActive;
    private bool isMoving;

    private PanelClickable currentHover;
    private Coroutine currentRoutine;

    public bool IsActive => isActive;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        SetColliders(clickableColliders, false);

        if (raycastCamera == null && playerCamera != null)
            raycastCamera = playerCamera.GetComponent<Camera>();
    }

    private void Update()
    {
        if (!isActive || isMoving)
            return;

        if (Input.GetKeyDown(exitKey) || Input.GetMouseButtonDown(exitMouseButton))
        {
            FinishCutscene();
            return;
        }

        UpdatePanelHoverAndClick();
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
        if (playerCamera == null || panelCameraTarget == null)
            yield break;

        isActive = true;
        isMoving = true;

        currentHover = null;
        hoverText?.Hide();

        SaveOriginalCameraTransform();
        FreezePlayer();

        playerControlLock?.LockControls();

        if (playerController != null)
            playerController.SetCameraExternallyControlled(true);
        
        interactionSystem?.LockInteraction();

        SetColliders(collidersToDisableWhileActive, false);
        SetColliders(clickableColliders, false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        onCutsceneStarted?.Invoke();

        yield return MoveCameraToPanel();

        SetColliders(clickableColliders, true);

        isMoving = false;
        currentRoutine = null;
    }

    private IEnumerator FinishCutsceneRoutine()
    {
        if (playerCamera == null)
            yield break;

        isMoving = true;

        currentHover = null;
        hoverText?.Hide();

        SetColliders(clickableColliders, false);

        yield return MoveCameraBackToPlayer();

        if (playerController != null)
            playerController.SetCameraExternallyControlled(false);

        playerControlLock?.UnlockControls();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetColliders(collidersToDisableWhileActive, true);

        isMoving = false;
        isActive = false;

        onCutsceneFinished?.Invoke();
        interactionSystem?.UnlockInteraction();

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

    private IEnumerator MoveCameraToPanel()
    {
        Vector3 startPosition = playerCamera.position;
        Quaternion startRotation = playerCamera.rotation;

        Vector3 targetPosition = panelCameraTarget.position;
        Quaternion targetRotation = panelCameraTarget.rotation;

        playerCamera.SetParent(null, true);

        float time = 0f;

        while (time < moveToPanelDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / moveToPanelDuration);
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

    private void UpdatePanelHoverAndClick()
    {
        if (raycastCamera == null)
            return;

        Ray ray = raycastCamera.ScreenPointToRay(Input.mousePosition);

        PanelClickable hoveredClickable = null;

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            raycastDistance,
            clickableLayerMask,
            QueryTriggerInteraction.Collide))
        {
            hoveredClickable = hit.collider.GetComponent<PanelClickable>();
        }

        if (hoveredClickable != currentHover)
        {
            currentHover = hoveredClickable;

            if (currentHover == null)
                hoverText?.Hide();
            else
                hoverText?.Show(currentHover.GetHoverText());
        }
        else if (currentHover != null)
        {
            hoverText?.Show(currentHover.GetHoverText());
        }

        if (currentHover != null && Input.GetMouseButtonDown(0))
        {
            currentHover.Click();
            hoverText?.Show(currentHover.GetHoverText());
        }
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

    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
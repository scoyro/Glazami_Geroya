using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float gravity = -9.81f;

    [Header("Sprint")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private float crisisSprintMultiplier = 1.8f;

    private bool isCrisis;

    [Header("Mouse Look")]
    public Transform cameraTransform;
    public float mouseSensitivity = 200f;
    public float minY = -90f;
    public float maxY = 90f;

    [Header("Body Look")]
    [SerializeField] private Transform headBone;
    [SerializeField] private Transform upperBodyBone;

    [SerializeField] private float headFollowAmount = 0.7f;
    [SerializeField] private float bodyFollowAmount = 0.25f;

    private Quaternion headStartRotation;
    private Quaternion upperBodyStartRotation;

    [Header("Head Bobbing")]
    public float bobSpeed = 14f;
    public float bobAmount = 0.08f;
    public float returnSpeed = 5f;

    [Header("Cinematic Walk")]
    [SerializeField] private float cinematicTurnSpeed = 1.5f;
    [SerializeField] private float cinematicStopDistance = 0.8f;

    [Header("Cinematic Limp Camera")]
    [SerializeField] private float limpBobSpeed = 4.5f;
    [SerializeField] private float limpVerticalAmount = 0.16f;
    [SerializeField] private float limpSideAmount = 0.055f;
    [SerializeField] private float limpTiltAmount = 7f;
    [SerializeField] private float limpForwardTiltAmount = 3f;
    [SerializeField] private float limpReturnSpeed = 4f;
    private Quaternion cinematicBaseCameraLocalRotation;
    private Vector3 cinematicBaseCameraLocalPosition;

    private CharacterController controller;
    private Vector3 velocity;

    private float xRotation = 0f;
    private float moveX;
    private float moveZ;

    private float defaultYPos;
    private Vector3 defaultCameraLocalPosition;
    private Quaternion defaultCameraLocalRotation;
    private float timer;

    private bool controlsLocked;
    private bool cameraExternallyControlled;

    private bool cinematicWalkMode;
    private Transform cinematicWalkTarget;
    private float cinematicWalkSpeed = 1f;
    private bool cinematicRequireW = true;
    private bool cinematicReachedTarget;
    private bool suppressNormalHeadBob;
    [Header("Cinematic Limp Preview")]
    [SerializeField] private bool previewCinematicLimpCamera = false;
    [SerializeField] private bool previewRequiresW = true;
    [SerializeField] private bool previewLockMovement = true;
    public bool ControlsLocked => controlsLocked;
    public bool CameraExternallyControlled => cameraExternallyControlled;
    public bool CinematicWalkMode => cinematicWalkMode;
    public bool CinematicReachedTarget => cinematicReachedTarget;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (headBone != null)
            headStartRotation = headBone.localRotation;

        if (upperBodyBone != null)
            upperBodyStartRotation = upperBodyBone.localRotation;

        if (GameManager.Instance != null && GameManager.Instance.EventManager != null)
            GameManager.Instance.EventManager.OnCrisisModeChanged += SetCrisisMode;

        if (cameraTransform != null)
        {
            defaultCameraLocalPosition = cameraTransform.localPosition;
            defaultCameraLocalRotation = cameraTransform.localRotation;
            defaultYPos = cameraTransform.localPosition.y;
        }
        cinematicBaseCameraLocalPosition = defaultCameraLocalPosition;
        cinematicBaseCameraLocalRotation = defaultCameraLocalRotation;
    }
    public void SetNormalHeadBobSuppressed(bool value)
    {
        suppressNormalHeadBob = value;

        if (value)
            ResetHeadBobImmediate();
    }

    private void Update()
{
    if (previewCinematicLimpCamera)
    {
        if (!previewLockMovement)
            Move();
        else
            ApplyGravityOnly();

        HandleCinematicLimpPreview();
        return;
    }

    if (!controlsLocked && !cameraExternallyControlled && !cinematicWalkMode)
        Look();

    Move();

    if (cameraExternallyControlled)
        return;

    if (cinematicWalkMode)
    {
        HandleCinematicLimpBob();
        return;
    }

    if (suppressNormalHeadBob)
    {
        ResetHeadBob();
        return;
    }

    if (!controlsLocked)
        HandleHeadBob();
    else
        ResetHeadBob();
}
    private void HandleCinematicLimpPreview()
{
    if (cameraTransform == null)
        return;

    if (previewRequiresW && !Input.GetKey(KeyCode.W))
    {
        ResetCinematicLimpBob();
        return;
    }

    // Чтобы preview работал даже без StartCinematicWalk()
    cinematicBaseCameraLocalPosition = defaultCameraLocalPosition;
    cinematicBaseCameraLocalRotation = defaultCameraLocalRotation;

    timer += Time.deltaTime * limpBobSpeed;

    float step = Mathf.Sin(timer);
    float heavyDrop = Mathf.Abs(step);
    float side = Mathf.Sin(timer * 0.5f);

    Vector3 pos = cinematicBaseCameraLocalPosition;
    pos.y -= heavyDrop * limpVerticalAmount;
    pos.x += side * limpSideAmount;

    cameraTransform.localPosition = Vector3.Lerp(
        cameraTransform.localPosition,
        pos,
        Time.deltaTime * 12f
    );

    float tiltZ = side * limpTiltAmount;
    float tiltX = -heavyDrop * limpForwardTiltAmount;

    Quaternion targetRotation =
        cinematicBaseCameraLocalRotation * Quaternion.Euler(tiltX, 0f, tiltZ);

    cameraTransform.localRotation = Quaternion.Slerp(
        cameraTransform.localRotation,
        targetRotation,
        Time.deltaTime * 10f
    );
}
private void ApplyGravityOnly()
{
    if (controller == null)
        return;

    ApplyGravity();

    Vector3 finalMove = Vector3.zero;
    finalMove.y = velocity.y;

    controller.Move(finalMove * Time.deltaTime);
}
    private void LateUpdate()
    {
        if (controlsLocked || cameraExternallyControlled)
            return;

        UpdateBodyLook();
    }

    public void LockControls()
    {
        controlsLocked = true;

        moveX = 0f;
        moveZ = 0f;
        timer = 0f;

        if (controller != null && controller.isGrounded)
            velocity.y = -2f;
    }

    public void UnlockControls()
    {
        controlsLocked = false;
    }

    public void SetCameraExternallyControlled(bool value)
    {
        cameraExternallyControlled = value;

        if (value)
        {
            moveX = 0f;
            moveZ = 0f;
            timer = 0f;
        }
    }

    public void StartCinematicWalk(Transform target, float walkSpeed, bool requireW)
    {
        if (target == null)
            return;

        cinematicWalkMode = true;
        cinematicWalkTarget = target;
        cinematicWalkSpeed = Mathf.Max(0f, walkSpeed);
        cinematicRequireW = requireW;
        cinematicReachedTarget = false;

        suppressNormalHeadBob = true;

        controlsLocked = false;
        cameraExternallyControlled = false;

        moveX = 0f;
        moveZ = 0f;
        timer = 0f;

        if (cameraTransform != null)
        {
            // ВАЖНО:
            // Берём текущий поворот камеры как базовый.
            // То есть если камера уже посмотрела на дверь,
            // хромающий bob будет накладываться поверх этого взгляда.
            cinematicBaseCameraLocalPosition = cameraTransform.localPosition;
            cinematicBaseCameraLocalRotation = cameraTransform.localRotation;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void StopCinematicWalk()
    {
        cinematicWalkMode = false;
        cinematicWalkTarget = null;
        cinematicReachedTarget = false;

        moveX = 0f;
        moveZ = 0f;
        timer = 0f;
    }

    private void UpdateBodyLook()
    {
        if (headBone != null)
        {
            Quaternion targetHeadRotation =
                headStartRotation * Quaternion.Euler(xRotation * headFollowAmount, 0f, 0f);

            headBone.localRotation =
                Quaternion.Lerp(headBone.localRotation, targetHeadRotation, Time.deltaTime * 10f);
        }

        if (upperBodyBone != null)
        {
            Quaternion targetBodyRotation =
                upperBodyStartRotation * Quaternion.Euler(xRotation * bodyFollowAmount, 0f, 0f);

            upperBodyBone.localRotation =
                Quaternion.Lerp(upperBodyBone.localRotation, targetBodyRotation, Time.deltaTime * 6f);
        }
    }

    private void Look()
    {
        if (cameraTransform == null)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minY, maxY);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void Move()
    {
        if (controller == null)
            return;

        if (cinematicWalkMode)
        {
            MoveCinematicWalk();
            return;
        }

        if (controlsLocked || cameraExternallyControlled)
        {
            moveX = 0f;
            moveZ = 0f;
        }
        else
        {
            moveX = Input.GetAxisRaw("Horizontal");
            moveZ = Input.GetAxisRaw("Vertical");
        }

        Vector3 horizontalMove = Vector3.zero;

        if (!controlsLocked && !cameraExternallyControlled)
        {
            horizontalMove = (transform.right * moveX + transform.forward * moveZ).normalized;
        }

        float currentSpeed = speed;

        if (!controlsLocked && !cameraExternallyControlled && isCrisis && Input.GetKey(sprintKey))
            currentSpeed *= crisisSprintMultiplier;

        ApplyGravity();

        Vector3 finalMove = horizontalMove * currentSpeed;
        finalMove.y = velocity.y;

        controller.Move(finalMove * Time.deltaTime);
    }

    private void MoveCinematicWalk()
    {
        moveX = 0f;
        moveZ = 0f;

        Vector3 horizontalMove = Vector3.zero;

        if (cinematicWalkTarget != null)
        {
            Vector3 toTarget = cinematicWalkTarget.position - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;

            if (distance <= cinematicStopDistance)
            {
                cinematicReachedTarget = true;
            }
            else
            {
                bool wantsToMove = !cinematicRequireW || Input.GetKey(KeyCode.W);

                if (wantsToMove)
                {
                    Vector3 direction = toTarget.normalized;

                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        Time.deltaTime * cinematicTurnSpeed
                    );

                    horizontalMove = transform.forward * cinematicWalkSpeed;
                    moveZ = 1f;
                }
            }
        }

        ApplyGravity();

        Vector3 finalMove = horizontalMove;
        finalMove.y = velocity.y;

        controller.Move(finalMove * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (controller == null)
            return;

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
    }

    private void SetCrisisMode(bool enabled)
    {
        isCrisis = enabled;
    }

    private void HandleHeadBob()
    {
        if (cameraTransform == null)
            return;

        bool isMoving = Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveZ) > 0.1f;

        if (isMoving && controller != null && controller.isGrounded)
        {
            timer += Time.deltaTime * bobSpeed;
            float bob = Mathf.Sin(timer) * bobAmount;

            Vector3 pos = cameraTransform.localPosition;
            pos.x = Mathf.Lerp(pos.x, defaultCameraLocalPosition.x, Time.deltaTime * returnSpeed);
            pos.y = defaultYPos + bob;
            pos.z = Mathf.Lerp(pos.z, defaultCameraLocalPosition.z, Time.deltaTime * returnSpeed);

            cameraTransform.localPosition = pos;
        }
        else
        {
            ResetHeadBob();
        }
    }

    private void HandleCinematicLimpBob()
    {
        if (cameraTransform == null)
            return;

        bool isTryingToMove = !cinematicRequireW || Input.GetKey(KeyCode.W);
        bool canBob = isTryingToMove && !cinematicReachedTarget && controller != null && controller.isGrounded;

        if (!canBob)
        {
            ResetCinematicLimpBob();
            return;
        }

        timer += Time.deltaTime * limpBobSpeed;

        float step = Mathf.Sin(timer);
        float heavyDrop = Mathf.Abs(step);
        float side = Mathf.Sin(timer * 0.5f);

        Vector3 pos = cinematicBaseCameraLocalPosition;
        pos.y -= heavyDrop * limpVerticalAmount;
        pos.x += side * limpSideAmount;

        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition,
            pos,
            Time.deltaTime * 12f
        );

        float tiltZ = side * limpTiltAmount;
        float tiltX = -heavyDrop * limpForwardTiltAmount;

        Quaternion targetRotation =
            cinematicBaseCameraLocalRotation * Quaternion.Euler(tiltX, 0f, tiltZ);

        cameraTransform.localRotation = Quaternion.Slerp(
            cameraTransform.localRotation,
            targetRotation,
            Time.deltaTime * 10f
        );
    }

    private void ResetCinematicLimpBob()
    {
        if (cameraTransform == null)
            return;

        timer = 0f;

        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition,
            cinematicBaseCameraLocalPosition,
            Time.deltaTime * limpReturnSpeed
        );

        cameraTransform.localRotation = Quaternion.Slerp(
            cameraTransform.localRotation,
            cinematicBaseCameraLocalRotation,
            Time.deltaTime * limpReturnSpeed
        );
    }

    private void ResetHeadBob()
    {
        if (cameraTransform == null)
            return;

        timer = 0f;

        Vector3 pos = cameraTransform.localPosition;
        pos.x = Mathf.Lerp(pos.x, defaultCameraLocalPosition.x, Time.deltaTime * returnSpeed);
        pos.y = Mathf.Lerp(pos.y, defaultYPos, Time.deltaTime * returnSpeed);
        pos.z = Mathf.Lerp(pos.z, defaultCameraLocalPosition.z, Time.deltaTime * returnSpeed);

        cameraTransform.localPosition = pos;
    }

    private void ResetHeadBobImmediate()
    {
        if (cameraTransform == null)
            return;

        timer = 0f;
        cameraTransform.localPosition = defaultCameraLocalPosition;
        cameraTransform.localRotation = defaultCameraLocalRotation;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.EventManager != null)
            GameManager.Instance.EventManager.OnCrisisModeChanged -= SetCrisisMode;
    }
}
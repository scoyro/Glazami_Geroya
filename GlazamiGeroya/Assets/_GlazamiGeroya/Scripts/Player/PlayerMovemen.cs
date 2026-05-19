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

    private float defaultYPos;
    private float timer;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    private float moveX;
    private float moveZ;

    private bool controlsLocked;
    private bool cameraExternallyControlled;

    public bool ControlsLocked => controlsLocked;
    public bool CameraExternallyControlled => cameraExternallyControlled;

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
            defaultYPos = cameraTransform.localPosition.y;
    }

    private void Update()
    {
        if (!controlsLocked && !cameraExternallyControlled)
            Look();

        Move();

        if (cameraExternallyControlled)
            return;

        if (!controlsLocked)
            HandleHeadBob();
        else
            ResetHeadBob();
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

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = horizontalMove * currentSpeed;
        finalMove.y = velocity.y;

        controller.Move(finalMove * Time.deltaTime);
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
            pos.y = defaultYPos + bob;
            cameraTransform.localPosition = pos;
        }
        else
        {
            ResetHeadBob();
        }
    }

    private void ResetHeadBob()
    {
        if (cameraTransform == null)
            return;

        timer = 0f;

        Vector3 pos = cameraTransform.localPosition;
        pos.y = Mathf.Lerp(pos.y, defaultYPos, Time.deltaTime * returnSpeed);
        cameraTransform.localPosition = pos;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.EventManager != null)
            GameManager.Instance.EventManager.OnCrisisModeChanged -= SetCrisisMode;
    }
}
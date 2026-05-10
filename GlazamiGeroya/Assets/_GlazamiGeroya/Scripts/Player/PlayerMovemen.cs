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

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (headBone != null)
            headStartRotation = headBone.localRotation;

        if (upperBodyBone != null)
            upperBodyStartRotation = upperBodyBone.localRotation;
    if (GameManager.Instance != null && GameManager.Instance.EventManager != null)
{
    GameManager.Instance.EventManager.OnCrisisModeChanged += SetCrisisMode;
}

        defaultYPos = cameraTransform.localPosition.y;
    }

    void Update()
    {
        Look();
        Move();
        HandleHeadBob();
    }
    void LateUpdate()
    {
        UpdateBodyLook();
    }
    void UpdateBodyLook()
{
    if (headBone != null)
    {
        Quaternion targetHeadRotation = headStartRotation * Quaternion.Euler(xRotation * headFollowAmount, 0f, 0f);
        headBone.localRotation = Quaternion.Lerp(headBone.localRotation, targetHeadRotation, Time.deltaTime * 10f);
    }

    if (upperBodyBone != null)
    {
        Quaternion targetBodyRotation = upperBodyStartRotation * Quaternion.Euler(xRotation * bodyFollowAmount, 0f, 0f);
        upperBodyBone.localRotation = Quaternion.Lerp(upperBodyBone.localRotation, targetBodyRotation, Time.deltaTime * 6f);
    }
}
    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minY, maxY);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void Move()
    {
        moveX = Input.GetAxis("Horizontal");
        moveZ = Input.GetAxis("Vertical");

        Vector3 move = (transform.right * moveX + transform.forward * moveZ).normalized;

        float currentSpeed = speed;

        if (isCrisis && Input.GetKey(sprintKey))
            currentSpeed *= crisisSprintMultiplier;

        controller.Move(move * currentSpeed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    private void SetCrisisMode(bool enabled)
    {
        isCrisis = enabled;
    }
    void HandleHeadBob()
    {
        bool isMoving = Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveZ) > 0.1f;

        if (isMoving && controller.isGrounded)
        {
            timer += Time.deltaTime * bobSpeed;
            float bob = Mathf.Sin(timer) * bobAmount;

            Vector3 pos = cameraTransform.localPosition;
            pos.y = defaultYPos + bob;
            cameraTransform.localPosition = pos;
        }
        else
        {
            timer = 0f;

            Vector3 pos = cameraTransform.localPosition;
            pos.y = Mathf.Lerp(pos.y, defaultYPos, Time.deltaTime * returnSpeed);
            cameraTransform.localPosition = pos;
        }
    }
    private void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.EventManager != null)
            GameManager.Instance.EventManager.OnCrisisModeChanged -= SetCrisisMode;
    }
}
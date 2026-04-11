using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public Transform cameraTransform;
    public float mouseSensitivity = 200f;
    public float minY = -90f;
    public float maxY = 90f;

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

        defaultYPos = cameraTransform.localPosition.y;
    }

    void Update()
    {
        Look();
        Move();
        HandleHeadBob();
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

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * speed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
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
}
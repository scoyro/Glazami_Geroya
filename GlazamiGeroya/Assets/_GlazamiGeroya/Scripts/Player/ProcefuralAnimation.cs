using UnityEngine;

public class ProceduralWalkAnimation : MonoBehaviour
{
    [Header("Bones")]
    public Transform leftUpperLeg;
    public Transform rightUpperLeg;
    public Transform leftLowerLeg;
    public Transform rightLowerLeg;
    public Transform leftUpperArm;
    public Transform rightUpperArm;

    [Header("Settings")]
    public float walkSpeed = 8f;
    public float legAngle = 25f;
    public float kneeAngle = 20f;
    public float armAngle = 18f;
    public float smooth = 10f;

    [Header("Movement Detection")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private float minRealSpeed = 0.05f;
    [SerializeField] private float minInputAmount = 0.1f;

    [Header("Axis Test")]
    public Vector3 legAxis = Vector3.right;
    public Vector3 armAxis = Vector3.right;

    private Quaternion leftUpperLegStart;
    private Quaternion rightUpperLegStart;
    private Quaternion leftLowerLegStart;
    private Quaternion rightLowerLegStart;
    private Quaternion leftUpperArmStart;
    private Quaternion rightUpperArmStart;

    private Vector3 lastPosition;
    private float timer;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponentInParent<CharacterController>();

        lastPosition = transform.position;
    }

    private void Start()
    {
        if (leftUpperLeg != null) leftUpperLegStart = leftUpperLeg.localRotation;
        if (rightUpperLeg != null) rightUpperLegStart = rightUpperLeg.localRotation;
        if (leftLowerLeg != null) leftLowerLegStart = leftLowerLeg.localRotation;
        if (rightLowerLeg != null) rightLowerLegStart = rightLowerLeg.localRotation;
        if (leftUpperArm != null) leftUpperArmStart = leftUpperArm.localRotation;
        if (rightUpperArm != null) rightUpperArmStart = rightUpperArm.localRotation;
    }

    private void LateUpdate()
    {
        bool isMoving = IsActuallyMoving();

        if (isMoving)
            PlayWalkAnimation();
        else
            ReturnToIdlePose();

        lastPosition = transform.position;
    }

    private bool IsActuallyMoving()
    {
        if (controller != null && !controller.isGrounded)
            return false;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        bool hasInput =
            new Vector2(horizontal, vertical).sqrMagnitude >=
            minInputAmount * minInputAmount;

        if (!hasInput)
            return false;

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;

        float realSpeed =
            delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        return realSpeed >= minRealSpeed;
    }

    private void PlayWalkAnimation()
    {
        timer += Time.deltaTime * walkSpeed;

        float leftSwing = Mathf.Sin(timer);
        float rightSwing = Mathf.Sin(timer + Mathf.PI);

        RotateBone(leftUpperLeg, leftUpperLegStart, legAxis, leftSwing * legAngle);
        RotateBone(rightUpperLeg, rightUpperLegStart, legAxis, rightSwing * legAngle);

        RotateBone(leftLowerLeg, leftLowerLegStart, legAxis, Mathf.Max(0f, -leftSwing) * kneeAngle);
        RotateBone(rightLowerLeg, rightLowerLegStart, legAxis, Mathf.Max(0f, -rightSwing) * kneeAngle);

        RotateBone(leftUpperArm, leftUpperArmStart, armAxis, leftSwing * armAngle);
        RotateBone(rightUpperArm, rightUpperArmStart, armAxis, rightSwing * armAngle);
    }

    private void ReturnToIdlePose()
    {
        timer = 0f;

        ReturnBone(leftUpperLeg, leftUpperLegStart);
        ReturnBone(rightUpperLeg, rightUpperLegStart);
        ReturnBone(leftLowerLeg, leftLowerLegStart);
        ReturnBone(rightLowerLeg, rightLowerLegStart);
        ReturnBone(leftUpperArm, leftUpperArmStart);
        ReturnBone(rightUpperArm, rightUpperArmStart);
    }

    private void RotateBone(Transform bone, Quaternion startRotation, Vector3 axis, float angle)
    {
        if (bone == null)
            return;

        Quaternion target = startRotation * Quaternion.AngleAxis(angle, axis);
        bone.localRotation = Quaternion.Lerp(bone.localRotation, target, Time.deltaTime * smooth);
    }

    private void ReturnBone(Transform bone, Quaternion startRotation)
    {
        if (bone == null)
            return;

        bone.localRotation = Quaternion.Lerp(bone.localRotation, startRotation, Time.deltaTime * smooth);
    }
}
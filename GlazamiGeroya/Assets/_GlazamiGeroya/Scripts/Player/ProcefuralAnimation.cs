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

    [Header("Axis Test")]
    public Vector3 legAxis = Vector3.right;
    public Vector3 armAxis = Vector3.right;

    private Quaternion leftUpperLegStart;
    private Quaternion rightUpperLegStart;
    private Quaternion leftLowerLegStart;
    private Quaternion rightLowerLegStart;
    private Quaternion leftUpperArmStart;
    private Quaternion rightUpperArmStart;

    private float timer;

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
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;

        if (isMoving)
        {
            timer += Time.deltaTime * walkSpeed;

            float leftSwing = Mathf.Sin(timer);
            float rightSwing = Mathf.Sin(timer + Mathf.PI);

            // Ноги
            RotateBone(leftUpperLeg, leftUpperLegStart, legAxis, leftSwing * legAngle);
            RotateBone(rightUpperLeg, rightUpperLegStart, legAxis, rightSwing * legAngle);

            // Колени
            RotateBone(leftLowerLeg, leftLowerLegStart, legAxis, Mathf.Max(0f, -leftSwing) * kneeAngle);
            RotateBone(rightLowerLeg, rightLowerLegStart, legAxis, Mathf.Max(0f, -rightSwing) * kneeAngle);

            // Руки — перекрёстно относительно ног
            RotateBone(leftUpperArm, leftUpperArmStart, armAxis, leftSwing * armAngle);
            RotateBone(rightUpperArm, rightUpperArmStart, armAxis, rightSwing * armAngle);
        }
        else
        {
            timer = 0f;

            ReturnBone(leftUpperLeg, leftUpperLegStart);
            ReturnBone(rightUpperLeg, rightUpperLegStart);
            ReturnBone(leftLowerLeg, leftLowerLegStart);
            ReturnBone(rightLowerLeg, rightLowerLegStart);
            ReturnBone(leftUpperArm, leftUpperArmStart);
            ReturnBone(rightUpperArm, rightUpperArmStart);
        }
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
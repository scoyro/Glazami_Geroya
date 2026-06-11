using UnityEngine;

public class ValveCameraEffectsController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Punch")]
    [SerializeField] private float punchAmount = 0.02f;
    [SerializeField] private float punchReturnSpeed = 8f;

    [Header("Shake")]
    [SerializeField] private float minShake = 0.001f;
    [SerializeField] private float maxShake = 0.008f;
    [SerializeField] private float shakeSpeed = 18f;

    private Vector3 baseLocalPosition;

    private bool effectsActive;
    private float stress;
    private float currentPunch;

    private void Awake()
    {
        if (cameraTransform == null)
            cameraTransform = transform;

        baseLocalPosition = cameraTransform.localPosition;
    }

    private void LateUpdate()
    {
        if (!effectsActive)
            return;

        float shakeAmount = Mathf.Lerp(minShake, maxShake, stress);

        Vector3 shakeOffset = new Vector3(
            Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f,
            Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f,
            0f
        ) * shakeAmount;

        currentPunch = Mathf.Lerp(
            currentPunch,
            0f,
            Time.deltaTime * punchReturnSpeed
        );

        Vector3 punchOffset = Vector3.back * currentPunch;

        cameraTransform.localPosition =
            baseLocalPosition +
            shakeOffset +
            punchOffset;
    }

    public void BeginEffects()
    {
        effectsActive = true;
        stress = 0f;
        currentPunch = 0f;

        baseLocalPosition = cameraTransform.localPosition;
    }

    public void EndEffects()
    {
        effectsActive = false;
        stress = 0f;
        currentPunch = 0f;

        if (cameraTransform != null)
            cameraTransform.localPosition = baseLocalPosition;
    }

    public void SetStress(float value)
    {
        stress = Mathf.Clamp01(value);
    }

    public void Punch()
    {
        if (!effectsActive)
            return;

        currentPunch += punchAmount;
    }
}
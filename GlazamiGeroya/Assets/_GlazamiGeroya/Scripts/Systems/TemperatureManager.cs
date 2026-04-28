using UnityEngine;

public class TemperatureManager : MonoBehaviour
{
    [SerializeField] private TemperatureProfile defaultProfile;

    [SerializeField] private float currentTemperature = 24f;
    [SerializeField] private float heatSourceTemperatureBonus;

    public TemperatureProfile ActiveProfile => activeProfile;
    public float CurrentTemperature => currentTemperature;

    private TemperatureProfile activeProfile;
    private GameManager gameManager;

    private float transitionTimer;
    private float transitionStartTemperature;
    private float transitionTargetTemperature;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        activeProfile = defaultProfile;

        if (defaultProfile != null)
        {
            currentTemperature = defaultProfile.targetTemperature;
            transitionTargetTemperature = currentTemperature;
        }
    }
    

    private void Update()
    {
        if (activeProfile == null)
            return;

        float desiredTarget = activeProfile.targetTemperature + heatSourceTemperatureBonus;

        if (Mathf.Abs(desiredTarget - transitionTargetTemperature) > 0.01f)
        {
            StartTemperatureTransition(desiredTarget);
        }

        UpdateTemperatureTransition();

        gameManager?.EventManager?.RaiseTemperatureChanged(currentTemperature);
    }

    public void SetProfile(TemperatureProfile profile)
    {
        if (profile == null)
            return;

        activeProfile = profile;
        StartTemperatureTransition(activeProfile.targetTemperature + heatSourceTemperatureBonus);
    }

    public void AddHeatSource(float value)
    {
        heatSourceTemperatureBonus += value;

        if (activeProfile != null)
            StartTemperatureTransition(activeProfile.targetTemperature + heatSourceTemperatureBonus);
    }

    public void RemoveHeatSource(float value)
    {
        heatSourceTemperatureBonus -= value;
        heatSourceTemperatureBonus = Mathf.Max(0f, heatSourceTemperatureBonus);

        if (activeProfile != null)
            StartTemperatureTransition(activeProfile.targetTemperature + heatSourceTemperatureBonus);
    }

    private void StartTemperatureTransition(float target)
    {
        transitionTimer = 0f;
        transitionStartTemperature = currentTemperature;
        transitionTargetTemperature = target;
    }

    private void UpdateTemperatureTransition()
    {
        float duration = Mathf.Max(0.01f, activeProfile.transitionDuration);

        transitionTimer += Time.deltaTime;

        float normalizedTime = Mathf.Clamp01(transitionTimer / duration);
        float curveValue = activeProfile.temperatureCurve.Evaluate(normalizedTime);

        currentTemperature = Mathf.Lerp(
            transitionStartTemperature,
            transitionTargetTemperature,
            curveValue
        );
    }
    public void ApplyTemperatureDelta(float delta)
    {
        if (Mathf.Abs(delta) <= 0.001f)
            return;

        currentTemperature = Mathf.Max(0f, currentTemperature + delta);
        heatSourceTemperatureBonus = Mathf.Max(0f, heatSourceTemperatureBonus + delta);

        if (activeProfile != null)
            StartTemperatureTransition(activeProfile.targetTemperature + heatSourceTemperatureBonus);

        gameManager?.EventManager?.RaiseTemperatureChanged(currentTemperature);
    }
    public void SetProfileFromInspector(TemperatureProfile profile)
    {
        SetProfile(profile);
    }
}
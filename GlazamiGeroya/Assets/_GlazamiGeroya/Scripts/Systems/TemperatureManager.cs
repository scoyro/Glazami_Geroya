using UnityEngine;

public class TemperatureManager : MonoBehaviour
{
    [SerializeField] private TemperatureProfile defaultProfile;

    [SerializeField] private float currentTemperature = 24f;
    [SerializeField] private float heatSourceTemperatureBonus;

    public float CurrentTemperature => currentTemperature;

    private TemperatureProfile activeProfile;
    private GameManager gameManager;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        activeProfile = defaultProfile;

        if (defaultProfile != null)
            currentTemperature = defaultProfile.targetTemperature;
    }

    private void Update()
    {
        if (activeProfile == null)
            return;

        float target = activeProfile.targetTemperature + heatSourceTemperatureBonus;

        currentTemperature = Mathf.MoveTowards(
            currentTemperature,
            target,
            activeProfile.changeSpeed * Time.deltaTime
        );

        gameManager?.EventManager?.RaiseTemperatureChanged(currentTemperature);
    }

    public void SetProfile(TemperatureProfile profile)
    {
        if (profile == null)
            return;

        activeProfile = profile;
    }

    public void AddHeatSource(float value)
    {
        heatSourceTemperatureBonus += value;
    }

    public void RemoveHeatSource(float value)
    {
        heatSourceTemperatureBonus -= value;
        heatSourceTemperatureBonus = Mathf.Max(0f, heatSourceTemperatureBonus);
    }
}
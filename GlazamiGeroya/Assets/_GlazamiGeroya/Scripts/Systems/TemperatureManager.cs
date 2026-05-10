using UnityEngine;
using System.Collections.Generic;

public class TemperatureManager : MonoBehaviour
{
    [SerializeField] private TemperatureProfile defaultProfile;

    [SerializeField] private float currentTemperature = 24f;
    [SerializeField] private float heatSourceTemperatureBonus;

    public TemperatureProfile ActiveProfile => activeProfile;
    public float CurrentTemperature => currentTemperature;

    private readonly List<TemperatureZone> activeZones = new List<TemperatureZone>();
    private readonly HashSet<HeatSource> activeHeatSources = new HashSet<HeatSource>();

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

        CleanupInvalidReferences();
        RecalculateTemperatureTarget();

        UpdateTemperatureTransition();

        gameManager?.EventManager?.RaiseTemperatureChanged(currentTemperature);
    }

    public void EnterZone(TemperatureZone zone)
    {
        if (zone == null || zone.Profile == null)
            return;

        if (!activeZones.Contains(zone))
            activeZones.Add(zone);

        RecalculateActiveProfile();
        RecalculateTemperatureTarget();
    }
    public void ExitZone(TemperatureZone zone)
    {
        if (zone == null)
            return;

        if (activeZones.Remove(zone))
        {
            RecalculateActiveProfile();
            RecalculateTemperatureTarget();
        }
    }
    public void ExitHeatSource(HeatSource source)
    {
        if (source == null)
            return;

        if (activeHeatSources.Remove(source))
        {
            RecalculateHeatBonus();
            RecalculateTemperatureTarget();
        }
    }
    public void EnterHeatSource(HeatSource source)
    {
        if (source == null)
            return;

        activeHeatSources.Add(source);
        RecalculateHeatBonus();
        RecalculateTemperatureTarget();
    }
    public void SetProfile(TemperatureProfile profile)
    {
        if (profile == null || profile == activeProfile)
            return;

        activeProfile = profile;
        RecalculateTemperatureTarget();
    }

    public void AddHeatSource(float value)
    {
        heatSourceTemperatureBonus += value;
        RecalculateTemperatureTarget();
    }

    public void RemoveHeatSource(float value)
    {
        heatSourceTemperatureBonus -= value;
        heatSourceTemperatureBonus = Mathf.Max(0f, heatSourceTemperatureBonus);
        RecalculateTemperatureTarget();
    }
    private void RecalculateActiveProfile()
    {
        TemperatureProfile bestProfile = defaultProfile;
        float bestTemperature = defaultProfile != null ? defaultProfile.targetTemperature : float.MinValue;

        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            TemperatureZone zone = activeZones[i];

            if (zone == null || !zone.isActiveAndEnabled || zone.Profile == null)
            {
                activeZones.RemoveAt(i);
                continue;
            }

            if (zone.Profile.targetTemperature > bestTemperature)
            {
                bestTemperature = zone.Profile.targetTemperature;
                bestProfile = zone.Profile;
            }
        }

        if (bestProfile != activeProfile)
            activeProfile = bestProfile;
    }
    private void RecalculateHeatBonus()
    {
        float totalBonus = 0f;

        foreach (HeatSource source in activeHeatSources)
        {
            if (source == null || !source.isActiveAndEnabled)
                continue;

            totalBonus += source.TemperatureBonus;
        }

        heatSourceTemperatureBonus = Mathf.Max(0f, totalBonus);
    }
    private void RecalculateTemperatureTarget()
    {
        if (activeProfile == null)
            return;

        float desiredTarget = activeProfile.targetTemperature + heatSourceTemperatureBonus;

        if (Mathf.Abs(desiredTarget - transitionTargetTemperature) > 0.01f)
            StartTemperatureTransition(desiredTarget);
    }
    private void StartTemperatureTransition(float target)
    {
        transitionTimer = 0f;
        transitionStartTemperature = currentTemperature;
        transitionTargetTemperature = target;
    }

    private void UpdateTemperatureTransition()
    {
        if (activeProfile == null)
            return;

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
    private void CleanupInvalidReferences()
    {
        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            TemperatureZone zone = activeZones[i];

            if (zone == null || !zone.isActiveAndEnabled || zone.Profile == null)
                activeZones.RemoveAt(i);
        }

        activeHeatSources.RemoveWhere(source => source == null || !source.isActiveAndEnabled);
        RecalculateActiveProfile();
        RecalculateHeatBonus();
    }
    public void RefreshZonesForPlayer(Collider playerCollider)
    {
        activeZones.Clear();

        if (playerCollider == null)
        {
            RecalculateActiveProfile();
            RecalculateTemperatureTarget();
            return;
        }

        Physics.SyncTransforms();

        Collider[] hits = Physics.OverlapBox(
            playerCollider.bounds.center,
            playerCollider.bounds.extents,
            playerCollider.transform.rotation,
            ~0,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
        {
            TemperatureZone zone = hit.GetComponentInParent<TemperatureZone>();

            if (zone == null || zone.Profile == null)
                continue;

            if (!activeZones.Contains(zone))
                activeZones.Add(zone);
        }

        RecalculateActiveProfile();
        RecalculateTemperatureTarget();
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
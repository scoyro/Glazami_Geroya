using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TemperatureVFXController : MonoBehaviour
{
    [Header("Volume")]
    [Tooltip("Назначь сюда именно ситуативный Global Volume для жара/пожара.")]
    [SerializeField] private Volume temperatureVolume;

    [Tooltip("Скорость плавного изменения Weight у температурного Volume.")]
    [SerializeField] private float volumeFadeSpeed = 2.5f;

    [Tooltip("За сколько градусов до dangerTemperature эффект начинает появляться.")]
    [SerializeField] private float preDangerTemperatureRange = 15f;

    [Header("Effect Strength")]
    [Range(0f, 1f)]
    [SerializeField] private float weightAtDangerTemperature = 0.5f;

    [Header("Vignette")]
    [SerializeField] private Color heatColor = new Color(0.35f, 0f, 0f);
    [SerializeField] private float maxVignetteIntensity = 0.65f;

    [Header("Blur / Depth of Field")]
    [SerializeField] private float minBlurRadius = 0f;
    [SerializeField] private float maxBlurRadius = 1.5f;

    [Header("Color")]
    [SerializeField] private float maxContrast = 30f;
    [SerializeField] private float maxSaturationLoss = -35f;

    private GameManager gameManager;

    private Vignette vignette;
    private DepthOfField depthOfField;
    private ColorAdjustments colorAdjustments;

    private float targetVolumeWeight;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;

        if (temperatureVolume == null)
        {
            Debug.LogError("TemperatureVFXController: назначь Temperature Volume вручную.");
            enabled = false;
            return;
        }

        if (temperatureVolume.sharedProfile == null && temperatureVolume.profile == null)
        {
            Debug.LogError("TemperatureVFXController: у Temperature Volume нет Profile.");
            enabled = false;
            return;
        }

        temperatureVolume.enabled = true;
        temperatureVolume.isGlobal = true;
        temperatureVolume.priority = Mathf.Max(temperatureVolume.priority, 10f);
        temperatureVolume.weight = 0f;
        targetVolumeWeight = 0f;

        // Используем instance profile, чтобы не портить asset профиля в проекте.
        VolumeProfile profile = temperatureVolume.profile;

        EnsureOverride(profile, ref vignette);
        EnsureOverride(profile, ref depthOfField);
        EnsureOverride(profile, ref colorAdjustments);

        if (gameManager?.EventManager != null)
        {
            gameManager.EventManager.OnTemperatureChanged -= ApplyTemperatureEffect;
            gameManager.EventManager.OnTemperatureChanged += ApplyTemperatureEffect;
        }
    }

    private void Update()
    {
        if (temperatureVolume == null)
            return;

        temperatureVolume.weight = Mathf.MoveTowards(
            temperatureVolume.weight,
            targetVolumeWeight,
            volumeFadeSpeed * Time.deltaTime
        );
    }

    private void ApplyTemperatureEffect(float temperature)
    {
        TemperatureProfile activeProfile = gameManager?.TemperatureManager?.ActiveProfile;

        if (activeProfile == null)
        {
            targetVolumeWeight = 0f;
            return;
        }

        float effectT = CalculateEffectT(temperature, activeProfile);
        targetVolumeWeight = CalculateVolumeWeight(temperature, activeProfile);

        ApplyVignette(effectT);
        ApplyDepthOfField(effectT);
        ApplyColorAdjustments(effectT);
    }


    private float CalculateVolumeWeight(float temperature, TemperatureProfile activeProfile)
    {
        float danger = activeProfile.dangerTemperature;
        float lethal = activeProfile.lethalTemperature;
        float start = danger - Mathf.Max(0.01f, preDangerTemperatureRange);

        if (temperature <= start)
            return 0f;

        if (temperature < danger)
        {
            float preDangerT = Mathf.InverseLerp(start, danger, temperature);
            return Mathf.Lerp(0f, weightAtDangerTemperature, preDangerT);
        }

        float dangerToLethalT = Mathf.InverseLerp(danger, lethal, temperature);
        return Mathf.Lerp(weightAtDangerTemperature, 1f, Mathf.Clamp01(dangerToLethalT));
    }

    private float CalculateEffectT(float temperature, TemperatureProfile activeProfile)
    {
        float danger = activeProfile.dangerTemperature;
        float lethal = activeProfile.lethalTemperature;
        float start = danger - Mathf.Max(0.01f, preDangerTemperatureRange);

        return Mathf.Clamp01(Mathf.InverseLerp(start, lethal, temperature));
    }

    private static void EnsureOverride<T>(VolumeProfile profile, ref T component) where T : VolumeComponent
    {
        if (!profile.TryGet(out component))
            component = profile.Add<T>(true);

        component.active = true;
        component.SetAllOverridesTo(true);
    }

    private void ApplyVignette(float t)
    {
        if (vignette == null)
            return;

        float vignetteT = Mathf.Pow(t, 0.6f);

        vignette.active = true;
        vignette.color.Override(heatColor);
        vignette.intensity.Override(Mathf.Lerp(0f, maxVignetteIntensity, vignetteT));
        vignette.smoothness.Override(Mathf.Lerp(0.35f, 0.8f, t));
    }

    private void ApplyDepthOfField(float t)
    {
        if (depthOfField == null)
            return;

        depthOfField.active = true;
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(0f);
        depthOfField.gaussianEnd.Override(3f);
        depthOfField.gaussianMaxRadius.Override(Mathf.Lerp(minBlurRadius, maxBlurRadius, t));
    }

    private void ApplyColorAdjustments(float t)
    {
        if (colorAdjustments == null)
            return;

        colorAdjustments.active = true;
        colorAdjustments.contrast.Override(Mathf.Lerp(0f, maxContrast, t));
        colorAdjustments.saturation.Override(Mathf.Lerp(0f, maxSaturationLoss, t));
    }

    private void OnDestroy()
    {
        if (gameManager?.EventManager == null)
            return;

        gameManager.EventManager.OnTemperatureChanged -= ApplyTemperatureEffect;
    }
}

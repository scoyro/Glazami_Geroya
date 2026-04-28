using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TemperatureVFXController : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume globalVolume;


    [Header("Vignette")]
    [SerializeField] private Color heatColor = new Color(0.25f, 0f, 0f);
    [SerializeField] private float maxVignetteIntensity = 1f;

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

    public void Initialize(GameManager manager)
    {
        gameManager = manager;

        if (globalVolume == null)
            globalVolume = FindFirstObjectByType<Volume>();

        if (globalVolume == null || globalVolume.profile == null)
        {
            Debug.LogError("TemperatureVFXController: Global Volume не назначен или нет Profile.");
            return;
        }

        globalVolume.profile.TryGet(out vignette);
        globalVolume.profile.TryGet(out depthOfField);
        globalVolume.profile.TryGet(out colorAdjustments);

        if (gameManager?.EventManager != null)
        {
            gameManager.EventManager.OnTemperatureChanged -= ApplyTemperatureEffect;
            gameManager.EventManager.OnTemperatureChanged += ApplyTemperatureEffect;
        }
    }

    private void ApplyTemperatureEffect(float temperature)
    {
        TemperatureProfile activeProfile = gameManager?.TemperatureManager?.ActiveProfile;

        if (activeProfile == null)
            return;

        float t = Mathf.InverseLerp(
            activeProfile.targetTemperature,
            activeProfile.lethalTemperature,
            temperature
        );

        t = Mathf.Clamp01(t);

        if (vignette != null)
        {
            float vingetteT = Mathf.Pow(t, 0.6f);
            vignette.active = true;
            vignette.color.Override(heatColor);
            vignette.intensity.Override(Mathf.Lerp(0f, maxVignetteIntensity, vingetteT));
            vignette.smoothness.Override(Mathf.Lerp(0.35f, 0.8f, t));
        }

        if (depthOfField != null)
        {
            depthOfField.active = true;
            depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
            depthOfField.gaussianStart.Override(0f);
            depthOfField.gaussianEnd.Override(3f);
            depthOfField.gaussianMaxRadius.Override(Mathf.Lerp(minBlurRadius, maxBlurRadius, t));
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.active = true;
            colorAdjustments.contrast.Override(Mathf.Lerp(0f, maxContrast, t));
            colorAdjustments.saturation.Override(Mathf.Lerp(0f, maxSaturationLoss, t));
        }
    }

    private void OnDestroy()
    {
        if (gameManager?.EventManager == null) return;
        gameManager.EventManager.OnTemperatureChanged -= ApplyTemperatureEffect;
    }
}
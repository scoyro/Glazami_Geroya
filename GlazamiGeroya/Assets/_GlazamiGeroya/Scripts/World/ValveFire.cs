using System.Collections;
using UnityEngine;

public class ValveFirePressureController : MonoBehaviour
{
    [System.Serializable]
    private class ParticleFireLayer
    {
        public ParticleSystem particleSystem;

        public float startRate = 60f;
        public float maxRate = 120f;

        public float startSize = 2.5f;
        public float maxSize = 4f;

        public float startSpeed = 1.5f;
        public float maxSpeed = 2.2f;
    }

    [Header("Fire Layers")]
    [SerializeField] private ParticleFireLayer[] fireLayers;

    [Header("Sparks")]
    [SerializeField] private ParticleSystem sparksVfx;
    [Range(0f, 1f)]
    [SerializeField] private float sparksStartPressure = 0.65f;

    [Header("Light")]
    [SerializeField] private Light fireLight;
    [SerializeField] private float startLightIntensity = 3f;
    [SerializeField] private float maxLightIntensity = 8f;

    private float currentPressure;

    public void ResetFire()
    {
        currentPressure = 0f;
        ApplyPressure(0f);

        if (sparksVfx != null)
            sparksVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void SetPressure(float pressure)
    {
        currentPressure = Mathf.Clamp01(pressure);
        ApplyPressure(currentPressure);
        UpdateSparks();
    }

    public void FadeDownAfterSuccess(float duration)
    {
        StartCoroutine(FadeDownRoutine(duration));
    }

    private IEnumerator FadeDownRoutine(float duration)
    {
        float startPressure = currentPressure;
        float targetPressure = 0.25f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = duration > 0f ? timer / duration : 1f;
            float pressure = Mathf.Lerp(startPressure, targetPressure, t);

            SetPressure(pressure);

            yield return null;
        }

        SetPressure(targetPressure);
    }

    private void ApplyPressure(float pressure)
    {
        foreach (ParticleFireLayer layer in fireLayers)
        {
            if (layer == null || layer.particleSystem == null)
                continue;

            ParticleSystem.MainModule main = layer.particleSystem.main;
            ParticleSystem.EmissionModule emission = layer.particleSystem.emission;

            main.startSize = Mathf.Lerp(layer.startSize, layer.maxSize, pressure);
            main.startSpeed = Mathf.Lerp(layer.startSpeed, layer.maxSpeed, pressure);
            emission.rateOverTime = Mathf.Lerp(layer.startRate, layer.maxRate, pressure);
        }

        if (fireLight != null)
            fireLight.intensity = Mathf.Lerp(startLightIntensity, maxLightIntensity, pressure);
    }

    private void UpdateSparks()
    {
        if (sparksVfx == null)
            return;

        if (currentPressure >= sparksStartPressure)
        {
            if (!sparksVfx.isPlaying)
                sparksVfx.Play();
        }
        else
        {
            if (sparksVfx.isPlaying)
                sparksVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
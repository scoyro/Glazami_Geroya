using UnityEngine;

[CreateAssetMenu(fileName = "TemperatureProfile", menuName = "GlazamiGeroya/Temperature Profile")]
public class TemperatureProfile : ScriptableObject
{
    public float targetTemperature = 25f;

    public float dangerTemperature = 60f;
    public float lethalTemperature = 110f;

    [Header("Curve")]
    public AnimationCurve temperatureCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float transitionDuration = 3f;

    [Range(0f, 1f)] public float redScreenIntensity;
    [Range(0f, 1f)] public float blurIntensity;
    [Range(0f, 1f)] public float breathingIntensity;
}
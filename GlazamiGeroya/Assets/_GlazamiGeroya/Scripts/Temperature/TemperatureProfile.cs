using UnityEngine;

[CreateAssetMenu(fileName = "TemperatureProfile", menuName = "GlazamiGeroya/Temperature Profile")]
public class TemperatureProfile : ScriptableObject
{
    public string id;
    public string displayName;

    public float targetTemperature = 25f;
    public float changeSpeed = 5f;

    public float dangerTemperature = 60f;
    public float lethalTemperature = 110f;

    [Range(0f, 1f)] public float redScreenIntensity;
    [Range(0f, 1f)] public float blurIntensity;
    [Range(0f, 1f)] public float breathingIntensity;
}
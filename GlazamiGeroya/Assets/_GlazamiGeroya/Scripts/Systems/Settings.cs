using UnityEngine;
using UnityEngine.Audio;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Ссылки")]
    [SerializeField] private AudioMixer audioMixer;

    // Текущие значения настроек
    public float MasterVolume { get; private set; }
    public float AmbienceVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public float MouseSensitivity { get; private set; }

    public event Action OnSettingsChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadSettings();
    }

    private void Start()
    {
        ApplyAllAudioSettings();
    }

    private void LoadSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f); 
        AmbienceVolume = PlayerPrefs.GetFloat("AmbienceVolume", 1f);
        SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        
        MouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 5f); 
    }

    public void SetMasterVolume(float linearValue)
    {
        // Изменили максимальный предел с 1f на 3f
        MasterVolume = Mathf.Clamp(linearValue, 0.0001f, 3f); 
        UpdateMixerVolume("Master", MasterVolume);
        SaveSetting("MasterVolume", MasterVolume);
    }

    public void SetAmbienceVolume(float linearValue)
    {
        // Изменили максимальный предел с 1f на 3f
        AmbienceVolume = Mathf.Clamp(linearValue, 0.0001f, 3f);
        UpdateMixerVolume("AmbienceVolume", AmbienceVolume);
        SaveSetting("AmbienceVolume", AmbienceVolume);
    }

    public void SetSFXVolume(float linearValue)
    {
        // Изменили максимальный предел с 1f на 3f
        SFXVolume = Mathf.Clamp(linearValue, 0.0001f, 3f);
        UpdateMixerVolume("SFXVolume", SFXVolume);
        SaveSetting("SFXVolume", SFXVolume);
    }

    public void SetMouseSensitivity(float value)
    {
        MouseSensitivity = value;
        SaveSetting("MouseSensitivity", MouseSensitivity);
        OnSettingsChanged?.Invoke(); 
    }

    public void ApplyAllAudioSettings()
    {
        UpdateMixerVolume("Master", MasterVolume); // Оставляем Master, он совпадает
        UpdateMixerVolume("AmbienceVolume", AmbienceVolume); // Меняем здесь
        UpdateMixerVolume("SFXVolume", SFXVolume); // Меняем здесь
    }

    private void UpdateMixerVolume(string parameterName, float linearValue)
    {
        if (audioMixer != null)
        {
            float decibelValue = Mathf.Log10(linearValue) * 20f;
            audioMixer.SetFloat(parameterName, decibelValue);
        }
        else
        {
            Debug.LogWarning("AudioMixer не назначен в SettingsManager!");
        }
    }

    private void SaveSetting(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save(); 
    }
}
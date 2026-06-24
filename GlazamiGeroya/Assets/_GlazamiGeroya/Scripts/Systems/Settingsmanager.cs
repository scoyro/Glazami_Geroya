using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI Панели")]
    [SerializeField] private GameObject settingsPanel; 

    [Header("Слайдеры")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider ambienceVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Настройки чувствительности (границы)")]
    [SerializeField] private float minSensitivity = 50f;
    [SerializeField] private float maxSensitivity = 500f;

    // ДОБАВЛЕН НОВЫЙ БЛОК ДЛЯ ЗВУКА
    [Header("Настройки громкости (границы)")]
    [Tooltip("Минимальное значение не должно быть 0 (иначе будет ошибка логарифма). Оставьте 0.0001")]
    [SerializeField] private float minVolume = 0.0001f;
    [SerializeField] private float maxVolume = 1f;

    private void Start()
    {
        settingsPanel.SetActive(false);

        // Применяем границы из Инспектора к слайдеру мыши
        sensitivitySlider.minValue = minSensitivity;
        sensitivitySlider.maxValue = maxSensitivity;

        // ПРИМЕНЯЕМ ГРАНИЦЫ ИЗ ИНСПЕКТОРА К СЛАЙДЕРАМ ЗВУКА
        masterVolumeSlider.minValue = minVolume; 
        masterVolumeSlider.maxValue = maxVolume;
        
        ambienceVolumeSlider.minValue = minVolume; 
        ambienceVolumeSlider.maxValue = maxVolume;
        
        sfxVolumeSlider.minValue = minVolume;    
        sfxVolumeSlider.maxValue = maxVolume;

        UpdateUIValues();

        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        ambienceVolumeSlider.onValueChanged.AddListener(OnAmbienceVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsMenu();
        }
    }

    public void ToggleSettingsMenu()
    {
        bool isActive = !settingsPanel.activeSelf;
        settingsPanel.SetActive(isActive);

        if (isActive)
        {
            UpdateUIValues();
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void UpdateUIValues()
    {
        if (SettingsManager.Instance != null)
        {
            sensitivitySlider.value = SettingsManager.Instance.MouseSensitivity;
            masterVolumeSlider.value = SettingsManager.Instance.MasterVolume;
            ambienceVolumeSlider.value = SettingsManager.Instance.AmbienceVolume;
            sfxVolumeSlider.value = SettingsManager.Instance.SFXVolume;
        }
    }

    private void OnSensitivityChanged(float value)
    {
        SettingsManager.Instance.SetMouseSensitivity(value);
    }

    private void OnMasterVolumeChanged(float value)
    {
        SettingsManager.Instance.SetMasterVolume(value);
    }

    private void OnAmbienceVolumeChanged(float value)
    {
        SettingsManager.Instance.SetAmbienceVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        SettingsManager.Instance.SetSFXVolume(value);
    }

    private void OnDestroy()
    {
        sensitivitySlider.onValueChanged.RemoveAllListeners();
        masterVolumeSlider.onValueChanged.RemoveAllListeners();
        ambienceVolumeSlider.onValueChanged.RemoveAllListeners();
        sfxVolumeSlider.onValueChanged.RemoveAllListeners();
    }
}
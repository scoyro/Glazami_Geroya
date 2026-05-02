using UnityEngine;

public class CrisisSceneState : MonoBehaviour
{
    [Header("Enable on crisis")]
    [SerializeField] private GameObject[] enableOnCrisis;

    [Header("Disable on crisis")]
    [SerializeField] private GameObject[] disableOnCrisis;

    [Header("Lights")]
    [SerializeField] private Light[] lightsToDisable;
    [SerializeField] private Light[] emergencyLights;

    [Header("Optional")]
    [SerializeField] private bool startDisabled = true;

    private GameManager gameManager;

    private void Start()
    {
        if (startDisabled)
        {
            foreach (var obj in enableOnCrisis)
                if (obj != null) obj.SetActive(false);

            foreach (var light in emergencyLights)
                if (light != null) light.enabled = false;
        }

        if (GameManager.Instance != null)
            Initialize(GameManager.Instance);
    }

    private void Initialize(GameManager manager)
    {
        gameManager = manager;

        if (gameManager.EventManager == null)
            return;

        gameManager.EventManager.OnCrisisModeChanged -= HandleCrisisModeChanged;
        gameManager.EventManager.OnCrisisModeChanged += HandleCrisisModeChanged;
    }

    private void HandleCrisisModeChanged(bool enabled)
    {
        if (!enabled)
            return;

        foreach (var obj in enableOnCrisis)
            if (obj != null) obj.SetActive(true);

        foreach (var obj in disableOnCrisis)
            if (obj != null) obj.SetActive(false);

        foreach (var light in lightsToDisable)
            if (light != null) light.enabled = false;

        foreach (var light in emergencyLights)
            if (light != null) light.enabled = true;
    }

    private void OnDestroy()
    {
        if (gameManager?.EventManager == null)
            return;

        gameManager.EventManager.OnCrisisModeChanged -= HandleCrisisModeChanged;
    }
}
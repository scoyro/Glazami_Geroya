using UnityEngine;

public class TemperatureZone : MonoBehaviour
{
    [Header("Profiles")]
    [SerializeField] private TemperatureProfile normalProfile;
    [SerializeField] private TemperatureProfile fireProfile;

    private TemperatureProfile currentProfile;
    private bool playerInside;

    public TemperatureProfile Profile => currentProfile;

    private void Start()
    {
        currentProfile = normalProfile;

        if (GameManager.Instance?.EventManager != null)
            GameManager.Instance.EventManager.OnCrisisModeChanged += HandleCrisisModeChanged;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        GameManager.Instance?.TemperatureManager?.EnterZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        GameManager.Instance?.TemperatureManager?.ExitZone(this);
    }

    private void HandleCrisisModeChanged(bool isCrisis)
    {
        currentProfile = isCrisis && fireProfile != null
            ? fireProfile
            : normalProfile;

        if (playerInside)
        {
            GameManager.Instance?.TemperatureManager?.ExitZone(this);
            GameManager.Instance?.TemperatureManager?.EnterZone(this);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance?.EventManager != null)
            GameManager.Instance.EventManager.OnCrisisModeChanged -= HandleCrisisModeChanged;

        if (playerInside)
            GameManager.Instance?.TemperatureManager?.ExitZone(this);
    }
}
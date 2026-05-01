using UnityEngine;

public class TemperatureZone : MonoBehaviour
{
    [SerializeField] private TemperatureProfile profile;
    public TemperatureProfile Profile => profile;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        GameManager.Instance?.TemperatureManager?.EnterZone(this);
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        GameManager.Instance?.TemperatureManager?.ExitZone(this);
    }
    private void OnDisable()
    {
        GameManager.Instance?.TemperatureManager?.ExitZone(this);
    }
}
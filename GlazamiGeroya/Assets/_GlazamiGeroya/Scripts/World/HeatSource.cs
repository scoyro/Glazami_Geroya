using UnityEngine;

public class HeatSource : MonoBehaviour
{
    [SerializeField] private float temperatureBonus = 15f;

    private bool playerInside;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || playerInside)
            return;

        playerInside = true;
        GameManager.Instance?.TemperatureManager?.AddHeatSource(temperatureBonus);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || !playerInside)
            return;

        playerInside = false;
        GameManager.Instance?.TemperatureManager?.RemoveHeatSource(temperatureBonus);
    }
}
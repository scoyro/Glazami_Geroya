using UnityEngine;

public class HeatSource : MonoBehaviour
{
    [SerializeField] private float temperatureBonus = 15f;
    public float TemperatureBonus => temperatureBonus;
    private bool playerInside;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || playerInside)
            return;

        playerInside = true;
        GameManager.Instance?.TemperatureManager?.EnterHeatSource(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || !playerInside)
            return;

        playerInside = false;
        GameManager.Instance?.TemperatureManager?.ExitHeatSource(this);
    }

    private void OnDisable()
    {
        if (!playerInside)
            return;

        playerInside = false;
        GameManager.Instance?.TemperatureManager?.ExitHeatSource(this);
    }
}
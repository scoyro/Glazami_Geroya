using UnityEngine;

public class TemperatureZone : MonoBehaviour
{
    [SerializeField] private TemperatureProfile profile;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        GameManager.Instance?.TemperatureManager?.SetProfile(profile);
    }
}
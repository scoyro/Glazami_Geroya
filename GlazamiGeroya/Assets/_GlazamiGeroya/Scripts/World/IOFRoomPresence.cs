using UnityEngine;

public class IofRoomPresence : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    public bool PlayerInside { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            PlayerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            PlayerInside = false;
    }
}
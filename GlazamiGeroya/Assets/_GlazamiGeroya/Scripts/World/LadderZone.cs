using UnityEngine;

public class LadderInteractionZone : MonoBehaviour
{
    [SerializeField] private LadderInteractionState ladderState;
    [SerializeField] private bool isTopZone;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (isTopZone)
            ladderState.EnableTopInteraction();
        else
            ladderState.EnableBottomInteraction();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        ladderState.SetNoneActive();
    }
}
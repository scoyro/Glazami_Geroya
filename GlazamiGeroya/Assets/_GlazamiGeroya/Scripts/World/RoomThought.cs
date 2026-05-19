using System.Collections;
using UnityEngine;

public class RoomThoughtTrigger : MonoBehaviour
{
    [Header("Thought")]
    [TextArea]
    [SerializeField] private string thoughtText;

    [SerializeField] private float thoughtDuration = 4f;
    [SerializeField] private float delayBeforeShow = 0f;

    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Optional Conditions")]
    [SerializeField] private bool onlyInCrisis;
    [SerializeField] private bool onlyInValveSequence;

    private bool wasTriggered;
    private Coroutine routine;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (triggerOnlyOnce && wasTriggered)
            return;

        if (!CanTriggerByGamePhase())
            return;

        wasTriggered = true;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ShowThoughtRoutine());
    }

    private bool CanTriggerByGamePhase()
    {
        if (GameManager.Instance == null || GameManager.Instance.GameStateManager == null)
            return true;

        GamePhase phase = GameManager.Instance.GameStateManager.Phase;

        if (onlyInCrisis && phase != GamePhase.Crisis)
            return false;

        if (onlyInValveSequence && phase != GamePhase.ValveSequence)
            return false;

        return true;
    }

    private IEnumerator ShowThoughtRoutine()
    {
        if (delayBeforeShow > 0f)
            yield return new WaitForSeconds(delayBeforeShow);

        if (!string.IsNullOrWhiteSpace(thoughtText))
        {
            GameManager.Instance?.EventManager?.RequestThought(
                thoughtText,
                thoughtDuration
            );
        }

        routine = null;
    }
}
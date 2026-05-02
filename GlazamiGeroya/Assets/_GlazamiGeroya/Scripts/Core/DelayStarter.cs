using System.Collections;
using UnityEngine;

public class DelayedIncidentStarter : MonoBehaviour
{
    [SerializeField] private float delaySeconds = 3f;

    private bool started;

    public void StartIncidentWithDelay()
    {
        if (started)
            return;

        started = true;
        StartCoroutine(StartRoutine());
    }

    private IEnumerator StartRoutine()
    {
        yield return new WaitForSeconds(delaySeconds);

        GameManager.Instance?.ChoiceSystem?.StartIncident();
    }
}
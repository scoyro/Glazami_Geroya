using UnityEngine;

public class CommanderPositionSwitcher : MonoBehaviour
{
    [Header("Commander Positions")]
    [SerializeField] private GameObject commanderStart;
    [SerializeField] private GameObject commanderAfterJournal;
    [SerializeField] private GameObject commanderReport;

    [Header("Start State")]
    [SerializeField] private bool setupOnStart = true;

    private void Start()
    {
        if (setupOnStart)
            ShowStartCommander();
    }

    public void ShowStartCommander()
    {
        SetActiveCommander(commanderStart);
    }

    public void ShowAfterJournalCommander()
    {
        SetActiveCommander(commanderAfterJournal);
    }

    public void ShowReportCommander()
    {
        SetActiveCommander(commanderReport);
    }

    private void SetActiveCommander(GameObject activeCommander)
    {
        if (commanderStart != null)
            commanderStart.SetActive(commanderStart == activeCommander);

        if (commanderAfterJournal != null)
            commanderAfterJournal.SetActive(commanderAfterJournal == activeCommander);

        if (commanderReport != null)
            commanderReport.SetActive(commanderReport == activeCommander);
    }
}
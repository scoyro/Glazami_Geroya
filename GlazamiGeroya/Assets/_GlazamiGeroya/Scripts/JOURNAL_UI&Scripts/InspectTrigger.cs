using UnityEngine;

public class InspectTrigger : MonoBehaviour
{
    [SerializeField] private TaskData linkedTask; // та же SO, что в журнале
    [SerializeField] private string promptText = "Нажмите [E] для осмотра";

    private bool playerInRange = false;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
            TryInspect();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            UIManagerJournal.Instance.ShowPrompt(promptText);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            UIManagerJournal.Instance.HidePrompt();
        }
    }

    void TryInspect()
    {
        var qm = QuestManager.Instance;

        // проверяем что это именно текущее задание
        if (!qm.IsJournalTaken) return;
        if (qm.CurrentTask != linkedTask) return;

        // показываем информацию об объекте
        UIManagerJournal.Instance.ShowObjectInfo(linkedTask);
    }
}
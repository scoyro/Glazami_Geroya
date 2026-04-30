using UnityEngine;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [SerializeField] private JournalData journalData;

    // индекс текущей активной задачи
    public int CurrentTaskIndex { get; private set; } = -1;
    public TaskData CurrentTask => IsJournalTaken && CurrentTaskIndex < journalData.tasks.Count
        ? journalData.tasks[CurrentTaskIndex] : null;

    public bool IsJournalTaken { get; private set; } = false;

    // событие: задача выполнена, передаём следующую (или null если всё)
    public System.Action<TaskData> OnTaskCompleted;
    public System.Action<TaskData> OnTaskChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TakeJournal()
    {
        IsJournalTaken = true;
        CurrentTaskIndex = 0;
        OnTaskChanged?.Invoke(CurrentTask);
        Debug.Log("Журнал взят! Первое задание: " + CurrentTask?.taskTitle);
    }

    public void CompleteCurrentTask()
    {
        if (CurrentTask == null) return;

        OnTaskCompleted?.Invoke(CurrentTask);
        CurrentTaskIndex++;

        if (CurrentTaskIndex < journalData.tasks.Count)
        {
            OnTaskChanged?.Invoke(CurrentTask);
            Debug.Log("Следующее задание: " + CurrentTask.taskTitle);
        }
        else
        {
            CurrentTaskIndex = journalData.tasks.Count; // за пределами списка = всё выполнено
            OnTaskChanged?.Invoke(null);
            Debug.Log("Все задания выполнены!");
        }
    }

    public bool AllTasksCompleted() =>
        IsJournalTaken && CurrentTaskIndex >= journalData.tasks.Count;
}
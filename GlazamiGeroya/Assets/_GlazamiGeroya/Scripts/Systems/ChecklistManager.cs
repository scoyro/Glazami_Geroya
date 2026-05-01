using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Журнал задач в спокойной фазе и краткие цели в кризисе.
/// </summary>
public class ChecklistManager : MonoBehaviour
{
    [SerializeField] private List<ChecklistTask> initialTasks = new List<ChecklistTask>();
    [SerializeField] private List<ChecklistTask> crisisTasks = new List<ChecklistTask>();

    private readonly Dictionary<string, ChecklistTask> tasks = new Dictionary<string, ChecklistTask>();
    private GameManager gameManager;

    public IReadOnlyDictionary<string, ChecklistTask> Tasks => tasks;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        BuildTaskSet(initialTasks);

        if (gameManager?.EventManager != null)
        {
            gameManager.EventManager.OnTaskCompleted -= HandleTaskCompleted;
            gameManager.EventManager.OnPhaseChanged -= HandlePhaseChanged;

            gameManager.EventManager.OnTaskCompleted += HandleTaskCompleted;
            gameManager.EventManager.OnPhaseChanged += HandlePhaseChanged;
        }

        PushUi();
    }
    // проверка на выполнение
    public bool IsTaskCompleted(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            return true;

        return tasks.TryGetValue(taskId, out var task) && task.isCompleted;
    }

    public void CompleteTask(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId) || !tasks.TryGetValue(taskId, out var task) || task.isCompleted)
            return;

        task.isCompleted = true;
        tasks[taskId] = task;

        if (!string.IsNullOrWhiteSpace(task.nextTaskId) && tasks.TryGetValue(task.nextTaskId, out var nextTask))
        {
            nextTask.isVisible = true;
            tasks[task.nextTaskId] = nextTask;
        }

        gameManager?.EventManager?.RaiseTaskCompleted(taskId);
        PushUi();
    }

    public void SetTaskVisible(string taskId, bool visible)
    {
        if (!tasks.TryGetValue(taskId, out var task)) return;
        task.isVisible = visible;
        tasks[taskId] = task;
        PushUi();
    }

    private void HandleTaskCompleted(string taskId)
    {
        if (!tasks.TryGetValue(taskId, out var task)) return;
        task.isCompleted = true;
        tasks[taskId] = task;
        PushUi();
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Crisis)
            BuildTaskSet(crisisTasks.Count > 0 ? crisisTasks : initialTasks);

        if (phase == GamePhase.Ended)
            ClearAll();

        PushUi();
    }

    private void BuildTaskSet(List<ChecklistTask> source)
    {
        tasks.Clear();

        foreach (var task in source)
        {
            if (task == null || string.IsNullOrWhiteSpace(task.id))
                continue;

            tasks[task.id] = new ChecklistTask(task);
        }
    }

    private void ClearAll()
    {
        tasks.Clear();
    }

    private void PushUi()
    {
        gameManager?.UIManager?.RefreshChecklist(tasks.Values);
    }

    private void OnDestroy()
    {
        if (gameManager?.EventManager == null) return;
        gameManager.EventManager.OnTaskCompleted -= HandleTaskCompleted;
        gameManager.EventManager.OnPhaseChanged -= HandlePhaseChanged;
    }
}

[Serializable]
public class ChecklistTask
{
    public string id;
    public string title;
    public bool isVisible = true;
    public bool isCompleted;

    [Header("Следующая задача")]
    public string nextTaskId;

    public ChecklistTask() { }

    public ChecklistTask(ChecklistTask other)
    {
        id = other.id;
        title = other.title;
        isVisible = other.isVisible;
        isCompleted = other.isCompleted;
        nextTaskId = other.nextTaskId;
    }
}

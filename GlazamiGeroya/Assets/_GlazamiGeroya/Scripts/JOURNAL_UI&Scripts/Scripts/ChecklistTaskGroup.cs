using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Группа скрытых шагов внутри одного видимого задания.
/// Например: один квест "Проверить топливные вентили" выполняется
/// только после проверки двух разных вентилей.
/// </summary>
public class ChecklistTaskGroup : MonoBehaviour
{
    [Serializable]
    private class Step
    {
        public string id;
        public bool completed;
    }

    [Header("Checklist")]
    [SerializeField] private string completesTaskId;
    [SerializeField] private bool completeOnlyOnce = true;

    [Header("Steps")]
    [SerializeField] private List<Step> requiredSteps = new List<Step>();

    [Header("Feedback")]
    [TextArea]
    [SerializeField] private string allStepsCompletedThought = "Проверка завершена.";
    [SerializeField] private float thoughtDuration = 2.5f;

    [Header("Events")]
    [SerializeField] private UnityEvent onAllStepsCompleted;

    private bool taskCompleted;

    public void CompleteStep(string stepId)
    {
        if (string.IsNullOrWhiteSpace(stepId))
            return;

        bool found = false;

        foreach (Step step in requiredSteps)
        {
            if (step == null || step.id != stepId)
                continue;

            step.completed = true;
            found = true;
            break;
        }

        if (!found)
            Debug.LogWarning($"ChecklistTaskGroup '{name}': stepId '{stepId}' не найден.", this);

        TryCompleteGroup();
    }

    public bool IsStepCompleted(string stepId)
    {
        if (string.IsNullOrWhiteSpace(stepId))
            return false;

        foreach (Step step in requiredSteps)
        {
            if (step != null && step.id == stepId)
                return step.completed;
        }

        return false;
    }

    private void TryCompleteGroup()
    {
        if (completeOnlyOnce && taskCompleted)
            return;

        if (requiredSteps == null || requiredSteps.Count == 0)
            return;

        foreach (Step step in requiredSteps)
        {
            if (step == null || !step.completed)
                return;
        }

        taskCompleted = true;

        if (!string.IsNullOrWhiteSpace(completesTaskId))
            GameManager.Instance?.ChecklistManager?.CompleteTask(completesTaskId);

        onAllStepsCompleted?.Invoke();

        if (!string.IsNullOrWhiteSpace(allStepsCompletedThought))
            GameManager.Instance?.EventManager?.RequestThought(allStepsCompletedThought, thoughtDuration);
    }
}

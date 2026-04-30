using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIManagerJournal : MonoBehaviour
{
    public static UIManagerJournal Instance { get; private set; }

    [Header("Журнал")]
    [SerializeField] private GameObject journalPanel;
    [SerializeField] private Transform taskListContainer; // ScrollView Content
    [SerializeField] private GameObject taskItemPrefab;   // префаб строки задачи

    [Header("Инфо об объекте")]
    [SerializeField] private GameObject objectInfoPanel;
    [SerializeField] private TMP_Text objectInfoTitle;
    [SerializeField] private TMP_Text objectInfoText;
    [SerializeField] private Button objectInfoCloseBtn; // кнопка "Понял, продолжаю"

    [Header("Подсказка")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        objectInfoCloseBtn.onClick.AddListener(OnObjectInfoConfirmed);
        QuestManager.Instance.OnTaskChanged += RefreshJournalUI;
    }

    // --- Журнал ---

    public void ToggleJournal()
    {
        journalPanel.SetActive(!journalPanel.activeSelf);
    }

    void RefreshJournalUI(TaskData currentTask)
    {
        // очищаем старый список
        foreach (Transform child in taskListContainer)
            Destroy(child.gameObject);

        var qm = QuestManager.Instance;
        var allTasks = /* получаем через QuestManager или напрямую */ qm; // см. примечание ниже

        // лучше сделать публичный метод GetAllTasks() в QuestManager
        // здесь для примера пропустим итерацию
    }

    // --- Подсказка ---

    public void ShowPrompt(string text)
    {
        promptPanel.SetActive(true);
        promptText.text = text;
    }

    public void HidePrompt() => promptPanel.SetActive(false);

    // --- Инфо об объекте ---

    public void ShowObjectInfo(TaskData task)
    {
        objectInfoPanel.SetActive(true);
        objectInfoTitle.text = task.taskTitle;
        objectInfoText.text = task.objectInfo;
        Time.timeScale = 0f; // пауза (опционально)
    }

    void OnObjectInfoConfirmed()
    {
        objectInfoPanel.SetActive(false);
        Time.timeScale = 1f;
        QuestManager.Instance.CompleteCurrentTask(); // выполняем задание!
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Выводит концовку и историческую справку.
/// </summary>
public class EndingController : MonoBehaviour
{
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;
    [SerializeField] private List<EndingData> endings = new List<EndingData>();
    [SerializeField] private string menuSceneName = "MainMenu";

    private readonly Dictionary<string, EndingData> endingMap = new Dictionary<string, EndingData>();
    private GameManager gameManager;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        endingMap.Clear();

        foreach (var ending in endings)
        {
            if (ending == null || string.IsNullOrWhiteSpace(ending.id))
                continue;

            endingMap[ending.id] = ending;
        }

        if (endingPanel != null)
            endingPanel.SetActive(false);
    }

    public void PlayEnding(GameResult result, string endingId)
    {
        if (endingPanel != null)
            endingPanel.SetActive(true);

        if (endingMap.TryGetValue(endingId, out var ending))
        {
            if (titleText != null) titleText.text = ending.title;
            if (bodyText != null) bodyText.text = ending.description;
        }
        else
        {
            if (titleText != null)
                titleText.text = result == GameResult.Victory ? "Подвиг совершен" : "Трагический исход";

            if (bodyText != null)
                bodyText.text = "Добавьте текст концовки и историческую справку в инспекторе.";
        }

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        gameManager?.SceneController?.ReloadCurrentScene();
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        gameManager?.SceneController?.LoadScene(menuSceneName);
    }
}

[System.Serializable]
public class EndingData
{
    public string id;
    public string title;
    [TextArea(4, 10)] public string description;
}

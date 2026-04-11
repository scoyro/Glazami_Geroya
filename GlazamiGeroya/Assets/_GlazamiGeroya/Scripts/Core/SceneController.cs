using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Загрузка сцен с плавным переходом.
/// </summary>
public class SceneController : MonoBehaviour
{
    private GameManager gameManager;
    private bool isLoading;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void LoadScene(string sceneName)
    {
        if (!gameObject.activeInHierarchy || isLoading) return;
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        if (ScreenFader.Instance != null)
            yield return StartCoroutine(FadeOutCoroutine());

        yield return SceneManager.LoadSceneAsync(sceneName);

        if (ScreenFader.Instance != null)
            yield return StartCoroutine(FadeInCoroutine());

        isLoading = false;
    }

    private IEnumerator FadeOutCoroutine()
    {
        ScreenFader.Instance.FadeOut();
        while (ScreenFader.Instance.IsBusy)
            yield return null;
    }

    private IEnumerator FadeInCoroutine()
    {
        ScreenFader.Instance.FadeIn();
        while (ScreenFader.Instance.IsBusy)
            yield return null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gameManager?.EventManager?.RaiseSceneLoaded(scene.name);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

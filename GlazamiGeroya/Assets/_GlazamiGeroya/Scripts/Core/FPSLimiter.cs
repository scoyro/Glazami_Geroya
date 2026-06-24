using UnityEngine;

public static class GameBootstrapper
{
    // Этот атрибут запускает метод автоматически при старте игры
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void ApplyGlobalSettings()
    {
        // Дублируем отключение VSync на случай непредвиденных сбросов графики
        QualitySettings.vSyncCount = 0;
        
        // Устанавливаем жесткий лимит для всего проекта
        Application.targetFrameRate = 30;
        
        Debug.Log("Глобальные настройки применены: VSync выключен, FPS ограничен до 120.");
    }
}
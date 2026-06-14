using UnityEngine;
using System.Collections;

public class ExplosionCinematic : MonoBehaviour
{
    [Header("Эффект взрыва")]
    [Tooltip("Источник света, который будет ослеплять (желательно оранжевый/желтый)")]
    [SerializeField] private Light explosionLight;
    [Tooltip("До какой интенсивности разгоняем свет")]
    [SerializeField] private float maxLightIntensity = 150f;
    [Tooltip("За сколько секунд свет достигает максимума")]
    [SerializeField] private float flashDuration = 0.2f;

    [Header("Звук")]
    [SerializeField] private AudioSource explosionAudioSource;

    [Header("Концовка")]
    [Tooltip("ID концовки из EndingController")]
    [SerializeField] private string explosionEndingId = "timeout_explosion";
    [Tooltip("Сколько секунд висит белый экран до появления UI концовки")]
    [SerializeField] private float delayBeforeEnding = 1.5f;

    // Этот метод нужно будет вызвать, когда таймер истечет
    public void TriggerExplosion()
    {
        StartCoroutine(ExplosionRoutine());
    }

    private IEnumerator ExplosionRoutine()
    {
        // 1. Блокируем игрока сразу, чтобы он не ходил во время взрыва
        if (GameManager.Instance != null && GameManager.Instance.GameStateManager != null)
        {
            // Если у вас есть метод блокировки в GameStateManager, вызываем его
            // ИЛИ просто ставим паузу/блокируем PlayerController напрямую
        }

        // 2. Включаем звук взрыва
        if (explosionAudioSource != null)
        {
            explosionAudioSource.Play();
        }

        // 3. Резко увеличиваем яркость света (Вспышка)
        if (explosionLight != null)
        {
            explosionLight.gameObject.SetActive(true);
            float elapsed = 0f;
            float initialIntensity = explosionLight.intensity;

            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashDuration;
                // Используем кривую для более резкой вспышки
                explosionLight.intensity = Mathf.Lerp(initialIntensity, maxLightIntensity, t * t);
                yield return null;
            }
            
            explosionLight.intensity = maxLightIntensity;
        }

        // 4. Ждем пару секунд, пока игрок "ослеплен" и слушает эхо взрыва
        yield return new WaitForSeconds(delayBeforeEnding);

        // 5. Вызываем экран концовки
        if (GameManager.Instance != null && GameManager.Instance.EndingController != null)
        {
            GameManager.Instance.EndingController.PlayEnding(explosionEndingId);
        }
    }
}
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class PlayerFireExposure : MonoBehaviour
{
    [Header("Fire Damage Settings")]
    [Tooltip("Время до смерти в огне (секунды)")]
    [SerializeField] private float maxFireTime = 9f;
    [Tooltip("Максимальная сила тряски от урона")]
    [SerializeField] private float maxShakeAmount = 0.1f;

    [Header("Cinematic Death Animation")]
    [Tooltip("Сколько секунд длится падение на пол")]
    [SerializeField] private float fallDuration = 1.2f;
    [Tooltip("На сколько метров вниз падает камера")]
    [SerializeField] private float fallHeightDrop = 1.2f;
    [Tooltip("Угол наклона камеры (заваливание на бок)")]
    [SerializeField] private float fallTiltAngle = 75f;

    [Header("Audio")]
    [Tooltip("Источник звука с клипом тяжелого дыхания")]
    [SerializeField] private AudioSource breathingAudioSource;

    [Header("Ending Settings")]
    [SerializeField] private EndingController endingController;
    [SerializeField] private string endingId = "death_in_fire";

    private float currentFireTime = 0f;
    private int overlappingFireZones = 0; 
    private bool IsInFire => overlappingFireZones > 0;

    private PlayerController playerController;
    private Vector3 originalCameraPosOffset = Vector3.zero;
    
    private bool isDying = false; 
    
    // НОВЫЙ ФЛАГ: гарантирует, что звук дыхания запустится только один раз
    private bool hasTriggeredBreathing = false; 

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void LateUpdate()
    {
        if (isDying) return;

        if (IsInFire)
        {
            // 1. Увеличиваем таймер
            currentFireTime += Time.deltaTime;

            // 2. Управляем звуком дыхания через флаг, чтобы не было повторных запусков
            if (breathingAudioSource != null && !hasTriggeredBreathing)
            {
                breathingAudioSource.Play();
                hasTriggeredBreathing = true;
            }

            // 3. Тряска камеры 
            float intensity = currentFireTime / maxFireTime;
            ApplyDamageShake(intensity);

            // 4. Смерть
            if (currentFireTime >= maxFireTime)
            {
                StartCoroutine(DeathSequence());
            }
        }
        else
        {
            // Плавно восстанавливаемся, если вышли из огня
            if (currentFireTime > 0)
            {
                currentFireTime -= Time.deltaTime * 1.5f; 
                currentFireTime = Mathf.Max(0, currentFireTime);

                if (breathingAudioSource != null && breathingAudioSource.isPlaying)
                {
                    breathingAudioSource.Stop();
                }

                // Сбрасываем флаг, чтобы звук мог начаться заново, если игрок снова войдет в огонь
                hasTriggeredBreathing = false;

                // Плавно убираем тряску
                ApplyDamageShake(0f);
            }
        }
    }

    private void ApplyDamageShake(float intensity)
    {
        if (playerController.cameraTransform == null) return;

        if (intensity > 0)
        {
            float currentShake = intensity * maxShakeAmount;
            originalCameraPosOffset = Random.insideUnitSphere * currentShake;
            originalCameraPosOffset.z = 0f; 
        }
        else
        {
            originalCameraPosOffset = Vector3.Lerp(originalCameraPosOffset, Vector3.zero, Time.deltaTime * 5f);
        }

        playerController.cameraTransform.localPosition += originalCameraPosOffset;
    }

    private IEnumerator DeathSequence()
    {
        isDying = true;

        // Принудительно отключаем звук дыхания перед падением
        if (breathingAudioSource != null && breathingAudioSource.isPlaying)
        {
            breathingAudioSource.Stop();
        }

        // 1. Блокируем управление
        playerController.LockControls();
        playerController.SetCameraExternallyControlled(true);

        Transform camTransform = playerController.cameraTransform;
        if (camTransform != null)
        {
            Vector3 startPos = camTransform.localPosition;
            Quaternion startRot = camTransform.localRotation;

            Vector3 targetPos = startPos;
            targetPos.y -= fallHeightDrop; 
            
            Quaternion targetRot = startRot * Quaternion.Euler(0, 0, fallTiltAngle);

            float elapsed = 0f;

            // 2. Плавная анимация падения
            while (elapsed < fallDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fallDuration;
                float curve = t * t * (3f - 2f * t); 

                camTransform.localPosition = Vector3.Lerp(startPos, targetPos, curve);
                camTransform.localRotation = Quaternion.Slerp(startRot, targetRot, curve);

                yield return null;
            }
        }

        // 3. Лежим на полу 1.5 секунды
        yield return new WaitForSeconds(1.5f);

        // 4. Вызываем экран концовки
        DieFromFire();
    }

    private void DieFromFire()
    {
        overlappingFireZones = 0;
        
        if (endingController != null)
        {
            endingController.PlayEnding(endingId);
        }
        else if (GameManager.Instance != null && GameManager.Instance.EndingController != null)
        {
            GameManager.Instance.EndingController.PlayEnding(endingId);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<LethalFireZone>() != null)
        {
            overlappingFireZones++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<LethalFireZone>() != null)
        {
            overlappingFireZones--;
            overlappingFireZones = Mathf.Max(0, overlappingFireZones);
        }
    }
}
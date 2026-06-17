using UnityEngine;

public class NpcEyeController : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Перетащи сюда объект с сеткой лица (MeshRenderer или SkinnedMeshRenderer)")]
    [SerializeField] private Renderer faceRenderer;
    
    [Tooltip("Индекс материала лица. Если у модели только один материал, оставь 0. Если материалов несколько (например: 0-тело, 1-лицо), укажи нужную цифру.")]
    [SerializeField] private int materialIndex = 0;

    [Header("Материалы")]
    [SerializeField] private Material openEyesMaterial;
    [SerializeField] private Material closedEyesMaterial;

    // Метод для вызова из анимации
    public void OpenEyes()
    {
        if (faceRenderer != null && openEyesMaterial != null)
        {
            // Получаем текущий список материалов на модели
            Material[] mats = faceRenderer.materials;
            
            // Подменяем материал под нужным индексом
            mats[materialIndex] = openEyesMaterial;
            
            // Возвращаем обновленный список обратно
            faceRenderer.materials = mats;
        }
    }

    public void CloseEyes()
    {
        if (faceRenderer != null && closedEyesMaterial != null)
        {
            Material[] mats = faceRenderer.materials;
            mats[materialIndex] = closedEyesMaterial;
            faceRenderer.materials = mats;
        }
    }
}
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LethalFireZone : MonoBehaviour
{
    // Пустой скрипт-маркер. 
    // Он нужен просто для того, чтобы игрок мог понять, что зашел именно в ОГОНЬ, а не просто к теплой бочке.
    
    private void Awake()
    {
        // На всякий случай проверяем, что коллайдер является триггером
        GetComponent<Collider>().isTrigger = true;
    }
}
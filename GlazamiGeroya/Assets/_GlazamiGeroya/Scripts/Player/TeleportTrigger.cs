using UnityEngine;

public class TeleportTrigger : MonoBehaviour
{
    [SerializeField] private Transform targetPoint; // Куда переместить
    [SerializeField] private float waitInDarkness = 0.5f; // Пауза в темноте
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && ScreenFader.Instance != null && !ScreenFader.Instance.IsBusy)
        {
            // Запускаем процесс через ваш ScreenFader
            ScreenFader.Instance.FadeAction(() =>
            {
                CharacterController cc = other.GetComponent<CharacterController>();

                // Из-за чарактера контролера тп не работает
                if (cc != null) cc.enabled = false;

                other.transform.position = targetPoint.position;
                other.transform.rotation = targetPoint.rotation;

                if (cc != null) cc.enabled = true;
            },-1f, -1f, waitInDarkness);
        }
    }
}

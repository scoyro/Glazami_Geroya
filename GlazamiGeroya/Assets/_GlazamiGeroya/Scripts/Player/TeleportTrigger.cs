using UnityEngine;

public class TeleportInteraction : MonoBehaviour
{
    [SerializeField] private Transform targetPoint;
    [SerializeField] private float waitInDarkness = 0.5f;

    private bool isTeleporting;

    public void TeleportPlayer()
    {
        if (isTeleporting || targetPoint == null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        isTeleporting = true;

        if (ScreenFader.Instance != null && !ScreenFader.Instance.IsBusy)
        {
            ScreenFader.Instance.FadeAction(() =>
            {
                MovePlayer(player);
            }, -1f, -1f, waitInDarkness);
        }
        else
        {
            MovePlayer(player);
        }
    }

    private void MovePlayer(GameObject player)
    {
        CharacterController cc = player.GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        player.transform.position = targetPoint.position;
        player.transform.rotation = targetPoint.rotation;

        if (cc != null)
            cc.enabled = true;

        Physics.SyncTransforms();

        Collider playerCollider = cc != null ? cc : player.GetComponent<Collider>();
        GameManager.Instance?.TemperatureManager?.RefreshZonesForPlayer(playerCollider);

        isTeleporting = false;
    }
}
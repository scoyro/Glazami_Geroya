using UnityEngine;

public class PlayerControlLock : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerController playerController;

    [Header("Scripts To Disable Completely")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    private bool isLocked;

    public void LockControls()
    {
        if (isLocked)
            return;

        isLocked = true;

        if (playerController != null)
            playerController.LockControls();

        SetScriptsEnabled(false);
    }

    public void UnlockControls()
    {
        if (!isLocked)
            return;

        isLocked = false;

        if (playerController != null)
            playerController.UnlockControls();

        SetScriptsEnabled(true);
    }

    private void SetScriptsEnabled(bool enabled)
    {
        if (scriptsToDisable == null)
            return;

        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null)
                script.enabled = enabled;
        }
    }
}
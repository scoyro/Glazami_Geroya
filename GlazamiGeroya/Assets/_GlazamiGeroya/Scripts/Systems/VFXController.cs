using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простое включение эффектов по id.
/// </summary>
public class VFXController : MonoBehaviour
{
    [SerializeField] private List<VfxEntry> vfxEntries = new List<VfxEntry>();

    private readonly Dictionary<string, GameObject> vfxMap = new Dictionary<string, GameObject>();
    private GameManager gameManager;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        vfxMap.Clear();

        foreach (var entry in vfxEntries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.target == null)
                continue;

            vfxMap[entry.id] = entry.target;
        }

        if (gameManager?.EventManager != null)
        {
            gameManager.EventManager.OnVfxRequested -= PlayVfx;
            gameManager.EventManager.OnVfxRequested += PlayVfx;
        }
    }

    public void PlayVfx(string vfxId)
    {
        if (string.IsNullOrWhiteSpace(vfxId)) return;
        if (vfxMap.TryGetValue(vfxId, out var target) && target != null)
            target.SetActive(true);
    }

    public void StopVfx(string vfxId)
    {
        if (string.IsNullOrWhiteSpace(vfxId)) return;
        if (vfxMap.TryGetValue(vfxId, out var target) && target != null)
            target.SetActive(false);
    }

    private void OnDestroy()
    {
        if (gameManager?.EventManager == null) return;
        gameManager.EventManager.OnVfxRequested -= PlayVfx;
    }
}

[System.Serializable]
public class VfxEntry
{
    public string id;
    public GameObject target;
}

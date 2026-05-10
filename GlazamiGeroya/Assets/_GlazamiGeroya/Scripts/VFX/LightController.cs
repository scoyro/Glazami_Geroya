using UnityEngine;

public class CrisisLightController : MonoBehaviour
{
    [System.Serializable]
    private class LightState
    {
        public Light light;

        [Header("Crisis Settings")]
        public Color crisisColor = Color.red;
        public float crisisIntensity = 2f;
        public float crisisRange = 8f;
    }

    [SerializeField] private LightState[] lights;
    [SerializeField] private float transitionSpeed = 2f;

    private GameManager gameManager;
    private bool crisisMode;

    private void Start()
    {
        if (GameManager.Instance != null)
            Initialize(GameManager.Instance);
    }

    private void Initialize(GameManager manager)
    {
        gameManager = manager;

        if (gameManager.EventManager == null)
            return;

        gameManager.EventManager.OnCrisisModeChanged -= SetCrisisMode;
        gameManager.EventManager.OnCrisisModeChanged += SetCrisisMode;
    }

    private void SetCrisisMode(bool enabled)
    {
        crisisMode = enabled;
    }

    private void Update()
    {
        if (!crisisMode)
            return;

        foreach (var state in lights)
        {
            if (state == null || state.light == null)
                continue;

            state.light.color = Color.Lerp(
                state.light.color,
                state.crisisColor,
                Time.deltaTime * transitionSpeed
            );

            state.light.intensity = Mathf.Lerp(
                state.light.intensity,
                state.crisisIntensity,
                Time.deltaTime * transitionSpeed
            );

            state.light.range = Mathf.Lerp(
                state.light.range,
                state.crisisRange,
                Time.deltaTime * transitionSpeed
            );
        }
    }

    private void OnDestroy()
    {
        if (gameManager?.EventManager == null)
            return;

        gameManager.EventManager.OnCrisisModeChanged -= SetCrisisMode;
    }
}
using System;
using UnityEngine;

/// <summary>
/// Корневой объект приложения. Держит ссылки на менеджеры и инициализирует их.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] private TemperatureManager temperatureManager;
    [SerializeField] private SceneController sceneController;
    [SerializeField] private EventManager eventManager;
    [SerializeField] private ChoiceSystem choiceSystem;
    [SerializeField] private InteractionSystem interactionSystem;
    [SerializeField] private ChecklistManager checklistManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private VFXController vfxController;
    [SerializeField] private AtmosphereManager atmosphereManager;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private EndingController endingController;
    [SerializeField] private TemperatureVFXController temperatureVFXController;

    public SceneController SceneController => sceneController;
    public EventManager EventManager => eventManager;
    public ChoiceSystem ChoiceSystem => choiceSystem;
    public InteractionSystem InteractionSystem => interactionSystem;
    public ChecklistManager ChecklistManager => checklistManager;
    public UIManager UIManager => uiManager;
    public VFXController VFXController => vfxController;
    public AtmosphereManager AtmosphereManager => atmosphereManager;
    public GameStateManager GameStateManager => gameStateManager;
    public EndingController EndingController => endingController;
    public TemperatureManager TemperatureManager => temperatureManager;
    public TemperatureVFXController TemperatureVFXController => temperatureVFXController;

    public static event Action<GameManager> OnGameManagerReady;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CacheManagers();
    }

    private void Start()
    {
        Bootstrap();
    }

    private void CacheManagers()
    {
        if (sceneController == null) sceneController = GetComponentInChildren<SceneController>(true);
        if (eventManager == null) eventManager = GetComponentInChildren<EventManager>(true);
        if (choiceSystem == null) choiceSystem = GetComponentInChildren<ChoiceSystem>(true);
        if (interactionSystem == null) interactionSystem = GetComponentInChildren<InteractionSystem>(true);
        if (checklistManager == null) checklistManager = GetComponentInChildren<ChecklistManager>(true);
        if (uiManager == null) uiManager = GetComponentInChildren<UIManager>(true);
        if (vfxController == null) vfxController = GetComponentInChildren<VFXController>(true);
        if (atmosphereManager == null) atmosphereManager = GetComponentInChildren<AtmosphereManager>(true);
        if (gameStateManager == null) gameStateManager = GetComponentInChildren<GameStateManager>(true);
        if (endingController == null) endingController = GetComponentInChildren<EndingController>(true);
        if (temperatureManager == null) temperatureManager = GetComponentInChildren<TemperatureManager>(true);
        if (temperatureVFXController == null) temperatureVFXController = GetComponentInChildren<TemperatureVFXController>(true);
    }

    public void Bootstrap()
    {
        CacheManagers();

        eventManager?.Initialize(this);
        sceneController?.Initialize(this);
        choiceSystem?.Initialize(this);
        interactionSystem?.Initialize(this);
        checklistManager?.Initialize(this);
        uiManager?.Initialize(this);
        vfxController?.Initialize(this);
        atmosphereManager?.Initialize(this);
        gameStateManager?.Initialize(this);
        endingController?.Initialize(this);
        temperatureManager?.Initialize(this);
        temperatureVFXController?.Initialize(this);

        OnGameManagerReady?.Invoke(this);
    }
}

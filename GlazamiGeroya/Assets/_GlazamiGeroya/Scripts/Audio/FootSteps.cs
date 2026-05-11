using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource footstepSource;

    [Header("Clips")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Movement Detection")]
    [SerializeField] private float minInputAmount = 0.1f;
    [SerializeField] private float minRealSpeed = 0.05f;

    [Header("Step Timing")]
    [SerializeField] private float walkStepInterval = 0.48f;
    [SerializeField] private float sprintStepInterval = 0.34f;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Randomization")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);
    [SerializeField] private Vector2 volumeRange = new Vector2(0.65f, 0.85f);

    private CharacterController controller;
    private Vector3 lastPosition;
    private float stepTimer;
    private bool isCrisis;
    private int lastClipIndex = -1;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (footstepSource == null)
            footstepSource = GetComponent<AudioSource>();

        if (footstepSource != null)
        {
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
            footstepSource.pitch = 1f;
            footstepSource.spatialBlend = 1f;
            footstepSource.dopplerLevel = 0f;
        }

        lastPosition = transform.position;
        stepTimer = walkStepInterval;
    }

    private void OnEnable()
    {
        TrySubscribe();
        SyncInitialState();
    }

    private void Start()
    {
        TrySubscribe();
        SyncInitialState();
    }

    private void OnDisable()
    {
        if (GameManager.Instance?.EventManager != null)
            GameManager.Instance.EventManager.OnCrisisModeChanged -= SetCrisisMode;
    }

    private void Update()
    {
        if (footstepSource == null || footstepClips == null || footstepClips.Length == 0)
            return;

        bool shouldPlaySteps = ShouldPlayFootsteps();

        if (!shouldPlaySteps)
        {
            stepTimer = Mathf.Min(stepTimer, walkStepInterval);
            lastPosition = transform.position;
            return;
        }

        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            PlayFootstep();
            stepTimer = GetStepInterval();
        }

        lastPosition = transform.position;
    }

    private bool ShouldPlayFootsteps()
    {
        if (!controller.isGrounded)
            return false;

        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        bool hasMoveInput = new Vector2(inputX, inputZ).sqrMagnitude >= minInputAmount * minInputAmount;

        if (!hasMoveInput)
            return false;

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;

        float realSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        return realSpeed >= minRealSpeed;
    }

    private float GetStepInterval()
    {
        bool sprinting = isCrisis && Input.GetKey(sprintKey);

        return sprinting
            ? sprintStepInterval
            : walkStepInterval;
    }

    private void PlayFootstep()
    {
        int clipIndex = GetRandomClipIndex();
        AudioClip clip = footstepClips[clipIndex];

        if (clip == null)
            return;

        lastClipIndex = clipIndex;

        footstepSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        footstepSource.PlayOneShot(clip, Random.Range(volumeRange.x, volumeRange.y));
    }

    private int GetRandomClipIndex()
    {
        if (footstepClips.Length == 1)
            return 0;

        int index;
        do
        {
            index = Random.Range(0, footstepClips.Length);
        }
        while (index == lastClipIndex);

        return index;
    }

    private void TrySubscribe()
    {
        if (GameManager.Instance?.EventManager == null)
            return;

        GameManager.Instance.EventManager.OnCrisisModeChanged -= SetCrisisMode;
        GameManager.Instance.EventManager.OnCrisisModeChanged += SetCrisisMode;
    }

    private void SyncInitialState()
    {
        if (GameManager.Instance?.GameStateManager == null)
            return;

        isCrisis = GameManager.Instance.GameStateManager.IsCrisisMode;
    }

    private void SetCrisisMode(bool enabled)
    {
        isCrisis = enabled;
    }
}
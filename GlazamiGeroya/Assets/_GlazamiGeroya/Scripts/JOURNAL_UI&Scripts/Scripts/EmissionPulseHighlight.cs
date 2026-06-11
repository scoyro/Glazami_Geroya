using System.Collections.Generic;
using UnityEngine;

public class EmissionPulseHighlight : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private bool findRenderersInChildren = true;

    [Header("Pulse")]
    [SerializeField] private Color highlightColor = Color.white;
    [SerializeField] private float minIntensity = 0f;
    [SerializeField] private float maxIntensity = 0.9f;
    [SerializeField] private float pulseSpeed = 1.6f;

    [Header("Behaviour")]
    [SerializeField] private bool startEnabled = false;
    [SerializeField] private bool restoreOriginalOnDisable = true;

    private readonly List<Material> materials = new List<Material>();
    private readonly List<Color> originalEmissionColors = new List<Color>();
    private readonly List<bool> originalEmissionStates = new List<bool>();

    private bool isHighlighted;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        CacheMaterials();

        if (startEnabled)
            EnableHighlight();
        else
            DisableHighlight();
    }

    private void Update()
    {
        if (!isHighlighted)
            return;

        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
        Color emission = highlightColor * intensity;

        for (int i = 0; i < materials.Count; i++)
        {
            Material material = materials[i];

            if (material == null)
                continue;

            material.EnableKeyword("_EMISSION");
            material.SetColor(EmissionColorId, emission);
        }
    }

    public void EnableHighlight()
    {
        if (materials.Count == 0)
            CacheMaterials();

        isHighlighted = true;
    }

    public void DisableHighlight()
    {
        isHighlighted = false;

        if (!restoreOriginalOnDisable)
            return;

        for (int i = 0; i < materials.Count; i++)
        {
            Material material = materials[i];

            if (material == null)
                continue;

            if (i < originalEmissionColors.Count)
                material.SetColor(EmissionColorId, originalEmissionColors[i]);

            if (i < originalEmissionStates.Count && originalEmissionStates[i])
                material.EnableKeyword("_EMISSION");
            else
                material.DisableKeyword("_EMISSION");
        }
    }

    private void CacheMaterials()
    {
        materials.Clear();
        originalEmissionColors.Clear();
        originalEmissionStates.Clear();

        if ((targetRenderers == null || targetRenderers.Length == 0) && findRenderersInChildren)
            targetRenderers = GetComponentsInChildren<Renderer>(true);

        if (targetRenderers == null)
            return;

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
                continue;

            Material[] rendererMaterials = targetRenderer.materials;

            foreach (Material material in rendererMaterials)
            {
                if (material == null)
                    continue;

                if (!material.HasProperty(EmissionColorId))
                    continue;

                materials.Add(material);
                originalEmissionColors.Add(material.GetColor(EmissionColorId));
                originalEmissionStates.Add(material.IsKeywordEnabled("_EMISSION"));
            }
        }
    }

    private void OnDisable()
    {
        DisableHighlight();
    }
}
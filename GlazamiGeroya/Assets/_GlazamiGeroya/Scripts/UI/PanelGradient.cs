using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DialogueGradientBackground : MonoBehaviour
{
    [Header("Gradient")]
    [SerializeField] private int textureWidth = 8;
    [SerializeField] private int textureHeight = 256;

    [SerializeField] private Color bottomColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private Color topColor = new Color(0f, 0f, 0f, 0f);

    private void Awake()
    {
        ApplyGradient();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            ApplyGradient();
    }

    private void ApplyGradient()
    {
        Image image = GetComponent<Image>();

        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < textureHeight; y++)
        {
            float t = y / (float)(textureHeight - 1);
            Color color = Color.Lerp(bottomColor, topColor, t);

            for (int x = 0; x < textureWidth; x++)
                texture.SetPixel(x, y, color);
        }

        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, textureWidth, textureHeight),
            new Vector2(0.5f, 0.5f),
            100f
        );

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
    }
}
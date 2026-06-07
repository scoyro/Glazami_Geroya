using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class JournalGradientBackground : MonoBehaviour
{
    [Header("Texture")]
    [SerializeField] private int textureWidth = 256;
    [SerializeField] private int textureHeight = 256;

    [Header("Color")]
    [SerializeField] private Color color = new Color(0f, 0f, 0f, 0.7f);

    [Header("Gradient")]
    [SerializeField] private float alphaPower = 1.35f;
    [SerializeField] private float fadeStart = 0f;
    [SerializeField] private float fadeEnd = 1f;

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
            for (int x = 0; x < textureWidth; x++)
            {
                float x01 = x / (float)(textureWidth - 1);
                float y01 = y / (float)(textureHeight - 1);

                float distanceFromTopLeft = (x01 + (1f - y01)) * 0.5f;
                float t = Mathf.InverseLerp(fadeStart, fadeEnd, distanceFromTopLeft);
                t = Mathf.Pow(Mathf.Clamp01(t), alphaPower);

                Color pixelColor = color;
                pixelColor.a = Mathf.Lerp(color.a, 0f, t);

                texture.SetPixel(x, y, pixelColor);
            }
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
        image.raycastTarget = false;
    }
}
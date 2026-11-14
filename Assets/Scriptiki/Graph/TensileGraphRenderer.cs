using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TensileGraphRenderer : MonoBehaviour
{
    public int width = 600;
    public int height = 300;
    public float amplitudeBoost = 4.0f;
    public RawImage graphImage;
    public StretchPlatformController platform;

    public float maxForce = 5000f;

    public MaterialSelector selector;
    public TestRigController rig;

    private MaterialProcessingProfile currentProfile;

    private Texture2D texture;
    private Color32[] clearPixels;

    private float[] values;
    private bool initialized = false;

    public Color lineColor = Color.red;
    public Color gridColor = new Color(0f, 0f, 0f, 0.25f);

    [Header("Axis Labels")]
    public TextMeshProUGUI labelX;
    public TextMeshProUGUI labelY;

    void Start()
    {
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        clearPixels = new Color32[width * height];
        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = new Color32(255, 255, 255, 255);

        values = new float[width];
        for (int x = 0; x < width; x++)
            values[x] = 0f;

        graphImage.texture = texture;

        if (labelX != null) labelX.text = "Время (с)";
        if (labelY != null) labelY.text = "Нагрузка (N)";

        initialized = true;
    }

    void Update()
    {
        if (!initialized || platform == null)
            return;

        if (selector != null)
            currentProfile = selector.GetCurrentProfile();

        float effectiveMaxForce = maxForce;

        if (currentProfile != null)
            effectiveMaxForce = currentProfile.tensileMaxForce;

        if (rig != null && rig.currentDetail != null)
        {
            var mat = rig.currentDetail.GetComponent<MaterialBehavior>();
            if (mat != null && mat.processed && currentProfile != null)
                effectiveMaxForce *= currentProfile.tensileStrengthBoost;
        }

        for (int x = 0; x < width - 1; x++)
            values[x] = values[x + 1];

        float normalized = 0f;
        if (effectiveMaxForce > 0f)
            normalized = Mathf.Clamp01(platform.currentForce / effectiveMaxForce);

        values[width - 1] = normalized;

        if (currentProfile != null)
            amplitudeBoost = currentProfile.tensileAmplitudeBoost;

        DrawGraph();
    }

    private void DrawGraph()
    {
        texture.SetPixels32(clearPixels);
        DrawGrid();
        DrawLine(values, lineColor);
        texture.Apply();
    }

    private void DrawGrid()
    {
        int stepX = 50;
        int stepY = 50;

        for (int x = 0; x < width; x += stepX)
            DrawVerticalLine(x, gridColor);

        for (int y = 0; y < height; y += stepY)
            DrawHorizontalLine(y, gridColor);
    }

    private void DrawVerticalLine(int x, Color color)
    {
        for (int y = 0; y < height; y++)
            texture.SetPixel(x, y, color);
    }

    private void DrawHorizontalLine(int y, Color color)
    {
        for (int x = 0; x < width; x++)
            texture.SetPixel(x, y, color);
    }

    private void DrawLine(float[] data, Color color)
    {
        for (int x = 1; x < width; x++)
        {
            int y1 = Mathf.Clamp((int)(data[x - 1] * height * amplitudeBoost), 0, height - 1);
            int y2 = Mathf.Clamp((int)(data[x] * height * amplitudeBoost), 0, height - 1);

            DrawLineSegment(x - 1, y1, x, y2, color);
        }
    }

    private void DrawLineSegment(int x1, int y1, int x2, int y2, Color color)
    {
        int dx = Mathf.Abs(x2 - x1);
        int dy = Mathf.Abs(y2 - y1);

        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;

        int err = dx - dy;

        while (true)
        {
            texture.SetPixel(x1, y1, color);

            if (x1 == x2 && y1 == y2)
                break;

            int e2 = 2 * err;

            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }
    }
}

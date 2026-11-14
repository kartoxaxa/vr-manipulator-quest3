using UnityEngine;

public class GraphRenderer5LinesSmooth : MonoBehaviour
{
    public int width = 600;
    public int height = 300;

    public ProcessState state;

    private Texture2D texture;
    private Color32[] clearPixels;

    private float[][] values;
    private bool initialized = false;

    private Color[] lineColors = new Color[5]
    {
        Color.red, Color.green, Color.blue, Color.yellow, Color.cyan
    };

    public Color gridColor = new Color(1f, 1f, 1f, 0.15f);

    void Start()
    {
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        clearPixels = new Color32[width * height];
        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = new Color32(0, 0, 0, 255);

        values = new float[5][];
        for (int i = 0; i < 5; i++)
        {
            values[i] = new float[width];
            for (int x = 0; x < width; x++)
                values[i][x] = 0f;
        }

        initialized = true;
    }

    void Update()
    {
        if (!initialized || state == null)
            return;

        float[] src = state.GetGraphValues();
        if (src == null || src.Length < 5)
            return;

        for (int i = 0; i < 5; i++)
        {
            // сдвиг влево
            for (int x = 0; x < width - 1; x++)
                values[i][x] = values[i][x + 1];

            // новый столбец = текущее значение из ProcessState
            values[i][width - 1] = src[i];
        }

        DrawGraph();
    }

    private void DrawGraph()
    {
        texture.SetPixels32(clearPixels);
        DrawGrid();

        for (int line = 0; line < 5; line++)
            DrawLine(values[line], lineColors[line]);

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
            int y1 = Mathf.Clamp((int)(data[x - 1] * height), 0, height - 1);
            int y2 = Mathf.Clamp((int)(data[x] * height), 0, height - 1);

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

    public Texture2D GetTexture()
    {
        return texture;
    }
}

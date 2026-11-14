using UnityEngine;

public class ProcessState : MonoBehaviour
{
    [Header("State")]
    public bool processRunning = false;
    public bool coolingDown = false;
    public ProcessMode mode = ProcessMode.Mode1;

    [Header("Graph Values (0..1)")]
    public float[] graphValues = new float[5];

    private float[] current = new float[5];
    private Vector2[] ranges = new Vector2[5];

    [Header("Physical Values")]
    public float temperature;   // Т садки
    public float currentA;      // Ток
    public float voltage;       // Напряжение
    public float pressure;      // Давление
    public float frequency;     // Частота

    [Header("Gases")]
    public float gasAr;
    public float gasN2;
    public float gasH2;

    [Header("Last point time")]
    public string lastPointDate = "";
    public string lastPointTime = "";

    [Header("Profile")]
    public MaterialProcessingProfile profile;

    void Start()
    {
        ApplyMode(mode);
    }

    void Update()
    {
        if (processRunning)
        {
            UpdateProfileDriven();
        }
        else if (coolingDown)
        {
            UpdateCooldown();
        }

        UpdatePhysical();
    }

    public void ApplyMode(ProcessMode m)
    {
        mode = m;

        if (profile != null && profile.plasmaGraphRanges != null && profile.plasmaGraphRanges.Length >= 5)
        {
            ranges = profile.plasmaGraphRanges;
        }
        else
        {
            // fallback, если профиль не задан
            ranges = new Vector2[]
            {
                new Vector2(0.55f, 0.95f), // Ar
                new Vector2(0.15f, 0.45f), // N2
                new Vector2(0.30f, 0.60f), // H2
                new Vector2(0.10f, 0.25f), // Pressure
                new Vector2(0.40f, 0.80f)  // Frequency
            };
        }
    }

    void UpdateProfileDriven()
    {
        float speed = (profile != null) ? profile.plasmaChangeSpeed : 0.5f;

        for (int i = 0; i < 5; i++)
        {
            // медленный синус → плавные колебания
            float t = Time.time * speed * 0.5f;
            float norm = (Mathf.Sin(t + i) + 1f) * 0.5f;

            float min = ranges[i].x;
            float max = ranges[i].y;

            float targetValue = Mathf.Lerp(min, max, norm);

            // сглаживание по времени
            current[i] = Mathf.Lerp(current[i], targetValue, Time.deltaTime * speed * 0.5f);

            graphValues[i] = current[i];
        }

        var now = System.DateTime.Now;
        lastPointDate = now.ToString("dd/MM/yy");
        lastPointTime = now.ToString("HH:mm:ss");
    }

    void UpdateCooldown()
    {
        bool allZero = true;

        for (int i = 0; i < 5; i++)
        {
            current[i] = Mathf.Lerp(current[i], 0f, Time.deltaTime * 0.7f);

            if (Mathf.Abs(current[i]) < 0.005f)
                current[i] = 0f;

            graphValues[i] = current[i];

            if (Mathf.Abs(current[i]) > 0.002f)
                allZero = false;
        }

        if (allZero)
            coolingDown = false;
    }

    void UpdatePhysical()
    {
        float v0 = graphValues[0];
        float v1 = graphValues[1];
        float v2 = graphValues[2];
        float v3 = graphValues[3];
        float v4 = graphValues[4];

        gasAr = v0 * 10f;
        gasN2 = v1 * 8f;
        gasH2 = v2 * 5f;

        temperature = v0 * 1000f;
        currentA = v1 * 10f;
        voltage = v2 * 50f;
        pressure = v3 * 5000f;
        frequency = v4 * 100f;
    }

    public float[] GetGraphValues()
    {
        return graphValues;
    }
}

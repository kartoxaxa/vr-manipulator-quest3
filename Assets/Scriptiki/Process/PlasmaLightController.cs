using UnityEngine;

public class PlasmaLightController : MonoBehaviour
{
    public Light plasmaLight;
    public ProcessState state;

    private float targetIntensity = 0f;
    private float currentIntensity = 0f;

    private bool isActive = false;

    void Start()
    {
        if (plasmaLight == null)
            plasmaLight = GetComponent<Light>();

        plasmaLight.enabled = false;
        plasmaLight.intensity = 0f;
        plasmaLight.color = Color.black;
    }

    void Update()
    {
        if (!isActive)
        {
            currentIntensity = Mathf.MoveTowards(currentIntensity, 0f, Time.deltaTime * 3f);
            plasmaLight.intensity = currentIntensity;

            if (currentIntensity <= 0.01f)
            {
                plasmaLight.enabled = false;
                plasmaLight.color = Color.black;
            }

            return;
        }

        if (state == null) return;

        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * 3f);

        float ar = state.gasAr / 10f;
        float n2 = state.gasN2 / 8f;
        float h2 = state.gasH2 / 5f;

        Color gasColor =
            Color.Lerp(Color.blue, Color.cyan, ar) * 0.6f +
            Color.Lerp(Color.magenta, Color.red, n2) * 0.4f +
            Color.Lerp(Color.white, Color.yellow, h2) * 0.5f;

        float pulse = Mathf.Sin(Time.time * (2f + state.frequency / 20f)) * 0.15f;

        float instability = Mathf.Lerp(0f, 0.25f, 1f - Mathf.Clamp01(state.pressure / 5000f));
        float noise = (Random.value - 0.5f) * instability;

        plasmaLight.intensity = Mathf.Max(0f, currentIntensity + pulse + noise);
        plasmaLight.color = Color.Lerp(plasmaLight.color, gasColor, Time.deltaTime * 2f);
    }

    public void Ignite()
    {
        isActive = true;
        currentIntensity = 0f;
        plasmaLight.enabled = true;
        plasmaLight.color = Color.black;
        targetIntensity = 2f;
    }

    public void Extinguish()
    {
        isActive = false;
        targetIntensity = 0f;
    }
}

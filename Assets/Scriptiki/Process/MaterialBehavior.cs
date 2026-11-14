using UnityEngine;

public class MaterialBehavior : MonoBehaviour
{
    [Header("Material Type")]
    public MaterialType type;

    [Header("Base Material Properties")]
    public float strength;
    public float plasticity;
    public float brittleness;
    public float hardness;
    public float stability;

    [Header("Processing Result (Plasma)")]
    public bool processed = false;
    public bool spoiled = false;

    // -----------------------------
    // ДАННЫЕ ТЕСТА РАСТЯЖЕНИЯ
    // -----------------------------
    [Header("Tensile Test Data")]
    public float area = 10f;
    public float speed = 5f;
    public float testTemperature = 20f;
    public string operatorName = "—";

    // -----------------------------
    // РЕЗУЛЬТАТЫ ТЕСТА РАСТЯЖЕНИЯ
    // -----------------------------
    [Header("Tensile Test Results")]
    public float peakLoad;
    public float breakTime;
    public float yieldStrength;
    public float elasticModulus;
    public float elongation;

    // ---------------------------------------------------------
    // ПЛАЗМЕННАЯ ОБРАБОТКА
    // ---------------------------------------------------------
    public void ApplyProcessing(ProcessState state)
    {
        if (processed) return;

        float T = state.temperature;
        float A = state.currentA;
        float P = state.pressure;
        float F = state.frequency;

        float Ar = state.gasAr;
        float N2 = state.gasN2;
        float H2 = state.gasH2;

        if (!IsCorrectModeForMaterial(state.mode))
        {
            SpoilMaterial();
            processed = true;
            return;
        }

        plasticity += Mathf.Clamp01(T / 1200f) * 0.2f;
        strength += Mathf.Clamp01(T / 1000f) * 0.1f;

        stability += Mathf.Clamp01(P / 5000f) * 0.15f;
        stability += Mathf.Clamp01(F / 100f) * 0.1f;

        hardness += Mathf.Clamp01(A / 10f) * 0.1f;

        stability += Ar * 0.02f;

        hardness += N2 * 0.05f;
        brittleness += N2 * 0.03f;

        plasticity += H2 * 0.04f;

        if (H2 > 3f)
        {
            brittleness += 0.3f;
            stability -= 0.2f;
        }

        strength = Mathf.Clamp01(strength);
        plasticity = Mathf.Clamp01(plasticity);
        brittleness = Mathf.Clamp01(brittleness);
        hardness = Mathf.Clamp01(hardness);
        stability = Mathf.Clamp01(stability);

        processed = true;
    }

    private void SpoilMaterial()
    {
        spoiled = true;

        strength = Random.Range(0.1f, 0.4f);
        plasticity = Random.Range(0.1f, 0.4f);
        brittleness = Random.Range(0.4f, 0.9f);
        hardness = Random.Range(0.1f, 0.5f);
        stability = Random.Range(0.1f, 0.4f);
    }

    private bool IsCorrectModeForMaterial(ProcessMode mode)
    {
        switch (mode)
        {
            case ProcessMode.Mode1: return type == MaterialType.Copper;
            case ProcessMode.Mode2: return type == MaterialType.Aluminum;
            case ProcessMode.Mode3: return type == MaterialType.Steel;
            case ProcessMode.Mode4: return type == MaterialType.Stainless;
            case ProcessMode.Mode5: return type == MaterialType.Brass;
            case ProcessMode.Mode6: return type == MaterialType.Titanium;
        }
        return false;
    }

    // ---------------------------------------------------------
    // СОХРАНЕНИЕ РЕЗУЛЬТАТОВ ТЕСТА
    // ---------------------------------------------------------
    public void SaveTensileResults(float peak, float time)
    {
        peakLoad = peak;
        breakTime = time;

        yieldStrength = (area > 0f) ? (peak / (area * 1e-6f)) / 1e6f : 0f;
        elasticModulus = yieldStrength * 0.4f;
        elongation = plasticity * 100f;
    }

    // ---------------------------------------------------------
    // ПЕРЕДАЧА ДАННЫХ В UI
    // ---------------------------------------------------------
    public void FillTestData(TestTableUI ui)
    {
        ui.rowMaterial.value.text = type.ToString();
        ui.rowSpeed.value.text = speed + " мм/мин";
        ui.rowArea.value.text = area + " мм²";
        ui.rowTemperature.value.text = testTemperature + " °C";
        ui.rowOperator.value.text = operatorName;
        ui.rowDate.value.text = System.DateTime.Now.ToString("dd.MM.yyyy");
    }

    public void FillTestResults(TestTableUI ui)
    {
        if (ui == null) return;

        if (ui.rowPeakLoadResult?.value != null)
            ui.rowPeakLoadResult.value.text = (peakLoad / 1000f).ToString("F2") + " кН";

        if (ui.rowBreakTime?.value != null)
            ui.rowBreakTime.value.text = breakTime.ToString("F2") + " c";

        if (ui.rowStrength?.value != null)
            ui.rowStrength.value.text = yieldStrength.ToString("F2") + " МПа";

        if (ui.rowModulus?.value != null)
            ui.rowModulus.value.text = elasticModulus.ToString("F2") + " МПа";

        if (ui.rowYield?.value != null)
            ui.rowYield.value.text = yieldStrength.ToString("F2") + " МПа";

        if (ui.rowElongation?.value != null)
            ui.rowElongation.value.text = elongation.ToString("F1") + " %";
    }

}

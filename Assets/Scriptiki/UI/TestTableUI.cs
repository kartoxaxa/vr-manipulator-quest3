using UnityEngine;
using TMPro;

public class TestTableUI : MonoBehaviour
{
    [System.Serializable]
    public class TableRow
    {
        public TextMeshProUGUI label;
        public TextMeshProUGUI value;
    }

    // -----------------------------
    // ВЕРХНИЕ ПОКАЗАТЕЛИ (ТОЛЬКО VALUE)
    // -----------------------------
    [Header("Top Test Values (Value Only)")]
    public TextMeshProUGUI valueCurrentLoad;   // Текущая нагрузка
    public TextMeshProUGUI valuePeakLoad;      // Пиковая нагрузка
    public TextMeshProUGUI valueStress;        // Напряжение
    public TextMeshProUGUI valueTestTime;      // Время испытания
    public TextMeshProUGUI valueDisplacement;  // Перемещение

    // -----------------------------
    // ДАННЫЕ ТЕСТА
    // -----------------------------
    [Header("Данные теста")]
    public TableRow rowMaterial;
    public TableRow rowSpeed;
    public TableRow rowArea;
    public TableRow rowTemperature;
    public TableRow rowOperator;
    public TableRow rowDate;

    // -----------------------------
    // РЕЗУЛЬТАТЫ ТЕСТА
    // -----------------------------
    [Header("Результаты теста")]
    public TableRow rowStrength;
    public TableRow rowModulus;
    public TableRow rowYield;
    public TableRow rowElongation;
    public TableRow rowPeakLoadResult;
    public TableRow rowBreakTime;

    void Start()
    {
        // Верхние значения
        valueCurrentLoad.text = "0 Н";
        valuePeakLoad.text = "0 Н";
        valueStress.text = "0 МПа";
        valueTestTime.text = "0.00 c";
        valueDisplacement.text = "0.00 мм";

        // Данные теста
        SetRow(rowMaterial, "Материал", "—");
        SetRow(rowSpeed, "Скорость испытания", "0 мм/мин");
        SetRow(rowArea, "Площадь сечения", "0.00 мм²");
        SetRow(rowTemperature, "Температура", "20 °C");
        SetRow(rowOperator, "Оператор", "—");
        SetRow(rowDate, "Дата", System.DateTime.Now.ToString("dd.MM.yyyy"));

        // Результаты теста
        SetRow(rowStrength, "Прочность", "0 МПа");
        SetRow(rowModulus, "Модуль упругости", "0 МПа");
        SetRow(rowYield, "Предел текучести", "0 МПа");
        SetRow(rowElongation, "Удлинение", "0 %");
        SetRow(rowPeakLoadResult, "Пиковая нагрузка", "0.00 кН");
        SetRow(rowBreakTime, "Время до разрыва", "0.00 c");
    }

    public void SetRow(TableRow row, string labelText, string valueText)
    {
        if (row.label != null) row.label.text = labelText;
        if (row.value != null) row.value.text = valueText;
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "MaterialProcessingProfile", menuName = "Processing/Material Profile")]
public class MaterialProcessingProfile : ScriptableObject
{
    [Header("ID")]
    public MaterialType materialType;
    public string displayName;

    [Header("Plasma Process")]
    [Range(5f, 10f)]
    public float plasmaDuration = 7f;

    [Tooltip("Диапазоны нормализованных значений для 5 линий графика (0..1)")]
    public Vector2[] plasmaGraphRanges = new Vector2[5]
    {
        new Vector2(0.3f, 0.7f),
        new Vector2(0.2f, 0.6f),
        new Vector2(0.1f, 0.5f),
        new Vector2(0.2f, 0.5f),
        new Vector2(0.3f, 0.8f)
    };

    [Tooltip("Скорость изменения значений графика плазмы (медленно/умеренно)")]
    public float plasmaChangeSpeed = 0.5f;

    [Header("Pointer Gauges")]
    [Range(0f, 1f)] public float vacuumValue = 0.7f;
    [Range(0f, 1f)] public float waterValue = 0.5f;

    [Header("Tensile Test")]
    [Tooltip("Максимальная сила для нормализации графика растяжения (Н)")]
    public float tensileMaxForce = 5000f;

    [Tooltip("Масштаб по вертикали для графика растяжения")]
    public float tensileAmplitudeBoost = 4.0f;

    [Tooltip("Множитель прочности после обработки (обработанная деталь сильнее)")]
    public float tensileStrengthBoost = 1.2f;
}

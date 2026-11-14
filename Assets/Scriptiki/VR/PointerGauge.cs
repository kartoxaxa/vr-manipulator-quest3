using UnityEngine;

public class PointerGauge : MonoBehaviour
{
    [Header("Gauge Settings")]
    public float minAngle = 0f;       // угол при значении 0
    public float maxAngle = -260f;    // угол при значении 1
    public float speed = 40f;         // скорость движения стрелки

    [Header("Value Settings")]
    public float currentValue = 0f;   // 0..1
    public float targetValue = 0f;    // 0..1

    private Quaternion baseRotation;

    void Start()
    {
        baseRotation = transform.rotation;
    }

    void Update()
    {
        // плавное движение значения
        currentValue = Mathf.MoveTowards(currentValue, targetValue, Time.deltaTime * 0.5f);

        // переводим значение в угол
        float angle = Mathf.Lerp(minAngle, maxAngle, currentValue);

        // вращение по глобальной X
        Quaternion globalX = Quaternion.AngleAxis(-angle, Vector3.right);
        transform.rotation = globalX * baseRotation;
    }

    // вызывается PanelController или PlasmaController
    public void SetTarget(float normalizedValue)
    {
        targetValue = Mathf.Clamp01(normalizedValue);
    }
}

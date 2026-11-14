using UnityEngine;

public class PointerGlobalX : MonoBehaviour
{
    public float minAngle = 0f;
    public float maxAngle = -260f;
    public float speed = 40f;

    private float currentAngle;
    private float targetAngle;
    private bool forward = true;

    private Quaternion baseRotation;

    void Start()
    {
        // Запоминаем исходный глобальный поворот стрелки
        baseRotation = transform.rotation;

        currentAngle = minAngle;
        targetAngle = maxAngle;
    }

    void Update()
    {
        // Плавное движение угла
        currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, speed * Time.deltaTime);

        // Вращение ПРОТИВ часовой стрелки по глобальной оси X
        Quaternion globalX = Quaternion.AngleAxis(-currentAngle, Vector3.right);

        // Итоговый поворот
        transform.rotation = globalX * baseRotation;

        // Меняем направление в крайних точках
        if (Mathf.Abs(currentAngle - targetAngle) < 0.1f)
        {
            targetAngle = forward ? minAngle : maxAngle;
            forward = !forward;
        }
    }
}

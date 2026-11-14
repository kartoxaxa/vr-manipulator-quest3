using UnityEngine;

[ExecuteAlways]
public class TargetFollower : MonoBehaviour
{
    [Header("Цель для преследования")]
    [Tooltip("Объект, к которому будет тянуться этот объект")]
    public Transform targetToFollow;

    [Header("Настройки движения")]
    [Tooltip("Скорость перемещения в единицах в секунду")]
    [Range(0.01f, 5f)]
    public float moveSpeed = 0.5f;

    [Tooltip("Скорость поворота в градусах в секунду")]
    [Range(1f, 360f)]
    public float rotateSpeed = 180f;

    [Tooltip("Минимальное расстояние для остановки")]
    [Range(0.001f, 0.1f)]
    public float stopDistance = 0.01f;

    [Tooltip("Поворачиваться лицом к цели")]
    public bool faceTarget = false;

    [Tooltip("Движение в Edit Mode")]
    public bool moveInEditMode = true;

    [Tooltip("Движение в Play Mode")]
    public bool moveInUpdate = true;

    private void Update()
    {
        if (targetToFollow == null) return;

        if (!Application.isPlaying)
        {
            if (!moveInEditMode) return;
        }
        else
        {
            if (!moveInUpdate) return;
        }

        Vector3 targetPos = targetToFollow.position;
        Vector3 currentPos = transform.position;

        Vector3 diff = targetPos - currentPos;
        float distance = diff.magnitude;

        if (distance > stopDistance)
        {
            float deltaTime = Application.isPlaying ? Time.deltaTime : 0.02f;
            float realStep = moveSpeed * (Application.isPlaying ? Time.deltaTime : 0.02f);
            transform.position = Vector3.MoveTowards(currentPos, targetPos, realStep);
        }

        if (faceTarget && distance > 0.001f)
        {
            float deltaTime = Application.isPlaying ? Time.deltaTime : 0.02f;
            Quaternion lookRot = Quaternion.LookRotation(diff);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, rotateSpeed * deltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        if (targetToFollow == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, targetToFollow.position);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(targetToFollow.position, 0.015f);
    }
}
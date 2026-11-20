using UnityEngine;

public class JointAxis : MonoBehaviour
{
    public enum Axis
    {
        X,
        Y,
        Z
    }

    [Header("Настройки сустава")]
    [SerializeField] private Axis rotationAxis = Axis.Y;

    [Tooltip("Скорость плавного поворота, град/с")]
    [SerializeField, Min(0f)] private float rotationSpeed = 40f;

    [Header("Ограничения")]
    [SerializeField] private bool useLimits = true;
    [SerializeField] private float minAngle = -90f;
    [SerializeField] private float maxAngle = 90f;

    [Header("Отладка")]
    [SerializeField] private float currentAngle;
    [SerializeField] private float targetAngle;

    private Quaternion _restLocalRotation;

    public float CurrentAngle => currentAngle;
    public float TargetAngle => targetAngle;
    public Axis AxisType => rotationAxis;
    public Vector3 LocalAxis => GetAxisVector(rotationAxis);
    public Vector3 WorldAxis => transform.TransformDirection(LocalAxis);

    public bool ManualInputEnabled { get; set; } = true;

    private void Awake()
    {
        CacheRestPose();
        currentAngle = 0f;
        targetAngle = 0f;
        ApplyAngle(currentAngle);
    }

    private void OnEnable()
    {
        if (_restLocalRotation == default)
            CacheRestPose();
    }

    private void Update()
    {
        if (!ManualInputEnabled) return;
        UpdateSmoothRotation();
    }

    public void CacheRestPose()
    {
        _restLocalRotation = transform.localRotation;
    }

    public void SetTargetAngle(float angle)
    {
        float clamped = angle;
        if (useLimits)
            clamped = Mathf.Clamp(angle, minAngle, maxAngle);

        targetAngle = clamped;
    }

    public void SnapToAngle(float angle)
    {
        float clamped = angle;
        if (useLimits)
            clamped = Mathf.Clamp(angle, minAngle, maxAngle);

        targetAngle = clamped;
        currentAngle = clamped;
        ApplyAngle(currentAngle);
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = Mathf.Max(0f, speed);
    }

    private void UpdateSmoothRotation()
    {
        if (Mathf.Approximately(currentAngle, targetAngle))
        {
            currentAngle = targetAngle;
            return;
        }

        if (rotationSpeed <= 0f)
        {
            currentAngle = targetAngle;
            ApplyAngle(currentAngle);
            return;
        }

        float step = rotationSpeed * Time.deltaTime;
        currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, step);
        ApplyAngle(currentAngle);
    }

    private void ApplyAngle(float angle)
    {
        transform.localRotation = _restLocalRotation * Quaternion.AngleAxis(angle, GetAxisVector(rotationAxis));
    }

    private Vector3 GetAxisVector(Axis axis)
    {
        switch (axis)
        {
            case Axis.X: return Vector3.right;
            case Axis.Y: return Vector3.up;
            case Axis.Z: return Vector3.forward;
            default: return Vector3.up;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.TransformDirection(GetAxisVector(rotationAxis)) * 0.1f);
    }
#endif
}
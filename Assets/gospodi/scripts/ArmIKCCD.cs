using UnityEngine;

[ExecuteAlways]
public class ArmIKCCD : MonoBehaviour
{
    [Header("Суставы по цепочке")]
    [Tooltip("Массив JointAxis в порядке от основания к концу. POINT_0 -> POINT_6")]
    public JointAxis[] joints;

    [Header("Конец и цель")]
    [Tooltip("Последняя точка манипулятора (как правило POINT_6). Если не заполнено - возьмётся последний сустав из массива")]
    public Transform tip;

    [Tooltip("Пустой объект TargetPoint, за которым мы будем следить")]
    public Transform target;

    [Header("Настройки CCD")]
    [Range(1, 30)]
    public int iterations = 15;

    [Range(0.001f, 0.05f)]
    public float tolerance = 0.01f;

    [Tooltip("Максимальный угол, на который можно повернуть один сустав за одну итерацию CCD")]
    [Range(1f, 30f)]
    public float maxDeltaPerIteration = 10f;

    [Tooltip("Общая скорость доворота суставов в град/с")]
    [Range(10f, 60f)]
    public float ikRotationSpeed = 30f;

    [Header("Режим")]
    public bool solveInEditMode = true;
    public bool solveInPlayMode = true;

    [Header("Отладка")]
    [SerializeField] private float tipError;

    private float[] _anglesBefore = new float[0];
    private float[] _anglesSolved = new float[0];

    private void OnEnable()
    {
        CacheArrays();
        ApplySpeed();
    }

    private void Start()
    {
        CacheArrays();
        ApplySpeed();
    }

    private void OnValidate()
    {
        CacheArrays();
        ApplySpeed();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            if (!solveInEditMode) return;
        }
        else
        {
            if (!solveInPlayMode) return;
        }

        if (joints == null || joints.Length == 0) return;
        if (target == null) return;
        if (tip == null && joints[joints.Length - 1] != null)
            tip = joints[joints.Length - 1].transform;

        if (tip == null) return;

        ApplySpeed();
        Solve();
        tipError = Vector3.Distance(tip.position, target.position);
    }

    private void CacheArrays()
    {
        if (joints == null) return;
        int count = joints.Length;
        _anglesBefore = new float[count];
        _anglesSolved = new float[count];
    }

    private void ApplySpeed()
    {
        if (joints == null) return;
        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] != null)
                joints[i].SetRotationSpeed(ikRotationSpeed);
        }
    }

    private void Solve()
    {
        int count = joints.Length;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            if (joints[i] != null)
                _anglesBefore[i] = joints[i].CurrentAngle;
        }

        Vector3 targetPos = target.position;

        for (int iter = 0; iter < iterations; iter++)
        {
            float dist = Vector3.Distance(tip.position, targetPos);
            if (dist <= tolerance) break;

            for (int i = count - 1; i >= 0; i--)
            {
                JointAxis joint = joints[i];
                if (joint == null) continue;

                Transform jt = joint.transform;
                Vector3 pivot = jt.position;
                Vector3 axisW = joint.WorldAxis.normalized;
                if (axisW.sqrMagnitude < 1e-8f) continue;

                Vector3 toTip = tip.position - pivot;
                Vector3 toTargetVec = targetPos - pivot;

                float delta = Vector3.SignedAngle(toTip, toTargetVec, axisW);
                if (Mathf.Abs(delta) < 1e-4f) continue;

                delta = Mathf.Clamp(delta, -maxDeltaPerIteration, maxDeltaPerIteration);

                joint.SnapToAngle(joint.CurrentAngle + delta);
            }
        }

        for (int i = 0; i < count; i++)
        {
            if (joints[i] != null)
                _anglesSolved[i] = joints[i].CurrentAngle;
        }

        for (int i = 0; i < count; i++)
        {
            if (joints[i] != null)
                joints[i].SnapToAngle(_anglesBefore[i]);
        }

        for (int i = 0; i < count; i++)
        {
            if (joints[i] != null)
                joints[i].SetTargetAngle(_anglesSolved[i]);
        }
    }

    private void OnDrawGizmos()
    {
        if (tip == null || target == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(tip.position, target.position);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(tip.position, 0.01f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position, 0.015f);
    }
        [ContextMenu("Вернуть манипулятор в исходное положение")]
    public void ReturnToRest()
    {
        if (joints == null) return;

        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] == null) continue;
            joints[i].SnapToAngle(0f);
            joints[i].SetTargetAngle(0f);
        }
    }
}
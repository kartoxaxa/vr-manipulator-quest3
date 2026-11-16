using System.Collections;
using UnityEngine;

public class ArmTransfer : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Объект с ArmIKCCD")]
    public ArmIKCCD armIK;
    [Tooltip("TargetPoint, к которому тянется манипулятор")]
    public Transform targetPoint;
    [Tooltip("Захват манипулятора")]
    public ArmGripper gripper;
    [Tooltip("Очередь кубиков у конвейера")]
    public ConveyorQueue conveyorQueue;

    [Header("Точки маршрута")]
    [Tooltip("Исходная позиция манипулятора")]
    public Transform homePoint;
    [Tooltip("Точка над местом захвата (дочерний объект GrabPoint)")]
    public Transform approachPoint;
    [Tooltip("Точка захвата кубика")]
    public Transform grabPoint;
    [Tooltip("Точка над местом сброса (дочерний объект DropPoint)")]
    public Transform upPoint;
    [Tooltip("Точка сброса кубика")]
    public Transform dropPoint;

    [Header("Настройки движения")]
    [Tooltip("Скорость перемещения TargetPoint")]
    public float moveSpeed = 0.3f;
    [Tooltip("Пауза перед захватом")]
    public float grabDelay = 0.3f;
    [Tooltip("Пауза перед отпусканием")]
    public float dropDelay = 0.3f;
    [Tooltip("Пауза между циклами")]
    public float cycleDelay = 0.2f;

    private bool isRunning = false;
    private Coroutine routine;

    public bool IsRunning => isRunning;

    public bool StartTransfer()
    {
        if (isRunning) return false;
        if (gripper == null || gripper.HasCube()) return false;
        if (conveyorQueue == null || !conveyorQueue.HasCube()) return false;

        routine = StartCoroutine(TransferRoutine());
        return true;
    }

    public void ResetTransfer()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (gripper != null && gripper.HasCube())
            gripper.Release();

        isRunning = false;

        // Возвращаем цель манипулятора в исходную точку, чтобы цикл начался заново
        if (targetPoint != null && homePoint != null)
        {
            targetPoint.position = homePoint.position;
            targetPoint.rotation = homePoint.rotation;
        }
    }

    private IEnumerator TransferRoutine()
    {
        isRunning = true;

        while (conveyorQueue != null && conveyorQueue.HasCube() && !gripper.HasCube())
        {
            // Берём первый кубик из очереди
            GameObject cube = conveyorQueue.TakeFirstCube();
            if (cube == null) break;

            // 1) Домой (если задано)
            if (homePoint != null)
                yield return MoveTo(homePoint);

            // 2) Подъезд к точке над захватом
            if (approachPoint != null)
                yield return MoveTo(approachPoint);

            // 3) Опускание к точке захвата
            if (grabPoint != null)
                yield return MoveTo(grabPoint);
            else if (cube != null)
                yield return MoveToPosition(cube.transform.position);

            yield return new WaitForSeconds(grabDelay);

            // 4) Захват кубика
            gripper.Grab(cube);

            // 5) Подъём над местом захвата
            if (approachPoint != null)
                yield return MoveTo(approachPoint);

            // 6) Перемещение к точке над сбросом
            if (upPoint != null)
                yield return MoveTo(upPoint);

            // 7) Опускание к точке сброса
            if (dropPoint != null)
                yield return MoveTo(dropPoint);

            yield return new WaitForSeconds(dropDelay);

            // 8) Отпускание кубика
            gripper.Release();

            // 9) Подъём после сброса
            if (upPoint != null)
                yield return MoveTo(upPoint);

            yield return new WaitForSeconds(cycleDelay);

            // 10) Возврат домой
            if (homePoint != null)
                yield return MoveTo(homePoint);

            yield return new WaitForSeconds(0.1f);
        }

        routine = null;
        isRunning = false;
    }

    private IEnumerator MoveTo(Transform target)
    {
        if (target == null || targetPoint == null) yield break;
        yield return MoveToPosition(target.position);
    }

    private IEnumerator MoveToPosition(Vector3 targetPos)
    {
        if (targetPoint == null) yield break;

        while (true)
        {
            Vector3 current = targetPoint.position;
            float step = Mathf.Max(moveSpeed, 0.0001f) * Time.deltaTime;
            Vector3 next = Vector3.MoveTowards(current, targetPos, step);
            targetPoint.position = next;

            if (Vector3.Distance(next, targetPos) <= 0.001f)
            {
                targetPoint.position = targetPos;
                yield break;
            }

            yield return null;
        }
    }
}

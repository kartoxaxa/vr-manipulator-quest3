using System.Collections.Generic;
using UnityEngine;

public class ConveyorQueue : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("ConveyorMover, который двигает кубики по конвейеру")]
    public ConveyorMover conveyorMover;

    [Tooltip("Точка, где должен стоять первый кубик в очереди")]
    public Transform stopPoint;

    [Header("Настройки очереди")]
    [Tooltip("Расстояние между кубиками")]
    public float spacing = 0.13f;

    [Tooltip("Дистанция, на которой кубик считается дошедшим до точки")]
    public float stopDistance = 0.05f;

    [Tooltip("Выравнивать кубики в очередь")]
    public bool keepAligned = true;

    [Header("Очередь")]
    public List<GameObject> queue = new List<GameObject>();

    private void FixedUpdate()
    {
        if (conveyorMover == null || stopPoint == null) return;

        // Удаляем null из очереди
        for (int i = queue.Count - 1; i >= 0; i--)
        {
            if (queue[i] == null)
                queue.RemoveAt(i);
        }

        // Проверяем кубики, которые ещё на конвейере
        for (int i = conveyorMover.cubesOnBelt.Count - 1; i >= 0; i--)
        {
            if (i >= conveyorMover.cubesOnBelt.Count) break;

            Rigidbody rb = conveyorMover.cubesOnBelt[i];
            if (rb == null)
            {
                conveyorMover.cubesOnBelt.RemoveAt(i);
                continue;
            }

            GameObject cube = rb.gameObject;
            if (queue.Contains(cube)) continue;

            // Если очередь пуста - первый кубик останавливается в stopPoint
            if (queue.Count == 0)
            {
                float distToStop = Vector3.Distance(cube.transform.position, stopPoint.position);
                if (distToStop <= stopDistance)
                {
                    conveyorMover.cubesOnBelt.RemoveAt(i);
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;

                    cube.transform.position = stopPoint.position;
                    cube.transform.rotation = stopPoint.rotation;

                    queue.Add(cube);
                    RebuildQueue();
                    continue;
                }
            }
            // Если в очереди уже есть кубики - останавливаемся позади последнего
            else
            {
                GameObject lastCube = queue[queue.Count - 1];
                if (lastCube == null) continue;

                float distToLast = Vector3.Distance(cube.transform.position, lastCube.transform.position);
                if (distToLast <= spacing + 0.01f)
                {
                    conveyorMover.cubesOnBelt.RemoveAt(i);
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;

                    queue.Add(cube);
                    RebuildQueue();
                    continue;
                }
            }
        }

        if (keepAligned)
            RebuildQueue();
    }

    private void RebuildQueue()
    {
        if (stopPoint == null) return;

        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i] == null) continue;

            GameObject cube = queue[i];
            Vector3 targetPos = stopPoint.position - stopPoint.forward * spacing * i;
            Quaternion targetRot = stopPoint.rotation;

            cube.transform.position = targetPos;
            cube.transform.rotation = targetRot;

            Rigidbody rb = cube.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
    }

    public GameObject TakeFirstCube()
    {
        if (queue.Count == 0) return null;

        GameObject first = queue[0];
        queue.RemoveAt(0);

        RebuildQueue();
        return first;
    }

    public bool HasCube()
    {
        for (int i = queue.Count - 1; i >= 0; i--)
            if (queue[i] == null) queue.RemoveAt(i);

        return queue.Count > 0;
    }

    public void Clear()
    {
        queue.Clear();
    }
}

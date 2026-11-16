using System.Collections.Generic;
using UnityEngine;

public class ConveyorMover : MonoBehaviour
{
    [Header("Настройки конвейера")]
    [Tooltip("Скорость движения кубиков по ленте")]
    public float speed = 0.25f;

    [Tooltip("Направление движения кубиков относительно этого объекта")]
    public Vector3 moveDirection = Vector3.forward;

    [Header("Кубики на ленте")]
    public List<Rigidbody> cubesOnBelt = new List<Rigidbody>();

    private void FixedUpdate()
    {
        if (cubesOnBelt.Count == 0) return;

        Vector3 dir = transform.TransformDirection(moveDirection).normalized;

        for (int i = cubesOnBelt.Count - 1; i >= 0; i--)
        {
            if (cubesOnBelt[i] == null)
            {
                cubesOnBelt.RemoveAt(i);
                continue;
            }

            Rigidbody rb = cubesOnBelt[i];
            if (rb.isKinematic)
            {
                rb.MovePosition(rb.position + dir * speed * Time.fixedDeltaTime);
            }
        }
    }

    public void Clear()
    {
        cubesOnBelt.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        CubeMarker marker = GetMarker(other);
        if (marker == null) return;

        // Кубики в очереди и уже взятые манипулятором лента не трогает
        if (marker.inQueue || marker.isTaken) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        if (!cubesOnBelt.Contains(rb))
            cubesOnBelt.Add(rb);
    }

    private void OnTriggerExit(Collider other)
    {
        if (GetMarker(other) == null) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        cubesOnBelt.Remove(rb);
    }

    // Кубик определяем по компоненту-маркеру, а не по тегу
    private static CubeMarker GetMarker(Collider other)
    {
        if (other == null) return null;

        CubeMarker marker = other.GetComponent<CubeMarker>();
        if (marker != null) return marker;

        Rigidbody rb = other.attachedRigidbody;
        return rb != null ? rb.GetComponent<CubeMarker>() : null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 dir = transform.TransformDirection(moveDirection).normalized;
        Gizmos.DrawLine(transform.position, transform.position + dir * 0.5f);
        Gizmos.DrawSphere(transform.position + dir * 0.5f, 0.01f);
    }
}

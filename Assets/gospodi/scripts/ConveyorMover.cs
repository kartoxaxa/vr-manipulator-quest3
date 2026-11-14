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
        if (!other.CompareTag("Cube")) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        if (!cubesOnBelt.Contains(rb))
            cubesOnBelt.Add(rb);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Cube")) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        cubesOnBelt.Remove(rb);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 dir = transform.TransformDirection(moveDirection).normalized;
        Gizmos.DrawLine(transform.position, transform.position + dir * 0.5f);
        Gizmos.DrawSphere(transform.position + dir * 0.5f, 0.01f);
    }
}

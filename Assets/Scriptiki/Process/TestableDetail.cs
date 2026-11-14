using UnityEngine;

public class TestableDetail : MonoBehaviour
{
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    private Rigidbody rb;

    public bool hasExitedInsertZone = false;
    public bool canBeInserted = true;

    private float baseHalfHeight;
    public bool locked = false;
    public bool stretching = false;

    public Transform topClampPoint;
    public Transform bottomClampPoint;

    void Start()
    {
        if (grab == null)
            grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        rb = GetComponent<Rigidbody>();

        Renderer r = GetComponentInChildren<Renderer>();
        baseHalfHeight = r.bounds.size.y * 0.5f;
    }

    // 🔒 Фиксация детали в установке
    public void LockAndPlace(Transform insertPoint)
    {
        if (locked) return;

        locked = true;
        stretching = false;
        canBeInserted = false;   // ❗ ВАЖНО: пока деталь в установке — нельзя вставлять снова
        hasExitedInsertZone = false;

        if (grab != null)
            grab.enabled = false;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        transform.position = insertPoint.position;
        transform.rotation = insertPoint.rotation;

        Debug.Log("Detail: зафиксирована в установке.");
    }

    // 🔥 Начало растяжения
    public void BeginStretching()
    {
        if (!locked) return;

        stretching = true;
        Debug.Log("Detail: растяжение началось.");
    }

    void Update()
    {
        if (!locked || !stretching) return;

        float topY = topClampPoint.position.y;
        float bottomY = bottomClampPoint.position.y;

        float height = topY - bottomY;
        if (height <= 0.001f) return;

        float scaleY = height / (baseHalfHeight * 2f);
        transform.localScale = new Vector3(1f, scaleY, 1f);

        float halfHeight = baseHalfHeight * scaleY;

        transform.position = new Vector3(
            transform.position.x,
            topY - halfHeight,
            transform.position.z
        );
    }

    // 🔓 Полная разблокировка после растяжения
    public void Unlock()
    {
        locked = false;
        stretching = false;

        // ❗ После разблокировки деталь всё ещё НЕ готова к повторной вставке
        // пока игрок не вынес её из зоны
        canBeInserted = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (grab != null)
            grab.enabled = true;

        Debug.Log("Detail: разблокирована, но нужно вынести из зоны.");
    }

    // 🔓 Вызывается зоной вставки, когда деталь покинула триггер
    public void MarkExitedInsertZone()
    {
        hasExitedInsertZone = true;
        canBeInserted = true;  // теперь можно снова вставлять
        Debug.Log("Detail: покинула зону, снова можно вставлять.");
    }
}

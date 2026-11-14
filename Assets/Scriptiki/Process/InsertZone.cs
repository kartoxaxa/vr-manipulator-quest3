using UnityEngine;

public class InsertZone : MonoBehaviour
{
    public Transform insertPoint;
    public float minOffset = 0.25f;

    private void OnTriggerEnter(Collider other)
    {
        var detail = other.GetComponent<TestableDetail>();
        if (detail == null) return;

        if (!detail.canBeInserted)
            return;

        detail.canBeInserted = false;
        detail.LockAndPlace(insertPoint);

        FindObjectOfType<TestRigController>().InsertDetail(detail);
    }

    private void OnTriggerExit(Collider other)
    {
        var detail = other.GetComponent<TestableDetail>();
        if (detail == null) return;

        float dy = Mathf.Abs(detail.transform.position.y - insertPoint.position.y);

        if (dy >= minOffset)
            detail.canBeInserted = true;
    }
}

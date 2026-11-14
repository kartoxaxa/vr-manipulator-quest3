using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DoorLatch : MonoBehaviour
{
    public ConfigurableJoint joint;
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    [Header("Settings")]
    public float latchAngle = 5f;      // угол, при котором дверь защёлкивается
    public float closeSpeed = 5f;      // скорость дотягивания

    private bool isLatched = false;
    public bool IsLatched => isLatched;   // ← ДОБАВЛЕНО

    private Quaternion closedRotation;

    void Start()
    {
        closedRotation = transform.localRotation;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void Update()
    {
        if (isLatched)
            return;

        float angle = Quaternion.Angle(transform.localRotation, closedRotation);

        // если дверь почти закрыта — защёлкиваем
        if (angle < latchAngle)
        {
            LatchDoor();
        }
    }

    private void LatchDoor()
    {
        isLatched = true;

        // дотягиваем дверь плавно
        StartCoroutine(CloseSmooth());

        // блокируем Joint
        LockJoint();
    }

    private System.Collections.IEnumerator CloseSmooth()
    {
        while (Quaternion.Angle(transform.localRotation, closedRotation) > 0.1f)
        {
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                closedRotation,
                Time.deltaTime * closeSpeed
            );
            yield return null;
        }

        transform.localRotation = closedRotation;
    }

    private void LockJoint()
    {
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;
    }

    private void UnlockJoint()
    {
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // если процесс идёт — запрещаем открывать
        if (FindObjectOfType<ProcessState>().processRunning)
        {
            grab.enabled = false; // нельзя взять
            return;
        }

        UnlockJoint();
        isLatched = false;
    }
    public void EnableDoor()
    {
        grab.enabled = true;
    }


    private void OnRelease(SelectExitEventArgs args)
    {
        // после отпускания дверь снова может защёлкнуться
        isLatched = false;
    }
}

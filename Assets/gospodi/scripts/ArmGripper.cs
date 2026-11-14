using UnityEngine;

public class ArmGripper : MonoBehaviour
{
    [Header("Точка захвата")]
    [Tooltip("На этом объекте должен быть kinematic Rigidbody")]
    public Transform targetEnd;

    [Header("Настройки")]
    public bool disableGravityWhileHeld = true;

    private GameObject grabbedCube;
    private Rigidbody grabbedRb;
    private FixedJoint joint;
    private bool prevUseGravity;

    public bool HasCube()
    {
        return grabbedCube != null;
    }

    public void Grab(GameObject cube)
    {
        if (cube == null || grabbedCube != null || targetEnd == null) return;

        var targetRb = targetEnd.GetComponent<Rigidbody>();
        if (targetRb == null)
        {
            Debug.LogError("ArmGripper: на targetEnd нет Rigidbody. Добавь kinematic Rigidbody.");
            return;
        }

        grabbedCube = cube;
        grabbedRb = cube.GetComponent<Rigidbody>();

        // 1) Сначала строго ставим кубик в точку захвата
        cube.transform.position = targetEnd.position;
        cube.transform.rotation = targetEnd.rotation;

        // 2) Готовим физику кубика
        if (grabbedRb != null)
        {
            prevUseGravity = grabbedRb.useGravity;
            grabbedRb.isKinematic = false; // FixedJoint требует non-kinematic Rigidbody
            grabbedRb.linearVelocity = Vector3.zero;
            grabbedRb.angularVelocity = Vector3.zero;
            grabbedRb.useGravity = !disableGravityWhileHeld ? prevUseGravity : false;
        }

        // 3) Создаём joint уже после того, как кубик стоит в нужной точке
        joint = cube.GetComponent<FixedJoint>();
        if (joint == null)
            joint = cube.AddComponent<FixedJoint>();

        joint.autoConfigureConnectedAnchor = true;
        joint.connectedBody = targetRb;
        joint.breakForce = Mathf.Infinity;
        joint.breakTorque = Mathf.Infinity;
        joint.enableCollision = false;
    }

    public void Release()
    {
        if (grabbedCube == null) return;

        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        if (grabbedRb != null)
        {
            grabbedRb.useGravity = true;
            grabbedRb.isKinematic = false;
            grabbedRb.linearVelocity = Vector3.zero;
            grabbedRb.angularVelocity = Vector3.zero;
        }

        grabbedCube = null;
        grabbedRb = null;
    }
}
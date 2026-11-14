using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class AutoDropOnDistance : MonoBehaviour
{
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    public float maxDistance = 0.7f;

    Transform hand;
    UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor;

    void Awake()
    {
        if (grab == null)
            grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void Update()
    {
        if (hand != null && grab.isSelected)
        {
            float dist = Vector3.Distance(hand.position, transform.position);

            if (dist > maxDistance)
            {
                // ����� ������ �����
                grab.interactionManager.SelectExit(interactor, grab);
            }
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        interactor = args.interactorObject;
        hand = args.interactorObject.transform;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        interactor = null;
        hand = null;
    }
}

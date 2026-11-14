using UnityEngine;
using UnityEngine.Events;

public class VRButtonDepthDetector : MonoBehaviour
{
    public float pressThreshold = 0.0025f;
    public UnityEvent onPressed;
    public bool interactable = true;

    [Header("Audio")]
    public AudioClip pressSound;
    public AudioClip releaseSound;
    private AudioSource audioSource;

    private float startZ;
    private bool pressed = false;

    void Start()
    {
        startZ = transform.localPosition.z;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (!interactable)
            return;

        float currentZ = transform.localPosition.z;
        float depth = currentZ - startZ;

        if (!pressed && depth >= pressThreshold)
        {
            pressed = true;
            PlayPressSound();
            onPressed.Invoke();
        }

        if (pressed && depth < pressThreshold * 0.5f)
        {
            pressed = false;
        }
    }

    public void LockButton()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        transform.localPosition = new Vector3(
            transform.localPosition.x,
            transform.localPosition.y,
            startZ + pressThreshold
        );

        PlayPressSound();
    }

    public void UnlockButton()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;

        transform.localPosition = new Vector3(
            transform.localPosition.x,
            transform.localPosition.y,
            startZ
        );

        PlayReleaseSound();
    }

    private void PlayPressSound()
    {
        if (pressSound != null)
            audioSource.PlayOneShot(pressSound);
    }

    private void PlayReleaseSound()
    {
        if (releaseSound != null)
            audioSource.PlayOneShot(releaseSound);
    }
}

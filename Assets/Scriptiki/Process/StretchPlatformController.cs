using UnityEngine;

public class StretchPlatformController : MonoBehaviour
{
    public static StretchPlatformController instance;

    public float speed = 0.002f; // медленное движение
    public float forceIncrease = 5f;

    public bool stretching = false;
    public float currentForce = 0f;

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (!stretching) return;

        transform.localPosition += Vector3.down * speed * Time.deltaTime;

        currentForce += forceIncrease * Time.deltaTime;
    }

    public void StartStretching()
    {
        stretching = true;
        currentForce = 0f;
        Debug.Log("Platform: Начало растяжения.");
    }
    public void ForceStop()
    {
        stretching = false;
        currentForce = 0f;
    }

    public void StopStretching()
    {
        stretching = false;
        currentForce = 0f;
        Debug.Log("Platform: Остановка растяжения.");
    }
}

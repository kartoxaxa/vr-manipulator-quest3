using UnityEngine;

public class TestRigController : MonoBehaviour
{
    public StretchPlatformController platform;
    public TestableDetail currentDetail;
    public TestTableUI ui;

    public float autoStartDelay = 1.5f;
    private float timer = 0f;

    private bool testRunning = false;
    private float testTimer = 0f;
    public float testDuration = 5f;

    public float TestTime => testTimer;
    public bool IsRunning => testRunning;

    void Update()
    {
        if (currentDetail == null)
        {
            return;
        }

        if (!testRunning)
        {
            timer += Time.deltaTime;

            if (timer >= autoStartDelay)
                StartTest();
        }
        else
        {
            testTimer += Time.deltaTime;

            if (testTimer >= testDuration)
                StopTest();
        }
    }

    public void InsertDetail(TestableDetail detail)
    {
        currentDetail = detail;
        timer = 0f;
        testTimer = 0f;

        var mat = detail.GetComponent<MaterialBehavior>();
        if (mat != null)
            mat.FillTestData(ui);
    }

    public void StartTest()
    {
        testRunning = true;
        testTimer = 0f;

        platform.StartStretching();
        currentDetail.BeginStretching();
    }

    public void StopTest()
    {
        testRunning = false;

        platform.ForceStop();

        if (currentDetail != null)
        {
            var mat = currentDetail.GetComponent<MaterialBehavior>();

            // 🔥 Проверка на null
            if (mat != null && ui != null)
            {
                // 🔥 Проверка: тест реально шёл хотя бы 0.1 сек
                if (testTimer > 0.1f)
                {
                    mat.SaveTensileResults(TensileMonitorUpdater.lastPeakForce, testTimer);

                    // 🔥 Безопасный вызов FillTestResults
                    try
                    {
                        mat.FillTestResults(ui);
                    }
                    catch
                    {
                        Debug.LogWarning("⚠ FillTestResults: UI не полностью назначен!");
                    }
                }
            }

            currentDetail.Unlock();
        }

        currentDetail = null;
    }

}

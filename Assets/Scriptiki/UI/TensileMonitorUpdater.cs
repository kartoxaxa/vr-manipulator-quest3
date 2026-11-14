using UnityEngine;

public class TensileMonitorUpdater : MonoBehaviour
{
    public TestRigController rig;
    public StretchPlatformController platform;
    public TestTableUI ui;

    public static float lastPeakForce = 0f;

    private float peakForce = 0f;
    private bool wasRunning = false;

    void Update()
    {
        if (rig == null || platform == null || ui == null)
            return;

        if (rig.IsRunning && !wasRunning)
        {
            peakForce = 0f;
            lastPeakForce = 0f;

            ui.valueCurrentLoad.text = "0 Н";
            ui.valuePeakLoad.text = "0 Н";
            ui.valueStress.text = "0 МПа";
            ui.valueTestTime.text = "0.00 c";
            ui.valueDisplacement.text = "0.00 мм";
        }

        wasRunning = rig.IsRunning;

        if (!rig.IsRunning)
            return;

        float F = platform.currentForce;
        ui.valueCurrentLoad.text = F.ToString("F1") + " Н";

        if (F > peakForce)
        {
            peakForce = F;
            lastPeakForce = peakForce;
        }

        ui.valuePeakLoad.text = peakForce.ToString("F1") + " Н";

        ui.valueTestTime.text = rig.TestTime.ToString("F2") + " c";

        float displacement = Mathf.Abs(platform.transform.localPosition.y) * 1000f;
        ui.valueDisplacement.text = displacement.ToString("F2") + " мм";

        float area = 12.56f;
        float stress = (area > 0f) ? (F / (area * 1e-6f)) / 1e6f : 0f;
        ui.valueStress.text = stress.ToString("F2") + " МПа";
    }
}

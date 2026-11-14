using UnityEngine;
using UnityEngine.UI;

public class PanelController : MonoBehaviour
{
    public ButtonAction startButton;
    public ButtonAction stopButton;
    public ButtonAction napuskButton;

    public PlasmaController plasmaController;

    public UIAutoUpdater ui;
    public ProcessState state;

    public ProcessMode selectedMode;
    public bool modeSelected = false;
    public bool detailInstalled = false;

    public MaterialSelector selector;

    public GraphRenderer5LinesSmooth[] graphRenderers;
    public RawImage graphImage;

    [Header("Status Lamps")]
    public Light lampReady;
    public Light lampProcessing;
    public Light lampError;

    [Header("Gauges")]
    public PointerGauge vacuumGauge;
    public PointerGauge waterGauge;

    [Header("Doors")]
    public DoorLatch innerDoor;
    public DoorLatch outerDoor;

    void Update()
    {
        lampReady.enabled =
            modeSelected &&
            detailInstalled &&
            !state.processRunning &&
            !state.coolingDown &&
            !lampError.enabled;
    }

    public void ActivateGraph(ProcessMode mode)
    {
        graphImage.texture = graphRenderers[(int)mode].GetTexture();
    }

    public void OnButtonPressed(ButtonAction btn)
    {
        switch (btn.type)
        {
            case ButtonAction.ButtonType.Start: HandleStart(); break;
            case ButtonAction.ButtonType.Stop: HandleStop(); break;
            case ButtonAction.ButtonType.Napusk: HandleNapusk(); break;
            case ButtonAction.ButtonType.EmergencyStop: EmergencyStop(); break;
        }
    }

    void HandleStart()
    {
        if (!modeSelected || !detailInstalled)
        {
            ui.AddSystemMessage("Ошибка: выберите режим и установите деталь");
            ShowErrorLamp();
            return;
        }

        if (!plasmaController.IsReady())
        {
            ui.AddSystemMessage("Ошибка: камера не готова к запуску");
            ShowErrorLamp();
            return;
        }

        startButton.detector.LockButton();
        napuskButton.detector.interactable = false;
        stopButton.detector.interactable = true;

        // 🔥 профиль текущего материала
        MaterialProcessingProfile profile = null;
        if (selector != null)
            profile = selector.GetCurrentProfile();

        if (profile != null)
            state.profile = profile;

        state.ApplyMode(selectedMode);
        state.processRunning = true;
        state.coolingDown = false;

        ui.SetProcessStatus("процесс идёт", "шаг 1/1");

        lampProcessing.enabled = true;
        lampReady.enabled = false;
        lampError.enabled = false;

        // 🔥 PointerGauge из профиля, если есть
        if (profile != null)
        {
            vacuumGauge.SetTarget(profile.vacuumValue);
            waterGauge.SetTarget(profile.waterValue);
        }
        else
        {
            vacuumGauge.SetTarget(GetVacuumValue(selectedMode));
            waterGauge.SetTarget(GetWaterValue(selectedMode));
        }

        plasmaController.StartProcessing();
    }

    public void HandleStop()
    {
        plasmaController.StopPlasmaNormal();

        startButton.detector.UnlockButton();
        napuskButton.detector.UnlockButton();

        startButton.detector.interactable = true;
        napuskButton.detector.interactable = true;
        stopButton.detector.interactable = true;

        state.processRunning = false;
        state.coolingDown = true;

        ui.ShowNormalStop();

        vacuumGauge.SetTarget(0f);
        waterGauge.SetTarget(0f);

        ShowErrorLamp();
    }

    public void HandleProcessFinished()
    {
        startButton.detector.UnlockButton();
        napuskButton.detector.UnlockButton();

        startButton.detector.interactable = true;
        napuskButton.detector.interactable = true;
        stopButton.detector.interactable = true;

        state.processRunning = false;
        state.coolingDown = true;

        ui.SetProcessStatus("процесс завершён", "шаг 1/1");

        vacuumGauge.SetTarget(0f);
        waterGauge.SetTarget(0f);

        lampProcessing.enabled = false;
        lampReady.enabled = false;
        lampError.enabled = true;
        innerDoor.EnableDoor();
        outerDoor.EnableDoor();
    }

    public void EmergencyStop()
    {
        plasmaController.ForceStopPlasma();

        startButton.detector.interactable = false;
        napuskButton.detector.interactable = false;
        stopButton.detector.interactable = false;

        state.processRunning = false;
        state.coolingDown = false;

        ui.ShowEmergencyStop();

        vacuumGauge.SetTarget(0f);
        waterGauge.SetTarget(0f);

        ShowErrorLamp();
    }

    public void ShowErrorLamp()
    {
        lampProcessing.enabled = false;
        lampReady.enabled = false;
        lampError.enabled = true;
    }

    void HandleNapusk()
    {
        napuskButton.detector.LockButton();
        startButton.detector.interactable = false;
        stopButton.detector.interactable = true;
    }

    // старые GetVacuumValue/GetWaterValue оставлены как fallback
    private float GetVacuumValue(ProcessMode mode)
    {
        switch (mode)
        {
            case ProcessMode.Mode1: return 0.85f;
            case ProcessMode.Mode2: return 0.60f;
            case ProcessMode.Mode3: return 1.00f;
            case ProcessMode.Mode4: return 0.95f;
            case ProcessMode.Mode5: return 0.40f;
            case ProcessMode.Mode6: return 0.75f;
        }
        return 0f;
    }

    private float GetWaterValue(ProcessMode mode)
    {
        switch (mode)
        {
            case ProcessMode.Mode1: return 0.7f;
            case ProcessMode.Mode2: return 0.3f;
            case ProcessMode.Mode3: return 1.0f;
            case ProcessMode.Mode4: return 0.9f;
            case ProcessMode.Mode5: return 0.2f;
            case ProcessMode.Mode6: return 0.5f;
        }
        return 0f;
    }
}

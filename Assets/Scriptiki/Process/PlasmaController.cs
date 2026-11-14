using UnityEngine;
using System.Collections;

public class PlasmaController : MonoBehaviour
{
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor[] slots;

    public DoorLatch innerDoor;
    public DoorLatch outerDoor;

    public PlasmaLightController lightController;
    public PlasmaAudioController audioController;

    public float plasmaDelay = 2f;
    public float plasmaDuration = 5f;

    public MaterialSelector materialSelector;

    public Material processedMaterial;

    public UIAutoUpdater messageSystem;
    public PanelController panel;

    private bool plasmaRunning = false;
    private bool forceStopped = false;
    private Coroutine plasmaRoutine;

    void Update()
    {
        if (panel != null)
            panel.detailInstalled = IsAnySlotOccupied();
    }

    private bool IsAnySlotOccupied()
    {
        foreach (var slot in slots)
            if (slot.hasSelection)
                return true;

        return false;
    }

    public bool IsReady()
    {
        return IsAnySlotOccupied() && innerDoor.IsLatched && outerDoor.IsLatched;
    }

    public void StartProcessing()
    {
        if (plasmaRunning)
            return;

        if (!IsReady())
        {
            messageSystem.AddSystemMessage("Ошибка: камера не готова к запуску", MessageType.Error);
            panel.ShowErrorLamp();
            return;
        }

        if (materialSelector != null)
        {
            var profile = materialSelector.GetCurrentProfile();
            if (profile != null)
                plasmaDuration = profile.plasmaDuration;
        }

        forceStopped = false;

        if (audioController != null)
            audioController.ApplyMode(panel.selectedMode);

        plasmaRoutine = StartCoroutine(PlasmaRoutine());
    }

    private IEnumerator PlasmaRoutine()
    {
        plasmaRunning = true;

        messageSystem.AddSystemMessage("Процесс обработки запущен", MessageType.Normal);

        yield return new WaitForSeconds(plasmaDelay);

        if (forceStopped) yield break;

        if (lightController != null)
            lightController.Ignite();

        if (audioController != null)
            audioController.IgniteAudio();

        yield return new WaitForSeconds(plasmaDuration);

        if (forceStopped) yield break;

        foreach (var slot in slots)
        {
            if (!slot.hasSelection)
                continue;

            var interactable = slot.firstInteractableSelected;
            if (interactable == null) continue;

            var obj = interactable.transform.gameObject;
            var renderer = obj.GetComponentInChildren<MeshRenderer>();

            if (materialSelector != null && materialSelector.materialLibrary != null)
            {
                int index = (int)materialSelector.GetCurrentMaterial();
                Material treatedMat = materialSelector.materialLibrary.treatedMaterials[index];

                if (renderer != null && treatedMat != null)
                    renderer.material = treatedMat;
            }
            else
            {
                if (renderer != null && processedMaterial != null)
                    renderer.material = processedMaterial;
            }

            var behavior = obj.GetComponent<MaterialBehavior>();
            if (behavior != null)
                behavior.ApplyProcessing(panel.state);
        }

        if (lightController != null)
            lightController.Extinguish();

        if (audioController != null)
            audioController.ExtinguishAudio();

        messageSystem.AddSystemMessage("Процесс обработки завершён", MessageType.Normal);

        plasmaRunning = false;

        if (!forceStopped && panel != null)
            panel.HandleProcessFinished();
    }

    public void StopPlasmaNormal()
    {
        if (plasmaRoutine != null)
            StopCoroutine(plasmaRoutine);

        plasmaRunning = false;
        forceStopped = false;

        if (lightController != null)
            lightController.Extinguish();

        if (audioController != null)
            audioController.ExtinguishAudio();
    }

    public void ForceStopPlasma()
    {
        forceStopped = true;

        if (plasmaRoutine != null)
            StopCoroutine(plasmaRoutine);

        plasmaRunning = false;

        if (lightController != null)
            lightController.Extinguish();

        if (audioController != null)
            audioController.ExtinguishAudio();

        messageSystem.AddSystemMessage("Плазма отключена аварийно", MessageType.Error);
    }
}

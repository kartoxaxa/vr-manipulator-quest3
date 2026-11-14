using UnityEngine;

public class PokeArmTrigger : MonoBehaviour
{
    [Header("Ссылки")]
    public ArmTransfer armTransfer;
    public ConveyorQueue conveyorQueue;

    [Header("Настройки")]
    [Tooltip("Запускать только если в очереди есть кубик")]
    public bool requireCubeInQueue = true;

    // Этот метод будем вызывать по событию пока
    public void TriggerTransfer()
    {
        if (armTransfer == null) return;

        if (requireCubeInQueue && conveyorQueue != null && !conveyorQueue.HasCube())
        {
            Debug.Log("В очереди нет кубиков. Сперва нужно заспавнить их.");
            return;
        }

        armTransfer.StartTransfer();
    }
}
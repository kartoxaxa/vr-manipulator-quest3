using UnityEngine;
using UnityEngine.InputSystem;

public class SequenceButton : MonoBehaviour
{
    [Header("Ссылки")]
    public CubeAutoSpawner spawner;
    public ArmTransfer armTransfer;
    public ConveyorQueue conveyorQueue;
    public ConveyorMover conveyorMover;

    [Header("Поведение 3-го нажатия")]
    [Tooltip("Удалить все кубики (где бы они ни находились) и запустить спавн заново")]
    public bool resetAndRespawnOnThirdPress = true;

    private int state = 0; // 0 - спавн, 1 - ожидание переноса, 2 - перенос запущен/завершён

    private void Update()
    {
        // Тест на ПК (New Input System): клавиша T
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            OnPoke();
        }
    }

    // Привяжи этот метод в XR Simple Interactable -> Activate Events -> Activated
    public void OnPoke()
    {
        if (state == 0)
        {
            if (spawner != null)
            {
                spawner.SpawnOnce();
                Debug.Log("SequenceButton: кубики начали спавниться");
                state = 1;
            }
            else
            {
                Debug.LogError("SequenceButton: CubeAutoSpawner не назначен!");
            }
            return;
        }

        if (state == 1)
        {
            if (conveyorQueue == null)
            {
                Debug.LogError("SequenceButton: ConveyorQueue не назначен!");
                return;
            }

            if (!conveyorQueue.HasCube())
            {
                Debug.Log("SequenceButton: ещё нет кубиков в очереди. Подожди, пока они доедут до GrabStopPoint.");
                return;
            }

            if (armTransfer != null)
            {
                if (armTransfer.StartTransfer())
                {
                    Debug.Log("SequenceButton: манипулятор начал перенос кубиков");
                    state = 2;
                }
                else
                {
                    Debug.Log("SequenceButton: перенос не запустился (манипулятор занят или кубик ещё не готов).");
                }
            }
            else
            {
                Debug.LogError("SequenceButton: ArmTransfer не назначен!");
            }
            return;
        }

        if (state == 2)
        {
            if (!resetAndRespawnOnThirdPress)
            {
                Debug.Log("SequenceButton: перенос уже запущен/завершён. 3-е нажатие отключено.");
                return;
            }

            ResetCycleAndRespawn();
            return;
        }
    }

    private void ResetCycleAndRespawn()
    {
        // 1) Остановить перенос, отпустить кубик и вернуть манипулятор в исходную точку
        if (armTransfer != null)
        {
            armTransfer.ResetTransfer();
        }

        // 2) Удалить все кубики, где бы они ни находились (лента, очередь, захват, пол)
        if (spawner != null)
        {
            spawner.DestroyAllSpawnedCubes();
        }
        else
        {
            DestroyAllCubesFallback();
        }

        // 3) Очистить очередь и ленту от ссылок на удалённые кубики
        if (conveyorQueue != null)
        {
            conveyorQueue.Clear();
        }
        if (conveyorMover != null)
        {
            conveyorMover.Clear();
        }

        // 4) Сбросить спавнер и запустить спавн заново
        if (spawner != null)
        {
            spawner.ResetSpawner();
            spawner.SpawnOnce();
            Debug.Log("SequenceButton: кубики удалены, спавн запущен заново.");
            state = 1; // ждём, пока кубики доедут до очереди, чтобы снова запустить перенос
        }
        else
        {
            Debug.LogError("SequenceButton: CubeAutoSpawner не назначен, не могу перезапустить спавн.");
            state = 0;
        }
    }

    // Резервный вариант, если спавнер не назначен
    private void DestroyAllCubesFallback()
    {
        CubeMarker[] markers = FindObjectsByType<CubeMarker>(FindObjectsInactive.Include);
        for (int i = 0; i < markers.Length; i++)
        {
            if (markers[i] != null)
                Destroy(markers[i].gameObject);
        }
    }
}
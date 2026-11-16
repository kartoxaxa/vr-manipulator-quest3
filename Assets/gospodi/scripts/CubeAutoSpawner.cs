using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeAutoSpawner : MonoBehaviour
{
    [Header("Префаб и точка спавна")]
    [Tooltip("Префаб кубика")]
    public GameObject cubePrefab;

    [Tooltip("Точка появления кубиков")]
    public Transform spawnPoint;

    [Tooltip("Интервал между спавном кубиков")]
    public float spawnInterval = 1.2f;

    [Tooltip("Сколько кубиков создать за один цикл")]
    public int cubesToSpawn = 4;

    [Header("Ссылки")]
    public ConveyorMover conveyorMover;
    public ConveyorQueue conveyorQueue;

    private bool hasSpawned = false;
    private Coroutine spawnCoroutine;

    // Все кубики, созданные этим спавнером (для гарантированного удаления)
    private readonly List<GameObject> spawnedCubes = new List<GameObject>();

    public void SpawnOnce()
    {
        if (hasSpawned) return;
        if (cubePrefab == null || spawnPoint == null || conveyorMover == null || conveyorQueue == null)
        {
            Debug.LogError("CubeAutoSpawner: не все ссылки заполнены.");
            return;
        }

        hasSpawned = true;
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    public void ResetSpawner()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        hasSpawned = false;
    }

    public void ResetAndRespawn()
    {
        ResetSpawner();
        DestroyAllSpawnedCubes();
        SpawnOnce();
    }

    /// <summary>
    /// Удаляет все кубики, созданные спавнером, где бы они ни находились:
    /// на ленте, в очереди, в захвате, в полёте или уже сброшенные.
    /// </summary>
    public void DestroyAllSpawnedCubes()
    {
        // 1) Находим все кубики по маркеру (включая те, чьи ссылки потерялись)
        CubeMarker[] markers = FindObjectsByType<CubeMarker>(FindObjectsInactive.Include);
        for (int i = 0; i < markers.Length; i++)
        {
            if (markers[i] != null)
                Destroy(markers[i].gameObject);
        }

        // 2) Дополнительно удаляем всё, что запомнили при спавне
        for (int i = 0; i < spawnedCubes.Count; i++)
        {
            if (spawnedCubes[i] != null)
                Destroy(spawnedCubes[i]);
        }
        spawnedCubes.Clear();

        // 3) Чистим списки ленты и очереди от ссылок на удалённые кубики
        if (conveyorMover != null)
            conveyorMover.Clear();
        if (conveyorQueue != null)
            conveyorQueue.Clear();
    }

    private IEnumerator SpawnRoutine()
    {
        int spawnedCount = 0;

        while (spawnedCount < cubesToSpawn)
        {
            if (conveyorMover == null || conveyorQueue == null) yield break;

            // Чистим null из списков
            spawnedCubes.RemoveAll(go => go == null);
            conveyorMover.cubesOnBelt.RemoveAll(rb => rb == null);
            conveyorQueue.queue.RemoveAll(go => go == null);

            int cubesOnBeltCount = conveyorMover.cubesOnBelt.Count;
            int cubesInQueueCount = conveyorQueue.queue.Count;
            int totalCubes = cubesOnBeltCount + cubesInQueueCount;

            // Если кубиков уже достаточно, прекращаем
            if (totalCubes >= cubesToSpawn) break;

            // Не переполняем ленту
            if (cubesOnBeltCount >= 3)
            {
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            GameObject cube = Instantiate(cubePrefab, spawnPoint.position, spawnPoint.rotation);
            cube.name = "Cube_" + spawnedCount;

            // Маркер вместо тега: тег "Cube" может быть не определён в проекте
            if (cube.GetComponent<CubeMarker>() == null)
                cube.AddComponent<CubeMarker>();
            spawnedCubes.Add(cube);

            Rigidbody rb = cube.GetComponent<Rigidbody>();
            if (rb == null) rb = cube.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (!conveyorMover.cubesOnBelt.Contains(rb))
                conveyorMover.cubesOnBelt.Add(rb);

            spawnedCount++;
            yield return new WaitForSeconds(spawnInterval);
        }

        spawnCoroutine = null;
    }
}

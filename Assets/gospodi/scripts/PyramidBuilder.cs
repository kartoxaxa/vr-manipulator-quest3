using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Собирает пирамиду 3-2-1 из кубиков, которые лежат в объекте More_cube.
/// Кубики берутся по порядку (дочерние объекты More_cube).
/// Тест на ПК — клавиша U. VR-кнопку можно привязать к OnPyramidButtonPressed()
/// или назвать её Pyramid — тогда скрипт подпишется сам.
/// </summary>
public class PyramidBuilder : MonoBehaviour
{
    [Header("Ссылки (можно оставить пустыми — найдёт сам)")]
    [Tooltip("Пустой объект, внутри которого лежат 6 кубиков по порядку. Если пусто — ищется объект More_cube.")]
    public Transform cubesRoot;

    [Tooltip("Точка на уровне стола, от которой строится пирамида. Если пусто — ищется PyramidOrigin, иначе DropPoint.")]
    public Transform pyramidOrigin;

    public ArmGripper gripper;
    public ArmTransfer armTransfer;
    public ArmIKCCD armIK;

    [Header("Движение манипулятора")]
    [Tooltip("Скорость перемещения цели IK, м/с")]
    public float moveSpeed = 0.4f;

    [Tooltip("Насколько высоко подойти к кубику перед захватом")]
    public float approachHeight = 0.12f;

    [Tooltip("Насколько поднять кубик после захвата")]
    public float liftHeight = 0.18f;

    [Tooltip("Насколько поднять руку после установки кубика")]
    public float retractHeight = 0.12f;

    [Tooltip("Пауза перед захватом, сек")]
    public float grabDelay = 0.15f;

    [Tooltip("Пауза перед отпусканием, сек")]
    public float releaseDelay = 0.15f;

    [Tooltip("Сколько ждать, пока рука доедет до точки, сек")]
    public float armSettleTimeout = 4f;

    [Tooltip("Допустимая ошибка руки при захвате, м")]
    public float grabTolerance = 0.035f;

    [Header("Пирамида")]
    [Tooltip("Держать кубики kinematic во время сборки, чтобы пирамида не разваливалась")]
    public bool snapCubesKinematic = true;

    [Tooltip("Включить физику у всех поставленных кубиков после сборки")]
    public bool enablePhysicsAfterBuild = true;

    [Tooltip("Небольшой зазор между кубиками, м")]
    public float cubeGap = 0.002f;

    [Tooltip("Пауза перед включением физики после установки последнего кубика")]
    public float settleBeforePhysics = 0.3f;

    [Header("Кубики")]
    [Tooltip("Добавить Rigidbody, если у кубика его нет")]
    public bool addRigidbodyIfMissing = true;

    [Tooltip("Добавить BoxCollider, если у кубика нет коллайдера")]
    public bool addBoxColliderIfMissing = true;

    [Tooltip("Добавить XRGrabInteractable, чтобы кубик можно было взять рукой")]
    public bool addGrabInteractableIfMissing = true;

    [Header("Кнопка VR")]
    [Tooltip("Автоматически подписаться на XR Simple Interactable с именем Pyramid")]
    public bool subscribePyramidButton = true;

    public string pyramidButtonName = "Pyramid";

    private Transform targetPoint;
    private Transform targetEnd;
    private Transform homePoint;

    private readonly List<Transform> cubes = new List<Transform>();
    private readonly List<Transform> placedCubes = new List<Transform>();
    private bool isBuilding;
    private float lastPressTime = -10f;

    public bool IsBuilding => isBuilding;
    public static bool IsAnyBuilding { get; private set; }

    // Запасной вариант: если компонент не добавлен в сцену, создаём его в рантайме.
    // Для постоянной настройки лучше добавить PyramidBuilder на ArmLogic в SampleScene.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindAnyObjectByType<PyramidBuilder>() != null) return;
        if (FindAnyObjectByType<ArmGripper>() == null) return;

        GameObject go = new GameObject("[PyramidBuilder]");
        go.AddComponent<PyramidBuilder>();
        Debug.Log("[Pyramid] Контроллер пирамиды создан автоматически (fallback).");
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        PrepareCubes();
        SubscribeButton();
    }

    private void Update()
    {
        // Тест на ПК: U. T — кубики, Y — розлив, U — пирамида.
        if (Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
            OnPyramidButtonPressed();
    }

    private void OnDisable()
    {
        if (!isBuilding) return;

        StopAllCoroutines();
        if (gripper != null && gripper.HasCube())
            gripper.Release();

        isBuilding = false;
        IsAnyBuilding = false;
    }

    // ------------------------------------------------------------------
    // Публичные точки входа
    // ------------------------------------------------------------------
    public void Build()
    {
        OnPyramidButtonPressed();
    }

    public void OnPyramidButtonPressed()
    {
        if (!enabled) return;
        if (Time.time - lastPressTime < 0.5f) return;
        lastPressTime = Time.time;

        if (isBuilding)
        {
            Debug.Log("[Pyramid] Сборка уже идёт.");
            return;
        }

        if (armTransfer != null && armTransfer.IsRunning)
        {
            Debug.Log("[Pyramid] Манипулятор занят переносом кубиков.");
            return;
        }

        if (BottlePourController.IsAnyPouring)
        {
            Debug.Log("[Pyramid] Сейчас идёт розлив воды.");
            return;
        }

        if (gripper != null && gripper.HasCube())
        {
            Debug.Log("[Pyramid] В захвате уже что-то есть.");
            return;
        }

        if (cubesRoot == null || pyramidOrigin == null)
            ResolveReferences();

        if (pyramidOrigin == null)
        {
            Debug.LogError("[Pyramid] Не найдена точка основания. Создай Empty 'PyramidOrigin' на уровне стола.");
            return;
        }

        if (cubes.Count < 6)
            PrepareCubes();

        if (cubes.Count < 6)
        {
            Debug.LogError($"[Pyramid] Нужно 6 кубиков в More_cube, найдено {cubes.Count}.");
            return;
        }

        if (targetPoint == null || targetEnd == null || gripper == null)
        {
            Debug.LogError("[Pyramid] Не хватает ссылок на манипулятор/цель IK/захват.");
            return;
        }

        StartCoroutine(BuildRoutine());
    }

    // ------------------------------------------------------------------
    // Поиск ссылок и подготовка кубиков
    // ------------------------------------------------------------------
    private void ResolveReferences()
    {
        if (gripper == null)
            gripper = FindAnyObjectByType<ArmGripper>();

        if (armTransfer == null)
            armTransfer = FindAnyObjectByType<ArmTransfer>();

        if (armTransfer != null)
        {
            if (targetPoint == null) targetPoint = armTransfer.targetPoint;
            if (homePoint == null) homePoint = armTransfer.homePoint;
            if (armIK == null) armIK = armTransfer.armIK;
        }

        if (armIK == null)
            armIK = FindAnyObjectByType<ArmIKCCD>();

        if (gripper != null && targetEnd == null)
            targetEnd = gripper.targetEnd;

        if (cubesRoot == null)
            cubesRoot = FindTransformByName("More_cube");

        if (pyramidOrigin == null)
            pyramidOrigin = FindTransformByName("PyramidOrigin");

        if (pyramidOrigin == null && armTransfer != null)
            pyramidOrigin = armTransfer.dropPoint;
    }

    private void PrepareCubes()
    {
        cubes.Clear();
        placedCubes.Clear();

        if (cubesRoot == null)
        {
            Debug.LogError("[Pyramid] Не найден объект More_cube. Создай Empty 'More_cube' и положи в него 6 кубиков.");
            return;
        }

        for (int i = 0; i < cubesRoot.childCount; i++)
        {
            Transform child = cubesRoot.GetChild(i);
            if (child == null) continue;
            if (child.GetComponentInChildren<Renderer>(true) == null) continue;

            cubes.Add(child);
            PrepareCube(child);
        }

        if (cubes.Count < 6)
        {
            Debug.LogError($"[Pyramid] В '{cubesRoot.name}' найдено {cubes.Count} кубиков, нужно 6. " +
                           "Кубики должны быть прямыми дочерними объектами More_cube.");
        }
    }

    private void PrepareCube(Transform cube)
    {
        if (cube == null) return;

        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb == null && addRigidbodyIfMissing)
            rb = cube.gameObject.AddComponent<Rigidbody>();

        if (rb != null)
        {
            rb.mass = 0.2f;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        if (cube.GetComponentInChildren<Collider>(true) == null && addBoxColliderIfMissing)
            AddBoxCollider(cube);

        if (addGrabInteractableIfMissing && cube.GetComponentInChildren<XRGrabInteractable>(true) == null)
            cube.gameObject.AddComponent<XRGrabInteractable>();
    }

    private static void AddBoxCollider(Transform root)
    {
        if (!TryGetWorldBounds(root, out Bounds worldBounds)) return;

        BoxCollider box = root.gameObject.AddComponent<BoxCollider>();
        Vector3 localCenter = root.InverseTransformPoint(worldBounds.center);
        Vector3 localSize = root.InverseTransformVector(worldBounds.size);
        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

        box.center = localCenter;
        box.size = localSize;
    }

    private void SubscribeButton()
    {
        if (!subscribePyramidButton) return;

        XRSimpleInteractable[] interactables =
            FindObjectsByType<XRSimpleInteractable>(FindObjectsInactive.Exclude);

        for (int i = 0; i < interactables.Length; i++)
        {
            if (interactables[i].gameObject.name != pyramidButtonName) continue;

            interactables[i].selectEntered.AddListener(_ => OnPyramidButtonPressed());
            Debug.Log($"[Pyramid] Кнопка '{pyramidButtonName}' подключена к сборке.");
            return;
        }
    }

    // ------------------------------------------------------------------
    // Сборка
    // ------------------------------------------------------------------
    private IEnumerator BuildRoutine()
    {
        isBuilding = true;
        IsAnyBuilding = true;
        placedCubes.Clear();

        bool grabbed = false;
        bool completed = false;

        try
        {
            float cubeSize = GetCubeSize(cubes[0]);
            Vector3[] slots = BuildSlotPositions(cubeSize);
            Quaternion slotRotation = pyramidOrigin != null ? pyramidOrigin.rotation : Quaternion.identity;
            List<Rigidbody> placedBodies = new List<Rigidbody>();

            for (int slotIndex = 0; slotIndex < 6; slotIndex++)
            {
                Transform cube = TakeNextFreeCube();
                if (cube == null)
                {
                    Debug.LogWarning("[Pyramid] Свободных кубиков не осталось. Сборка остановлена.");
                    yield break;
                }

                Vector3 cubePosition = cube.position;

                // 1) Подход сверху и опускание к кубику
                yield return MoveTargetAndWait(cubePosition + Vector3.up * approachHeight, 2.5f, false);
                yield return MoveTargetAndWait(cubePosition, armSettleTimeout, true);

                float grabError = targetEnd != null
                    ? Vector3.Distance(targetEnd.position, cubePosition)
                    : 999f;

                if (grabError > grabTolerance)
                {
                    Debug.LogWarning($"[Pyramid] Рука не дотянулась до '{cube.name}': ошибка {grabError:F3} м. " +
                                     "Передвинь кубики ближе к манипулятору.");
                    yield break;
                }

                yield return new WaitForSeconds(grabDelay);

                // 2) Захват
                gripper.Grab(cube.gameObject);
                grabbed = gripper.HasCube();
                if (!grabbed)
                {
                    Debug.LogWarning($"[Pyramid] Не удалось захватить '{cube.name}'.");
                    yield break;
                }

                // 3) Поднять кубик
                yield return MoveTargetAndWait(targetPoint.position + Vector3.up * liftHeight, 2f, false);

                // 4) Подойти сверху к слоту и опустить
                int layer = slotIndex < 3 ? 0 : (slotIndex < 5 ? 1 : 2);
                Vector3 aboveSlot = slots[slotIndex] + Vector3.up * (approachHeight + cubeSize * layer);

                yield return MoveTargetAndWait(aboveSlot, 3f, false);
                yield return MoveTargetAndWait(slots[slotIndex], 3f, false);

                yield return new WaitForSeconds(releaseDelay);

                // 5) Отпустить и зафиксировать в слоте
                gripper.Release();
                grabbed = false;

                Rigidbody rb = cube.GetComponent<Rigidbody>();
                if (snapCubesKinematic && rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                cube.SetPositionAndRotation(slots[slotIndex], slotRotation);
                placedCubes.Add(cube);
                if (rb != null) placedBodies.Add(rb);

                // 6) Убрать руку вверх, чтобы не задеть поставленный кубик
                yield return MoveTargetAndWait(targetPoint.position + Vector3.up * retractHeight, 2f, false);
                yield return new WaitForSeconds(0.1f);
            }

            completed = true;

            // 7) Вернуть манипулятор домой
            if (homePoint != null)
                yield return MoveTargetAndWait(homePoint.position, 3f, false);

            // 8) Включить физику, чтобы пирамиду можно было развалить рукой
            if (enablePhysicsAfterBuild && placedBodies.Count > 0)
            {
                yield return new WaitForSeconds(settleBeforePhysics);

                for (int i = 0; i < placedBodies.Count; i++)
                {
                    Rigidbody rb = placedBodies[i];
                    if (rb == null) continue;

                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.WakeUp();
                }
            }

            Debug.Log("[Pyramid] Пирамида собрана.");
        }
        finally
        {
            if (grabbed && gripper != null && gripper.HasCube())
                gripper.Release();

            if (!completed && targetPoint != null && homePoint != null)
                targetPoint.position = homePoint.position;

            isBuilding = false;
            IsAnyBuilding = false;
        }
    }

    private Transform TakeNextFreeCube()
    {
        for (int i = 0; i < cubes.Count; i++)
        {
            Transform cube = cubes[i];
            if (cube == null) continue;
            if (placedCubes.Contains(cube)) continue;
            if (IsHeldByPlayer(cube)) continue;

            return cube;
        }

        return null;
    }

    private static bool IsHeldByPlayer(Transform cube)
    {
        XRGrabInteractable grab = cube.GetComponentInChildren<XRGrabInteractable>(true);
        return grab != null && grab.isSelected;
    }

    private Vector3[] BuildSlotPositions(float cubeSize)
    {
        Vector3 origin = pyramidOrigin != null ? pyramidOrigin.position : Vector3.zero;
        Quaternion rotation = pyramidOrigin != null ? pyramidOrigin.rotation : Quaternion.identity;

        float step = cubeSize + cubeGap;
        Vector3[] local = new Vector3[6];

        // 3 в основании
        local[0] = new Vector3(-step, cubeSize * 0.5f, 0f);
        local[1] = new Vector3(0f, cubeSize * 0.5f, 0f);
        local[2] = new Vector3(step, cubeSize * 0.5f, 0f);

        // 2 в середине
        local[3] = new Vector3(-step * 0.5f, cubeSize * 1.5f, 0f);
        local[4] = new Vector3(step * 0.5f, cubeSize * 1.5f, 0f);

        // 1 на вершине
        local[5] = new Vector3(0f, cubeSize * 2.5f, 0f);

        Vector3[] world = new Vector3[6];
        for (int i = 0; i < 6; i++)
            world[i] = origin + rotation * local[i];

        return world;
    }

    private float GetCubeSize(Transform cube)
    {
        if (TryGetWorldBounds(cube, out Bounds bounds))
        {
            float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (size > 0.001f) return size;
        }

        return 0.1f;
    }

    private IEnumerator MoveTargetAndWait(Vector3 position, float timeout, bool requireReach)
    {
        if (targetPoint == null) yield break;

        Vector3 start = targetPoint.position;
        float distance = Vector3.Distance(start, position);
        float duration = Mathf.Max(0.15f, distance / Mathf.Max(moveSpeed, 0.01f));
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            targetPoint.position = Vector3.Lerp(
                start, position, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)));
            yield return null;
        }

        targetPoint.position = position;

        float waited = 0f;
        while (targetEnd != null && waited < timeout)
        {
            if (Vector3.Distance(targetEnd.position, position) <= 0.02f) break;
            waited += Time.deltaTime;
            yield return null;
        }

        if (requireReach && targetEnd != null)
        {
            float error = Vector3.Distance(targetEnd.position, position);
            if (error > grabTolerance)
                Debug.LogWarning($"[Pyramid] Конец руки не дошёл до цели: {error:F3} м.");
        }
    }

    // ------------------------------------------------------------------
    // Вспомогательные методы
    // ------------------------------------------------------------------
    private static Transform FindTransformByName(string objectName)
    {
        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == objectName)
                return all[i];
        }

        return null;
    }

    private static bool TryGetWorldBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        bounds = new Bounds(root.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;
            if (renderer is ParticleSystemRenderer) continue;
            if (!renderer.enabled) continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }
}

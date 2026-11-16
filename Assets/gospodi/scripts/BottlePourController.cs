using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Розлив воды из бутылки в стакан.
/// Скрипт сам находит манипулятор, бутылку и стакан, сам готовит физику,
/// якорь захвата, носик бутылки, воду и струю.
/// Кнопка "Bottle" (XR Simple Interactable) подписывается на событие автоматически.
/// Для теста на ПК — клавиша Y (рядом с T — кубики).
/// </summary>
[DefaultExecutionOrder(10000)] // LateUpdate должен идти после ArmIKCCD, чтобы поворачивать кисть POINT_7, а не бутылку
public class BottlePourController : MonoBehaviour
{
    [Header("Ссылки (можно оставить пустыми — найдёт сам)")]
    public Transform bottle;
    public Transform glass;
    public ArmGripper gripper;
    public ArmTransfer armTransfer;

    [Tooltip("Кисть манипулятора POINT_7. Если пусто — возьмётся родитель TARGET_END.")]
    public Transform wrist;

    [Header("Движение манипулятора")]
    [Tooltip("Скорость перемещения цели IK, м/с")]
    public float moveSpeed = 0.4f;

    [Tooltip("Насколько высоко подойти к якорю бутылки перед захватом")]
    public float approachHeight = 0.16f;

    [Tooltip("Насколько поднять бутылку после захвата")]
    public float liftHeight = 0.22f;

    [Tooltip("Высота кисти над краем стакана при розливе")]
    public float pourHandHeight = 0.16f;

    [Tooltip("Угол наклона бутылки при розливе")]
    [Range(45f, 135f)]
    public float tiltAngle = 95f;

    [Tooltip("Длительность наклона / возврата, сек")]
    public float tiltDuration = 1.1f;

    [Tooltip("Длительность полного розлива, сек")]
    public float pourDuration = 3f;

    [Tooltip("Сколько ждать, пока рука доедет до точки, сек")]
    public float armSettleTimeout = 4f;

    [Tooltip("Допустимая ошибка руки при захвате, м")]
    public float grabTolerance = 0.035f;

    [Header("Вода")]
    public Color waterColor = new Color(0.25f, 0.65f, 1f, 0.55f);
    public float streamRadius = 0.006f;

    [Header("Геометрия захвата (если GrabAnchor/Spout не созданы вручную)")]
    [Range(0f, 1f)] public float grabHeight01 = 0.62f;
    [Range(0f, 1f)] public float spoutHeight01 = 0.98f;

    // --- найденное/созданное ---
    private Transform targetPoint;
    private Transform targetEnd;
    private Transform bottleAnchor;
    private Transform bottleSpout;
    private Transform bottleWater;
    private Transform glassWater;
    private Vector3 bottleWaterFullScale;
    private Vector3 bottleWaterFullPos;
    private Vector3 glassWaterFullScale;
    private Vector3 glassWaterFullPos;
    private Bounds bottleBounds;
    private Bounds glassBounds;
    private Rigidbody bottleRb;
    private Collider bottleCollider;
    private Collider glassCollider;
    private Material waterMaterial;
    private Material streamMaterial;
    private Transform stream;
    private XRSimpleInteractable bottleButton;
    private ArmIKCCD armIK;
    private JointAxis wristJoint;
    private int wristJointIndex = -1;

    private Vector3 bottleStartPos;
    private Quaternion bottleStartRot;
    private Vector3 handOffsetAtGrab;

    private float bottleFill = 1f;
    private float glassFill;
    private bool isPouring;
    private bool handRotationLocked;
    private Quaternion heldRotation;
    private bool pourVisualActive;
    private float lastPressTime = -10f;

    public bool IsPouring => isPouring;
    public static bool IsAnyPouring { get; private set; }

    // ------------------------------------------------------------------
    // Автозапуск: не нужно ничего добавлять в сцену вручную
    // ------------------------------------------------------------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindAnyObjectByType<BottlePourController>() != null) return;
        if (FindAnyObjectByType<ArmGripper>() == null) return;

        Transform b = FindModelRoot("Bottle");
        Transform g = FindModelRoot("Glass");
        if (b == null || g == null) return;

        GameObject go = new GameObject("[BottlePourController]");
        go.AddComponent<BottlePourController>();
        Debug.Log("[BottlePour] Контроллер розлива создан автоматически.");
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        if (!SetupScene())
        {
            enabled = false;
            return;
        }

        SubscribeButton();
    }

    private void Update()
    {
        // Тест на ПК: T — кубики, Y — розлив (клавиши рядом)
        if (Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame)
            OnBottleButtonPressed();

        // Важно: кисть должна быть повёрнута до Solve() в ArmIKCCD (LateUpdate),
        // иначе IK посчитает цель для старой ориентации кисти.
        ApplyWristLock();
    }

    private void LateUpdate()
    {
        // Повторно фиксируем кисть после ArmIKCCD — на случай, если JointAxis успел её тронуть.
        ApplyWristLock();

        if (pourVisualActive)
            UpdateStream();
    }

    private void ApplyWristLock()
    {
        if (!handRotationLocked || targetEnd == null) return;

        if (wrist != null)
        {
            // Держим TARGET_END/бутылку в нужной мировой ориентации, поворачивая
            // именно кисть POINT_7. Формула работает и для непрямого родителя.
            Quaternion rel = Quaternion.Inverse(wrist.rotation) * targetEnd.rotation;
            wrist.rotation = heldRotation * Quaternion.Inverse(rel);
        }
        else
        {
            // Страховка, если POINT_7 не найден
            targetEnd.rotation = heldRotation;
        }
    }

    /// <summary>
    /// На время розлива убираем POINT_7 из цепочки ArmIKCCD и останавливаем его JointAxis.
    /// Тогда IK решает оставшиеся 6 суставов уже с нужной ориентацией кисти,
    /// а не борется с ней. После розлива всё возвращается на место.
    /// </summary>
    private void SetWristIkLocked(bool locked)
    {
        if (armIK == null || wristJoint == null || armIK.joints == null) return;

        if (locked)
        {
            if (wristJointIndex >= 0) return;

            for (int i = 0; i < armIK.joints.Length; i++)
            {
                if (armIK.joints[i] != wristJoint) continue;

                wristJointIndex = i;
                armIK.joints[i] = null;
                wristJoint.SnapToAngle(0f);
                wristJoint.ManualInputEnabled = false;
                break;
            }
        }
        else
        {
            if (wristJointIndex < 0) return;

            armIK.joints[wristJointIndex] = wristJoint;
            wristJointIndex = -1;
            wristJoint.ManualInputEnabled = true;
        }
    }

    private void OnDisable()
    {
        // Если объект выключили/удалили прямо во время розлива — вернуть POINT_7 в IK.
        SetWristIkLocked(false);
    }

    private void OnDestroy()
    {
        SetWristIkLocked(false);

        if (bottleCollider != null && glassCollider != null)
            Physics.IgnoreCollision(bottleCollider, glassCollider, false);

        if (IsAnyPouring) IsAnyPouring = false;
    }

    // ------------------------------------------------------------------
    // Публичные точки входа
    // ------------------------------------------------------------------
    // Можно привязать вручную: XR Simple Interactable -> Select Entered -> Pour()
    public void Pour()
    {
        OnBottleButtonPressed();
    }

    public void OnBottleButtonPressed()
    {
        if (!enabled) return;
        if (Time.time - lastPressTime < 0.5f) return;
        lastPressTime = Time.time;

        if (isPouring)
        {
            Debug.Log("[BottlePour] Уже наливаю. Подожди.");
            return;
        }

        if (armTransfer != null && armTransfer.IsRunning)
        {
            Debug.Log("[BottlePour] Манипулятор занят переносом кубиков.");
            return;
        }

        if (gripper != null && gripper.HasCube())
        {
            Debug.Log("[BottlePour] В захвате уже что-то есть.");
            return;
        }

        if (bottle == null || glass == null || gripper == null || targetPoint == null)
        {
            Debug.LogError("[BottlePour] Не хватает ссылок. Бутылка/стакан/гриппер/цель IK не найдены.");
            return;
        }

        StartCoroutine(PourRoutine());
    }

    // ------------------------------------------------------------------
    // Настройка сцены в рантайме
    // ------------------------------------------------------------------
    private void ResolveReferences()
    {
        if (gripper == null) gripper = FindAnyObjectByType<ArmGripper>();
        if (armTransfer == null) armTransfer = FindAnyObjectByType<ArmTransfer>();

        if (targetPoint == null && armTransfer != null)
            targetPoint = armTransfer.targetPoint;

        if (targetPoint == null)
        {
            ArmIKCCD ik = FindAnyObjectByType<ArmIKCCD>();
            if (ik != null) targetPoint = ik.target;
        }

        if (armIK == null && armTransfer != null) armIK = armTransfer.armIK;
        if (armIK == null) armIK = FindAnyObjectByType<ArmIKCCD>();

        if (gripper != null)
            targetEnd = gripper.targetEnd;

        if (wrist == null && targetEnd != null)
        {
            wrist = targetEnd.parent;
            if (wrist != null && wrist.name != "POINT_7")
            {
                Debug.LogWarning($"[BottlePour] Кисть — '{wrist.name}', а не POINT_7. " +
                                 "Наклоняю родителя TARGET_END.");
            }
        }

        if (wrist != null && wristJoint == null)
            wristJoint = wrist.GetComponent<JointAxis>();

        if (bottle == null) bottle = FindModelRoot("Bottle");
        if (glass == null) glass = FindModelRoot("Glass");
    }

    private bool SetupScene()
    {
        if (gripper == null || targetPoint == null || bottle == null || glass == null)
        {
            Debug.LogError("[BottlePour] Не найдены манипулятор, бутылка или стакан.");
            return false;
        }

        if (targetEnd == null) targetEnd = gripper.targetEnd;
        if (targetEnd == null)
        {
            Debug.LogError("[BottlePour] У ArmGripper не назначен targetEnd.");
            return false;
        }

        bottleStartPos = bottle.position;
        bottleStartRot = bottle.rotation;

        if (!TryGetWorldBounds(bottle, out bottleBounds))
        {
            Debug.LogError("[BottlePour] У бутылки нет рендереров — не могу посчитать габариты.");
            return false;
        }

        if (!TryGetWorldBounds(glass, out glassBounds))
        {
            Debug.LogError("[BottlePour] У стакана нет рендереров — не могу посчитать габариты.");
            return false;
        }

        PrepareBottle();
        PrepareGlass();
        PrepareWaterVisuals();
        CreateWaterMaterials();
        CreateStream();

        bottleFill = 1f;
        glassFill = 0f;
        UpdateFillVisuals();

        Debug.Log($"[BottlePour] Готово: бутылка '{bottle.name}', стакан '{glass.name}', " +
                  $"высота бутылки {bottleBounds.size.y:F3} м, высота стакана {glassBounds.size.y:F3} м.");
        return true;
    }

    private void PrepareBottle()
    {
        // Физика бутылки. Пока стоит на столе — kinematic, чтобы не падала.
        bottleRb = bottle.GetComponent<Rigidbody>();
        if (bottleRb == null)
            bottleRb = bottle.gameObject.AddComponent<Rigidbody>();

        bottleRb.mass = 0.25f;
        bottleRb.isKinematic = true;
        bottleRb.useGravity = false;
        bottleRb.interpolation = RigidbodyInterpolation.Interpolate;
        bottleRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        bottleCollider = AddBoxCollider(bottle, bottleBounds);

        bottleAnchor = FindChildByName(bottle, "GrabAnchor");
        if (bottleAnchor == null)
        {
            GameObject anchorGo = new GameObject("GrabAnchor");
            bottleAnchor = anchorGo.transform;
            bottleAnchor.SetParent(bottle, false);

            float worldY = Mathf.Lerp(bottleBounds.min.y, bottleBounds.max.y, grabHeight01);
            bottleAnchor.position = new Vector3(bottleBounds.center.x, worldY, bottleBounds.center.z);
        }

        bottleSpout = FindChildByName(bottle, "Spout");
        if (bottleSpout == null)
        {
            GameObject spoutGo = new GameObject("Spout");
            bottleSpout = spoutGo.transform;
            bottleSpout.SetParent(bottle, false);

            float worldY = Mathf.Lerp(bottleBounds.min.y, bottleBounds.max.y, spoutHeight01);
            bottleSpout.position = new Vector3(bottleBounds.center.x, worldY, bottleBounds.center.z);
        }
    }

    private void PrepareGlass()
    {
        glassCollider = AddBoxCollider(glass, glassBounds);
    }

    private void PrepareWaterVisuals()
    {
        bottleWater = FindWaterCylinder(bottle);
        if (bottleWater != null)
        {
            DisableColliders(bottleWater);
            bottleWaterFullScale = bottleWater.localScale;
            bottleWaterFullPos = bottleWater.localPosition;
        }
        else
        {
            Debug.LogWarning("[BottlePour] У бутылки не найден дочерний Cylinder с коллайдером (вода).");
        }

        glassWater = FindWaterCylinder(glass);
        if (glassWater != null)
        {
            DisableColliders(glassWater);
            glassWaterFullScale = glassWater.localScale;
            glassWaterFullPos = glassWater.localPosition;
        }
        else
        {
            Debug.LogWarning("[BottlePour] У стакана не найден дочерний Cylinder с коллайдером (вода).");
        }
    }

    private void CreateWaterMaterials()
    {
        Shader shader = Shader.Find("Custom/WaterUnlit");
        if (shader == null) shader = Resources.Load<Shader>("WaterUnlit");
        if (shader == null)
        {
            Debug.LogError("[BottlePour] Не найден шейдер Custom/WaterUnlit. Вода будет с материалом по умолчанию.");
            return;
        }

        waterMaterial = new Material(shader);
        waterMaterial.name = "BottleWater (runtime)";
        if (waterMaterial.HasProperty("_Color")) waterMaterial.SetColor("_Color", waterColor);
        if (waterMaterial.HasProperty("_BaseColor")) waterMaterial.SetColor("_BaseColor", waterColor);
        waterMaterial.renderQueue = 3000;

        Color streamColor = waterColor;
        streamColor.a = Mathf.Clamp01(waterColor.a + 0.25f);
        streamMaterial = new Material(shader);
        streamMaterial.name = "WaterStream (runtime)";
        if (streamMaterial.HasProperty("_Color")) streamMaterial.SetColor("_Color", streamColor);
        if (streamMaterial.HasProperty("_BaseColor")) streamMaterial.SetColor("_BaseColor", streamColor);
        streamMaterial.renderQueue = 3000;

        SetMaterial(bottleWater, waterMaterial);
        SetMaterial(glassWater, waterMaterial);
    }

    private void CreateStream()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "WaterStream";

        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            if (streamMaterial != null) renderer.sharedMaterial = streamMaterial;
            else if (waterMaterial != null) renderer.sharedMaterial = waterMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        stream = go.transform;
        stream.SetParent(transform, false);
        stream.gameObject.SetActive(false);
    }

    private void SubscribeButton()
    {
        XRSimpleInteractable[] interactables =
            FindObjectsByType<XRSimpleInteractable>(FindObjectsInactive.Exclude);

        for (int i = 0; i < interactables.Length; i++)
        {
            if (interactables[i].gameObject.name != "Bottle") continue;

            bottleButton = interactables[i];
            bottleButton.selectEntered.AddListener(_ => OnBottleButtonPressed());
            Debug.Log("[BottlePour] Кнопка 'Bottle' подключена к розливу.");
            return;
        }

        Debug.LogWarning("[BottlePour] Кнопка 'Bottle' с XR Simple Interactable не найдена. " +
                         "Переименуй кнопку в 'Bottle' или привяжи Select Entered -> Pour() вручную. " +
                         "На ПК можно нажать Y.");
    }

    // ------------------------------------------------------------------
    // Сценарий розлива
    // ------------------------------------------------------------------
    private IEnumerator PourRoutine()
    {
        isPouring = true;
        IsAnyPouring = true;
        bool grabbed = false;

        try
        {
            // Если предыдущий цикл закончился (бутылка пуста / стакан полон) — начинаем заново
            if (bottleFill <= 0.01f || glassFill >= 0.99f)
            {
                bottleFill = 1f;
                glassFill = 0f;
                UpdateFillVisuals();
            }

            Vector3 anchorWorld = bottleAnchor.position;
            Vector3 approach = anchorWorld + Vector3.up * approachHeight;

            // Держим TARGET_END в ориентации бутылки, поворачивая кисть POINT_7:
            // так бутылка не крутится в клешне, а наклоняется вместе с кистью.
            handRotationLocked = true;
            heldRotation = bottle.rotation;
            SetWristIkLocked(true);

            // 1) Подъезд сверху и опускание к якорю
            yield return MoveTargetAndWait(approach, 2.5f, false);
            yield return MoveTargetAndWait(anchorWorld, armSettleTimeout, true);

            float grabError = targetEnd != null
                ? Vector3.Distance(targetEnd.position, anchorWorld)
                : 999f;

            if (grabError > grabTolerance)
            {
                Debug.LogWarning($"[BottlePour] Рука не дотянулась до бутылки: ошибка {grabError:F3} м. " +
                                 "Передвинь бутылку ближе к манипулятору или увеличь вылет руки.");
                yield break;
            }

            // 2) Захват
            gripper.Grab(bottle.gameObject);
            grabbed = gripper.HasCube();
            if (!grabbed)
            {
                Debug.LogWarning("[BottlePour] Захват бутылки не удался.");
                yield break;
            }

            handOffsetAtGrab = targetEnd.position - bottle.position;

            // Не даём бутылке толкать стакан во время наклона
            if (bottleCollider != null && glassCollider != null)
                Physics.IgnoreCollision(bottleCollider, glassCollider, true);

            // 3) Поднять
            yield return MoveTargetAndWait(targetPoint.position + Vector3.up * liftHeight, 2f, false);
            if (!StillHoldingBottle()) { Debug.LogWarning("[BottlePour] Захват бутылки прерван."); yield break; }

            // 4) Перенести к стакану: сначала вверх, потом по горизонтали
            Vector3 glassCenter = new Vector3(glassBounds.center.x, 0f, glassBounds.center.z);
            float handY = glassBounds.max.y + pourHandHeight;

            Vector3 highAboveGlass = new Vector3(
                glassBounds.center.x,
                Mathf.Max(targetPoint.position.y, handY + 0.12f),
                glassBounds.center.z);

            yield return MoveTargetAndWait(highAboveGlass, 3f, false);
            yield return MoveTargetAndWait(new Vector3(glassBounds.center.x, handY, glassBounds.center.z), 3f, false);
            if (!StillHoldingBottle()) { Debug.LogWarning("[BottlePour] Захват бутылки прерван."); yield break; }

            // 5) Наклон в сторону стакана
            Vector3 toGlass = glassCenter - bottle.position;
            toGlass.y = 0f;
            if (toGlass.sqrMagnitude < 0.0001f) toGlass = bottle.forward;

            Vector3 tiltAxis = Vector3.Cross(Vector3.up, toGlass.normalized);
            if (tiltAxis.sqrMagnitude < 0.0001f) tiltAxis = bottle.right;

            Quaternion upright = bottle.rotation;
            Quaternion tilted = Quaternion.AngleAxis(tiltAngle, tiltAxis.normalized) * upright;
            yield return RotateHeld(upright, tilted, tiltDuration);
            if (!StillHoldingBottle()) { Debug.LogWarning("[BottlePour] Захват бутылки прерван."); yield break; }

            // 6) Точно подвести носик к стакану
            yield return AlignSpoutOverGlass();
            if (!StillHoldingBottle()) { Debug.LogWarning("[BottlePour] Захват бутылки прерван."); yield break; }

            // 7) Лить воду
            float startBottle = bottleFill;
            float startGlass = glassFill;
            float amount = Mathf.Min(startBottle, 1f - startGlass);

            if (amount > 0.001f)
            {
                pourVisualActive = true;
                if (stream != null) stream.gameObject.SetActive(true);

                float duration = Mathf.Max(0.5f, pourDuration * Mathf.Clamp(amount, 0.25f, 1f));
                float t = 0f;

                while (t < 1f)
                {
                    t += Time.deltaTime / duration;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                    bottleFill = startBottle - amount * k;
                    glassFill = startGlass + amount * k;
                    UpdateFillVisuals();
                    yield return null;

                    if (!StillHoldingBottle())
                    {
                        Debug.LogWarning("[BottlePour] Захват бутылки прерван во время розлива.");
                        yield break;
                    }
                }

                bottleFill = startBottle - amount;
                glassFill = startGlass + amount;
                UpdateFillVisuals();

                yield return new WaitForSeconds(0.25f);

                pourVisualActive = false;
                if (stream != null) stream.gameObject.SetActive(false);
            }

            // 8) Вернуть бутылку в вертикальное положение
            yield return RotateHeld(tilted, upright, tiltDuration);
            if (!StillHoldingBottle()) { Debug.LogWarning("[BottlePour] Захват бутылки прерван."); yield break; }

            // 9) Вернуть бутылку на место
            Vector3 startHand = bottleStartPos + handOffsetAtGrab;
            Vector3 aboveStart = new Vector3(
                startHand.x,
                Mathf.Max(targetPoint.position.y, startHand.y + 0.15f),
                startHand.z);

            yield return MoveTargetAndWait(aboveStart, 2.5f, false);
            yield return MoveTargetAndWait(startHand, 2.5f, false);
        }
        finally
        {
            // Всегда отпускаем и ставим бутылку ровно на исходное место
            if (grabbed)
            {
                if (gripper != null && gripper.HasCube()) gripper.Release();

                bottle.SetPositionAndRotation(bottleStartPos, bottleStartRot);
                if (bottleRb != null)
                {
                    bottleRb.linearVelocity = Vector3.zero;
                    bottleRb.angularVelocity = Vector3.zero;
                    bottleRb.useGravity = false;
                    bottleRb.isKinematic = true;
                }
            }

            if (bottleCollider != null && glassCollider != null)
                Physics.IgnoreCollision(bottleCollider, glassCollider, false);

            handRotationLocked = false;
            SetWristIkLocked(false);
            pourVisualActive = false;
            if (stream != null) stream.gameObject.SetActive(false);

            // Возврат манипулятора домой
            if (targetPoint != null && armTransfer != null && armTransfer.homePoint != null)
                targetPoint.position = armTransfer.homePoint.position;

            isPouring = false;
            IsAnyPouring = false;
        }
    }

    private bool StillHoldingBottle()
    {
        return gripper != null && gripper.HasCube();
    }

    private IEnumerator MoveTargetAndWait(Vector3 position, float waitTimeout, bool requireReach)
    {
        if (targetPoint == null) yield break;

        Vector3 start = targetPoint.position;
        float distance = Vector3.Distance(start, position);
        float duration = Mathf.Max(0.15f, distance / Mathf.Max(moveSpeed, 0.01f));
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            targetPoint.position = Vector3.Lerp(start, position, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)));
            yield return null;
        }

        targetPoint.position = position;

        // Ждём, пока IK реально доведёт конец руки до цели
        float waited = 0f;
        while (targetEnd != null && waited < waitTimeout)
        {
            if (Vector3.Distance(targetEnd.position, position) <= 0.02f) break;
            waited += Time.deltaTime;
            yield return null;
        }

        if (requireReach && targetEnd != null)
        {
            float error = Vector3.Distance(targetEnd.position, position);
            if (error > grabTolerance)
                Debug.LogWarning($"[BottlePour] Конец руки не дошёл до цели: {error:F3} м.");
        }
    }

    private IEnumerator RotateHeld(Quaternion from, Quaternion to, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, duration);
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            heldRotation = Quaternion.Slerp(from, to, k);
            yield return null;
        }

        heldRotation = to;
    }

    private IEnumerator AlignSpoutOverGlass()
    {
        if (bottleSpout == null || targetPoint == null) yield break;

        for (int i = 0; i < 3; i++)
        {
            // Даём LateUpdate применить наклон и физике подвинуть бутылку
            yield return null;
            yield return new WaitForFixedUpdate();

            Vector3 spout = bottleSpout.position;
            Vector3 wanted = new Vector3(
                glassBounds.center.x,
                glassBounds.max.y + 0.06f,
                glassBounds.center.z);

            Vector3 delta = wanted - spout;
            if (delta.magnitude < 0.006f) yield break;

            delta.y = Mathf.Clamp(delta.y, -0.08f, 0.3f);
            yield return MoveTargetAndWait(targetPoint.position + delta, 2f, false);
        }
    }

    // ------------------------------------------------------------------
    // Вода и струя
    // ------------------------------------------------------------------
    private void UpdateFillVisuals()
    {
        ApplyWaterFill(glassWater, glassWaterFullScale, glassWaterFullPos, glassFill, false);
        ApplyWaterFill(bottleWater, bottleWaterFullScale, bottleWaterFullPos, bottleFill, true);
    }

    /// <summary>
    /// Меняет высоту цилиндра воды. anchorTop=true — «дно» воды остаётся у горлышка
    /// (нужно для наклонённой бутылки), иначе — у дна сосуда.
    /// </summary>
    private static void ApplyWaterFill(
        Transform water,
        Vector3 fullScale,
        Vector3 fullPos,
        float fill,
        bool anchorTop)
    {
        if (water == null) return;

        fill = Mathf.Clamp01(fill);
        bool visible = fill > 0.005f;
        water.gameObject.SetActive(visible);
        if (!visible) return;

        float bottomY = fullPos.y - fullScale.y;
        float topY = fullPos.y + fullScale.y;

        Vector3 scale = fullScale;
        scale.y = Mathf.Max(0.0001f, fullScale.y * fill);

        Vector3 pos = fullPos;
        pos.y = anchorTop ? topY - scale.y : bottomY + scale.y;

        water.localScale = scale;
        water.localPosition = pos;
    }

    private void UpdateStream()
    {
        if (stream == null || bottleSpout == null) return;

        Vector3 from = bottleSpout.position;
        Vector3 to = new Vector3(
            glassBounds.center.x,
            Mathf.Lerp(glassBounds.min.y + 0.01f, glassBounds.max.y - 0.01f, glassFill),
            glassBounds.center.z);

        Vector3 delta = to - from;
        float length = delta.magnitude;
        if (length < 0.01f)
        {
            stream.gameObject.SetActive(false);
            return;
        }

        stream.gameObject.SetActive(true);
        stream.position = (from + to) * 0.5f;
        stream.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);

        // Cylinder primitive: высота 2 * scale.y, радиус 0.5 * scale.x
        stream.localScale = new Vector3(streamRadius * 2f, length * 0.5f, streamRadius * 2f);
    }

    // ------------------------------------------------------------------
    // Вспомогательные методы
    // ------------------------------------------------------------------
    private static Transform FindModelRoot(string objectName)
    {
        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        Transform best = null;
        int bestDepth = int.MaxValue;

        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t.name != objectName) continue;
            if (t.GetComponentInChildren<XRSimpleInteractable>(true) != null) continue;
            if (t.GetComponentInChildren<Renderer>(true) == null) continue;

            // Берём самый верхний объект с таким именем: у модели может быть
            // дочерний объект с тем же именем, и тогда Rigidbody попал бы не туда.
            int depth = 0;
            for (Transform p = t.parent; p != null; p = p.parent) depth++;

            if (depth < bestDepth)
            {
                best = t;
                bestDepth = depth;
            }
        }

        return best;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null) return null;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != root && all[i].name == childName) return all[i];
        }

        return null;
    }

    private static Transform FindWaterCylinder(Transform root)
    {
        if (root == null) return null;

        foreach (Transform child in root)
        {
            if (child.name != "Cylinder") continue;
            if (child.GetComponent<MeshRenderer>() == null) continue;
            if (child.GetComponent<Collider>() == null) continue;
            return child;
        }

        // На всякий случай — Cylinder.001 / Cylinder.002 и т.п.
        foreach (Transform child in root)
        {
            if (!child.name.StartsWith("Cylinder")) continue;
            if (child.GetComponent<MeshRenderer>() == null) continue;
            if (child.GetComponent<Collider>() == null) continue;
            return child;
        }

        return null;
    }

    private static void DisableColliders(Transform root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    private static void SetMaterial(Transform root, Material material)
    {
        if (root == null || material == null) return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sharedMaterial = material;
    }

    private static Collider AddBoxCollider(Transform root, Bounds worldBounds)
    {
        BoxCollider box = root.GetComponent<BoxCollider>();
        if (box == null)
            box = root.gameObject.AddComponent<BoxCollider>();

        Vector3 localCenter = root.InverseTransformPoint(worldBounds.center);
        Vector3 localSize = root.InverseTransformVector(worldBounds.size);
        localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

        box.center = localCenter;
        box.size = localSize;
        return box;
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


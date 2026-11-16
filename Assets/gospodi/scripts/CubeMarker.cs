using UnityEngine;

/// <summary>
/// Маркер кубика, созданного спавнером.
/// Позволяет находить и удалять кубики без использования тегов
/// (тег "Cube" может быть не определён в Tag Manager).
/// </summary>
public class CubeMarker : MonoBehaviour
{
    // Кубик стоит в очереди у конвейера
    [HideInInspector] public bool inQueue;

    // Кубик уже взят из очереди на перенос — лента и очередь его больше не трогают
    [HideInInspector] public bool isTaken;
}

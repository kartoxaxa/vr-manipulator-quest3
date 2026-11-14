using UnityEngine;

[CreateAssetMenu(fileName = "MaterialLibrary", menuName = "Processing/Material Library")]
public class MaterialLibrary : ScriptableObject
{
    public Material[] rawMaterials;      // 5 материалов до обработки
    public Material[] treatedMaterials;  // 5 материалов после обработки
}

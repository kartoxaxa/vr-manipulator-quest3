using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MaterialSelector : MonoBehaviour
{
    public Button btnCopper;
    public Button btnAluminum;
    public Button btnSteel;
    public Button btnStainless;
    public Button btnBrass;
    public Button btnTitanium;

    public PanelController panel;
    public TextMeshProUGUI labelMaterial;
    public TextMeshProUGUI labelGasMix;
    public UIAutoUpdater messageSystem;

    public MaterialLibrary materialLibrary;

    // 6 моделей деталей
    public GameObject[] detailModels;

    // Активный Renderer выбранной модели
    public Renderer activeRenderer;

    private MaterialType currentMaterial;

    // 🔥 Профили для 6 материалов (по индексу MaterialType)
    public MaterialProcessingProfile[] profiles;

    void Start()
    {
        btnCopper.onClick.AddListener(() => OnMaterialSelected(MaterialType.Copper));
        btnAluminum.onClick.AddListener(() => OnMaterialSelected(MaterialType.Aluminum));
        btnSteel.onClick.AddListener(() => OnMaterialSelected(MaterialType.Steel));
        btnStainless.onClick.AddListener(() => OnMaterialSelected(MaterialType.Stainless));
        btnBrass.onClick.AddListener(() => OnMaterialSelected(MaterialType.Brass));
        btnTitanium.onClick.AddListener(() => OnMaterialSelected(MaterialType.Titanium));

        OnMaterialSelected(MaterialType.Copper);
    }

    void OnMaterialSelected(MaterialType type)
    {
        UpdateUI(type);
        ActivateModel(type);
        ApplyRawMaterial(type);
        NotifyPanel(type);
    }

    void ActivateModel(MaterialType type)
    {
        int index = (int)type;

        for (int i = 0; i < detailModels.Length; i++)
            detailModels[i].SetActive(true);

        activeRenderer = detailModels[index].GetComponentInChildren<Renderer>();

        if (activeRenderer == null)
            Debug.LogError("Renderer не найден в модели: " + detailModels[index].name);
    }

    void ApplyRawMaterial(MaterialType type)
    {
        int index = (int)type;

        for (int i = 0; i < detailModels.Length; i++)
        {
            var rend = detailModels[i].GetComponentInChildren<Renderer>();
            if (rend != null && materialLibrary != null && i < materialLibrary.rawMaterials.Length)
                rend.material = materialLibrary.rawMaterials[i];
        }

        activeRenderer = detailModels[index].GetComponentInChildren<Renderer>();
    }

    void UpdateUI(MaterialType type)
    {
        currentMaterial = type;

        switch (type)
        {
            case MaterialType.Copper:
                labelMaterial.text = "Материал: Медь (Cu)";
                labelGasMix.text = "Газ: Ar + H₂";
                messageSystem.AddSystemMessage("Выбрана деталь: Медь (Ar + H₂)");
                break;

            case MaterialType.Aluminum:
                labelMaterial.text = "Материал: Алюминий (Al)";
                labelGasMix.text = "Газ: Ar";
                messageSystem.AddSystemMessage("Выбрана деталь: Алюминий (Ar)");
                break;

            case MaterialType.Steel:
                labelMaterial.text = "Материал: Сталь (Fe)";
                labelGasMix.text = "Газ: Ar + N₂";
                messageSystem.AddSystemMessage("Выбрана деталь: Сталь (Ar + N₂)");
                break;

            case MaterialType.Stainless:
                labelMaterial.text = "Материал: Нержавейка (SS)";
                labelGasMix.text = "Газ: N₂ + H₂";
                messageSystem.AddSystemMessage("Выбрана деталь: Нержавейка (N₂ + H₂)");
                break;

            case MaterialType.Brass:
                labelMaterial.text = "Материал: Латунь (Cu+Zn)";
                labelGasMix.text = "Газ: Ar";
                messageSystem.AddSystemMessage("Выбрана деталь: Латунь (Ar)");
                break;

            case MaterialType.Titanium:
                labelMaterial.text = "Материал: Титан (Ti)";
                labelGasMix.text = "Газ: Ar (или Ar+He)";
                messageSystem.AddSystemMessage("Выбрана деталь: Титан (Ar)");
                break;
        }
    }

    void NotifyPanel(MaterialType type)
    {
        currentMaterial = type;

        panel.selectedMode = (ProcessMode)type;
        panel.modeSelected = true;

        panel.ActivateGraph((ProcessMode)type);
    }

    public MaterialType GetCurrentMaterial()
    {
        return currentMaterial;
    }

    public MaterialProcessingProfile GetCurrentProfile()
    {
        if (profiles == null || profiles.Length == 0)
            return null;

        int index = (int)currentMaterial;
        if (index < 0 || index >= profiles.Length)
            return null;

        return profiles[index];
    }
}

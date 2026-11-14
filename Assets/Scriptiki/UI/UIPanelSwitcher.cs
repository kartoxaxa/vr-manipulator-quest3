using UnityEngine;

public class UIPanelSwitcher : MonoBehaviour
{
    public GameObject Panel_Work;
    public GameObject Panel_ProcessControl;
    public GameObject Panel_History;
    public GameObject Panel_Graph;
    public GameObject Panel_Extra;

    private GameObject currentPanel;

    void Start()
    {
        ShowPanel(Panel_Work); // главная панель
    }

    public void ShowPanel(GameObject panel)
    {
        // выключаем предыдущую панель
        if (currentPanel != null)
            currentPanel.SetActive(false);

        // включаем новую
        panel.SetActive(true);
        currentPanel = panel;
    }

    // Методы для кнопок
    public void OnWorkPanel() => ShowPanel(Panel_Work);
    public void OnProcessControl() => ShowPanel(Panel_ProcessControl);
    public void OnHistory() => ShowPanel(Panel_History);
    public void OnGraph() => ShowPanel(Panel_Graph);
    public void OnExtra() => ShowPanel(Panel_Extra);
}

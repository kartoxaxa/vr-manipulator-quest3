using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum MessageType
{
    Normal,
    Warning,
    Error
}

public class UIAutoUpdater : MonoBehaviour
{
    private float nextUpdateTime = 0f;
    public float updateInterval = 0.1f; // 10 раз в секунду

    [Header("Message Colors")]
    public Color normalColor = Color.white;
    public Color warningColor = new Color32(0xEE, 0xCC, 0x5B, 255);
    public Color errorColor = new Color32(255, 80, 80, 255);

    [Header("Top Warning Panel")]
    public TextMeshProUGUI textWarning1;
    public TextMeshProUGUI textWarning2;

    [Header("Last Messages Panel")]
    public TextMeshProUGUI lastMessage1;
    public TextMeshProUGUI lastMessage2;

    [Header("History Table")]
    public Transform historyContent;
    public GameObject historyRowPrefab;
    public ScrollRect scrollRect;

    [Header("Top Status Panel")]
    public TextMeshProUGUI textTemp;
    public TextMeshProUGUI textCurrent;
    public TextMeshProUGUI textVoltage;
    public TextMeshProUGUI textPressure;
    public TextMeshProUGUI textProcess;
    public TextMeshProUGUI textStep;

    [Header("Graph Panel")]
    public TextMeshProUGUI textDate;
    public TextMeshProUGUI textTime;

    public TextMeshProUGUI textAr;
    public TextMeshProUGUI textN2;
    public TextMeshProUGUI textH2;

    public TextMeshProUGUI textTsadki;
    public TextMeshProUGUI textI;
    public TextMeshProUGUI textU;
    public TextMeshProUGUI textPa;
    public TextMeshProUGUI textDVN;

    public TextMeshProUGUI textLP_Date;
    public TextMeshProUGUI textLP_Time;

    [Header("Clock")]
    public TextMeshProUGUI textClock;

    [Header("Extra Temperature Field")]
    public TextMeshProUGUI textExtraTemp;

    [Header("Process State")]
    public ProcessState state;

    void Update()
    {
        if (Time.time < nextUpdateTime)
            return;

        nextUpdateTime = Time.time + updateInterval;

        var now = System.DateTime.Now;

        if (textClock != null)
            textClock.text = now.ToString("HH:mm:ss");

        if (textDate != null)
            textDate.text = now.ToString("dd/MM/yy");

        if (textTime != null)
            textTime.text = now.ToString("HH:mm:ss");

        if (state == null)
            return;

        // 1:1 с ProcessState (а он уже плавный)
        textTemp.text = state.temperature.ToString("F0");
        textCurrent.text = state.currentA.ToString("F2");
        textVoltage.text = state.voltage.ToString("F1");
        textPressure.text = state.pressure.ToString("F0");

        if (textExtraTemp != null)
            textExtraTemp.text = state.temperature.ToString("F0");

        textAr.text = state.gasAr.ToString("F2");
        textN2.text = state.gasN2.ToString("F2");
        textH2.text = state.gasH2.ToString("F2");

        textTsadki.text = state.temperature.ToString("F0");
        textI.text = state.currentA.ToString("F2");
        textU.text = state.voltage.ToString("F1");
        textPa.text = state.pressure.ToString("F0");
        textDVN.text = state.frequency.ToString("F1");

        textLP_Date.text = state.lastPointDate;
        textLP_Time.text = state.lastPointTime;
    }

    public void SetProcessStatus(string status, string step)
    {
        if (textProcess != null)
            textProcess.text = status;

        if (textStep != null)
            textStep.text = step;
    }

    public void AddSystemMessage(string message)
    {
        MessageType type = MessageCatalog.Types.ContainsKey(message)
            ? MessageCatalog.Types[message]
            : MessageType.Normal;

        AddSystemMessage(message, type);
    }

    public void AddSystemMessage(string message, MessageType type, string restoreTime = "")
    {
        Color color = GetColor(type);

        textWarning2.text = textWarning1.text;
        textWarning2.color = textWarning1.color;

        textWarning1.text = message;
        textWarning1.color = color;

        lastMessage2.text = lastMessage1.text;
        lastMessage2.color = lastMessage1.color;

        lastMessage1.text = message;
        lastMessage1.color = color;

        AddHistoryRow(message, color, restoreTime);
    }

    private Color GetColor(MessageType type)
    {
        switch (type)
        {
            case MessageType.Warning: return warningColor;
            case MessageType.Error: return errorColor;
            default: return normalColor;
        }
    }

    public void ShowNormalStop()
    {
        SetProcessStatus("процесс остановлен", "шаг 1/1");
        AddSystemMessage("Процесс остановлен", MessageType.Warning);
    }

    public void ShowEmergencyStop()
    {
        SetProcessStatus("АВАРИЙНАЯ ОСТАНОВКА", "ошибка");
        AddSystemMessage("Аварийная остановка процесса", MessageType.Error);
    }

    private void AddHistoryRow(string message, Color color, string restoreTime = "")
    {
        string time = System.DateTime.Now.ToString("dd/MM/yy HH:mm:ss");

        GameObject row = Instantiate(historyRowPrefab, historyContent);

        row.transform.Find("Text_Time").GetComponent<TextMeshProUGUI>().text = time;

        var msg = row.transform.Find("Text_Message").GetComponent<TextMeshProUGUI>();
        msg.text = message;
        msg.color = color;

        row.transform.Find("Text_Restore").GetComponent<TextMeshProUGUI>().text = restoreTime;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }
}

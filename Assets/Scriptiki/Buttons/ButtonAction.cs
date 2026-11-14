using UnityEngine;

public class ButtonAction : MonoBehaviour
{
    public VRButtonDepthDetector detector;
    public enum ButtonType
    {
        Start,
        Stop,
        Napusk,
        EmergencyStop
    }

    public ButtonType type;
    public PanelController panel; // ← добавляем управление всей панелью
    public void OnPressed()
    {
        panel.OnButtonPressed(this);
    }
}

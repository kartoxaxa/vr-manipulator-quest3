using UnityEngine;
using UnityEngine.UI;

public class HistoryScrollController : MonoBehaviour
{
    public ScrollRect scrollRect;

    [Header("Scroll Settings")]
    public float scrollStep = 0.1f; // 0.1 = 10% от высоты

    public void ScrollUp()
    {
        if (scrollRect == null) return;

        float newPos = scrollRect.verticalNormalizedPosition + scrollStep;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(newPos);
        Canvas.ForceUpdateCanvases();
    }

    public void ScrollDown()
    {
        if (scrollRect == null) return;

        float newPos = scrollRect.verticalNormalizedPosition - scrollStep;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(newPos);
        Canvas.ForceUpdateCanvases();
    }

}

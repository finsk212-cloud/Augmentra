using UnityEngine;
using UnityEngine.EventSystems;

public class PanelDragger : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform target;
    public float edgeMargin = 32f;

    private Canvas canvas;
    private bool dragging;

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = false;

        if (target == null) return;

        if (canvas == null)
        {
            canvas = target.GetComponentInParent<Canvas>();
        }

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(target, eventData.position, eventData.pressEventCamera, out local))
        {
            return;
        }

        Rect rect = target.rect;
        bool nearEdge =
            local.x <= rect.xMin + edgeMargin ||
            local.x >= rect.xMax - edgeMargin ||
            local.y <= rect.yMin + edgeMargin ||
            local.y >= rect.yMax - edgeMargin;

        dragging = nearEdge;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || target == null) return;

        float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        target.anchoredPosition += eventData.delta / scale;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Augmentra.UI
{
    [RequireComponent(typeof(Selectable))]
    public sealed class UIButtonFeedback : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [SerializeField, Range(0.8f, 1f)] private float pressedScale = 0.96f;
        [SerializeField] private Graphic selectionGraphic;
        [SerializeField] private Color selectedColor = new Color(0.95f, 0.78f, 0.35f, 1f);

        private Vector3 normalScale;
        private Color normalSelectionColor;

        private void Awake()
        {
            normalScale = transform.localScale;

            if (selectionGraphic == null)
            {
                selectionGraphic = GetComponent<Graphic>();
            }

            if (selectionGraphic != null)
            {
                normalSelectionColor = selectionGraphic.color;
            }
        }

        private void OnDisable()
        {
            transform.localScale = normalScale;
            SetSelected(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = normalScale * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = normalScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = normalScale;
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetSelected(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetSelected(false);
        }

        private void SetSelected(bool selected)
        {
            if (selectionGraphic != null)
            {
                selectionGraphic.color = selected ? selectedColor : normalSelectionColor;
            }
        }
    }
}

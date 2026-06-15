using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyFamilyTreeCardHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverScale = 1.13f;
        private const float ScaleSpeed = 9f;

        private RectTransform rect;
        private StrategyFamilyTreeHudController owner;
        private int residentId;
        private bool hovering;
        private float scale = 1f;

        public void Configure(StrategyFamilyTreeHudController controller, int familyResidentId)
        {
            owner = controller;
            residentId = familyResidentId;
        }

        private void Awake()
        {
            rect = transform as RectTransform;
        }

        private void Update()
        {
            float target = hovering ? HoverScale : 1f;
            scale = Mathf.Lerp(scale, target, 1f - Mathf.Exp(-ScaleSpeed * Time.unscaledDeltaTime));
            if (rect != null)
            {
                rect.localScale = new Vector3(scale, scale, 1f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            owner?.SetHoveredFamilyTreeMember(residentId, true);
            transform.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            owner?.SetHoveredFamilyTreeMember(residentId, false);
        }
    }
}

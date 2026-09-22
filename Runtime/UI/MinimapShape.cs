using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    [RequireComponent(typeof(RectTransform))]
    public class MinimapShape : MonoBehaviour
    {
        [SerializeField] private MinimapShapeKind shapeKind = MinimapShapeKind.Sprite;
        [SerializeField] private Sprite sprite;
        [SerializeField] [HideInInspector] private bool ownsImageComponent;

        public MinimapShapeKind ShapeKind { get { return shapeKind; } }

        private void OnEnable()
        {
            ApplyShape();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return;
            }
            UnityEditor.EditorApplication.delayCall += DelayedApplyShape;
#endif
        }

        public void SetShapeKind(MinimapShapeKind value)
        {
            shapeKind = value;
            ApplyShape();
        }

        public void SetSprite(Sprite value)
        {
            sprite = value;
            ApplyShape();
        }

        private void ApplyShape()
        {
            if (this == null)
            {
                return;
            }

            if (shapeKind == MinimapShapeKind.Rectangle)
            {
                ApplyRectangle();
            }
            else
            {
                ApplySprite();
            }
        }

        private void ApplyRectangle()
        {
            RemoveComponent(GetComponent<Mask>());
            if (ownsImageComponent)
            {
                RemoveComponent(GetComponent<Image>());
                ownsImageComponent = false;
            }

            if (GetComponent<RectMask2D>() == null)
            {
                gameObject.AddComponent<RectMask2D>();
            }
        }

        private void ApplySprite()
        {
            RemoveComponent(GetComponent<RectMask2D>());

            Image image = GetComponent<Image>();
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
                ownsImageComponent = true;
            }
            image.sprite = sprite;

            Mask mask = GetComponent<Mask>();
            if (mask == null)
            {
                mask = gameObject.AddComponent<Mask>();
            }
            mask.showMaskGraphic = false;
        }

        private void RemoveComponent(Component component)
        {
            if (component == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(component);
            }
            else
            {
                DestroyImmediate(component);
            }
        }

#if UNITY_EDITOR
        private void DelayedApplyShape()
        {
            if (this == null)
            {
                return;
            }
            ApplyShape();
        }
#endif
    }
}

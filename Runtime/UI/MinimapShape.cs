using UnityEngine;
using UnityEngine.UI;

namespace Gley.NavigationSystem
{
    internal class MinimapShape
    {
        private readonly RectTransform viewport;
        private readonly MinimapShapeSettings settings;

        internal MinimapShape(RectTransform viewport, MinimapShapeSettings settings)
        {
            this.viewport = viewport;
            this.settings = settings;
        }

        internal void Apply()
        {
            if (viewport == null)
            {
                return;
            }

            if (settings.ShapeKind == MinimapShapeKind.Rectangle)
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
            RemoveComponent(viewport.GetComponent<Mask>());
            RemoveComponent(viewport.GetComponent<Image>());

            if (viewport.GetComponent<RectMask2D>() == null)
            {
                viewport.gameObject.AddComponent<RectMask2D>();
            }
        }

        private void ApplySprite()
        {
            RemoveComponent(viewport.GetComponent<RectMask2D>());

            Image image = viewport.GetComponent<Image>();
            if (image == null)
            {
                image = viewport.gameObject.AddComponent<Image>();
            }
            image.sprite = settings.Sprite;
            image.raycastTarget = true;

            Mask mask = viewport.GetComponent<Mask>();
            if (mask == null)
            {
                mask = viewport.gameObject.AddComponent<Mask>();
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
                Object.Destroy(component);
            }
            else
            {
                Object.DestroyImmediate(component);
            }
        }
    }
}

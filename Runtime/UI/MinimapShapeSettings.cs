using UnityEngine;

namespace Gley.NavigationSystem
{
    [System.Serializable]
    public class MinimapShapeSettings
    {
        [SerializeField] private MinimapShapeKind shapeKind = MinimapShapeKind.Sprite;
        [SerializeField] private Sprite sprite;
        [SerializeField] private EdgeShape spriteOutline = EdgeShape.Circle;

        public MinimapShapeKind ShapeKind { get { return shapeKind; } }
        public Sprite Sprite { get { return sprite; } }
        public EdgeShape SpriteOutline { get { return spriteOutline; } }

        public EdgeShape Outline
        {
            get
            {
                if (shapeKind == MinimapShapeKind.Rectangle)
                {
                    return EdgeShape.Rectangle;
                }
                return spriteOutline;
            }
        }

        internal void SetShapeKind(MinimapShapeKind value)
        {
            shapeKind = value;
        }

        internal void SetSprite(Sprite value)
        {
            sprite = value;
        }

        internal void SetSpriteOutline(EdgeShape value)
        {
            spriteOutline = value;
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace Gley.NavigationSystem.Editor
{
    public class CustomImageAssigner
    {
        private readonly CapturePlanner planner;

        public CustomImageAssigner()
        {
            planner = new CapturePlanner();
        }

        public void Assign(MapData data, Texture2D image)
        {
            float width = data.RectangleSize.x;
            float height = width * image.height / (float)image.width;

            Undo.RecordObject(data, "Assign Custom Map Image");
            data.SetRectangleSize(new Vector2(width, height));
            data.SetImage(image);
            data.SetImageState(MapImageState.Custom);
            data.SetLocked(false);
            EditorUtility.SetDirty(data);
        }

        public ImageGuidance GetGuidance(MapData data)
        {
            Vector2 size = data.RectangleSize;
            float ratio = size.x / size.y;
            string ratioText = FormatRatioText(size, ratio);

            ImageSizePlan recommended2048 = planner.PlanImageSize(size, 2048);
            ImageSizePlan recommended4096 = planner.PlanImageSize(size, 4096);

            return new ImageGuidance(recommended2048, recommended4096, ratioText, ratio);
        }

        private string FormatRatioText(Vector2 size, float ratio)
        {
            return size.x.ToString("0") + " × " + size.y.ToString("0") + " m → " + ratio.ToString("0.000") + " : 1";
        }
    }
}

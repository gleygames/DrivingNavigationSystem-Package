using UnityEngine;
using UnityEngine.Rendering;

namespace Gley.NavigationSystem.Editor
{
    public class BuiltInCaptureAdapter : ICapturePipelineAdapter
    {
        public bool SupportsExposure { get { return false; } }

        public bool IsActive()
        {
            return GraphicsSettings.currentRenderPipeline == null;
        }

        public void Prepare(Camera camera, CaptureSettings settings)
        {
        }

        public void Restore()
        {
        }

        public bool TryGetDefaultExposure(out float ev)
        {
            ev = 0f;
            return false;
        }
    }
}
